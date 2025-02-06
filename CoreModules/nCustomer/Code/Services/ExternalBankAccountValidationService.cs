using HtmlAgilityPack;
using NTech.Banking.BankAccounts;
using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace nCustomer.Code.Services
{
    public class ExternalBankAccountValidationService
    {
        private readonly IServiceClientSyncConverter asyncConverter;

        public ExternalBankAccountValidationService(IServiceClientSyncConverter asyncConverter)
        {
            this.asyncConverter = asyncConverter;
        }

        public Dictionary<string, string> GetExternalData(IBankAccountNumber nr)
        {
            if (nr.AccountType == BankAccountNumberTypeCode.BankGiroSe)
                return GetBankGiroData(nr);
            else if (nr.AccountType == BankAccountNumberTypeCode.PlusGiroSe)
                return GetPlusGiroData(nr);
            else
                return null;
        }

        private Dictionary<string, string> GetBankGiroData(IBankAccountNumber bankAccountNumber)
        {
            try
            {
                var bgNrs = LookupBankGiroNumber(bankAccountNumber.FormatFor(null));
                if (bgNrs.Count == 1)
                {
                    return new Dictionary<string, string>
                    {
                        ["bankGiroOwnerName"] = bgNrs[0].OwnerName,
                        ["bankGiroOwnerOrgnr"] = bgNrs[0].Orgnr
                    };
                }
                return null;
            }
            catch
            {
                //This is probably very flaky and will break eventually. Data is used as bonus when it works.
                return null;
            }
        }

        private Dictionary<string, string> GetPlusGiroData(IBankAccountNumber bankAccountNumber)
        {
            try
            {
                var description = asyncConverter.ToSync(() => LookupPlusgiroNumberDescription(bankAccountNumber.FormatFor(null)));
                if (description != null)
                {
                    return new Dictionary<string, string>
                    {
                        ["plusGiroDescription"] = description
                    };
                }
                return null;
            }
            catch
            {
                //This is probably very flaky and will break eventually. Data is used as bonus when it works.
                return null;
            }
        }

        private List<BankGiroNumber> LookupBankGiroNumber(string searchTerm)
        {
            var web = new HtmlWeb();
            web.BrowserTimeout = TimeSpan.FromSeconds(5);
            var doc = web.Load($"https://www.bankgirot.se/sok-bankgironummer/?bgnr={searchTerm}");
            var resultElements = doc
                .DocumentNode
                .Descendants()
                .Where(x => x.HasAttributes && x.GetAttributeValue("class", "").Contains("result-container")).ToList();

            var result = new List<BankGiroNumber>();
            foreach (var resultElement in resultElements)
            {
                var nr = new BankGiroNumber();
                nr.OwnerName = resultElement.Descendants().Where(x => x.Name == "h3" && x.GetClasses().Contains("title")).FirstOrDefault()?.InnerText?.Trim();
                if (!string.IsNullOrWhiteSpace(nr.OwnerName))
                    nr.OwnerName = WebUtility.HtmlDecode(nr.OwnerName);
                var ulElements = resultElement.Descendants().Where(x => x.Name == "ul" && x.GetClasses().Contains("meta")).ToList();

                foreach (var ulElement in ulElements)
                {
                    var liElements = ulElement.Descendants().Where(x => x.Name == "li").ToList();
                    var titleLi = liElements.FirstOrDefault();
                    var title = titleLi?.InnerText?.Trim();
                    if (title == "Adress")
                    {
                        nr.Address = new List<string>();
                        liElements.Skip(1).ToList().ForEach(x => nr.Address.Add(WebUtility.HtmlDecode(x.InnerText?.Trim() ?? "")));
                    }
                    else if (title == "Organisationsnummer")
                    {
                        nr.Orgnr = liElements.Skip(1).FirstOrDefault()?.InnerText?.Trim();
                    }
                    else if (title == "Bankgironummer")
                    {
                        nr.BankGiroNr = liElements.Skip(1).FirstOrDefault()?.InnerText?.Trim();

                    }
                    if (nr.BankGiroNr?.Length > 0)
                        result.Add(nr);
                }
            }
            return result;
        }

        private async Task<string> LookupPlusgiroNumberDescription(string searchTerm)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            var result = await client.PostAsync("https://kontoutdrag.plusgirot.se/ku/sokko002", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["SO_KTO"] = searchTerm
            }));

            var document = new HtmlDocument();
            document.Load(await result.Content.ReadAsStreamAsync());

            var searchResultTable = document
                .DocumentNode
                .Descendants()
                .Where(x => x.Name == "b" && x.InnerHtml?.Trim() == "Resultat")
                .Single()
                .ParentNode
                .ParentNode
                .ParentNode
                .ParentNode;

            var searchHit = searchResultTable
                .Descendants()
                .Where(x => x.Name == "tr")
                .Skip(2)
                .Single();

            var btSpans = searchHit.Descendants().Where(x => x.Name == "span" && x.HasClass("bt")).Skip(1).ToList();
            var resultText = string.Join(" ", btSpans.Select(x => x.InnerText.Replace("\r\n", " ")));
            resultText = Regex.Replace(resultText, @"\s+", " ");

            return string.IsNullOrWhiteSpace(resultText) ? null : resultText.Trim();
        }

        private class BankGiroNumber
        {
            public string OwnerName { get; set; }
            public List<string> Address { get; set; }
            public string Orgnr { get; set; }
            public string BankGiroNr { get; set; }
        }
    }
}