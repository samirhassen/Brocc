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
    public interface ISignicatAuthenticationService
    {
        Task<LoginSession> StartLoginSession(
                    ICivicRegNumber expectedCivicRegNr,
                    List<SignicatLoginMethodCode> loginMethods,
                    Uri redirectAfterSuccessUrl,
                    Uri redirectAfterFailedUrl,
                    Dictionary<string, string> customData = null);
        Task<LoginSession> ReceiveSignicatSuccessCallback(string sessionId, string code);
        LoginSession ReceiveSignicatErrorCallback(string sessionId, string errorCode, string errorMessage);
        LoginSession CompleteInternalLogin(string sessionId, string loginToken);
        LoginSession GetLoginSession(string sessionId);
    }
}
