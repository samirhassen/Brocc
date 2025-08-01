﻿using NTech.Core.Module.Shared.Database;
using System;

namespace nCredit
{
    public class DailyKycScreenHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public DateTime TransactionDate { get; set; }
        public int NrOfCustomersScreened { get; set; }
        public int NrOfCustomersConflicted { get; set; }
        public string ResultModel { get; set; }
    }
}