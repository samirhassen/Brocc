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
    public class TokenSetModel
    {
        public string AccessToken { get; set; }
        public string IdToken { get; set; }
        public DateTime? ExpiresDateUtc { get; set; }
        public ISet<string> Scopes { get; set; }
    }
}
