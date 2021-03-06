﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ViewEnginePlus
{
    public class RazorViewEnginePlus : ViewEnginePlus, IViewEngine
    {
        private readonly RazorViewEngine fallbackViewEngine = new RazorViewEngine();
        private string lastTheme;
        private RazorViewEngine lastEngine;
        private readonly object @lock = new object();

        // Temp
        private string activeTheme;

        // Implementations for IViewEngine

        public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            TestViewCases();
            return ThemeViewEngine().FindPartialView(controllerContext, partialViewName, useCache);
        }

        public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            // Refresh all of the test cases
            TestViewCases();

            ViewEngineResult resultView = ThemeViewEngine().FindView(controllerContext, ModifyViewName(viewName), masterName, useCache);

            // If there is no modified view then try the original name
            if (resultView.View == null)
            {
                resultView = ThemeViewEngine().FindView(controllerContext, viewName, masterName, useCache);
            }

            return resultView;
        }

        public void ReleaseView(ControllerContext controllerContext, IView view)
        {
            ThemeViewEngine().ReleaseView(controllerContext, view);
        }

        private RazorViewEngine ThemeViewEngine()
        {
            lock (@lock)
            {
                try
                {
                    // [todo] Determine where active theme is coming from
                    activeTheme = "Custom1";

                    // Allow the Theme to be overridden via the querystring ?theme=<newtheme>
                    // Will probably need securing with Request.IsAuthenticated || IsLocal?
                    var previewTheme = HttpContext.Current.Request.QueryString["theme"];
                    if (!string.IsNullOrEmpty(previewTheme))
                    {
                        activeTheme = previewTheme;
                    }

                    // Check if requested theme is the same as the last one and just return that
                    if (activeTheme == lastTheme)
                    {
                        return lastEngine;
                    }
                }
                catch (Exception)
                {
                    return fallbackViewEngine;
                }

                // If we have got this far then we are creating a new theme engine

                // Create a new base egnine
                lastEngine = new RazorViewEngine();

                // New View locations
                // This leave all the default locations so /Views/{1}/{0}.cshtml will be used as a fall back aswell
                lastEngine.ViewLocationFormats =
                    new[]
                    {
                        "~/Views/{1}/{0}_" + viewCase + ".cshtml",
                        "~/Themes/" + activeTheme + "/Views/{1}/{0}.cshtml",
                        "~/Themes/" + activeTheme + "/Views/{0}.cshtml",
                        "~/Themes/" + activeTheme + "/Views/Shared/{0}.cshtml",
                        "~/Themes/" + activeTheme + "/Views/Shared/{1}/{0}.cshtml"
                    }.Union(lastEngine.ViewLocationFormats).ToArray();


                // New PartialView locations
                lastEngine.PartialViewLocationFormats =
                    new[]
                    {
                        "~/Themes/" + activeTheme + "/Views/{1}/{0}.cshtml",
                        "~/Themes/" + activeTheme + "/Views/Shared/{0}.cshtml",
                        "~/Themes/" + activeTheme + "/Views/Shared/{1}/{0}.cshtml",
                        "~/Themes/" + activeTheme + "/Views/{0}.cshtml"
                    }.Union(lastEngine.PartialViewLocationFormats).ToArray();


                // New Master locations
                lastEngine.MasterLocationFormats =
                    new[]
                    {
                        "~/Themes/" + activeTheme + "/Views/{1}/{0}.cshtml",
                        "~/Themes/" + activeTheme + "/Views/Shared/{1}/{0}.cshtml",
                        "~/Themes/" + activeTheme + "/Views/Shared/{0}.cshtml",
                        "~/Themes/" + activeTheme + "/Views/{0}.cshtml"
                    }.Union(lastEngine.MasterLocationFormats).ToArray();

                // Set the last used theme to the current active theme
                lastTheme = activeTheme;

                return lastEngine;
            }

        }

    }
}