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
    public class UserInfoModel
    {
        public string CivicRegNr { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
