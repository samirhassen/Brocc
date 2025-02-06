using nCustomer.DbModel;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.OrganisationNumbers;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Customer.Shared.Services.Aml.Cm1;
using NTech.Core.Module;
using NTech.Core.Module.Shared;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace nCustomer.Code.Services.Aml.Cm1
{
    public class Cm1FileFormat
    {
        private readonly Lazy<NTechSimpleSettingsCore> cm1Settings;
        private readonly CustomerContextFactory contextFactory;
        private readonly IClientConfigurationCore clientConfiguration;

        public Cm1FileFormat(Lazy<NTechSimpleSettingsCore> cm1Settings, CustomerContextFactory contextFactory, 
            IClientConfigurationCore clientConfiguration)
        {
            this.cm1Settings = cm1Settings;
            this.contextFactory = contextFactory;
            this.clientConfiguration = clientConfiguration;
        }

        private class Cm1XmlModel
        {
            private Dictionary<string, List<XElement>> Items = new Dictionary<string, List<XElement>>();

            public class Item
            {
                public Item(XElement e, Cm1XmlModel parent)
                {
                    Parent = parent;
                    Element = e;
                }

                private Cm1XmlModel Parent { get; set; }
                private XElement Element { get; set; }
                private XNamespace ns = @"http://Modul1/CM1/TransactionImport/GCCCapital/v1";

                public Item DecimalAttr(string name, decimal? value)
                {
                    if (value.HasValue)
                    {
                        //‘####.####’ with a precision of 16 and a scale of 4. “.” is decimal separator. No thousand separator or any other separator should be used.
                        Element.SetElementValue(ns + name, Math.Round(value.Value, 4).ToString(CultureInfo.InvariantCulture));
                    }
                    return this;
                }

                public Item DateAttr(string name, DateTime? value)
                {
                    if (value.HasValue)
                    {
                        //“yyyy-MM-dd HH:mm:ss”, example: 2015-01-01 12:00:00 (time part may be omitted if not available, i.e. yyyy-MM-dd)
                        Element.SetElementValue(ns + name, value.Value.ToString("yyyy-MM-dd"));
                    }
                    return this;
                }

                public Item DateTimeAttr(string name, DateTime? value)
                {
                    if (value.HasValue)
                    {
                        //“yyyy-MM-dd HH:mm:ss”, example: 2015-01-01 12:00:00 (time part may be omitted if not available, i.e. yyyy-MM-dd)
                        Element.SetElementValue(ns + name, value.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                    return this;
                }

                public Item StringAttr(string name, string value)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        Element.SetElementValue(ns + name, value.Trim());
                    }
                    return this;
                }

                public Item IntAttr(string name, int? value)
                {
                    if (value.HasValue)
                    {
                        Element.SetElementValue(ns + name, value.Value.ToString(CultureInfo.InvariantCulture));
                    }
                    return this;
                }

                public Item BoolAttr(string name, bool? value)
                {
                    if (value.HasValue)
                    {
                        Element.SetElementValue(ns + name, value.Value ? "1" : "0");
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
                XNamespace ns = @"http://Modul1/CM1/TransactionImport/GCCCapital/v1";

                var e = new XElement(ns + name);
                var i = new Item(e, this);
                if (!Items.ContainsKey(wrapperName))
                    Items[wrapperName] = new List<XElement>();
                Items[wrapperName].Add(e);
                return i;
            }

            public XDocument ToDocument(NTechSimpleSettingsCore cm1Settings)
            {
                XNamespace ns = ElementNameSpace;
                XNamespace xsi = @"http://www.w3.org/2001/XMLSchema-instance";
                XNamespace xmlnsxsd = @"http://www.w3.org/2001/XMLSchema";

                var root = new XElement(ns + "Imports",
                    new XAttribute(XNamespace.Xmlns + "xsi", xsi.NamespaceName),
                    new XAttribute(XNamespace.Xmlns + "xsd", xmlnsxsd.NamespaceName),
                    new XElement(ns + "Import", new XElement(ns + "DestinationGuid", cm1Settings.Req("DestinationGuid")),
                    new XElement(ns + "Transactions", Items.Values))
                    );

                return new XDocument(root);
            }

            public static XNamespace ElementNameSpace = @"http://Modul1/CM1/TransactionImport/GCCCapital/v1";
        }

        private class CustomerDataHelper
        {
            private IDictionary<int, IList<CustomerPropertyModel>> result;
            private CustomerDataHelper(IDictionary<int, IList<CustomerPropertyModel>> result)
            {
                this.result = result;
            }

            public static CustomerDataHelper Lookup(ISet<int> customerIds, CustomerContextFactory contextFactory, Func<ICustomerContextExtended, CustomerWriteRepository> createRepo, params CustomerProperty.Codes[] codes)
            {
                using (var context = contextFactory.CreateContext())
                {
                    var customerRepo = createRepo(context);
                    var result = customerRepo.BulkFetch(customerIds, propertyNames: new HashSet<string>(codes.Select(x => x.ToString())));
                    return new CustomerDataHelper(result);
                }
            }

            public string Opt(int customerId, CustomerProperty.Codes propertyCode)
            {
                return result[customerId].FirstOrDefault(y => y.Name == propertyCode.ToString())?.Value;
            }

            public (int CustomerId, Func<CustomerProperty.Codes, string> Opt) OptForCustomer(int customerId)
            {
                return (CustomerId: customerId, Opt: x => Opt(customerId, x));
            }
        }

        private class CustomerModelHelper
        {
            private readonly CompleteCmlExportFileRequest.CustomerModel customer;
            private readonly CustomerDataHelper companyData;
            private readonly IClientConfigurationCore clientConfiguration;

            public CustomerModelHelper(CompleteCmlExportFileRequest.CustomerModel customer, CustomerDataHelper companyData, 
                IClientConfigurationCore clientConfiguration)
            {
                this.customer = customer;
                this.companyData = companyData;
                this.clientConfiguration = clientConfiguration;
            }

            public int CustomerId => customer.CustomerId;

            public string CustomerOpt(CustomerProperty.Codes code)
            {
                return companyData?.Opt(customer.CustomerId, code);
            }

            public string GetTwoLetterIsoAddressCountry() =>(
                    IsoCountry.FromTwoLetterIsoCode(CustomerOpt(CustomerProperty.Codes.addressCountry), returnNullWhenNotExists: true)
                    ?? IsoCountry.FromTwoLetterIsoCode(clientConfiguration.Country.BaseCountry)
                ).Iso2Name;
        }

        private XDocument CreateCustomerFile(CompleteCmlExportFileRequest model, Func<ICustomerContextExtended, CustomerWriteRepository> createRepo)
        {
            var f = cm1Settings.Value;

            const string FileHeaderPattern = "<?xml version=\"1.0\" encoding=\"utf-8\"?><Imports xmlns=\"http://Modul1/CM1/CustomerImport/GCCCapital/v1\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><Import><DestinationGuid>{DestinationGuid}</DestinationGuid><Settings><InsertFallbackToUpdateWhenExisting>true</InsertFallbackToUpdateWhenExisting><UseISO3166A2>true</UseISO3166A2></Settings>";

            var customerIds = model.Customers.Select(x => x.CustomerId).ToHashSetShared();
            var result = CustomerDataHelper.Lookup(
                new HashSet<int>(customerIds.ToArray()),
                contextFactory,
                createRepo,
                CustomerProperty.Codes.firstName,
                CustomerProperty.Codes.lastName,
                CustomerProperty.Codes.civicRegNr,
                CustomerProperty.Codes.isCompany,
                CustomerProperty.Codes.orgnr,
                CustomerProperty.Codes.companyName,
                CustomerProperty.Codes.addressStreet,
                CustomerProperty.Codes.addressCity,
                CustomerProperty.Codes.addressZipcode,
                CustomerProperty.Codes.addressCountry,
                CustomerProperty.Codes.email,
                CustomerProperty.Codes.phone);

            var xmldata = "";

            xmldata += FileHeaderPattern
                .Replace("{DestinationGuid}", f.Req("DestinationGuid"));

            if (model.Customers.Where(x => x.TransferedStatus == "Inserts").Count() > 0)
                xmldata += "<Inserts>";

            Func<CompleteCmlExportFileRequest.CustomerModel, string, string> appendXmlOnInsertUpdateOrDelete = (x, d) =>
            {
                var h = new CustomerModelHelper(x, result, clientConfiguration);
                if (result.Opt(x.CustomerId, CustomerProperty.Codes.isCompany) == "true")
                    return AppendCompanyLoanCustomerData(h, d);
                else
                    return AppendNonCompanyLoanCustomerData(h, d, model.ExportType.Value);
            };

            foreach (var x in model.Customers.Where(x => x.TransferedStatus == "Inserts"))
            {
                xmldata = appendXmlOnInsertUpdateOrDelete(x, xmldata);
            }
            if (model.Customers.Where(x => x.TransferedStatus == "Inserts").Count() > 0)
                xmldata += "</Inserts>";

            if (model.Customers.Where(x => x.TransferedStatus == "Updates").Count() > 0)
                xmldata += "<Updates>";

            foreach (var x in model.Customers.Where(x => x.TransferedStatus == "Updates"))
            {
                xmldata = appendXmlOnInsertUpdateOrDelete(x, xmldata);
            }

            if (model.Customers.Where(x => x.TransferedStatus == "Updates").Count() > 0)
                xmldata += "</Updates>";

            if (model.Customers.Where(x => x.TransferedStatus == "Deletes").Count() > 0)
                xmldata += "<Deletes>";

            foreach (var x in model.Customers.Where(x => x.TransferedStatus == "Deletes"))
            {
                xmldata = appendXmlOnInsertUpdateOrDelete(x, xmldata);
            }
            if (model.Customers.Where(x => x.TransferedStatus == "Deletes").Count() > 0)
                xmldata += "</Deletes>";

            xmldata += "</Import></Imports>";

            var document = XDocuments.Parse(xmldata);

            if(!cm1Settings.Value.OptBool("skipKycAnswers"))
            {
                AppendLatestKycAnswers(document, customerIds);
            }            

            return document;
        }

        private void AppendLatestKycAnswers(XDocument document, HashSet<int> customerIds)
        {
            XNamespace ns = "http://Modul1/CM1/CustomerImport/GCCCapital/v1";
            var kycAnswerPropertiesPerCustomer = new Cm1CustomerKycQuestionsRepository(contextFactory, null).GetLatestAnswersCustomerPropertiesPerCustomer(customerIds);

            foreach (var personElement in document.Descendants().Where(x => x.Name.LocalName == "Person").ToList())
            {
                var customerId = int.Parse(personElement.Elements().Single(x => x.Name.LocalName == "CustomId").Value);
                var customerProperties = kycAnswerPropertiesPerCustomer.Opt(customerId);
                if (customerProperties == null || customerProperties.Count == 0)
                    continue;

                var additionalDataElement = personElement.Descendants().FirstOrDefault(x => x.Name.LocalName == "PersonAdditionalData");
                if(additionalDataElement == null)
                {
                    additionalDataElement = new XElement(ns + "PersonAdditionalData");
                    personElement.Add(additionalDataElement);
                }

                foreach(var answerProperty in customerProperties)
                {
                    additionalDataElement.Add(new XElement(ns + "CustomerAdditionalDataElement",
                        new XElement(ns + "ParameterName", answerProperty.QuestionPropertyName),
                        new XElement(ns + "ParameterValue", answerProperty.QuestionPropertyValue),
                        new XElement(ns + "ParameterType", "String")));
                }
            }
        }

        private string AppendNonCompanyLoanCustomerData(CustomerModelHelper customer, string xmldata,
            CompleteCmlExportFileRequest.ExportTypeCode exportType)
        {
            var civicNrParser = new CivicRegNumberParser(clientConfiguration.Country.BaseCountry);

            var civicNr = civicNrParser.Parse(customer.CustomerOpt(CustomerProperty.Codes.civicRegNr));
            var IdentifierType = "SWE_Personnummer";
            if (civicNr.Country == "FI")
                IdentifierType = "FIN_Personnummer";

            xmldata += "<Person>";
            xmldata += new XElement("CustomId", customer.CustomerId.ToString()).ToString();
            xmldata += "<PersonNumber>";
            xmldata += new XElement("IdentifierType", IdentifierType).ToString();
            xmldata += new XElement("Identifier", $"{civicNr.NormalizedValue}").ToString();
            xmldata += "</PersonNumber>";
            xmldata += new XElement("FirstName", customer.CustomerOpt(CustomerProperty.Codes.firstName)).ToString();
            xmldata += new XElement("LastName", customer.CustomerOpt(CustomerProperty.Codes.lastName)).ToString();

            var zipCode = customer.CustomerOpt(CustomerProperty.Codes.addressZipcode);
            var phone = customer.CustomerOpt(CustomerProperty.Codes.phone);
            var email = customer.CustomerOpt(CustomerProperty.Codes.email);
            var hasAnyContactInfo = !(string.IsNullOrWhiteSpace(zipCode) && string.IsNullOrWhiteSpace(phone) &&
                                    string.IsNullOrWhiteSpace(email));
            Func<string, string, object> createElement = (elementName, value) =>
            {
                if (string.IsNullOrWhiteSpace(value))
                    return null;
                return new XElement(elementName, value.Trim());
            };
            if (hasAnyContactInfo)
            {
                xmldata += new XElement("ContactInformation", Enumerables.SkipNulls(
                    createElement("AddressRow1", customer.CustomerOpt(CustomerProperty.Codes.addressStreet)),
                    createElement("PostalRegion", customer.CustomerOpt(CustomerProperty.Codes.addressCity)),
                    createElement("PostalNumber", zipCode),
                    createElement("Telephone1", phone),
                    createElement("Email", email),
                    createElement("Country", customer.GetTwoLetterIsoAddressCountry())
                ).ToArray());
            }

            switch (exportType)
            {
                case CompleteCmlExportFileRequest.ExportTypeCode.Savings:
                    {
                        xmldata += "<PersonAdditionalData>";
                        xmldata += "<CustomerAdditionalDataElement>";
                        xmldata += "<ParameterName>ProductType</ParameterName>";
                        xmldata += "<ParameterValue>" + cm1Settings.Value.Req("CustomerSavingsProductType") + "</ParameterValue>";
                        xmldata += "<ParameterType>String</ParameterType>";
                        xmldata += "</CustomerAdditionalDataElement>";
                        xmldata += "</PersonAdditionalData>";
                    }
                    break;
                case CompleteCmlExportFileRequest.ExportTypeCode.Credit:
                    {
                        xmldata += "<PersonAdditionalData>";
                        xmldata += "<CustomerAdditionalDataElement>";
                        xmldata += "<ParameterName>ProductType</ParameterName>";
                        xmldata += "<ParameterValue>" + cm1Settings.Value.Req("CustomerCreditProductType") + "</ParameterValue>";
                        xmldata += "<ParameterType>String</ParameterType>";
                        xmldata += "</CustomerAdditionalDataElement>";
                        xmldata += "</PersonAdditionalData>";
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            xmldata += "</Person>";

            return xmldata;
        }

        private string AppendCompanyLoanCustomerData(CustomerModelHelper companyCustomer, string xmldata)
        {
            var orgNrParser = new OrganisationNumberParser(clientConfiguration.Country.BaseCountry);

            var orgnumber = orgNrParser.Parse(companyCustomer.CustomerOpt(CustomerProperty.Codes.orgnr));
            xmldata += "<Organization>";
            xmldata += new XElement("CustomId", companyCustomer.CustomerId.ToString()).ToString();
            xmldata += "<OrganizationNumber>";

            //NOTE: Preserving previous logic for identifier type but it seems to make no sense whatsoever
            xmldata += new XElement("IdentifierType", orgnumber.Country == "FI" ? "" : "SWE_Orgnummer").ToString();
            xmldata += new XElement("IssuingCountry", clientConfiguration.Country.BaseCountry).ToString();
            xmldata += new XElement("Identifier", $"{orgnumber.NormalizedValue}").ToString();
            xmldata += "</OrganizationNumber>";
            xmldata += new XElement("OrganizationName", companyCustomer.CustomerOpt(CustomerProperty.Codes.companyName));

            var customerAdditionalDataElements = new List<string>();

            #region Producttype
            customerAdditionalDataElements.Add(
                "<CustomerAdditionalDataElement>"
                    + "<ParameterName>ProductType</ParameterName>"
                    + "<ParameterValue>" + cm1Settings.Value.Req("CustomerCreditProductType") + "</ParameterValue>"
                    + "<ParameterType>String</ParameterType>"
                    + "</CustomerAdditionalDataElement>");

            #endregion Producttype

            if (customerAdditionalDataElements.Count > 0)
            {
                xmldata += "<OrganizationAdditionalData>";
                xmldata += string.Concat(customerAdditionalDataElements);
                xmldata += "</OrganizationAdditionalData>";
            }

            xmldata += "</Organization>";

            return xmldata;
        }

        private XDocument CreateTransactionFile(CompleteCmlExportFileRequest model, int limit, int skipped)
        {
            var xml = new Cm1XmlModel();

            string productType;
            string transactionTypeIncomingPayment;
            string transactionTypeOutgoingPayment;

            switch (model.ExportType.Value)
            {
                case CompleteCmlExportFileRequest.ExportTypeCode.Savings:
                    {
                        productType = cm1Settings.Value.Req("SavingsProductType");
                        transactionTypeIncomingPayment = cm1Settings.Value.Req("SavingsTransactionTypeIncomingPayment");
                        transactionTypeOutgoingPayment = cm1Settings.Value.Req("SavingsTransactionTypeOutgoingPayment");
                    }
                    break;
                case CompleteCmlExportFileRequest.ExportTypeCode.Credit:
                    {
                        productType = cm1Settings.Value.Req("CreditProductType");
                        transactionTypeIncomingPayment = cm1Settings.Value.Req("CreditTransactionTypeIncomingPayment");
                        transactionTypeOutgoingPayment = cm1Settings.Value.Req("CreditTransactionTypeOutgoingPayment");
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            foreach (var t in model.ProductRequest.Transactions.Skip(skipped).Take(limit)) //TODO: Isnt this skipping thing pure madness .. we dont seem to track externally where we stopped and send those later right?
            {
                xml
                    .NewItem("Transactions", "Transaction")
                    .StringAttr("TempId", t.Id.ToString())
                    .StringAttr("CustomId", t.CustomerId.ToString())
                    .DecimalAttr("Amount", Math.Abs(t.Amount)) //Remove sign since that is represented by the TransactionType on the receiving end
                    .DateAttr("Date", t.TransactionDate)
                    .StringAttr("Currency", clientConfiguration.Country.BaseCurrency)
                    .StringAttr("TransactionType", t.IsConnectedToIncomingPayment ? transactionTypeIncomingPayment : t.IsConnectedToOutgoingPayment ? transactionTypeOutgoingPayment : "Unknown")
                    .StringAttr("ProductType", productType);
            }

            //NOTE: This "hack" is how everything here should have been done. We should remove this crazy xml model and just use XDocuments everywhere.
            var document = xml.ToDocument(cm1Settings.Value);
            var transactionById = model.ProductRequest.Transactions.ToDictionary(x => x.Id.ToString(), x => x);
            foreach(var transactionElement in document.Descendants().Where(x => x.Name.LocalName == "Transaction").ToList())
            {
                var idElement = transactionElement.Elements().Single(x => x.Name.LocalName == "TempId");
                idElement.Remove();
                var id = idElement.Value;
                var transaction = transactionById[idElement.Value];
                if(transaction.IsConnectedToIncomingPayment && !string.IsNullOrWhiteSpace(transaction.TransactionCustomerName))
                {
                    transactionElement.Add(new XElement(Cm1XmlModel.ElementNameSpace + "TransactionAdditionalData",
                        new XElement(Cm1XmlModel.ElementNameSpace + "ParameterName", "Inbetalare Namn"),
                        new XElement(Cm1XmlModel.ElementNameSpace + "ParameterValue", transaction.TransactionCustomerName),
                        new XElement(Cm1XmlModel.ElementNameSpace + "ParameterType", "String")));
                }
            }

            return document;
        }

        public void CreateExportFiles(CompleteCmlExportFileRequest model, Action<string> onCustomerFileCreated, Action<string> onTransactionsFileCreated, 
            Func<ICustomerContextExtended, CustomerWriteRepository> createRepo)
        {
            if (model.Customers != null && model.Customers.Count > 0)
            {
                FileUtilities.WithTempFile(tmp =>
                {
                    var customerDocument = CreateCustomerFile(model, createRepo);
                    customerDocument.Save(tmp);
                    onCustomerFileCreated(tmp);
                }, suffix: ".xml");
            }

            if (model.ProductRequest.Transactions != null && model.ProductRequest.Transactions.Count > 0)
            {
                var f = cm1Settings;
                var limit = int.Parse(cm1Settings.Value.Req("LimitTransactions"));
                var skipped = 0;
                var originalQuantityTransactions = model.ProductRequest.Transactions.Count();
                while (originalQuantityTransactions > 0)
                {
                    FileUtilities.WithTempFile(tmp =>
                    {
                        var transactionDocument = CreateTransactionFile(model, limit, skipped);
                        transactionDocument.Save(tmp);
                        originalQuantityTransactions = originalQuantityTransactions - limit;
                        skipped = skipped + limit;
                        onTransactionsFileCreated(tmp);
                    }, suffix: ".xml");
                }
            }
        }
    }
}