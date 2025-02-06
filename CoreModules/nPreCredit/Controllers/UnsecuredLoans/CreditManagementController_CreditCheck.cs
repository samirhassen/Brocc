using nPreCredit.Code;
using nPreCredit.Code.Services;
using System;
using System.Linq;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    public partial class CreditManagementController
    {
        [Route("NewCreditCheck")]
        public ActionResult NewCreditCheck(string applicationNr)
        {
            using (var context = new PreCreditContext())
            {
                var app = context.CreditApplicationHeaders.Single(x => x.ApplicationNr == applicationNr);

                if (app.IsFinalDecisionMade)
                {
                    throw new Exception($"Attempted to reactivate application {applicationNr} where the credit has already been created");
                }

                if (!app.IsActive)
                {
                    var now = Clock.Now;
                    var c = new CreditApplicationComment
                    {
                        ApplicationNr = applicationNr,
                        CommentText = "Application reactivated",
                        CommentById = CurrentUserId,
                        ChangedById = CurrentUserId,
                        CommentDate = now,
                        ChangedDate = now,
                        EventType = "ApplicationReactivated",
                        InformationMetaData = InformationMetadata
                    };
                    app.IsActive = true;

                    app.IsCancelled = false;
                    app.CancelledBy = null;
                    app.CancelledDate = null;
                    app.CancelledState = null;

                    app.IsRejected = false;
                    app.RejectedById = null;
                    app.RejectedDate = null;
                    app.RejectedById = null;
                    app.RejectedDate = null;

                    context.CreditApplicationComments.Add(c);

                    context.SaveChanges();
                }

                var handler = DependancyInjection.Services.Resolve<ICreditApplicationTypeHandler>();
                return Redirect(handler.GetNewCreditCheckUrl(DependancyInjection.Services.Resolve<IServiceRegistryUrlService>(), applicationNr));
            }
        }
    }
}