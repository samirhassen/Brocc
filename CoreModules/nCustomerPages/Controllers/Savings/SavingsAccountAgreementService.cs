using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using nCustomerPages.Code;
using NTech;
using NTech.Banking.Shared.BankAccounts.Fi;
using static nCustomerPages.Controllers.Savings.SavingsStandardApplicationController;

namespace nCustomerPages.Controllers.Savings;

public class SavingsAccountAgreementService
{
    public static bool TryGetAgreementPdf(
        Dictionary<string, string> applicationItems,
        IClock clock,
        out byte[] pdfBytes, out string failedMessage)
    {
        var context = new Dictionary<string, object>();

        var savingsAccountNr = applicationItems[nameof(SavingsApplicationItemName.savingsAccountNr)];

        var civicRegNrRaw = applicationItems[nameof(SavingsApplicationItemName.customerCivicRegNr)];
        var civicRegNr = NEnv.BaseCivicRegNumberParser.Parse(civicRegNrRaw);

        var cc = new SystemUserCustomerClient();
        var applicationCustomerId = new Lazy<int>(() => cc.GetCustomerId(civicRegNr));

        var printFormattingCulture = CultureInfo.GetCultureInfo(NEnv.ClientCfg.Country.BaseFormattingCulture);
        if (!TryAssembleContactInfo(applicationCustomerId, cc, applicationItems, out var contactInfo,
                out failedMessage))
        {
            pdfBytes = null;
            return false;
        }

        context["contact"] = contactInfo;

        var sc = new SystemUserSavingsClient();
        if (!sc.TryGetCurrentInterestRateForStandardAccount(out var interestRatePercent))
        {
            pdfBytes = null;
            failedMessage = "There is no interest rate defined for standard accounts.";
            return false;
        }

        var iban = IBANFi.Parse(applicationItems[nameof(SavingsApplicationItemName.withdrawalIban)]);
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


    private static bool TryAssembleContactInfo(Lazy<int> customerId, SystemUserCustomerClient customerClient,
        Dictionary<string, string> itemsDict, out AgreementCustomerContactInfo contactInfo,
        out string failedMessage)
    {
        var missingRequiredProperties = new List<string>();

        var c = new AgreementCustomerContactInfo
        {
            civicRegNr = Req(itemsDict, nameof(SavingsApplicationItemName.customerCivicRegNr)),
            isNameSourceApplication =
                itemsDict.ContainsKey(nameof(SavingsApplicationItemName.customerFirstName)),
            isAddressSourceApplication =
                itemsDict.ContainsKey(nameof(SavingsApplicationItemName.customerAddressZipcode)),
            isContactInfoSourceApplication =
                itemsDict.ContainsKey(nameof(SavingsApplicationItemName.customerEmail))
        };

        IDictionary<string, string> customerCardItems = null;
        if (!c.isAddressSourceApplication.GetValueOrDefault() ||
            !c.isContactInfoSourceApplication.GetValueOrDefault() || !c.isNameSourceApplication.GetValueOrDefault())
        {
            customerCardItems = customerClient.GetCustomerCardItems(customerId.Value, "addressCity",
                "addressStreet", "addressZipcode", "addressCountry", "firstName", "lastName", "email", "phone");
        }

        c.fullName = !c.isNameSourceApplication.GetValueOrDefault()
            ? $"{Req(customerCardItems, "firstName")} {Opt(customerCardItems, "lastName")}"
            : $"{Req(itemsDict, nameof(SavingsApplicationItemName.customerFirstName))} {Opt(itemsDict, nameof(SavingsApplicationItemName.customerLastName))}";

        if (!c.isAddressSourceApplication.GetValueOrDefault())
        {
            c.streetAddress = Opt(customerCardItems, "addressStreet");
            c.areaAndZipcode =
                $"{Req(customerCardItems, "addressZipcode")} {Req(customerCardItems, "addressCity")}";
        }
        else
        {
            c.streetAddress = Opt(itemsDict, nameof(SavingsApplicationItemName.customerAddressStreet));
            c.areaAndZipcode =
                $"{Req(itemsDict, nameof(SavingsApplicationItemName.customerAddressZipcode))} {Req(itemsDict, nameof(SavingsApplicationItemName.customerAddressCity))}";
        }

        if (!c.isContactInfoSourceApplication.GetValueOrDefault())
        {
            c.phone = Opt(customerCardItems, "phone");
            c.email = Opt(customerCardItems, "email");
        }
        else
        {
            c.phone = Opt(itemsDict, nameof(SavingsApplicationItemName.customerPhone));
            c.email = Opt(itemsDict, nameof(SavingsApplicationItemName.customerEmail));
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

        string Req(IDictionary<string, string> d, string n)
        {
            if (d.TryGetValue(n, out var req))
            {
                return req;
            }

            missingRequiredProperties.Add(n);
            return "";
        }

        string Opt(IDictionary<string, string> d, string n) => d.TryGetValue(n, out var value) ? value : "";
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