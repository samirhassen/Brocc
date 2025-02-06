using Newtonsoft.Json;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NTech.Services.Infrastructure.NTechWs
{
    public abstract class NTechWebserviceMethod
    {
        public abstract string Path { get; }

        public virtual bool IsEnabled { get { return true; } }
        public virtual IEnumerable<string> LimitAccessToGroupNames => Enumerable.Empty<string>();

        public string GetFullMethodPath(string apiPrefix)
        {
            var p = Path.TrimStart('/').TrimEnd('/');

            return $"/{apiPrefix.TrimStart('/').TrimEnd('/')}/{Path}";
        }

        public virtual string HttpVerb { get { return "POST"; } }

        public abstract Type RequestType { get; }
        public abstract Type ResponseType { get; }

        public class ActionResult
        {
            public bool IsError { get; set; }
            public string ErrorMessage { get; set; }
            public string ErrorCode { get; set; }
            public int? ErrorHttpStatusCode { get; set; }
            public string JsonResult { get; set; }
            public FileStream FileStreamResult { get; set; }

            public class FileStream
            {
                public string DownloadFileName { get; set; }
                public Stream Stream { get; set; }
                public string ContentType { get; set; }
            }
        }

        private const string OverrideRequestLogJsonCustomDataKey = "OverrideRequestLogJson";
        private const string OverrideResponseLogJsonCustomDataKey = "OverrideResponseLogJson";

        public string GetRequestLogJson(NTechWebserviceMethodRequestContext requestContext)
        {
            return requestContext.GetCustomDataValueOrNull<string>(OverrideRequestLogJsonCustomDataKey) ?? (requestContext.IsJsonRequest ? requestContext.RequestJson : null);
        }

        public string GetResponseLogJson(NTechWebserviceMethodRequestContext requestContext, ActionResult result)
        {
            if (result.FileStreamResult != null)
                return null;

            return requestContext.GetCustomDataValueOrNull<string>(OverrideResponseLogJsonCustomDataKey) ?? result?.JsonResult;
        }

        protected void SetCustomResponseLogJson(NTechWebserviceMethodRequestContext requestContext, Func<string> customJson)
        {
            if (requestContext.IsRequestLoggingDisabled)
                return;
            requestContext.SetCustomData(OverrideResponseLogJsonCustomDataKey, customJson);
        }

        protected void SetCustomRequestLogJson(NTechWebserviceMethodRequestContext requestContext, Func<string> customJson)
        {
            if (requestContext.IsRequestLoggingDisabled)
                return;
            requestContext.SetCustomData(OverrideRequestLogJsonCustomDataKey, customJson);
        }

        private void RestrictAccessByGroup(NTechWebserviceMethodRequestContext requestContext)
        {
            var limitAccessToGroupNames = (this.LimitAccessToGroupNames ?? Enumerable.Empty<string>()).ToHashSet();
            if (limitAccessToGroupNames.Count == 0)
                return;
            if (requestContext.CurrentUserIdentity.FindFirst("ntech.issystemuser")?.Value == "true")
                return;

            var userGroupNames = requestContext.CurrentUserIdentity.FindAll("ntech.group").Select(x => x.Value).ToHashSet();

            if (limitAccessToGroupNames.Intersect(userGroupNames).Count() == 0)
                throw new NTechWebserviceMethodException("Access denied, missing required group.")
                {
                    ErrorCode = "accessDeniedMissingRequiredGroup",
                    ErrorHttpStatusCode = 403,
                    IsUserFacing = true
                };
        }

        public ActionResult Execute(NTechWebserviceMethodRequestContext requestContext,
            Action<Exception> logException,
            Action<string, string> logRequestAndResponse)
        {
            Func<ActionResult> exec = () =>
                {
                    try
                    {
                        RestrictAccessByGroup(requestContext);
                        return DoExecute(requestContext);
                    }
                    catch (NTechWebserviceMethodException ex)
                    {
                        if (ex.IsUserFacing)
                            return Error(ex.Message, httpStatusCode: ex.ErrorHttpStatusCode, errorCode: ex.ErrorCode);
                        else
                        {
                            if (!requestContext.IsExceptionLoggingDisabled)
                                logException?.Invoke(ex);
                            return Error("Internal server error", httpStatusCode: 500);
                        }
                    }
                    catch (NTechCoreWebserviceException ex)
                    {
                        if (ex.IsUserFacing)
                            return Error(ex.Message, httpStatusCode: ex.ErrorHttpStatusCode, errorCode: ex.ErrorCode);
                        else
                        {
                            if (!requestContext.IsExceptionLoggingDisabled)
                                logException?.Invoke(ex);
                            return Error("Internal server error", httpStatusCode: 500);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!requestContext.IsExceptionLoggingDisabled)
                            logException?.Invoke(ex);
                        return Error("Internal server error", httpStatusCode: 500);
                    }
                };
            var result = exec();

            if (!requestContext.IsRequestLoggingDisabled)
                logRequestAndResponse?.Invoke(GetRequestLogJson(requestContext), GetResponseLogJson(requestContext, result));

            return result;
        }

        private ActionResult Error(string errorMessage, string errorCode = null, int? httpStatusCode = null)
        {
            return CreateErrorResponse(errorMessage, errorCode: errorCode, httpStatusCode: httpStatusCode);
        }

        public static ActionResult CreateErrorResponse(string errorMessage, string errorCode = null, int? httpStatusCode = null)
        {
            return new ActionResult
            {
                IsError = true,
                JsonResult = JsonConvert.SerializeObject(new
                {
                    errorMessage = errorMessage ?? GenericErrorCode,
                    errorCode = errorCode ?? GenericErrorCode
                }, Newtonsoft.Json.Formatting.None,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                ErrorMessage = errorMessage ?? GenericErrorCode,
                ErrorHttpStatusCode = httpStatusCode ?? 400,
                ErrorCode = errorCode ?? GenericErrorCode
            };
        }

        public static RawJsonActionResult ToFrameworkErrorActionResult(ActionResult result)
        {
            return new RawJsonActionResult
            {
                JsonData = result.JsonResult,
                CustomHttpStatusCode = result.ErrorHttpStatusCode ?? 400,
                CustomStatusDescription = result.ErrorMessage,
                IsNTechApiError = true
            };
        }

        public const string GenericErrorCode = "generic";

        protected abstract ActionResult DoExecute(NTechWebserviceMethodRequestContext requestContext);

        protected void Validate<TRequest>(TRequest r, Action<ValidationHelper<TRequest>> doValidation)
        {
            var h = new ValidationHelper<TRequest>(r);
            doValidation(h);
            if (h.MissingRequiredProperties.Any())
            {
                throw new NTechWebserviceMethodException($"Missing required properties: {string.Join(", ", h.MissingRequiredProperties)}")
                {
                    ErrorCode = "missingRequiredProperties",
                    ErrorHttpStatusCode = 400,
                    IsUserFacing = true
                };
            }
        }

        protected void ValidateUsingAnnotations<TRequest>(TRequest r)
        {
            if (r == null)
                throw new NTechWebserviceMethodException("Missing request")
                {
                    ErrorCode = "missingRequest",
                    ErrorHttpStatusCode = 400,
                    IsUserFacing = true
                };

            var validator = new NTechWebserviceRequestValidator();
            var errors = validator.Validate(r);

            if (errors.Any())
            {
                var msgs = errors.Select(x =>
                   $"{x.Path}{(x.ListFirstErrorIndex.HasValue ? $"[{x.ListFirstErrorIndex.Value}]" : "")}"
                );

                throw new NTechWebserviceMethodException($"Invalid or missing properties: {string.Join(", ", msgs)}")
                {
                    ErrorCode = "invalidOrMissingProperties",
                    ErrorHttpStatusCode = 400,
                    IsUserFacing = true
                };
            }
        }

        public class ValidationHelper<TRequest>
        {
            private readonly TRequest request;
            public List<string> MissingRequiredProperties { get; set; } = new List<string>();

            public ValidationHelper(TRequest request)
            {
                this.request = request;
            }

            public void Require<TValue>(Expression<Func<TRequest, TValue>> propertyExpression)
            {
                var p = ExpressionExtensions.GetPropertyInfo(request, propertyExpression);
                if (p == null)
                    MissingRequiredProperties.Add(p.Name);
                else
                {
                    var v = p.GetValue(request);

                    if (v == null || (p.PropertyType.FullName == "System.String" && string.IsNullOrWhiteSpace(v as string)))
                        MissingRequiredProperties.Add(p.Name);
                }
            }            
        }
    }
}