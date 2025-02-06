using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NTech.Services.Infrastructure;
using NTechSignicat.Clients;
using NTechSignicat.Services;
using NTechSignicat.Shared;

namespace NTechSignicat
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            NEnv = new NEnv(env, configuration);
        }

        public IConfiguration Configuration { get; }
        public INEnv NEnv { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            var signicatSettings = new SignicatSettings(NEnv);

            services.AddSingleton<SignicatSettings>(signicatSettings);
            services.AddSingleton<INEnv>(NEnv);

            if (!NEnv.IsProduction && signicatSettings.UseLocalMockForLogin)
            {
                services.AddSingleton<ISignicatAuthenticationService, MockAuthenticationService>();
            }
            else
            {
                services.AddSingleton<ISignicatAuthenticationService, SignicatAuthenticationService>();
            }

            if (!NEnv.IsProduction && signicatSettings.UseLocalMockForSignatures)
            {
                services.AddSingleton<ISignicatSignatureService, MockSignatureService>();
            }
            else
            {
                services.AddSingleton<ISignicatSignatureService, SignicatSignatureService>();
            }

            services.AddSingleton<SignicatLoginMethodValidator>();
            services.AddHttpClient<IAuditClient, AuditClient>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
            services.AddSingleton<NTechAuditSystemLogBatchingService>();
            services.AddHostedService<BackgroundServiceStarter<NTechAuditSystemLogBatchingService>>();

            services.AddSingleton<SqliteDocumentDatabaseService>();
            services.AddHostedService<BackgroundServiceStarter<SqliteDocumentDatabaseService>>();
            services.AddSingleton<IDocumentDatabaseService>(x => x.GetRequiredService<SqliteDocumentDatabaseService>());
            services.AddTransient<WcfLoggingMessageInspectorAndBehaviour>();
            services.AddSingleton<NTechServiceRegistry>(NEnv.ServiceRegistry);
            services.AddSingleton<IDocumentService, DocumentService>();
            services.AddSingleton<SignicatMessageEncryptionService>();

            services.AddHttpClient(SignicatSignatureService.SignicatSdsServiceHttpClientName).ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new System.Net.Http.HttpClientHandler();

                if (signicatSettings.HasSignatureClientCertificate)
                {
                    handler.ClientCertificateOptions = System.Net.Http.ClientCertificateOption.Manual;
                    var clientCert = SignicatSignatureService.LoadClientCertificate(signicatSettings);
                    handler.ClientCertificates.Add(SignicatSignatureService.LoadClientCertificate(signicatSettings));
                }

                return handler;
            });

            NTechBearerTokenLogin.ConfigureAuthenticationServices(services, NEnv);

            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            NTechHttpHardening.HandleCachingAndInformationLeakHeader(app, true);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseMiddleware<NTechErrorLoggingMiddleware>();

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            loggerFactory.AddProvider(new NTechLoggerProvider(serviceProvider));
        }
    }
}