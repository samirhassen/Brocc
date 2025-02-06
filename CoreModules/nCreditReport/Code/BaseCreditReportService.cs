using nCreditReport.Models;
using System;
using System.Collections;
using System.Collections.Generic;

namespace nCreditReport.Code
{
    public abstract class BaseCreditReportService
    {
        protected string providerName;

        public string ProviderName
        {
            get
            {
                return providerName;
            }
        }

        public abstract string ForCountry { get; }

        public abstract bool IsCompanyProvider { get; }

        public BaseCreditReportService(string providerName)
        {
            this.providerName = providerName;
        }

        public virtual List<DictionaryEntry> FetchTabledValues(CreditReportRepository.FetchResult creditReport)
        {
            throw new NotImplementedException("Not supported for this provider yet. ");
        }

        public virtual bool CanFetchTabledValues() => false;

        public class Result
        {
            public bool IsError { get; set; }
            public bool IsInvalidCredentialsError { get; set; }
            public string ErrorMessage { get; set; }
            public SaveCreditReportRequest CreditReport { get; set; }
            public bool IsTimeoutError { get; set; }
        }
    }

    public class CreditReportField
    {
        public string Title { get; set; }
        public string Field { get; set; }
    }

    public class CreditReportRequestData
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string InformationMetadata { get; set; }
        public Dictionary<string, string> AdditionalParameters { get; set; }
        public string ReasonType { get; set; }
        public string ReasonData { get; set; }
    }
}