using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;

namespace nPreCredit.Code.Services
{
    public static class KycQuestionCopyService
    {
        public static void CopyUnsecuredLoanKycQuestions(List<KycQuestionCopyTask> tasks, ICustomerClient customerClient)
        {
            foreach (var task in tasks)
            {
                customerClient.CopyCustomerQuestionsSetIfNotExists(task.CustomerIds,
                    "UnsecuredLoanApplication", task.ApplicationNr,
                    "Credit_UnsecuredLoan", task.CreditNr,
                    task.ApplicationDate.Date);
            }
        }
    }
    public class KycQuestionCopyTask
    {
        public string ApplicationNr { get; set; }
        public string CreditNr { get; set; }
        public DateTime ApplicationDate { get; set; }
        public HashSet<int> CustomerIds { get; set; }
    }
}