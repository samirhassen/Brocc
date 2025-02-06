using nCredit.Code;
using Newtonsoft.Json;
using NTech.Core.Module.Shared.Clients;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure.NTechWs;
using Renci.SshNet.Security;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace nCredit.WebserviceMethods.MortgageLoanCustomerPages
{
    public class MortgageLoanCustomerPagesGetLoanKycQuestionsMethod : MortgageLoanCustomerPagesMethod<MortgageLoanCustomerPagesGetLoanKycQuestionsMethod.Request, MortgageLoanCustomerPagesGetLoanKycQuestionsMethod.Response>
    {
        protected override string MethodName => "loan-kyc-questions";

        protected override Response DoCustomerLockedExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request, int customerPagesUserCustomerId)
        {
            using (var context = new CreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var loan = Controllers.ApiCustomerPagesController.GetCustomerFacingCreditModels(context, customerPagesUserCustomerId)
                    .Where(x => x.CreditNr == request.LoanNr)
                    .SingleOrDefault();
                if (loan == null)
                    return Error("No such loan", httpStatusCode: 400, errorCode: "noSuchLoan");
                
                var questionAndAnswerSets = new List<Response.QuestionSetModel>();

                if (!string.IsNullOrWhiteSpace(loan.KycQuestionsJsonDocumentArchiveKey))
                {
                    var initialQuestions = LoadFromArchiveKey(loan.KycQuestionsJsonDocumentArchiveKey, loan.CustomerApplicantNr, loan.StartDate.DateTime,
                        requestContext.Service().DocumentClientHttpContext);
                    if (initialQuestions != null)
                        questionAndAnswerSets.Add(initialQuestions);
                }

                return new Response
                {
                    QuestionAndAnswerSets = questionAndAnswerSets.OrderByDescending(x => x.AnswerDate).ToList()
                };
            }
        }

        private Response.QuestionSetModel LoadFromArchiveKey(string archiveKey, int customerApplicantNr, DateTime defaultDate, IDocumentClient d)
        {
            var fetchResult = d.TryFetchRaw(archiveKey);
            if(!fetchResult.IsSuccess)
                throw new Exception($"Missing document {archiveKey} in the archive"); 

            var data = fetchResult.FileData;
            if (data == null)
                return null;
            if (!(fetchResult.ContentType ?? "").Contains("json"))
                return null;

            var dataStr = Encoding.UTF8.GetString(data);

            var model = JsonConvert.DeserializeObject<KycQuestionsDocumentModel>(dataStr);
            if (model == null || model.Items == null || model.Items.Count == 0)
                return null;

            var currentCustomerContextualQuestions = model
                .Items
                .Where(x => (x.QuestionGroup ?? "").IsOneOfIgnoreCase("customer", "product") && (x.ApplicantNr == null || x.ApplicantNr == customerApplicantNr))
                .Select(x => new Response.QuestionSetModel.QAModel
                {
                    AnswerCode = x.AnswerCode,
                    AnswerText = x.AnswerText,
                    QuestionCode = x.QuestionCode,
                    QuestionGroup = x.QuestionGroup,
                    QuestionText = x.QuestionText
                }).ToList();

            return new Response.QuestionSetModel
            {
                AnswerDate = model.AnswerDate ?? defaultDate,
                QuestionsAndAnswers = currentCustomerContextualQuestions
            };
        }

        public class Request : MortgageLoanCustomerPagesRequestBase
        {
            [Required]
            public string LoanNr { get; set; }
        }

        public class Response
        {
            public class QuestionSetModel
            {
                public DateTime AnswerDate { get; set; }

                public List<QAModel> QuestionsAndAnswers { get; set; }

                public class QAModel
                {
                    public string QuestionGroup { get; set; }
                    public string QuestionCode { get; set; }
                    public string AnswerCode { get; set; }
                    public string QuestionText { get; set; }
                    public string AnswerText { get; set; }
                }
            }

            public List<QuestionSetModel> QuestionAndAnswerSets { get; set; }
        }

        public class KycQuestionsDocumentModel
        {
            public DateTime? AnswerDate { get; set; }

            public List<ItemModel> Items { get; set; }

            public class ItemModel
            {
                public int? ApplicantNr { get; set; }
                public string QuestionGroup { get; set; }
                public string QuestionCode { get; set; }
                public string AnswerCode { get; set; }
                public string QuestionText { get; set; }
                public string AnswerText { get; set; }
            }
        }
    }
}