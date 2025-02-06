using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Web.Mvc;

namespace NTech.Services.Infrastructure
{
    public class NTechInitialDataHandler
    {
        /// <summary>
        /// Set in controller:
        ///  ViewBag.JsonInitialData = EncodeInitialData(new { [...] })
        ///
        /// Parse in cshtml:
        /// <script>
        ///   initialData = JSON.parse(atob('@Html.Raw(ViewBag.JsonInitialData)'))
        /// </script>
        ///
        /// </summary>
        public string EncodeInitialData<T>(T data)
        {
            return Convert.ToBase64String(Encoding.GetEncoding("iso-8859-1").GetBytes(JsonConvert.SerializeObject(data)));
        }

        //BEWARE: Dont use this in Url.Action
        private object AppendBackToObject(object t, NTechNavigationTarget back)
        {
            if (t == null || back == null)
                return t;
            var p = JObject.Parse(JsonConvert.SerializeObject(t));

            var backTarget = back?.GetBackTargetOrNull();
            if (backTarget != null && p.GetStringPropertyValue("backTarget", true) == null)
            {
                p.AddOrReplaceJsonProperty("backTarget", new JValue(backTarget), true);
            }

            p.RemoveJsonProperty("backUrl", true);

            return JsonConvert.DeserializeObject(p.ToString());
        }

        public void SetInitialData<T>(T data, ControllerBase controller)
        {
            controller.ViewBag.InitialDataPre = data;
        }

        /// <summary>
        /// Override protected override void OnActionExecuted(ActionExecutedContext filterContext) and then call base.OnActionExecuted(filterContext);
        /// </summary>
        /// <param name="filterContext"></param>
        public void HandleOnActionExecuted(ActionExecutedContext filterContext, Controller controller)
        {
            var r = filterContext.Result as ViewResult;
            if (r != null)
            {
                var p = controller.ViewBag.InitialDataPre;
                if (p != null)
                {
                    controller.ViewBag.JsonInitialData = EncodeInitialData(AppendBackToObject(p, GetBack(controller)));
                }
            }
        }

        public NTechNavigationTarget GetBack(Controller controller)
        {
            var backTarget = controller.Request.Params["backTarget"];

            return ParseBack(backTarget);
        }

        public NTechNavigationTarget ParseBack(string backTarget)
        {
            return NTechNavigationTarget.CreateFromTargetCode(backTarget);
        }
    }
}