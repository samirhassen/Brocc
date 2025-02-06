﻿using System;
using System.Web.Mvc;

namespace nCustomerPages.Controllers
{
    public class HandleApiErrorAttribute : FilterAttribute, IExceptionFilter
    {
        public virtual void OnException(ExceptionContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }
            if (filterContext.Exception != null)
            {
                filterContext.ExceptionHandled = true;
                filterContext.HttpContext.Response.Clear();
                filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
                filterContext.HttpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                filterContext.Result = new HttpStatusCodeResult(System.Net.HttpStatusCode.InternalServerError, "Internal server error");
            }
        }
    }
}