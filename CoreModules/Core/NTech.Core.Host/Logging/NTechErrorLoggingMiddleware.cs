namespace NTech.Core.Host.Logging
{
    public class NTechErrorLoggingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<NTechErrorLoggingMiddleware> logger;

        public NTechErrorLoggingMiddleware(RequestDelegate next, ILogger<NTechErrorLoggingMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in ...");
                throw;
            }
        }
    }
}
