using NTech.Services.Infrastructure;
using System;
using System.IO;
using System.Web.Mvc;

namespace nScheduler.Controllers
{
    [AllowAnonymous]
    [RoutePrefix("Skinning")]
    public class SkinningController : Controller
    {
        /* Requires the below in web.config to work
         <system.webServer>    
          <handlers>      
            <add name="UrlRoutingHandler" 
                 type="System.Web.Routing.UrlRoutingHandler, 
                       System.Web, Version=4.0.0.0, 
                       Culture=neutral, 
                       PublicKeyToken=b03f5f7f11d50a3a" 
                 path="/Skinning/*" 
                 verb="GET"/>      
          </handlers>
        </system.webServer>

            Link to resources like this Url.Content("~/Skinning/img/menu-header-logo.png")

            Also link to the css into the master page. Dont add it to a bundle as bundling doesnt play nice with this solution

            Something like:
                @RenderSection("Styles", true)
                @if (NEnv.IsSkinningCssEnabled)
                {
                    <link href="@(Url.Content("~/Skinning/css/skinning.css") + "?v=" + DateTime.Now.ToString("yyyyMMddHH"))" rel="stylesheet" />
                }    
         */

        private static byte[] ReadResource(string relativePath)
        {
            return NTechCache.WithCache($"ntech.cache.skinning.{relativePath}", TimeSpan.FromHours(1), () =>
            {
                var root = NEnv.SkinningRootFolder;
                var file = new FileInfo(Path.Combine(root.FullName, relativePath));
                if (!IsSafeMapping(root, file))
                    return null;
                return System.IO.File.ReadAllBytes(file.FullName);
            });
        }

        private static bool IsSafeMapping(DirectoryInfo parent, FileInfo child)
        {
            if (!parent.Exists)
                return false;

            //Make sure the file reference is actually inside the skinning directory
            var p = child.Directory;
            int guard = 0;
            while (guard++ < 100)
            {
                if (p.FullName == parent.Root.FullName)
                    return false;

                if (p.FullName == parent.FullName)
                    return true;

                p = p.Parent;
            }
            throw new Exception("Hit guard code!");
        }

        [Route("{*path}")]
        public ActionResult Content()
        {
            if (!NEnv.IsSkinningEnabled)
                return HttpNotFound();

            var path = RouteData?.Values["path"]?.ToString()?.Replace(@"/", @"\");

            var fileName = Path.GetFileName(path);

            var data = ReadResource(path);
            if (data == null)
                return HttpNotFound();
            else
                return File(data, System.Web.MimeMapping.GetMimeMapping(fileName));
        }
    }
}