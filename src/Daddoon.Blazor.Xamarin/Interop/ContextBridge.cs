﻿using Daddoon.Blazor.Xam.Common.Interop;
using Daddoon.Blazor.Xam.Common.Serialization;
using Daddoon.Blazor.Xam.Components;
using Daddoon.Blazor.Xam.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Daddoon.Blazor.Xam.Interop
{
    public static class ContextBridge
    {
        private static object GetDefault(Type type)
        {
            if (type == typeof(void))
            {
                return null;
            }

            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        public static void ClearRequestValues(MethodProxy methodResult)
        {
            if (methodResult == null)
                return;

            methodResult.GenericTypes = null;
            methodResult.InterfaceType = null;
            methodResult.Parameters = null;
        }

        private static object GetResultFromTask(Type returnType, Task taskResult)
        {
            if (returnType == null || returnType == typeof(void) || returnType == typeof(Task))
            {
                return null;
            }

            try
            {
                if (taskResult.IsCompleted == false)
                {
                    taskResult.GetAwaiter().GetResult();
                }

                var result = taskResult.GetType().GetProperty("Result").GetValue(taskResult);

                return result;
            }
            catch (Exception)
            {
                return GetDefault(returnType);
            }
        }

        public static string GetJSONReturnValue(MethodProxy methodResult)
        {
            ClearRequestValues(methodResult);
            return BridgeSerializer.Serialize(methodResult);
        }

        public static MethodProxy Receive(string methodProxyJson)
        {
            object defaultValue = default(object);
            MethodProxy methodProxy = null;

            try
            {
                methodProxy = BridgeSerializer.Deserialize<MethodProxy>(methodProxyJson);

                Type iface = methodProxy.InterfaceType.ResolvedType();
                object concreteService = DependencyServiceExtension.Get(iface);

                MethodInfo baseMethod = MethodProxyHelper.GetClassMethodInfo(concreteService.GetType(), iface, methodProxy.MethodIndex);

                //In case of failure, getting Default Return Type
                defaultValue = GetDefault(baseMethod.ReturnType);

                if (methodProxy.GenericTypes != null && methodProxy.GenericTypes.Length > 0)
                {
                    Type[] genericTypes = methodProxy.GenericTypes.Select(p => p.ResolvedType()).ToArray();

                    methodProxy.ReturnValue = baseMethod.MakeGenericMethod(genericTypes).Invoke(concreteService, methodProxy.Parameters);
                    methodProxy.TaskSuccess = true;
                }
                else
                {
                    methodProxy.ReturnValue = baseMethod.Invoke(concreteService, methodProxy.Parameters);
                    methodProxy.TaskSuccess = true;
                }

                if (methodProxy.AsyncTask)
                {
                    methodProxy.ReturnValue = GetResultFromTask(methodProxy.ReturnType.ResolvedType(), (Task)methodProxy.ReturnValue);
                }
            }
            catch (Exception)
            {
                methodProxy.ReturnValue = defaultValue;
                methodProxy.TaskSuccess = false;
            }

            return methodProxy;
        }

        /// <summary>
        /// Manage In and Out call of Method
        /// </summary>
        /// <param name="webview"></param>
        /// <param name="json"></param>
        public static void BridgeEvaluator(BlazorWebView webview, string json, Action<string> outEvaluator = null)
        {
            //We must evaluate data on main thread, as some platform doesn't
            //support to be executed from a non-UI thread for UI 
            //or Webview bridge
            Device.BeginInvokeOnMainThread(delegate ()
            {
                MethodProxy returnValue = Receive(json);
                string jsonReturnValue = GetJSONReturnValue(returnValue);

                //TODO: Manage missed returns value if the websocket disconnect, or discard them ?
                WebApplicationFactory.GetBlazorContextBridgeServer().SendMessageToClient(jsonReturnValue);

                //var receiveEvaluator = webview.GetReceiveEvaluator(jsonReturnValue);

                //if (outEvaluator != null)
                //{
                //    outEvaluator(receiveEvaluator);
                //}
                //else
                //{
                //    webview.Eval(receiveEvaluator);
                //}
            });
        }
    }
}
