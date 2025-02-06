using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code
{
    public class StandardFraudControlEngine
    {

        private readonly ICustomerClient customerClient;
        private readonly IPreCreditContext context;

        public StandardFraudControlEngine(ICustomerClient customerClient, IPreCreditContext context)
        {
            this.customerClient = customerClient;
            this.context = context;
        }

        public RunFraudControlsResult RunFraudChecks(string applicationNr, List<string> fraudChecks)
        {
            var result = new RunFraudControlsResult { FraudControls = new List<RunFraudControlsResult.Control>() };

            var baseQuery = context.ComplexApplicationListItems.Where(x => x.ApplicationNr == applicationNr);

            var currentApplicationCustomerIds = baseQuery.Where(x => x.ListName == "Applicant" && x.ItemName == "customerId")
                .Select(x => x.ItemValue).ToList().Select(x => Convert.ToInt32(x)).ToList();

            List<int> FilterOutCurrentCustomerIds(List<int> matchingCustomerIds)
            {
                return matchingCustomerIds.Where(x => !currentApplicationCustomerIds.Contains(x)).ToList();
            }


            foreach (var check in fraudChecks)
            {
                switch (check)
                {
                    case "SameAddressCheck":
                        var matchingCustomerIds = new List<int>();
                        foreach (var customerId in currentApplicationCustomerIds)
                        {
                            var customerResult = customerClient.GetCustomerIdsWithSameAdress(customerId, true);
                            matchingCustomerIds.AddRange(FilterOutCurrentCustomerIds(customerResult));
                        }

                        var matchingAddressApplicationNrs = GetApplicationNrsFromCustomerIds(matchingCustomerIds, applicationNr);
                        result.FraudControls.Add(new RunFraudControlsResult.Control("SameAddressCheck", matchingAddressApplicationNrs, FraudCheckStatusCode.Initial, matchingAddressApplicationNrs.Any()));

                        break;
                    case "SameEmailCheck":
                        var customerEmails =
                            customerClient.BulkFetchPropertiesByCustomerIdsD(currentApplicationCustomerIds.ToHashSet(), new[] { "email" });

                        var sameEmailResult = new List<int>();
                        var emails = currentApplicationCustomerIds.Select(c => customerEmails.Opt(c).Opt("email")).ToList();
                        foreach (var email in emails)
                        {
                            var customerResult = customerClient.GetCustomerIdsWithSameData("email", email);
                            sameEmailResult.AddRange(FilterOutCurrentCustomerIds(customerResult));
                        }
                        var matchingEmailApplicationNrs = GetApplicationNrsFromCustomerIds(sameEmailResult, applicationNr);
                        result.FraudControls.Add(new RunFraudControlsResult.Control("SameEmailCheck", matchingEmailApplicationNrs, FraudCheckStatusCode.Initial, matchingEmailApplicationNrs.Any()));

                        break;
                    case "SameAccountNrCheck":
                        var currentApplicationBankAccount = baseQuery
                            .SingleOrDefault(x => x.ListName == "Application"
                                                  && x.ItemName == "paidToCustomerBankAccountNr")
                            ?.ItemValue;

                        var applicationsWithSameAccount = context
                            .ComplexApplicationListItems
                            .Where(x => x.ApplicationNr != applicationNr &&
                                        x.ListName == "Application"
                                        && x.ItemName == "paidToCustomerBankAccountNr"
                                        && x.ItemValue == currentApplicationBankAccount)
                            .Select(x => x.ApplicationNr).ToList();

                        result.FraudControls.Add(new RunFraudControlsResult.Control("SameAccountNrCheck", applicationsWithSameAccount, FraudCheckStatusCode.Initial, applicationsWithSameAccount.Any()));

                        break;
                    default:
                        throw new NotImplementedException();
                }
            }


            return result;
        }

        public RunFraudControlsResult GetFraudControls(string applicationNr)
        {
            var existingControl = context.FraudControls
                .SingleOrDefault(x => x.ApplicationNr == applicationNr && x.IsCurrentData);

            if (existingControl == null)
            {
                return null;
            }
            else
            {
                var result = new RunFraudControlsResult { FraudControls = new List<RunFraudControlsResult.Control>() };
                var items = existingControl.FraudControlItems;

                foreach (var item in items)
                {
                    result.FraudControls.Add(new RunFraudControlsResult.Control(item.Key,
                        item.Value?.Split(',').ToList(), item.Status, item.Value != null));
                }

                return result;
            }
        }

        public void SaveFraudControls(string applicationNr, List<RunFraudControlsResult.Control> controls, string informationMetadata, int userId, DateTimeOffset now)
        {
            var fraudControlItems = new List<FraudControlItem>();
            foreach (var control in controls)
            {
                fraudControlItems.Add(new FraudControlItem
                {
                    ChangedById = userId,
                    ChangedDate = now,
                    InformationMetaData = informationMetadata,
                    Key = control.CheckName,
                    Value = control.Values == null || !control.Values.Any() ? null : string.Join(",", control.Values),
                    Status = FraudControlItem.Initial
                });
            }

            var existingFraudControl = context.FraudControls
                .SingleOrDefault(x => x.ApplicationNr == applicationNr && x.IsCurrentData);

            if (existingFraudControl != null)
            {
                existingFraudControl.IsCurrentData = false;
            }

            var fraudControl = new FraudControl
            {
                InformationMetaData = informationMetadata,
                ApplicantNr = 0, // set 0 to indicate it's not connected to an applicant
                ChangedById = userId,
                ChangedDate = now,
                ApplicationNr = applicationNr,
                Status = FraudCheckStatusCode.Unresolved,
                FraudControlItems = fraudControlItems,
                ReplacesFraudControl = existingFraudControl,
                IsCurrentData = true
            };

            context.FraudControls.Add(fraudControl);
        }

        public void SetFraudControlItemStatusApproved(string fraudControlName, string applicationNr)
        {
            var existingFraudControl = context.FraudControls
                .Single(x => x.ApplicationNr == applicationNr && x.IsCurrentData);

            var item = existingFraudControl.FraudControlItems
                .Single(x => x.Key == fraudControlName);
            item.Status = FraudCheckStatusCode.Approved;
        }

        public void UpdateControlStatusBasedOnChildItems(string applicationNr)
        {
            var existingFraudControl = context.FraudControls
                .Single(x => x.ApplicationNr == applicationNr && x.IsCurrentData);

            var items = existingFraudControl.FraudControlItems;
            existingFraudControl.Status =
                items.All(x => x.Status == FraudCheckStatusCode.Approved)
                    ? FraudCheckStatusCode.Approved
                    : FraudCheckStatusCode.Unresolved;
        }

        public void SetFraudControlItemStatusInitial(string fraudControlName, string applicationNr)
        {
            var existingFraudControl = context.FraudControls
                .Single(x => x.ApplicationNr == applicationNr && x.IsCurrentData);

            var item = existingFraudControl.FraudControlItems
                .Single(x => x.Key == fraudControlName);
            item.Status = FraudCheckStatusCode.Initial;
        }

        /// <summary>
        /// All applicationnrs that is not the current. 
        /// </summary>
        /// <param name="customerIds"></param>
        /// <param name="currentApplicationNr"></param>
        /// <returns></returns>
        private List<string> GetApplicationNrsFromCustomerIds(IEnumerable<int> customerIds, string currentApplicationNr)
        {
            var customersAsStr = customerIds.Select(x => x.ToString());
            var query = context.ComplexApplicationListItems
                .Where(x => x.ApplicationNr != currentApplicationNr);

            query = query.Where(x => x.ListName == "Applicant" &&
                                     x.ItemName == "customerId" &&
                                     customersAsStr.Contains(x.ItemValue));

            return query.Select(x => x.ApplicationNr).ToList();
        }

    }

    public class RunFraudControlsResult
    {

        public List<Control> FraudControls { get; set; }

        public class Control
        {

            public Control(string name, List<string> values, string status, bool match = false)
            {
                CheckName = name;
                Values = (values != null && values.Any()) ? values : null;
                HasMatch = match;
                IsApproved = status == FraudCheckStatusCode.Approved;
            }


            public string CheckName { get; set; }
            public List<string> Values { get; set; }
            public bool HasMatch { get; set; }
            public bool IsApproved { get; set; }
        }
    }

}