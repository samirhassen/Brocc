using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.BankAccounts.Se;
using System.Web.Mvc;
using NTech.Banking.Shared.BankAccounts.Fi;

namespace nTest.Controllers
{
    public class BankAccountsController : NController
    {
        [Route("Api/BankAccounts/Parse")]
        public ActionResult Parse(string nr)
        {
            var country = NEnv.ClientCfg.Country.BaseCountry;
            if (country == "SE")
            {
                BankAccountNumberSe b;
                string errorMessage;
                if (BankAccountNumberSe.TryParse(nr, out b, out errorMessage))
                {
                    return Json2(new
                    {
                        isValid = true,
                        nr = b.PaymentFileFormattedNr,
                        bankName = b.BankName,
                        seSpecific = new
                        {
                            clearingNr = b.ClearingNr,
                            accountNr = b.AccountNr
                        }
                    });
                }
                else
                {
                    return Json2(new
                    {
                        isValid = false,
                        errorMessage = errorMessage
                    });
                }
            }
            else if (country == "FI")
            {
                IBANFi b;
                if (IBANFi.TryParse(nr, out b))
                {
                    return Json2(new
                    {
                        isValid = true,
                        nr = b.NormalizedValue,
                        bankName = NEnv.IBANToBICTranslatorInstance.InferBankName(b),
                        fiSpecific = new
                        {
                            bic = NEnv.IBANToBICTranslatorInstance.InferBic(b),
                            groupedNr = b.GroupsOfFourValue
                        }
                    });
                }
                else
                {
                    return Json2(new
                    {
                        isValid = false,
                        errorMessage = "Invalid finnish iban"
                    });
                }
            }
            else
            {
                return Json2(new
                {
                    isValid = false,
                    errorMessge = $"Not supported for client country {country}"
                });
            }
        }

        [Route("Ui/BankAccounts")]
        public ActionResult Index()
        {
            ViewBag.JsonInitialData = this.EncodeInitialData(new
            {
            });
            return View();
        }
    }
}