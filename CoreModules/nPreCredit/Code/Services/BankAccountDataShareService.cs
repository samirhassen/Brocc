using NTech.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class BankAccountDataShareService : BankAccountDataShareServiceBase, IBankAccountDataShareService
    {
        private readonly PreCreditContextFactoryService contextService;
        private readonly IApplicationCommentServiceComposable applicationCommentService;

        public BankAccountDataShareService(PreCreditContextFactoryService contextService, IApplicationCommentServiceComposable applicationCommentService)
        {
            this.contextService = contextService;
            this.applicationCommentService = applicationCommentService;
        }

        public void OnDataShared(string applicationNr, int applicantNr, string rawAccountDataArchiveKey, string pdfPreviewArchiveKey)
        {
            using (var context = contextService.CreateExtendedConcrete())
            {
                context.DoUsingTransaction(() =>
                {
                    var application = context.CreditApplicationHeaders.Include("Items").Single(x => x.ApplicationNr == applicationNr);
                    var evt = context.CreateAndAddEvent(CreditApplicationEventCode.SharedBankAccountDataAttached, creditApplicationHeader: application);
                    context.AddOrUpdateCreditApplicationItems(application, new List<PreCreditContextExtended.CreditApplicationItemModel>
                    {
                        new PreCreditContextExtended.CreditApplicationItemModel
                        {
                            GroupName = $"applicant{applicantNr}",
                            Name = "sharedAccountDataRawAccountDataArchiveKey",
                            Value = rawAccountDataArchiveKey
                        },
                        new PreCreditContextExtended.CreditApplicationItemModel
                        {
                            GroupName = $"applicant{applicantNr}",
                            Name = "sharedAccountDataPdfPreviewArchiveKey",
                            Value = pdfPreviewArchiveKey
                        }
                    }, CreditApplicationEventCode.SharedBankAccountDataAttached.ToString());

                    //Add a comment with the data and the pdf attached
                    if (!applicationCommentService.TryAddCommentComposable(
                        applicationNr, $"Applicant {applicantNr} shared bank account data",
                        CreditApplicationEventCode.SharedBankAccountDataAttached.ToString(),
                        CommentAttachment.CreateSharedBankAccountData(rawAccountDataArchiveKey, pdfPreviewArchiveKey),
                        out var failedMessage,
                        context))
                        throw new Exception(failedMessage);

                    context.SaveChanges();
                });
            }
        }
    }

    public interface IBankAccountDataShareService
    {
        void OnDataShared(string applicationNr, int applicantNr, string rawAccountDataArchiveKey, string pdfPreviewArchiveKey);
    }
}