using nPreCredit.Code.Services;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    public partial class CreditManagementController
    {
        [Route("CancelApplication")]
        [HttpPost()]
        public ActionResult CancelApplication(string applicationNr, CancelledByExternalRequestModel cancelledByExternalRequest)
        {
            var contextFactory = Service.Resolve<IPreCreditContextFactoryService>();
            var repo = Service.Resolve<CreditManagementWorkListService>();

            using (var context = contextFactory.CreateExtended())
            {
                context.BeginTransaction();
                try
                {
                    var app = context
                                .CreditApplicationHeadersWithItemsIncludedQueryable
                                .Where(x => x.ApplicationNr == applicationNr)
                                .Single();

                    var providerApplicationId = app.Items.Where(y => y.GroupName == "application" && y.Name == "providerApplicationId" && !y.IsEncrypted).Select(y => y.Value).FirstOrDefault();

                    var m = repo.GetSearchModel(context, true).Where(x => x.ApplicationNr == applicationNr).SingleOrDefault();

                    if (app.IsFinalDecisionMade)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Attempted to cancel application {applicationNr} where the credit has already been created");
                    }

                    if (!app.IsActive)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Attempted to cancel application {applicationNr} that is not active");
                    }

                    if (app.IsCancelled)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Attempted to cancel application {applicationNr} that is already cancelled");
                    }

                    var now = Clock.Now;
                    var primaryCategoryCode = CreditManagementWorkListService.PickCancelleationCategoryCode(m.CategoryCodes);
                    string cancelledState = "Category_" + primaryCategoryCode;
                    var commentText = $"Application manually cancelled from category {primaryCategoryCode}";

                    if (cancelledByExternalRequest.WasCancelledByExternal)
                    {
                        commentText = $"Application cancelled by provider from category {primaryCategoryCode} using status code {cancelledByExternalRequest.CancelStatusCode}";

                        //TODO: Remove the and port AddOrUpdateCreditApplicationItems to shared
                        ((PreCreditContextExtended)context).AddOrUpdateCreditApplicationItems(app, new List<PreCreditContextExtended.CreditApplicationItemModel>
                        {
                            new PreCreditContextExtended.CreditApplicationItemModel
                            {
                                GroupName = "application",
                                IsEncrypted = false,
                                Name = "providerCancellationStatus",
                                Value = cancelledByExternalRequest.CancelStatusCode ?? "Unknown"
                            }
                        }, "CancelApplication");
                    }

                    Code.Services.ApplicationCancellationService.SetApplicationAsCancelled(app, now, false, cancelledState, commentText, CurrentUserId, InformationMetadata, context, providerApplicationId);
                    context.SaveChanges();
                    context.CommitTransaction();

                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }
            }
        }

        [Route("ReactivateApplication")]
        [HttpPost()]
        public ActionResult ReactivateApplication(string applicationNr)
        {
            using (var context = new PreCreditContext())
            {
                var app = context.CreditApplicationHeaders.Single(x => x.ApplicationNr == applicationNr);

                if (app.IsFinalDecisionMade)
                {
                    NLog.Warning("Reactivate got called on an application that is already a loan. This is not expected. {applicationNr}", applicationNr);
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Attempted to reactivate application {applicationNr} where the credit has already been created");
                }

                if (!(app.IsCancelled || app.IsRejected))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Attempted to reactivate application {applicationNr} that is not cancelled or rejected");
                }

                if (app.IsActive)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Attempted to reactivate application {applicationNr} that is already active");
                }

                var now = Clock.Now;
                var c = new CreditApplicationComment
                {
                    ApplicationNr = applicationNr,
                    CommentText = $"Application reactivated",
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

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }
    }

    public class CancelledByExternalRequestModel
    {
        public bool WasCancelledByExternal { get; set; }
        public string CancelStatusCode { get; set; }
    }
}