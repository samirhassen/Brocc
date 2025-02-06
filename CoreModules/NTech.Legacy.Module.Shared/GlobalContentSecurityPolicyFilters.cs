using NWebsec.Csp;
using NWebsec.Mvc.HttpHeaders.Csp;
using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace NTech.Services.Infrastructure
{
    public class GlobalContentSecurityPolicyFilters
    {
        /// <summary>
        /// Use Enabled = true/false for either CspReportOnlyAttribute or CspAttribute to steer the usage of CSP.
        /// We should always test the CSP-settings with ReportOnly first in production before they are added for real,
        /// otherwise things might break for the users and we do not want to hotfix this.
        /// We can add an endpoint using the NWebSec NuGet, see more in their documentation on how to do that.
        /// See more: https://docs.nwebsec.com/en/aspnet4/nwebsec/Configuring-csp.html
        /// Note that it also exists for .NET Core, however this solution still uses .NET Framework.
        /// 
        /// Comments: this is using the "Configure using MVC Attributes" way. It can be configured using Web.config or Middleware as well.
        /// Specific attributes can be used on specific routes to enable ex. a certain iFrame for a site.
        /// 
        /// The generated policy can be checked here: https://csp-evaluator.withgoogle.com/
        ///
        /// If we want to test a new policy, just change the value in the ReportOnly-attribute. It will yield a second policy 'content-security-policy-report-only" in the
        /// response header that will only call the report-uri that is setup. 
        /// </summary>
        /// <param name="filters">Filters sent in from Global.Asax. </param>
        /// <param name="reportOnly">Should the CSP be set to report only. Default false. </param>
        public static void RegisterGlobalFilters(GlobalFilterCollection filters, Action<GlobalFilterCollection> overrideBaseUriSetup = null, bool scriptsAllowUnsafeEval = false)
        {
            // Modulesearch calls backoffice from all modules. 
            var reportOnly = NTechEnvironment.Instance.OptBoolSetting("ntech.contentsecuritypolicy.reportonly");

            filters.Add(new CspAttribute { Enabled = !reportOnly });
            filters.Add(new CspReportOnlyAttribute { Enabled = reportOnly });

            // Fallback for all other srcs not defined. 
            filters.Add(new CspDefaultSrcAttribute { Self = true });
            filters.Add(new CspDefaultSrcReportOnlyAttribute { Self = true });

            // Javascript. For pages that uses a nonce on <script>, it will be added automatically by the framework in the response header. 
            //  'strict-dynamic' allows the execution of scripts dynamically added to the page, as long as they were loaded by a safe, already-trusted script 
            filters.Add(new CspScriptSrcAttribute { Self = true, UnsafeEval = scriptsAllowUnsafeEval });
            filters.Add(new CspScriptSrcReportOnlyAttribute { Self = true, UnsafeEval = scriptsAllowUnsafeEval });

            // We need unsafe-inline since we have a lot of inline css on element (<div style="margin: 5px;"> etc.)
            // We use font Roboto from Google. https://content-security-policy.com/examples/google-fonts/
            filters.Add(new CspStyleSrcAttribute { Self = true, UnsafeInline = true, CustomSources = "https://www.uc.se/ucwebresources/ fonts.googleapis.com" });
            filters.Add(new CspStyleSrcReportOnlyAttribute { Self = true, UnsafeInline = true, CustomSources = "https://www.uc.se/ucwebresources/ fonts.googleapis.com" });

            // We use font Roboto from Google. 
            filters.Add(new CspFontSrcAttribute { Self = true, CustomSources = "fonts.gstatic.com" });
            filters.Add(new CspFontSrcReportOnlyAttribute { Self = true, CustomSources = "fonts.gstatic.com" });

            // https://security.stackexchange.com/a/167244 ex. toastr loads background-image: data:image/png<base64>.
            // data: is unsafe since it allows for arbitrary injection of any uris. Nothing can be specified after : (port). 
            filters.Add(new CspImgSrcAttribute { Self = true, CustomSources = "https://www.uc.se/ucwebresources/ data:" });
            filters.Add(new CspImgSrcReportOnlyAttribute { Self = true, CustomSources = "https://www.uc.se/ucwebresources/ data:" });

            SetupCrossServiceCommunication(filters);

            // To disallow attackers from injecting base-tag, which changes all urls (a, script etc.). Does not fallback to default-src so need to be set explicitly. 
            if (overrideBaseUriSetup == null)
            {
                filters.Add(new CspBaseUriAttribute { None = true });
                filters.Add(new CspBaseUriReportOnlyAttribute { None = true });
            }
            else
                overrideBaseUriSetup(filters);

            // Disallow usage of elements <object>, <applet> and <embed>. Recommended. 
            filters.Add(new CspObjectSrcAttribute { None = true });
            filters.Add(new CspObjectSrcReportOnlyAttribute { None = true });

            // Adds report-uri /WebResource.axd?cspReport=true to the CSP, needs NWebsecHttpHeaderSecurityModule_CspViolationReported in Global.Asax.cs. 
            filters.Add(new CspReportUriAttribute { EnableBuiltinHandler = true });
            filters.Add(new CspReportUriReportOnlyAttribute { EnableBuiltinHandler = true });
        }

        private static void SetupCrossServiceCommunication(GlobalFilterCollection filters)
        {
            var serviceRegistry = NTechEnvironment.Instance.ServiceRegistry;
            var customSources = "";
            serviceRegistry.External.Values.ToList().ForEach(x =>
            {
                customSources += (customSources.Length == 0 ? "" : " ") + x;
            });

            // We use modulesearch that calls a url in our backoffice, which needs to be allowed explicitly. 
            filters.Add(new CspConnectSrcAttribute { Self = true, CustomSources = customSources });
            filters.Add(new CspConnectSrcReportOnlyAttribute { Self = true, CustomSources = customSources });
        }

        public static void LogToFile(CspViolationReport violationReport, DirectoryInfo logFolder)
        {
            var reportDetails = violationReport.Details;

            var violationReportString = $"UserAgent:<{violationReport.UserAgent}>\r\n" +
                                        $"BlockedUri:<{reportDetails.BlockedUri}>\r\n" +
                                        $"DocumentUri:<{reportDetails.DocumentUri}>\r\n" +
                                        $"EffectiveDirective:<{reportDetails.EffectiveDirective}>\r\n" +
                                        $"OriginalPolicy:<{reportDetails.OriginalPolicy}>\r\n" +
                                        $"ViolatedDirective:<{reportDetails.ViolatedDirective}>\r\n" +
                                        $"\n";

            var reportOnly = NTechEnvironment.Instance.OptBoolSetting("ntech.contentsecuritypolicy.reportonly");

            if (logFolder != null)
            {

                var logger = new RotatingLogFile(
                    logFolder.FullName + "/CSPReports",
                    "CSP",
                    () => DateTime.Now,
                    new RotatingLogFile.FileSystem());
                logger.Log(violationReportString, reportOnly ? "reportOnly" : "activePolicy");
            }
        }

    }
}
