using System.Text.Json.Serialization;
using System.Xml.Linq;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using NTech.Core;
using NTech.Core.Credit.Services;
using NTech.Core.Host.Email;
using NTech.Core.Host.Infrastructure;
using NTech.Core.Host.Services;
using NTech.Core.Host.Startup;
using NTech.Core.Module;
using NTech.Core.Module.Infrastrucutre;
using NTech.Core.Module.Infrastrucutre.HttpClient;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var externalConfigFile = configuration.GetValue<string>("ntech.corehost.externalconfigfile");
if (externalConfigFile != null)
{
    //Used mainly for localhost development
    builder.Configuration.AddJsonFile(new PhysicalFileProvider(Path.GetDirectoryName(externalConfigFile)),
        Path.GetFileName(externalConfigFile), false, true);
}

var env = new NEnv(configuration);
NEnv.SharedInstance = env;

// Add services to the container.
var services = builder.Services;

services.AddSingleton<NEnv>();
services.AddSingleton<INTechEnvironment>(x => x.GetRequiredService<NEnv>());
services.AddSingleton<NTechServiceRegistry>(env.ServiceRegistry);
services.AddSingleton<INTechServiceRegistry>(env.ServiceRegistry);
services.AddHttpContextAccessor(); //Probably needed for INtechCurrentUserMetadata
services.AddTransient<IClientConfigurationCore, ClientConfigurationCore>(x =>
{
    // TODO: Add caching + changetracking of the file on disk that breaks the cache
    var env = x.GetRequiredService<NEnv>();
    var clientConfigurationDocumentFile = env.ClientConfigurationDocumentFile;
    return ClientConfigurationCore.CreateUsingXDocument(XDocument.Load(clientConfigurationDocumentFile));
});
services.AddSingleton<ServiceClientSyncConverterCore>();
services.AddScoped<INTechCurrentUserMetadata, HttpContextCurrentUserMetadata>();
services.AddHostedService<CrossModuleEventService>();
services.AddSingleton<CrossModuleEventQueue>();
services.AddSingleton<ICrossModuleEventQueue>(x => x.GetRequiredService<CrossModuleEventQueue>());
services.AddSingleton<TelemetryService>();
ModuleClientRegistration.AddModuleClients(services, env);
SettingsRegistration.AddSettingsServices(services);

services.AddSingleton<CoreHostClock>(x =>
{
    var user = x.GetRequiredService<NHttpServiceSystemUser>();
    var clientFactory = x.GetRequiredService<ServiceClientFactory>();
    var systemUserTestClient = new TestClient(user, clientFactory);
    return new CoreHostClock(env, systemUserTestClient);
});
services.AddSingleton<ICoreClock>(x => x.GetRequiredService<CoreHostClock>());
services.AddSingleton<IMustacheTemplateRenderingService, StubbleMustacheTemplateService>();
services.AddSingleton<IMarkdownTemplateRenderingService, CommonMarkMarkdownTemplateRenderingService>();

EmailServices.AddServices(services);

LoggingSetup.AddApiRequestLogging(services, env);
SwaggerSetup.AddServices(services, env);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policyBuilder =>
    {
        var serviceRegistry = env.ServiceRegistry;
        var corsDomains = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        serviceRegistry.External.Values.ToList().ForEach(url =>
        {
            corsDomains.Add(new Uri(url).GetLeftPart(UriPartial.Authority));
        });
        policyBuilder.AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins(corsDomains.ToArray())
            .AllowCredentials();
    });
});

services
    .AddUserModuleBearerTokenAuthentication(env);

services
    .AddControllers(options =>
    {
        options.Filters.Add<FeatureToggleActionFilter>();
        options.Filters.Add<NTechErrorActionFilter>();
        options.Filters.Add<PreserveCaseActionFilter>();
    })
    .ConfigureActiveModules(env)
    .AddNewtonsoftJson()
    .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });

services
    .AddBearerTokenAuthorizationPolicy();

LoggingSetup.AddServices(services);

foreach (var module in ModuleLoader.AllModules.Value.Where(module => module.IsActive(env)))
{
    module.AddServices(services, env);
}

var app = builder.Build();

LoggingSetup.UseApiRequestLogging(app, env);
LoggingSetup.UseApplication(app, app.Services.GetService<ILoggerFactory>(), app.Services, env);

app.MapGet("/", () => "Ok");
app.MapGet("/hb", () => "Ok");

app.UseCors();
app.UseRouting();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    // This is to increase the chance that HttpContext.Connection.RemoteIpAddress works through proxies
    // This must be kept before UseAuthentication
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints
        .MapControllers()
        .RequireAuthorization();
    endpoints.MapSwagger();
});

app.Lifetime.ApplicationStarted.Register(() =>
{
    foreach (var module in ModuleLoader.AllModules.Value.Where(module => module.IsActive(env)))
    {
        module.OnApplicationStarted(app.Logger);
    }
});

SwaggerSetup.UseApplication(app);

NTechHttpHardening.HandleCachingAndInformationLeakHeader(app, true);

CoreHostClock.RegisterAfterStartupInit(app.Lifetime);

SettingsRegistration.UseSettings(app);

//Enable logging (when we add whatever creates the logging database, move this code to just after that)
app.Services.GetRequiredService<SystemLogService>().IsPendingStartup = false;

app.Run();