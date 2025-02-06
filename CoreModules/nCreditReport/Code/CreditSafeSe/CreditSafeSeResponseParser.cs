using nCreditReport.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace nCreditReport.Code.CreditSafeSe
{
    public static class CreditSafeSeResponseParser
    {
        public static (Dictionary<string, string> TemplateItems, List<(string Code, string Text)> Errors) ParseTemplateXml(XElement xml)
        {
            var errors = xml
                .Descendants()
                .Where(x => x.Name.LocalName == "ERROR")
                .ToList()
                .Select(x => (
                    Code: x.Descendants().Single(y => y.Name.LocalName == "Cause_of_Reject").Value,
                    Text: x.Descendants().Single(y => y.Name.LocalName == "Reject_text").Value))
                .ToList();
            var allItems = new Dictionary<string, string>();
            var resultElement = xml.Descendants().Where(x => x.Name.LocalName == "CasPersonServiceResult").FirstOrDefault();
            if (resultElement != null)
                foreach (var childElement in resultElement.Elements().Where(x => !x.HasElements))
                {
                    allItems[childElement.Name.LocalName] = childElement.Value;
                }
            return (TemplateItems: allItems, Errors: errors);
        }

        public static (Dictionary<string, string> DataItems, string DataBlockName) ParseDataXml(XElement xml)
        {
            var allDataItems = new Dictionary<string, string>();
            var dataElement = xml.Descendants().Where(x => x.Name.LocalName == "GETDATA_RESPONSE").FirstOrDefault();
            if (dataElement != null)
            {
                foreach (var element in dataElement.Elements())
                {
                    allDataItems[element.Name.LocalName] = element.Value;
                }
            }
            var dataBlockName = xml.Descendants().Where(x => x.Name.LocalName == "Block_Name").FirstOrDefault()?.Value;
            return (DataItems: allDataItems, DataBlockName: dataBlockName);
        }

        public static List<SaveCreditReportRequest.Item> GetCreditReportItems(
            Dictionary<string, string> dataItems,
            Dictionary<string, string> templateItems,
            List<(string Code, string Text)> templateErrors)
        {
            var creditReportItems = new List<SaveCreditReportRequest.Item>();
            void AddCreditReportItem(string name, string value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    creditReportItems.Add(new SaveCreditReportRequest.Item { Name = name, Value = value });
            }

            var hasPostBoxAddress = false;
            var hasPosteRestanteAddress = false;
            var personStatus = dataItems.Count > 0 ? "normal" : "nodata";
            foreach (var error in templateErrors)
            {
                var causeOfReject = error.Code;

                if (causeOfReject == "S4")
                    personStatus = "dead";
                else if (causeOfReject == "S6")
                    personStatus = "emigrated";
                else if (causeOfReject == "S2")
                    personStatus = "protectedidentity";
                else if (causeOfReject == "S3")
                    personStatus = "locked";
                else if (causeOfReject == "S5")
                    personStatus = "deactivated";

                if (causeOfReject == "P52")
                    hasPostBoxAddress = true;

                if (causeOfReject == "P51")
                    hasPosteRestanteAddress = true;
            }

            var status = templateItems.Opt("Status");
            AddCreditReportItem("templateAccepted", status.IsOneOfIgnoreCase("1", "4") ? "true" : "false");
            AddCreditReportItem("templateManualAttention", status.IsOneOfIgnoreCase("4") ? "true" : "false");
            AddCreditReportItem("firstName", templateItems.Opt("FirstName"));
            AddCreditReportItem("lastName", templateItems.Opt("LastName"));
            AddCreditReportItem("addressStreet", templateItems.Opt("RegisteredAddress"));
            AddCreditReportItem("addressZipcode", templateItems.Opt("ZIP"));
            AddCreditReportItem("addressCity", templateItems.Opt("Town"));

            var hasGuardian = dataItems.Opt("HAS_TRUSTEE") == "Ja";
            AddCreditReportItem("hasGuardian", hasGuardian ? "true" : "false");

            var scoring = dataItems.Opt("SCORING");
            if (!string.IsNullOrWhiteSpace(scoring) && int.TryParse(scoring, out var intScoring))
            {
                AddCreditReportItem("scoreValue", intScoring.ToString(CultureInfo.InvariantCulture)); //0 -> 100
                AddCreditReportItem("riskValue", (100 - intScoring).ToString(CultureInfo.InvariantCulture));
            }

            AddCreditReportItem("hasSpecialAddress", new[] { "SPEC_ADDRESS", "SPEC_REGISTERED_ADDRESS", "SPEC_CO_ADDRESS",
                "SPEC_ZIPCODE", "SPEC_TOWN", "SPEC_COUNTRY" }.Any(x => !string.IsNullOrWhiteSpace(dataItems.Opt(x)))
                ? "true"
                : "false");

            AddCreditReportItem("hasDomesticAddress", (dataItems.Opt("ZIPCODE") ?? "").Trim().Length > 0 ? "true" : "false");
            AddCreditReportItem("hasPosteRestanteAddress", hasPosteRestanteAddress ? "true" : "false");
            AddCreditReportItem("hasPostBoxAddress", hasPostBoxAddress ? "true" : "false");

            if (hasGuardian)
                personStatus = "hasguardian";

            if (dataItems.Opt("PROTECTED") == "Ja")
                personStatus = "protectedidentity"; //Normally handled by error S2 but the service can be configured to return data anyway

            if (dataItems.Opt("EMIGRATED") == "Ja")
                personStatus = "emigrated";//Normally handled by error S6 but the service can be configured to return data anyway

            AddCreditReportItem("personstatus", personStatus);

            var nrOfPaymentRemarks = dataItems.Opt("ROP_NUMBER1")?.NormalizeNullOrWhitespace() ?? "0";
            AddCreditReportItem("nrOfPaymentRemarks", nrOfPaymentRemarks);
            AddCreditReportItem("hasPaymentRemark", nrOfPaymentRemarks == "0" ? "false" : "true");

            var debtSum = dataItems.Opt("DEBT_SUM")?.NormalizeNullOrWhitespace() ?? "0";
            AddCreditReportItem("hasKfmBalance", debtSum == "0" ? "false" : "true");

            AddCreditReportItem("hasSwedishSkuldsanering", dataItems.Opt("DEBT_PERSON") == "Ja" ? "true" : "false");

            if (int.TryParse(dataItems.Opt("INCOME") ?? "0", out var applicantCreditReportIncomePerYear))
            {
                AddCreditReportItem("latestIncomeYear", applicantCreditReportIncomePerYear.ToString(CultureInfo.InvariantCulture));
                AddCreditReportItem("latestIncomePerYear", applicantCreditReportIncomePerYear.ToString(CultureInfo.InvariantCulture));                
            }                

            return creditReportItems;
        }

        public static List<DictionaryEntry> GetTabledValuesFromStoredXml(XDocument document)
        {
            var rows = new List<DictionaryEntry>();
            void AddRow(string text, string value) => rows.Add(new DictionaryEntry { Key = text, Value = value });
            void AddHeader(string text, int level)
            {
                var headerMarker = new string('-', level);
                AddRow($"{headerMarker}{text}", "");
            }
            var parsedTemplate = ParseTemplateXml(document.Root.Elements().Where(x => x.Name.LocalName == "TemplateResponse").FirstOrDefault());
            var parsedData = ParseDataXml(document.Root.Elements().Where(x => x.Name.LocalName == "DataResponse").FirstOrDefault());
            var creditReportItems = GetCreditReportItems(parsedData.DataItems, parsedTemplate.TemplateItems, parsedTemplate.Errors);

            AddHeader("Scoring variables", 1);
            foreach (var s in creditReportItems)
                AddRow(s.Name, s.Value);

            AddHeader("Template result", 1);

            AddHeader("Raw items", 2);
            foreach (var d in parsedTemplate.TemplateItems)
                AddRow(d.Key, d.Value);
            if (parsedTemplate.Errors.Any())
            {
                AddHeader("Errors", 2);
                foreach (var error in parsedTemplate.Errors)
                    AddRow(error.Code, error.Text);
            }

            AddHeader("Data result", 1);
            AddRow("Data block name", parsedData.DataBlockName);
            foreach (var d in parsedData.DataItems)
                AddRow(d.Key, d.Value);

            return rows;
        }
    }
}