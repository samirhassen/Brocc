using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlxMigrator
{
    internal interface IMigrator
    {
        JObject CreateLoansFileCustomers(HashSet<int> customerIds);
    }
}
