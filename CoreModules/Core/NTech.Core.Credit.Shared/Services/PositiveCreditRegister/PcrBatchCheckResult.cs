using System;
using System.Collections.Generic;
using System.Text;

namespace NTech.Core.Credit.Shared.Services.PositiveCreditRegister
{
    internal class PcrBatchCheckResult
    {
        public string BatchReference { get; set; }
        public bool IsFinishedSuccess { get; set; }
        /*
            - Rejected (FinishedFailed)
            - Being processed (Processed)
            - Accepted (FinishedSuccess)
            - Accepted in part (FinishedPartialSuccess)         
         */
        public string BatchStatusCode { get; set; }
    }
}
