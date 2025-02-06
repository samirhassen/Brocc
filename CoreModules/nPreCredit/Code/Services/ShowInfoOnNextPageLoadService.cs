using System;
using System.Dynamic;
using System.Web;

namespace nPreCredit.Code.Services
{
    public class ShowInfoOnNextPageLoadService : IShowInfoOnNextPageLoadService
    {
        private readonly HttpContextBase httpContext;

        public ShowInfoOnNextPageLoadService(HttpContextBase httpContext)
        {
            this.httpContext = httpContext;
        }

        /// <summary>
        /// Shown to user on the next page load
        /// </summary>
        /// <param name="title"></param>
        /// <param name="text"></param>
        public void ShowInfoMessageOnNextPageLoad(string title, string text, Uri link = null)
        {
            var s = this?.httpContext?.Session;
            if (s != null)
            {
                dynamic m = new ExpandoObject();
                m.Title = title;
                m.Text = text;
                m.Link = link == null ? null : link.AbsoluteUri;
                s["infoMessageOnNextPageLoad"] = m;
            }
        }
    }
}