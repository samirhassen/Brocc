using Newtonsoft.Json;
using NTech.Core.PreCredit.Shared.Services;
using System.Globalization;

namespace NTech.Core.Host.IntegrationTests.UlLegacy
{
    public static class UlLegacyCreditApplicationRequestGenerator
    {
        public static LegacyUnsecuredLoanApplicationRequest CreateRequest(string civicRegNr, string email, string phone, 
            int requestedRepaymentTimeInYears, int requestedAmount)
        {
            var request = JsonConvert.DeserializeObject<LegacyUnsecuredLoanApplicationRequest>(JsonRequestPattern.Replace("'", "\""))!;
            request.Items.Single(x => x.Name == "civicRegNr").Value = civicRegNr;
            request.Items.Single(x => x.Name == "email").Value = email;
            request.Items.Single(x => x.Name == "phone").Value = phone;

            request.Items.Single(x => x.Name == "amount").Value = requestedAmount.ToString(CultureInfo.InvariantCulture);
            request.Items.Single(x => x.Name == "repaymentTimeInYears").Value = requestedRepaymentTimeInYears.ToString(CultureInfo.InvariantCulture);

            return request;
        }

        private const string JsonRequestPattern =
@"{
  'ProviderName': 'self',
  'RequestIpAddress': null,
  'NrOfApplicants': 1,
  'Items': [
    {
      'Group': 'application',
      'Name': 'amount',
      'Value': '8000'
    },
    {
      'Group': 'application',
      'Name': 'repaymentTimeInYears',
      'Value': '7'
    },
    {
      'Group': 'application',
      'Name': 'loansToSettleAmount',
      'Value': '0'
    },
    {
      'Group': 'applicant1',
      'Name': 'creditReportConsent',
      'Value': 'True'
    },
    {
      'Group': 'applicant1',
      'Name': 'customerConsent',
      'Value': 'True'
    },
    {
      'Group': 'applicant1',
      'Name': 'educationText',
      'Value': 'Yrkesskola'
    },
    {
      'Group': 'applicant1',
      'Name': 'housingText',
      'Value': 'Hyresbostad'
    },
    {
      'Group': 'applicant1',
      'Name': 'employmentText',
      'Value': 'Fast anställd'
    },
    {
      'Group': 'applicant1',
      'Name': 'marriageText',
      'Value': 'Gift'
    },
    {
      'Group': 'applicant1',
      'Name': 'civicRegNr',
      'Value': '011082-882A'
    },
    {
      'Group': 'applicant1',
      'Name': 'email',
      'Value': 'ErkkiUtrio94254@superrito.com'
    },
    {
      'Group': 'applicant1',
      'Name': 'phone',
      'Value': '071 494 4408'
    },
    {
      'Group': 'applicant1',
      'Name': 'education',
      'Value': 'education_yrkesskola'
    },
    {
      'Group': 'applicant1',
      'Name': 'housing',
      'Value': 'housing_hyresbostad'
    },
    {
      'Group': 'applicant1',
      'Name': 'housingCostPerMonthAmount',
      'Value': '250'
    },
    {
      'Group': 'applicant1',
      'Name': 'employment',
      'Value': 'employment_fastanstalld'
    },
    {
      'Group': 'applicant1',
      'Name': 'employedSinceMonth',
      'Value': '2005-08'
    },
    {
      'Group': 'applicant1',
      'Name': 'employer',
      'Value': 'ica'
    },
    {
      'Group': 'applicant1',
      'Name': 'employerPhone',
      'Value': '46546546'
    },
    {
      'Group': 'applicant1',
      'Name': 'incomePerMonthAmount',
      'Value': '5000'
    },
    {
      'Group': 'applicant1',
      'Name': 'marriage',
      'Value': 'marriage_gift'
    },
    {
      'Group': 'applicant1',
      'Name': 'nrOfChildren',
      'Value': '0'
    },
    {
      'Group': 'applicant1',
      'Name': 'mortgageLoanAmount',
      'Value': '10000'
    },
    {
      'Group': 'applicant1',
      'Name': 'carOrBoatLoanAmount',
      'Value': '0'
    },
    {
      'Group': 'applicant1',
      'Name': 'studentLoanAmount',
      'Value': '0'
    },
    {
      'Group': 'applicant1',
      'Name': 'otherLoanAmount',
      'Value': '0'
    },
    {
      'Group': 'applicant1',
      'Name': 'creditCardAmount',
      'Value': '0'
    },
    {
      'Group': 'applicant1',
      'Name': 'civicRegNrCountry',
      'Value': 'FI'
    },
    {
      'Group': 'applicant1',
      'Name': 'approvedSat',
      'Value': 'true'
    }
  ]
}";
    }
}
