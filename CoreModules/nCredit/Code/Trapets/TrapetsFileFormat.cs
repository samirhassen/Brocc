using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace nCredit.Code.Trapets
{
    public class TrapetsFileFormat
    {
        //public const string Suffix = "-KFI";
        //public const string AccountType = "Blancolån Finland"; //Från riskdokumentet
        //public const decimal BaseAccountRisk = 3m; //Från Riskdokument
        //public const string BaseCustomerType = "Private person";

        private T[] SkipNulls<T>(params T[] ts) where T : class
        {
            return ts.Where(x => x != null).ToArray();
        }

        private class TrapetsXmlModel
        {
            private Dictionary<string, List<XElement>> Items = new Dictionary<string, List<XElement>>();

            public class Item
            {
                public Item(XElement e, TrapetsXmlModel parent)
                {
                    this.Parent = parent;
                    this.Element = e;
                }

                private TrapetsXmlModel Parent { get; set; }
                private XElement Element { get; set; }

                public Item DecimalAttr(string name, decimal? value)
                {
                    if (value.HasValue)
                    {
                        //‘####.####’ with a precision of 16 and a scale of 4. “.” is decimal separator. No thousand separator or any other separator should be used.
                        Element.SetAttributeValue(name, Math.Round(value.Value, 4).ToString(CultureInfo.InvariantCulture));
                    }
                    return this;
                }

                public Item DateAttr(string name, DateTime? value)
                {
                    if (value.HasValue)
                    {
                        //“yyyy-MM-dd HH:mm:ss”, example: 2015-01-01 12:00:00 (time part may be omitted if not available, i.e. yyyy-MM-dd)
                        Element.SetAttributeValue(name, value.Value.ToString("yyyy-MM-dd"));
                    }
                    return this;
                }

                public Item DateTimeAttr(string name, DateTime? value)
                {
                    if (value.HasValue)
                    {
                        //“yyyy-MM-dd HH:mm:ss”, example: 2015-01-01 12:00:00 (time part may be omitted if not available, i.e. yyyy-MM-dd)
                        Element.SetAttributeValue(name, value.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                    return this;
                }

                public Item StringAttr(string name, string value)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        Element.SetAttributeValue(name, value.Trim());
                    }
                    return this;
                }

                public Item IntAttr(string name, int? value)
                {
                    if (value.HasValue)
                    {
                        Element.SetAttributeValue(name, value.Value.ToString(CultureInfo.InvariantCulture));
                    }
                    return this;
                }

                public Item BoolAttr(string name, bool? value)
                {
                    if (value.HasValue)
                    {
                        //0 | 1
                        Element.SetAttributeValue(name, value.Value ? "1" : "0");
                    }
                    return this;
                }

                public Item NewItem(string wrapperName, string name)
                {
                    return Parent.NewItem(wrapperName, name);
                }
            }

            public Item NewItem(string wrapperName, string name)
            {
                var e = new XElement(name);
                var i = new Item(e, this);
                if (!Items.ContainsKey(wrapperName))
                    Items[wrapperName] = new List<XElement>();
                Items[wrapperName].Add(e);
                return i;
            }

            public XDocument ToDocument()
            {
                var root = new XElement("transfer");
                foreach (var wrapperName in this.Items.Keys)
                {
                    var wrapperElement = new XElement(wrapperName);
                    root.Add(wrapperElement);
                    foreach (var e in this.Items[wrapperName])
                    {
                        wrapperElement.Add(e);
                    }
                }
                return new XDocument(root);
            }
        }

        private XDocument CreateFile(TrapetsDomainModel model, DateTime deliveryDate, TrapetsKycConfiguration config)
        {
            Func<DateTime, string> formatDatetime = a => a.ToString("yyyy-MM-dd");
            Func<decimal, string> formatDecimal = a => a.ToString(System.Globalization.CultureInfo.InvariantCulture);

            var xml = new TrapetsXmlModel();

            var d = new XElement("transfer");

            //TODO: Need the actual fileformat
            foreach (var x in model.Accounts)
            {
                DateTime? deregistrationDate = null;

                if (x.Status == CreditStatus.Settled.ToString() || x.Status == CreditStatus.WrittenOff.ToString())
                {
                    deregistrationDate = x.StatusDate;
                }

                xml
                    .NewItem("accounts", "account")
                    .DateTimeAttr("Timestamp", x.ChangedDate.DateTime)
                    .StringAttr("AccountID", $"{x.CreditNr}{config.IdSuffix}")
                    .StringAttr("AccountType", config.BaseAccountType)
                    .DateAttr("RegistrationDate", x.StartDate.DateTime)
                    .DateAttr("DeregistrationDate", deregistrationDate)
                    .StringAttr("BaseCurrency", NEnv.ClientCfg.Country.BaseCurrency)
                    .DecimalAttr("ExternalRisk", decimal.Parse(config.BaseAccountRisk, CultureInfo.InvariantCulture));
            }

            foreach (var x in model.Assets)
            {
                xml
                    .NewItem("assets", "asset")
                    .DateAttr("Timestamp", x.TransactionDate)
                    .StringAttr("AccountID", $"{x.CreditNr}{config.IdSuffix}")
                    .DecimalAttr("Balance", x.CapitalBalance)
                    .DecimalAttr("AssetValue", x.CapitalBalance);
            }

            var civicNrParser = NEnv.BaseCivicRegNumberParser;
            foreach (var x in model.Customers)
            {
                var civicNr = civicNrParser.Parse(x.Item.CivicRegNr);

                string taxCountry;
                var taxCountriesRaw = x.Item.Taxcountries;
                var taxCountries = taxCountriesRaw == null ? null : JsonConvert.DeserializeAnonymousType(taxCountriesRaw, new[] { new { countryIsoCode = "", taxNumber = "" } });
                if (taxCountries != null && taxCountries.Length > 0 && !taxCountries.Any(y => y.countryIsoCode == NEnv.ClientCfg.Country.BaseCountry))
                    taxCountry = taxCountries.Select(y => y.countryIsoCode).First();
                else
                    taxCountry = NEnv.ClientCfg.Country.BaseCountry;

                xml
                    .NewItem("customers", "customer")
                    .DateTimeAttr("Timestamp", x.Item.ChangeDate)
                    .StringAttr("CustomerID", $"{civicNr.NormalizedValue}{config.IdSuffix}")
                    .StringAttr("RegistrationID", civicNr.NormalizedValue)
                    .StringAttr("CustomerTypeID", config.BaseCustomerType)
                    .DateAttr("DateOfBirth", civicNr.BirthDate)
                    .StringAttr("Address1", x.Item.AddressStreet)
                    .StringAttr("CitizenCountryCode", NEnv.ClientCfg.Country.BaseCountry)
                    .StringAttr("City", x.Item.AddressCity)
                    .DateAttr("RegistrationDate", x.Item.CreationDate)
                    .StringAttr("AddressCountryCode", NEnv.ClientCfg.Country.BaseCountry)
                    .StringAttr("RegistrationIDCountryCode", NEnv.ClientCfg.Country.BaseCountry)
                    .StringAttr("Email", x.Item.Email)
                    .StringAttr("FirstName", x.Item.FirstName)
                    .StringAttr("LastName", x.Item.LastName)
                    .StringAttr("Phone", x.Item.Phone)
                    .StringAttr("TaxCountryCode", taxCountry)
                    .IntAttr("ExternalPEP", (x.Item.ExternalPep == "true" || x.Item.Ispep == "true") ? 1 : 0);

                foreach (var y in x.Credits)
                {
                    xml
                        .NewItem("accountowners", "accountowner")
                        .DateAttr("Timestamp", deliveryDate)
                        .StringAttr("AccountID", $"{y.CreditNr}{config.IdSuffix}")
                        .StringAttr("CustomerID", $"{civicNr.NormalizedValue}{config.IdSuffix}")
                        .BoolAttr("IsMainAccountOwner", y.ApplicantNr == 1)
                        .DecimalAttr("AccountShare", 1m / ((decimal)y.NrOfApplicants));
                    //We could potentially use the credits registration and deregistration date here though it seems kind of meaningless
                }
            }

            foreach (var q in model.KycQuestionsAndAnswers)
            {
                var civicNr = civicNrParser.Parse(q.Item.CivicRegNr);
                foreach (var a in q.Item.QuestionAnswerCodes)
                {
                    xml
                        .NewItem("kycanswers", "kycanswer")
                        .DateAttr("Timestamp", q.Item.AnswerDate)
                        .StringAttr("CustomerID", $"{civicNr.NormalizedValue}{config.IdSuffix}")
                        .StringAttr("AccountID", $"{q.Item.CreditNr}{config.IdSuffix}")
                        .StringAttr("QuestionID", a.Q)
                        .StringAttr("Answer", a.A);
                }
            }

            foreach (var t in model.Transactions)
            {
                xml
                    .NewItem("transactions", "transaction")
                    .DateAttr("Timestamp", t.TransactionDate)
                    .StringAttr("TransID", t.Id.ToString())
                    .StringAttr("AccountID", $"{t.CreditNr}{config.IdSuffix}")
                    .StringAttr("TransactionTypeID", t.IsConnectedToIncomingPayment ? "Inbetalning via bankkonto" : (t.IsConnectedToOutgoingPayment ? "Utbetalning inhemsk bank" : "Unknown"))
                    .StringAttr("CurrencyCode", NEnv.ClientCfg.Country.BaseCurrency)
                    .DecimalAttr("Amount", -t.Amount)
                    .DecimalAttr("AmountBaseCurrency", -t.Amount);
            }

            return xml.ToDocument();
        }

        public void WithTemporaryExportFile(TrapetsDomainModel model, DateTime deliveryDate, Action<string> withFile, TrapetsKycConfiguration config)
        {
            var document = CreateFile(model, deliveryDate, config);
            var tmp = Path.Combine(Path.GetTempPath(), $"TrapetsAmlExport_{Guid.NewGuid().ToString()}.xml");
            document.Save(tmp);
            try
            {
                withFile(tmp);
            }
            finally
            {
                try { System.IO.File.Delete(tmp); } catch { /*ignored*/ }
            }
        }
    }
}