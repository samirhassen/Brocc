using System;
using System.Collections.Generic;
using System.Linq;
using nCustomerPages.Code;
using nCustomerPages.Code.ElectronicIdSignature;
using Newtonsoft.Json;
using static nCustomerPages.Controllers.Savings.SavingsBaseController;
using static nCustomerPages.Controllers.Savings.SavingsStandardApplicationController;

namespace nCustomerPages.Controllers.Savings;

public class SavingsAccountSignatureService
{
    public static (bool IsSuccess, MessageTypeCode? SuccessCode, string CreatedSavingsAccountNr)
        CreateSavingsAccountAfterAgreementSigned(Func<string, string> getRequestParameter,
            Lazy<ISavingsAgreementElectronicIdSignatureProvider> electronicIdSignatureProvider)
    {
        var signatureResult = electronicIdSignatureProvider.Value.HandleSignatureCallback(getRequestParameter);

        if (!signatureResult.Success)
            return (IsSuccess: false, SuccessCode: null, CreatedSavingsAccountNr: null);

        var d = JsonConvert.DeserializeAnonymousType(signatureResult.PlainData, new
        {
            applicationItems = (Dictionary<string, string>)null,
            externalApplicationVariables = (List<AffiliateTrackingModel.ExternalApplicationVariable>)null
        });

        var applicationItems = d.applicationItems;

        var items = ToDict(applicationItems);
        items.Add(Tuple.Create(nameof(SavingsApplicationItemName.signedAgreementDocumentArchiveKey),
            signatureResult.SignedAgreementArchiveKey));
        //items.Add(Tuple.Create(nameof(SavingsApplicationItemName.savingsAccountTypeCode), "StandardAccount"));

        var sc = new SystemUserSavingsClient();
        var result = sc.CreateAccount(
            items,
            d.externalApplicationVariables?.Select(x => Tuple.Create(x.Name, x.Value)).ToList());

        var messageCode = result.Status == "Active"
            ? MessageTypeCode.newaccountgreeting
            : MessageTypeCode.accountbeingprocessed;
        return (IsSuccess: true, SuccessCode: messageCode, CreatedSavingsAccountNr: result.SavingsAccountNr);

        IList<Tuple<string, string>> ToDict(IDictionary<string, string> dd) =>
            dd == null
                ? new List<Tuple<string, string>>()
                : new List<Tuple<string, string>>(dd.Select(x => Tuple.Create(x.Key, x.Value)));
    }
}