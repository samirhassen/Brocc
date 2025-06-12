using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.BankAccounts.Se;
using NTech.Services.Infrastructure;
using System.Web.Mvc;
using NTech.Banking.Shared.BankAccounts.Fi;


namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api")]
    public class ApiBankAccountNrController : NController
    {
        [Route("bankaccount/validate-nr")]
        [HttpPost]
        public ActionResult ValidateBankAccountNr(string bankAccountNr, string bankAccountNrType)
        {
            var p = new BankAccountNumberParser(NEnv.ClientCfg.Country.BaseCountry);
            var isValid = p.TryParseFromStringWithDefaults(bankAccountNr, bankAccountNrType, out var b);

            string type = null;
            string bankName = null;
            string clearingNr = null;
            string accountNr = null;
            string normalizedNr = null;
            string displayNr = null;
            string bic = null;

            if (isValid)
            {
                displayNr = b.FormatFor("display");
                if (b.AccountType == BankAccountNumberTypeCode.BankAccountSe)
                {
                    var bse = (BankAccountNumberSe)b;
                    type = "bankaccount";
                    bankName = bse.BankName;
                    clearingNr = bse.ClearingNr;
                    accountNr = bse.AccountNr;
                    normalizedNr = bse.PaymentFileFormattedNr;
                }
                else if (b.AccountType == BankAccountNumberTypeCode.IBANFi)
                {
                    var bfi = (IBANFi)b;
                    type = "iban";
                    bankName = NEnv.IBANToBICTranslatorInstance.InferBankName(bfi);
                    bic = NEnv.IBANToBICTranslatorInstance.InferBic(bfi);
                    normalizedNr = bfi.NormalizedValue;
                }
                else
                {
                    type = b.AccountType.ToString();
                    normalizedNr = b.FormatFor(null);
                }
            }

            return Json(new
            {
                rawNr = bankAccountNr,
                isValid,
                validAccount = isValid ? new
                {
                    type = type,
                    bankName = bankName,
                    clearingNr = clearingNr,
                    accountNr = accountNr,
                    normalizedNr = normalizedNr,
                    bic = bic,
                    displayNr = b.FormatFor("display"),
                    bankAccountNrType = b.AccountType.ToString()
                } : null
            });
        }
    }
}