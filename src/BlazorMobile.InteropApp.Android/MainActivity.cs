﻿using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using BlazorMobile.InteropApp;
using BlazorMobile.Droid.Services;
using Android.Support.V7.App;
using BlazorMobile.Services;
using BlazorMobile.InteropApp.AppPackage;
using BlazorMobile.InteropApp.Services;
using Xamarin.Forms;
using BlazorMobile.InteropApp.Droid.Services;
using Android.Support.V4.App;
using Android;
using Android.Support.Design.Widget;
using BlazorMobile.Droid.Platform;

namespace BlazorMobile.InteropApp.Droid
{
    [Activity(Label = "BlazorMobile.InteropApp", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : BlazorMobileFormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            BlazorWebViewService.Init(this);

            DependencyService.Register<IAssemblyService, AssemblyService>();

            //Register our Blazor app package
            WebApplicationFactory.RegisterAppStreamResolver(AppPackageHelper.ResolveAppPackageStream);

            LoadApplication(new App());

        }
    }
}

