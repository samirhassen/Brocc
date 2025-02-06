using NTech;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace nCreditReport.Code.UcSe
{
    public class UcSeResponseParser
    {
        public class Item
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public class Result
        {
            public bool IsError { get; set; }
            public bool IsInvalidCredentialsError { get; set; }
            public string ErrorMessage { get; set; }
            public List<Item> SuccessItems { get; set; }
        }

        public Result Parse(UcSeService2.ucReply r)
        {
            Func<string, Result> createError = errorMessage => new Result
            {
                IsError = true,
                IsInvalidCredentialsError = (errorMessage?.ToString() ?? "").Contains("Userid är felaktigt angivet"),
                ErrorMessage = errorMessage?.ToString(),
                SuccessItems = null
            };

            if (r?.status?.result == "error")
            {
                return createError(r?.status?.message?.Value);
            }

            var report = r?.ucReport?.FirstOrDefault()?.xmlReply?.reports?.FirstOrDefault().report?.FirstOrDefault();

            if (report == null)
                return createError("Missing report");

            var h = new UcReplyHelper(report);
            var historicalAddressChangeDates = new List<DateTime>();

            void CollectRiskPercent(string termName, UcSeService2.group g)
            {
                //Format is like 10,6 %
                var riskPercentString = g.OptSingleValue(termName)?.Replace("%", "")?.Replace(",", ".")?.NormalizeNullOrWhitespace();
                if (riskPercentString != null)
                {
                    var riskPercent = decimal.Parse(riskPercentString, CultureInfo.InvariantCulture);
                    h.Add("riskPercent", riskPercent.ToString(CultureInfo.InvariantCulture));
                    h.Add("riskValue", riskPercent.ToString(CultureInfo.InvariantCulture)); //riskValue is a more general concept that could be things like 1-5 for other providers than uc but happens to map to risk percent here
                }
            }

            //Risk percent when no template is used. By F31 or K39 to get this.
            //Value intentionally replaced by W131 when it exists so keep the order of these two groups
            h.MapSingleOptionalGroup("W1A0", g =>
            {
                CollectRiskPercent("W1A091", g); //Product F31
                CollectRiskPercent("W1A081", g); //Product K39
            });

            h.MapSingleOptionalGroup("W990", g => h.Add("templateName", g.OptSingleValue("W99004")));
            h.MapSingleOptionalGroup("W131", g =>
            {
                var templateDecision = g.OptSingleValue("W13105");
                h.Add("templateAccepted", templateDecision.IsOneOfIgnoreCase("J", "P", "H") ? "true" : "false");
                h.Add("templateManualAttention", templateDecision.IsOneOfIgnoreCase("P", "H") ? "true" : "false");
                h.Add("templateReasonCode", g.OptSingleValue("W13114"));
                CollectRiskPercent("W13111", g);//When using a template
            });

            h.MapSingleOptionalGroup("W080", g =>
            {
                //name
                h.Add("firstName", g.OptSingleValue("W08083"));
                h.Add("lastName", g.OptSingleValue("W08084"));

                //address
                var foreignAddressCountry = g.OptSingleValue("W08052");
                h.Add("addressCountry", foreignAddressCountry ?? "SE");
                h.Add("hasDomesticAddress", string.IsNullOrWhiteSpace(foreignAddressCountry) ? "true" : "false");
                var recentDomesticAddressDate = g.OptSingleValue("W08054");

                h.Add("addressStreet", g.OptSingleValue("W08004"));
                h.Add("addressZipcode", g.OptSingleValue("W08005"));
                h.Add("addressCity", g.OptSingleValue("W08006"));

                var registeredMunicipality = g.OptSingleValue("W08013");

                var recentRegistrationDateRaw = g.OptSingleValue("W08054");
                h.Add("isRecentCitizen", string.IsNullOrWhiteSpace(recentRegistrationDateRaw) ? "false" : "true");

                var recentRegistrationDate = Dates.ParseDateTimeExactOrNull(recentRegistrationDateRaw, "yyyyMMdd");
                if (recentRegistrationDate.HasValue)
                    historicalAddressChangeDates.Add(recentRegistrationDate.Value);

                h.Add("hasRegisteredMunicipality", string.IsNullOrWhiteSpace(registeredMunicipality) ? "false" : "true");
                h.Add("registeredMunicipality", registeredMunicipality);

                //status
                var hasGuardian = (g.OptSingleValue("W08018") == "J");
                var status = g.OptSingleValue("W08020");
                string personstatus = "normal";
                if (status == "05")
                    personstatus = "dead";
                else if (status.IsOneOf("03", "08", "09", "10", "11"))
                    personstatus = "deactivated";
                else if (hasGuardian)
                    personstatus = "hasguardian";
                h.Add("personstatus", personstatus);

                h.Add("hasGuardian", hasGuardian ? "true" : "false");

                var addressMarker = g.OptSingleValue("W08049");
                h.Add("hasPostBoxAddress", addressMarker?.ToLowerInvariant() == "B" ? "true" : "false");
                h.Add("hasPosteRestanteAddress", addressMarker?.ToLowerInvariant() == "P" ? "true" : "false");
            },
            Tuple.Create("hasDomesticAddress", "false"), Tuple.Create("hasRegisteredMunicipality", "false"),
            Tuple.Create("personstatus", "nodata"), Tuple.Create("isRecentCitizen", "false"),
            Tuple.Create("hasGuardian", "false"), Tuple.Create("hasPostBoxAddress", "false"), Tuple.Create("hasPosteRestanteAddress", "false"));

            h.MapSingleOptionalGroup("W611", g =>
            {
                var count = g.OptSingleIntValue("W61109");
                if (count.HasValue)
                {
                    h.Add("nrOfPaymentRemarks", count.Value.ToString());
                    h.Add("hasPaymentRemark", count.Value > 0 ? "true" : "false");
                }
            }, Tuple.Create("nrOfPaymentRemarks", "0"), Tuple.Create("hasPaymentRemark", "false"));

            h.HandleRepeatingGroup("W2B0", g =>
            {
                var changeDate = g.OptSingleValue("W2B001");
                var d = Dates.ParseDateTimeExactOrNull(changeDate + "01", "yyyyMMdd");
                if (d.HasValue)
                    historicalAddressChangeDates.Add(d.Value);
            });

            int? incomeYear = null;
            int? latestIncomePerYear = null;
            h.HandleRepeatingGroup("W495", g =>
            {
                var incomeYearRaw = g.OptSingleValue("W49501");
                if (!string.IsNullOrWhiteSpace(incomeYearRaw))
                {
                    var incomeYearLatest = int.Parse(incomeYearRaw);
                    if (incomeYear.HasValue && incomeYearLatest < incomeYear.Value)
                        return;

                    var incomeRaw = g.OptSingleValue("W49522");
                    if (!string.IsNullOrWhiteSpace(incomeRaw))
                    {
                        //<1 is because some uc fields claim to use this for low amounts ... possibly this field acutally does not so thing may be a useless check.
                        latestIncomePerYear = incomeRaw.Contains("<1") ? 0 : int.Parse(incomeRaw);
                        incomeYear = incomeYearLatest;
                    }
                }
            });

            if (incomeYear.HasValue && latestIncomePerYear.HasValue)
            {
                h.Add("latestIncomePerYear", latestIncomePerYear.Value.ToString(CultureInfo.InvariantCulture));
                h.Add("latestIncomeYear", incomeYear.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (historicalAddressChangeDates.Any())
            {
                h.Add("hasAddressChange", "true");
                var latestAddressChangeDate = historicalAddressChangeDates.OrderByDescending(x => x).First();
                h.Add("addressChangeDate", latestAddressChangeDate.ToString("yyyy-MM-dd"));
            }
            else
            {
                h.Add("hasAddressChange", "false");
            }

            return new Result
            {
                IsError = false,
                IsInvalidCredentialsError = false,
                ErrorMessage = null,
                SuccessItems = h.Values.Select(x => new Item { Name = x.Key, Value = x.Value }).ToList()
            };
        }
    }
}