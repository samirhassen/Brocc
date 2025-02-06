using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NTech.Banking.CivicRegNumbers;
using NTech.Shared.Randomization;

namespace NTechSignicat.Services
{
    public class LoginSession
    {
        public string ExpectedCivicRegNr { get; set; }
        public string ExpectedCivicRegNrCountryIsoCode { get; set; }
        public bool UsesTestReplacementCivicRegNr { get; set; } //Has no effect in production
        public string Id { get; set; }
        public string SessionStateCode { get; set; }
        public DateTime ExpirationDateUtc { get; set; }
        public DateTime StartDateUtc { get; set; }
        public DateTime? CallbackDateUtc { get; set; }
        public DateTime? LoginDateUtc { get; set; }
        public string SignicatReturnUrl { get; set; }
        public string SignicatInitialUrl { get; set; }
        public TokenSetModel Tokens { get; set; }
        public UserInfoModel UserInfo { get; set; }
        public string FailedCode { get; set; }
        public string FailedMessage { get; set; }
        public string RedirectAfterSuccessUrl { get; set; }
        public string RedirectAfterFailedUrl { get; set; }
        public string OneTimeInternalLoginToken { get; set; }
        public Dictionary<string, string> CustomData { get; set; }

        public LoginSessionStateCode GetState()
        {
            LoginSessionStateCode s;
            return Enum.TryParse(this.SessionStateCode, out s) ? s : LoginSessionStateCode.Broken;
        }

        public void SetState(LoginSessionStateCode stateCode)
        {
            this.SessionStateCode = stateCode.ToString();
        }
    }
}