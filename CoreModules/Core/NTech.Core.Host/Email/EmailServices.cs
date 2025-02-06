using NTech.Services.Infrastructure.Email;

namespace NTech.Core.Host.Email
{
    public class EmailServices
    {
        internal static void AddServices(IServiceCollection services)
        {
            services.AddScoped<IEmailRenderer, EmailRenderer>();
            services.AddScoped<INTechEmailServiceFactory, NTechEmailServiceFactory>();
        }
    }
}
