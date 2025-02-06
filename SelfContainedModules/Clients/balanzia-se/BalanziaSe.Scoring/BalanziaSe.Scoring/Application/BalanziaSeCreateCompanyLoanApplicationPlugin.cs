using NTech.Banking.CivicRegNumbers;
using NTech.Banking.CivicRegNumbers.Se;
using NTech.Banking.OrganisationNumbers;
using NTech.Banking.PluginApis.CreateApplication;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring
{
    public class BalanziaSeCreateCompanyLoanApplicationPlugin : CreateCompanyLoanApplicationPlugin<BalanziaSeCreateCompanyLoanRequest>
    {
        public override Tuple<bool, CreateApplicationRequestModel, Tuple<string, string>> TryTranslateRequest(BalanziaSeCreateCompanyLoanRequest request)
        {
            if (!CivicRegNumberSe.TryParse(request.Applicant.CivicRegNr, out var applicantCivicRegNr))
                return Error("Invalid Applicant.CivicRegNr","invalidPropertyValue");

            if (!OrganisationNumberSe.TryParse(request.Customer.Orgnr, out var customerOrgnr))
                return Error("Invalid Customer.Orgnr", "invalidPropertyValue");

            
            var r = CreateNewApplication(1, request.ProviderName);
            r.HideFromManualListsUntilDate = request.SkipHideFromManualUserLists.GetValueOrDefault()
                    ? null
                    : new DateTimeOffset?(Context.Now.AddMinutes(5));
            var nr = r.ApplicationNr;

            var companyCustomerId = AddCompanyToCustomerModule(nr, customerOrgnr, request.Customer);
            var applicantCustomerId = AddApplicantToCustomerModule(nr, applicantCivicRegNr, request.Applicant);

            r.AddApplicationItem("companyCustomerId", companyCustomerId.ToString());
            r.AddApplicationItem("companyOrgnr", customerOrgnr.NormalizedValue, isEncrypted: true);
            r.AddApplicationItem("applicantCustomerId", applicantCustomerId.ToString());
            r.AddApplicationItem("applicantCivicRegNr", applicantCivicRegNr.NormalizedValue, isEncrypted: true);
            if (!string.IsNullOrWhiteSpace(request.Applicant.FirstName))
                r.AddApplicationItem("applicantFirstName", request.Applicant.FirstName);
            if (!string.IsNullOrWhiteSpace(request.Applicant.LastName))
                r.AddApplicationItem("applicantLastName", request.Applicant.LastName, isEncrypted: true);
            if (!string.IsNullOrWhiteSpace(request.Applicant.Email))
                r.AddApplicationItem("applicantEmail", request.Applicant.Email, isEncrypted: true);
            if (!string.IsNullOrWhiteSpace(request.Applicant.Phone))
                r.AddApplicationItem("applicantPhone", request.Applicant.Phone, isEncrypted: true);
            r.AddApplicationItem("amount", request.RequestedAmount.Value.ToString(CultureInfo.InvariantCulture));
            r.AddApplicationItem("repaymentTimeInMonths", request.RequestedRepaymentTimeInMonths.Value.ToString());
            if (!string.IsNullOrWhiteSpace(request.CustomerIpAddress))
                r.AddApplicationItem("customerIpAddress", request.CustomerIpAddress, isEncrypted: true);

            r.AddApplicationItem("workflowVersion", Context.WorkflowVersion.ToString());

            foreach (var p in (request.AdditionalApplicationProperties ?? new Dictionary<string, string>()))
            {
                r.AddApplicationItem(p.Key, p.Value);
            }

            r.SetComment("Application created", customerIpAddress: request.CustomerIpAddress);

            return Sucess(r);
        }

        private int AddApplicantToCustomerModule(string applicationNr, ICivicRegNumber civicRegNr, BalanziaSeCreateCompanyLoanRequest.ApplicantModel a)
        {
            var customerData = new Dictionary<string, string>();
            Action<string, string> add = (n, v) =>
            {
                if (!string.IsNullOrWhiteSpace(v))
                    customerData[n] = v;
            };

            add("firstName", a.FirstName);
            add("lastName", a.LastName);
            add("email", a.Email);
            add("phone", a.Phone);

            return Context.CreateOrUpdatePerson(civicRegNr, customerData, false, applicationNr, birthDate: a.BirthDate);
        }

        private int AddCompanyToCustomerModule(string applicationNr, IOrganisationNumber orgnr, BalanziaSeCreateCompanyLoanRequest.CompanyModel c)
        {
            var customerData = new Dictionary<string, string>();
            Action<string, string> add = (n, v) =>
            {
                if (!string.IsNullOrWhiteSpace(v))
                    customerData[n] = v;
            };

            add("companyName", c.CompanyName);
            add("email", c.Email);
            add("phone", c.Phone);

            return Context.CreateOrUpdateCompany(orgnr, customerData, false, applicationNr);
        }
    }
}
