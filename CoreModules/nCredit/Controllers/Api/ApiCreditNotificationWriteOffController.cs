using nCredit.DbModel.BusinessEvents;
using Newtonsoft.Json;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class ApiCreditNotificationWriteOffController : NController
    {
        [HttpPost]
        [Route("Api/Credit/SingleNotificationWriteOff")]
        public ActionResult SingleNotificationWriteOff()
        {
            //Built in shit serializer cannot handle dicitionary but instead fills it with superstrange random values
            Request.InputStream.Position = 0;
            using (var r = new StreamReader(Request.InputStream))
            {
                var requestString = r.ReadToEnd();
                var request = JsonConvert.DeserializeAnonymousType(requestString, new
                {
                    notificationId = default(int),
                    fullWriteOffAmountTypes = (IList<string>)null,
                    fullWriteOffAmountTypeUniqueIds = (IList<string>)null,
                    partialWriteOffAmountTypeUniqueIdsAndAmounts = (IDictionary<string, decimal>)null,
                    writeOffEntireNotification = (bool?)null
                });

                var mgr = new NotificationWriteOffBusinessEventManager(GetCurrentUserMetadata(), NEnv.IsMortgageLoansEnabled, Service.ContextFactory,
                    CoreClock.SharedInstance, NEnv.ClientCfgCore, NEnv.EnvSettings, Service.PaymentOrder);

                if (mgr.TryWriteOffNotifications(new List<NotificationWriteOffBusinessEventManager.WriteOffInstruction>
                    {
                        new NotificationWriteOffBusinessEventManager.WriteOffInstruction
                        {
                            NotificationId = request.notificationId,
                            FullWriteOffAmountTypeUniqueIds = request.fullWriteOffAmountTypeUniqueIds,
                            PartialWriteOffAmountTypeUniqueIdsAndAmounts = request.partialWriteOffAmountTypeUniqueIdsAndAmounts,
                            WriteOffEntireNotification = request.writeOffEntireNotification
                        }
                    },
                    Service.CreditTerminationLettersInactivationBusinessEventManager,
                    out var errors, out var _))
                {
                    return Json2(new { });
                }
                else
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, string.Join(", ", errors));
                }
            }
        }
    }
}