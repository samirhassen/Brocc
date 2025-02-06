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
    public enum LoginSessionStateCode
    {
        Broken,
        PendingCallback,
        Failed,
        PendingLogin,
        LoginSuccessful
    }
}
