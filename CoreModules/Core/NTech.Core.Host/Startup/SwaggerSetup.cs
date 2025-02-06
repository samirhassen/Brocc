using Microsoft.OpenApi.Models;
using NTech.Core.Module;
using System.Reflection;

namespace NTech.Core.Host.Startup
{
    internal static class SwaggerSetup
    {
        const string SwaggerPrefix = "v1";

        public static void AddServices(IServiceCollection services, NEnv env)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(SwaggerPrefix, new OpenApiInfo { Title = "NTech Apis", Version = "v1" });
                c.DocumentFilter<NTechSwaggerFilter>();

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\"",
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme {
                                Reference = new OpenApiReference {
                                    Type = ReferenceType.SecurityScheme,
                                        Id = "Bearer"
                                }
                            },
                            new string[] {}
                        }
                    });

                // Make sure xml comments on controllers are included in the swagger.
                var documentationAssemblies = new List<Assembly>();
                documentationAssemblies.Add(Assembly.GetEntryAssembly());
                foreach (var module in ModuleLoader.AllModules.Value)
                {
                    if (module.IsActive(env))
                    {
                        documentationAssemblies.Add(module.GetType().Assembly);
                        foreach(var extraAssembly in module.ExtraDocumentationAssemblies)
                        {
                            documentationAssemblies.Add(extraAssembly);
                        }
                    }
                }

                foreach (var documentationAssembly in documentationAssemblies)
                {
                    var xmlDocumentationFilename = Path.Combine(
                        AppContext.BaseDirectory,
                        $"{documentationAssembly.GetName().Name}.xml");
                    if (!File.Exists(xmlDocumentationFilename))
                    {
                        throw new Exception(documentationAssembly.GetName().Name + @": The xml documentation file needed for swagger docs to work is missing. You most likely forgot to add the following to your csproj file:
                <PropertyGroup>
                  <GenerateDocumentationFile>true</GenerateDocumentationFile>
                  <NoWarn>$(NoWarn);1591</NoWarn>
                </PropertyGroup>");
                    }
                    c.IncludeXmlComments(xmlDocumentationFilename);
                }
            });

            //This allows things like [JsonConverter(typeof(StringEnumConverter))] to be respected by the documentation
            services.AddSwaggerGenNewtonsoftSupport();
        }

        public static void UseApplication(WebApplication app)
        {
            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                /*
                 Swagger json will be at: /swagger/v1/swagger.json
                 Swagger ui will be at: /swagger/index.html
                 */
                c.SwaggerEndpoint($"{SwaggerPrefix}/swagger.json", "Api description");
            });
        }
    }
}
