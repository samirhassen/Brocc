using NTech.Services.Infrastructure.Email;

namespace nScheduler.Code.Email
{
    public static class EmailServiceFactory
    {
        public static INTechEmailService CreateEmailService()
        {
            var renderer = new EmailRenderer(
                x => CommonMark.CommonMarkConverter.Convert(x),
                (x, y) => Nustache.Core.Render.StringToString(x, y));
            var factory = new NTechEmailServiceFactory(renderer);
            return factory.CreateEmailService();
        }
    }
}