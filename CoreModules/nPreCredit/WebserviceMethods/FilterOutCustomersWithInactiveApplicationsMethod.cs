using NTech;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods
{
    public class FilterOutCustomersWithInactiveApplicationsMethod : TypedWebserviceMethod<FilterOutCustomersWithInactiveApplicationsMethod.Request, FilterOutCustomersWithInactiveApplicationsMethod.Response>
    {
        public override string Path => "ApplicationCustomerList/Filter-Out-Customers-With-Inactive-Applications";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var clock = ClockFactory.SharedInstance;
            using (var context = new PreCreditContext())
            {
                //Convert custIds to List<string> for Application CustomerId Contains-comparer 
                var strCustomerIds = request.CustomerIds
                    .ConvertAll<string>(delegate (int i) { return i.ToString(); })
                    .Distinct()
                    .ToList();

                var applications = context
                     .CreditApplicationItems
                     .Where(x => x.Name == "customerId" && strCustomerIds.Contains(x.Value))
                     .Select(x => new ApplicationModel
                     {
                         CustomerId = x.Value,
                         IsActive = x.CreditApplication.IsActive,
                         FinalDecisionDate = x.CreditApplication.FinalDecisionDate,
                         RejectedDate = x.CreditApplication.RejectedDate,
                         CancelledDate = x.CreditApplication.CancelledDate,
                         ChangedDate = x.CreditApplication.ChangedDate
                     })
                     .ToList();

                foreach (var a in applications)
                {
                    if (DoesApplicationVetoArchiving(a, clock.Now, request.MinNumberOfDaysInactive))
                    {
                        strCustomerIds.Remove(a.CustomerId);
                    };
                }

                //Convert custIds back to List<int>
                var candidateCustomerIds = strCustomerIds
                    .ConvertAll<int>(delegate (string i) { return int.Parse(i); })
                    .ToHashSet();

                return new Response
                {
                    ArchivableCustomerIds = candidateCustomerIds.ToList()
                };
            }
        }

        public static bool DoesApplicationVetoArchiving(ApplicationModel a, DateTimeOffset now, int minNrOfDays)
        {
            var minNrOfDaysSinceTime = TimeSpan.FromDays(minNrOfDays);
            var d = now.Subtract(minNrOfDaysSinceTime);

            return (a.IsActive || a.FinalDecisionDate >= d
                                || a.RejectedDate >= d
                                || a.CancelledDate >= d
                                || a.ChangedDate >= d);
        }
        public class Response
        {
            public List<int> ArchivableCustomerIds { get; set; }
        }

        public class Request
        {
            [Required]
            public List<int> CustomerIds { get; set; }
            [Required]
            public int MinNumberOfDaysInactive { get; set; }
        }

        public class ApplicationModel
        {
            public string CustomerId { get; set; }
            public bool IsActive { get; set; }
            public DateTimeOffset? FinalDecisionDate { get; set; }
            public DateTimeOffset? RejectedDate { get; set; }
            public DateTimeOffset? CancelledDate { get; set; }
            public DateTimeOffset? ChangedDate { get; set; }
        }
    }
}