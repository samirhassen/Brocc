using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PsdTwoPrototype.Models
{
    public class BankAccountModel
    {
        [JsonProperty("service")]
        public Service Service { get; set; }

        [JsonProperty("integration")]
        public Integration Integration { get; set; }
        [JsonProperty("basis")]
        public Basis Basis { get; set; }
    }

    public class Service
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("lenderName")]
        public string LenderName { get; set; }
        [JsonProperty("noRawData")]
        public bool NoRawData { get; set; }

    }

    public class Integration
    {
        [JsonProperty("accountDataCallback")]
        public string AccountDataCallback { get; set; }
        [JsonProperty("calculationResultCallback")]
        public string CalculationResultCallback { get; set; }
        [JsonProperty("endUserRedirectSuccess")]
        public string EndUserRedirectSuccess { get; set; }
        [JsonProperty("endUserRedirectError")]
        public string EndUserRedirectError { get; set; }
    }

    public class Basis
    {
        [JsonProperty("requestId")]
        public string RequestId { get; set; }
        [JsonProperty("companyName")]
        public string CompanyName { get; set; }
        [JsonProperty("locale")]
        public string Locale { get; set; }
        [JsonProperty("purposeCode")]
        public string PurposeCode { get; set; }
    }
}
