using nCustomerPages.Code;
using NTech;
using NTech.Banking.BankAccounts.Fi;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static nCustomerPages.Controllers.Savings.SavingsStandardApplicationController;

namespace nCustomerPages.Controllers.Savings
{
    public class SavingsAccountAgreementService
    {
        public bool TryGetAgreementPdf(
            Dictionary<string, string> applicationItems,
            IClock clock,
            out byte[] pdfBytes, out string failedMessage)
        {
            var context = new Dictionary<string, object>();

            var savingsAccountNr = applicationItems[SavingsApplicationItemName.savingsAccountNr.ToString()];

            var civicRegNrRaw = applicationItems[SavingsApplicationItemName.customerCivicRegNr.ToString()];
            var civicRegNr = NEnv.BaseCivicRegNumberParser.Parse(civicRegNrRaw);

            var cc = new SystemUserCustomerClient();
            Lazy<int> applicationCustomerId = new Lazy<int>(() => cc.GetCustomerId(civicRegNr));

            var printFormattingCulture = CultureInfo.GetCultureInfo(NEnv.ClientCfg.Country.BaseFormattingCulture);
            AgreementCustomerContactInfo contactInfo;
            if (!TryAssembleContactInfo(applicationCustomerId, cc, applicationItems, out contactInfo, out failedMessage))
            {
                pdfBytes = null;
                return false;
            }
            context["contact"] = contactInfo;

            var sc = new SystemUserSavingsClient();
            decimal interestRatePercent;
            if (!sc.TryGetCurrentInterestRateForStandardAccount(out interestRatePercent))
            {
                pdfBytes = null;
                failedMessage = "There is no interest rate defined for standard accounts.";
                return false;
            }

            var iban = IBANFi.Parse(applicationItems[SavingsApplicationItemName.withdrawalIban.ToString()]);
            context["interestRate"] = (interestRatePercent / 100m).ToString("P");
            context["agreementDate"] = clock.Today.ToString("d", printFormattingCulture);
            context["savingsAccountNr"] = savingsAccountNr;
            context["iban"] = iban.NormalizedValue;
            context["ibanFormatted"] = new
            {
                ibanReadable = iban.GroupsOfFourValue,
                bankName = NEnv.IBANToBICTranslatorInstance.InferBankName(iban)
            };

            var dc = new SystemUserDocumentClient();
            pdfBytes = dc.PdfRenderDirect("savings-agreement", context).ToArray();
            failedMessage = null;
            return true;
        }


        private static bool TryAssembleContactInfo(Lazy<int> customerId, SystemUserCustomerClient customerClient, Dictionary<string, string> itemsDict, out AgreementCustomerContactInfo contactInfo, out string failedMessage)
        {
            var missingRequiredProperties = new List<string>();
            Func<IDictionary<string, string>, string, string> opt = (d, n) => d.ContainsKey(n) ? d[n] : "";
            Func<IDictionary<string, string>, string, string> req = (d, n) =>
            {
                if (d.ContainsKey(n))
                {
                    return d[n];
                }
                else
                {
                    missingRequiredProperties.Add(n);
                    return "";
                }
            };

            var c = new AgreementCustomerContactInfo
            {
                civicRegNr = req(itemsDict, SavingsApplicationItemName.customerCivicRegNr.ToString()),
                isNameSourceApplication = itemsDict.ContainsKey(SavingsApplicationItemName.customerFirstName.ToString()),
                isAddressSourceApplication = itemsDict.ContainsKey(SavingsApplicationItemName.customerAddressZipcode.ToString()),
                isContactInfoSourceApplication = itemsDict.ContainsKey(SavingsApplicationItemName.customerEmail.ToString())
            };

            IDictionary<string, string> customerCardItems = null;
            if (!c.isAddressSourceApplication.GetValueOrDefault() || !c.isContactInfoSourceApplication.GetValueOrDefault() || !c.isNameSourceApplication.GetValueOrDefault())
            {
                customerCardItems = customerClient.GetCustomerCardItems(customerId.Value, "addressCity", "addressStreet", "addressZipcode", "addressCountry", "firstName", "lastName", "email", "phone");
            }

            if (!c.isNameSourceApplication.GetValueOrDefault())
            {
                c.fullName = $"{req(customerCardItems, "firstName")} {opt(customerCardItems, "lastName")}";
            }
            else
            {
                c.fullName = $"{req(itemsDict, SavingsApplicationItemName.customerFirstName.ToString())} {opt(itemsDict, SavingsApplicationItemName.customerLastName.ToString())}";
            }

            if (!c.isAddressSourceApplication.GetValueOrDefault())
            {
                c.streetAddress = opt(customerCardItems, "addressStreet");
                c.areaAndZipcode = $"{req(customerCardItems, "addressZipcode")} {req(customerCardItems, "addressCity")}";
            }
            else
            {
                c.streetAddress = opt(itemsDict, SavingsApplicationItemName.customerAddressStreet.ToString());
                c.areaAndZipcode = $"{req(itemsDict, SavingsApplicationItemName.customerAddressZipcode.ToString())} {req(itemsDict, SavingsApplicationItemName.customerAddressCity.ToString())}";
            }

            if (!c.isContactInfoSourceApplication.GetValueOrDefault())
            {
                c.phone = opt(customerCardItems, "phone");
                c.email = opt(customerCardItems, "email");
            }
            else
            {
                c.phone = opt(itemsDict, SavingsApplicationItemName.customerPhone.ToString());
                c.email = opt(itemsDict, SavingsApplicationItemName.customerEmail.ToString());
            }

            if (missingRequiredProperties.Any())
            {
                contactInfo = null;
                failedMessage = $"Missing required customer properties: {string.Join(", ", missingRequiredProperties)}";
                return false;
            }

            contactInfo = c;
            failedMessage = null;
            return true;
        }

        private class AgreementCustomerContactInfo
        {
            public string civicRegNr { get; set; }
            public string fullName { get; set; }
            public string streetAddress { get; set; }
            public string areaAndZipcode { get; set; }
            public string phone { get; set; }
            public string email { get; set; }
            public bool? isNameSourceApplication { get; set; }
            public bool? isAddressSourceApplication { get; set; }
            public bool? isContactInfoSourceApplication { get; set; }
        }
    }
}