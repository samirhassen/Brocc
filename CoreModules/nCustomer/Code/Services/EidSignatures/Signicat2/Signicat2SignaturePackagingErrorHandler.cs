using NTech.Services.Infrastructure;
using System;
using System.Threading.Tasks;

namespace nCustomer.Code.Services.EidSignatures.Signicat2
{
    /*
     Signicats new api breaks the workflow that the old one supported.
     Before when the customer came back with a result you could call the package service synchronously
     and use the signed pdf right away. 

     In the new version the package service is gone and replaced with
     a package service that they call themselves asynchronously and is sometimes done when the customer
     returns and sometimes not. Support this for real would mean having the customer actually leave
     and get a notification when signicat is done packaging which we somehow need to figure out in the system
     which requires rebuilding the entire workflow for things like additional questions. 

     We will try waiting up to 10*3 seconds for the packaging task to complete but this may sometimes just fail.

     If we got no package result by then we just give up fail the session as if the customer aborted so it can be retried.
     Override with optional settings:
     packagingTryCount and packagingTrySleepMs
     So:
     packagingTryCount = 1
     packagingTrySleepMs = <whatever>
     Means try just once, never sleep

     packagingTryCount = 2
     packagingTrySleepMs = 2000
     Means try twice and wait 2 seconds between tries

      Setup logging retries with logFolder and isLoggingPackagingRetries
     */
    internal static class Signicat2SignaturePackagingErrorHandler
    {
        public static async Task RetryOnPackagingError(Func<bool, Task<bool>> taskToRetry, NTechSimpleSettings settings, string localSessionId)
        {
            var logRetryEvent = CreatePackagingErrorLogger(settings, localSessionId);

            var maxTryCount = int.Parse(settings.Opt("packagingTryCount") ?? "10");
            var timeBetweenRetries = TimeSpan.FromMilliseconds(int.Parse(settings.Opt("packagingTrySleepMs") ?? "3000"));

            var tryCount = 0;
            while (tryCount < maxTryCount)
            {
                tryCount++;

                if(tryCount > 0)
                {
                    await Task.Delay(timeBetweenRetries);
                }

                var isRetryRequested = await taskToRetry(tryCount >= maxTryCount);

                if(!isRetryRequested)
                {
                    if(tryCount > 1)
                    {
                        logRetryEvent($"Success after {tryCount} tries");
                    }                    
                    return;
                }
            }

            logRetryEvent($"Failed after {tryCount} tries");

            throw new Exception("Failed after max nr of tries");
        }

        private static Action<string> CreatePackagingErrorLogger(NTechSimpleSettings settings, string localSessionId)
        {
            Action<string> result = x => { };
            var logFolder = settings.Opt("logFolder");
            if (logFolder == null)
                return result;

            if (!settings.OptBool("isLoggingPackagingRetries"))
                return result;

            var f = new RotatingLogFile(logFolder, "packagingRetries");
            return x =>
            {
                f.Log($"{localSessionId}: {x}");
            };
        }
    }
}