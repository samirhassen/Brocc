using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace NTech.Core.Credit.Shared.Models
{
    public class FromDateToDateReportRequest
    {
        [Required]
        public DateTime? FromDate { get; set; }

        [Required]
        public DateTime? ToDate { get; set; }
    }
}
