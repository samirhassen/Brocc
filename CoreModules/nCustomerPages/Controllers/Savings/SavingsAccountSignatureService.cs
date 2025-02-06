using nCustomerPages.Code;
using nCustomerPages.Code.ElectronicIdSignature;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using static nCustomerPages.Controllers.Savings.SavingsBaseController;
using static nCustomerPages.Controllers.Savings.SavingsStandardApplicationController;

namespace nCustomerPages.Controllers.Savings
{
    public class SavingsAccountSignatureService
    {
        public (bool IsSuccess, MessageTypeCode? SuccessCode, string CreatedSavingsAccountNr) CreateSavingsAccountAfterAgreementSigned(Func<string, string> getRequestParameter,
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

            Func<IDictionary<string, string>, IList<Tuple<string, string>>> toDict = dd =>
                dd == null
                ? new List<Tuple<string, string>>()
                : new List<Tuple<string, string>>(dd.Select(x => Tuple.Create(x.Key, x.Value)));

            var applicationItems = d.applicationItems;

            var items = toDict(applicationItems);
            items.Add(Tuple.Create(SavingsApplicationItemName.signedAgreementDocumentArchiveKey.ToString(), signatureResult.SignedAgreementArchiveKey));
            items.Add(Tuple.Create(SavingsApplicationItemName.savingsAccountTypeCode.ToString(), "StandardAccount"));

            var sc = new SystemUserSavingsClient();
            var result = sc.CreateAccount(
                items,
                d?.externalApplicationVariables?.Select(x => Tuple.Create(x.Name, x.Value))?.ToList());

            var messageCode = result.Status == "Active" ? MessageTypeCode.newaccountgreeting : MessageTypeCode.accountbeingprocessed;
            return (IsSuccess: true, SuccessCode: messageCode, CreatedSavingsAccountNr: result.SavingsAccountNr);
        }
    }
}