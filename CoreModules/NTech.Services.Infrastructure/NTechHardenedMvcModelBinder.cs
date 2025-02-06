using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace NTech.Services.Infrastructure
{
    public class NTechHardenedMvcModelBinder : DefaultModelBinder
    {
        public NTechHardenedMvcModelBinder(string currentServiceName)
        {
            Func<bool, string> getLogFolder = isRequired =>
            {
                var globalLogFolder = NTechEnvironment.Instance.Setting("ntech.logfolder", isRequired);
                if (globalLogFolder == null)
                    return null;
                return Path.Combine(globalLogFolder, "NTechHardenedMvcModelBinder");
            };

            this.currentServiceName = currentServiceName;
            this.log = getLogFolder(false) == null ? null : new Lazy<RotatingLogFile>(() =>
            {
                var logFolder = getLogFolder(true);
                return new RotatingLogFile(logFolder, $"rejected-requests-{currentServiceName}");
            });
        }

        private const string FormEncodedContentType = "application/x-www-form-urlencoded";
        private readonly string currentServiceName;
        private readonly Lazy<RotatingLogFile> log;

        private void LogRejected(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            if (log == null)
                return;

            try
            {
                var actionName = controllerContext?.RouteData?.Values["action"];
                var controllerName = controllerContext?.RouteData?.Values["controller"];

                var form = controllerContext.RequestContext.HttpContext.Request.Form;
                var formData = string.Join(", ", form.AllKeys.Select(x => $"{x}={form[x]}"));
                log.Value.Log($"{currentServiceName}.{controllerName}.{actionName}.{bindingContext.ModelName}: {formData}");
            }
            catch
            {
                /*
                 * We dont really know if there are cases here where things can be missing
                 * Should we log the failed logging or is that too much inception?
                 */
            }
        }

        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var r = controllerContext.HttpContext?.Request;
            if (r != null && r.ContentType != null && r.ContentType.ToLowerInvariant().Contains(FormEncodedContentType))
            {
                return BindFormsEncodedModel(controllerContext, bindingContext);
            }
            else
            {
                return base.BindModel(controllerContext, bindingContext);
            }
        }

        public object BindFormsEncodedModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            if (NTechHardenedMvcModelBinderAllowFormsAttribute.IsPresent(controllerContext))
                return base.BindModel(controllerContext, bindingContext);

            LogRejected(controllerContext, bindingContext);

            bindingContext.ModelState.AddModelError("global", "Forms encoded input not allowed");
            return null;
        }

        //Call this Application_Start in Global.asax: NTechHardenedMvcModelBinder.Register();
        //Order probably doesn't matter but it's been tried just before the filters
        public static void Register(string moduleName)
        {
            if (NTechEnvironment.Instance.OptBoolSetting("ntech.hardenmodelbinding.disabled"))
                return;

            NLog.Information("NTechHardenedMvcModelBinder enabled");

            ModelBinders.Binders.DefaultBinder = new NTechHardenedMvcModelBinder(moduleName);
        }
    }

    //Marker attribute to allow forms binding when needed
    //Just decorate a controller or action with this to allow forms encoding in that context
    public class NTechHardenedMvcModelBinderAllowFormsAttribute : Attribute
    {
        public static bool IsPresent(ControllerContext controllerContext)
        {
            if (controllerContext == null || controllerContext.Controller == null)
                return false;

            if (controllerContext.Controller.GetType().GetCustomAttributes(typeof(NTechHardenedMvcModelBinderAllowFormsAttribute), true).Length > 0)
                return true;

            if (controllerContext.RouteData == null)
                return false;

            string action = (string)controllerContext.RouteData.Values["action"];
            if (!string.IsNullOrEmpty(action) && controllerContext.Controller.GetType().GetMethod(action).GetCustomAttributes(typeof(NTechHardenedMvcModelBinderAllowFormsAttribute), true).Length > 0)
                return true;

            return false;
        }
    }
}