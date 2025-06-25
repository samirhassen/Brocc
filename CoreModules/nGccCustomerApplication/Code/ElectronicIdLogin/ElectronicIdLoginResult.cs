using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace nGccCustomerApplication.Code.ElectronicIdLogin
{
    public class ElectronicIdLoginResult
    {
        public bool IsSuccess { get; set; }
        public string CivicNr { get; set; }
        public string AdditionalData { get; set; }
        public string TargetName { get; set; }
    }
}