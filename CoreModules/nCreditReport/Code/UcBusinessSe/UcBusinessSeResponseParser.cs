using NTech;
using NTech.Banking.CivicRegNumbers.Se;
using NTech.Banking.OrganisationNumbers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace nCreditReport.Code.UcSe
{
    public class UcBusinessSeResponseParser
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
            public CompanyUcModel CompanyModel { get; set; }
        }

        public class CompanyUcModel
        {
            public string CompanyName { get; set; }
            public string CompanyStreetAddress { get; set; }
            public string CompanyZipCode { get; set; }
            public string CompanyPostalArea { get; set; }
            public List<BokslutModel> Bokslut { get; set; } = new List<BokslutModel>();
            public List<ModerbolagModel> Moderbolag { get; set; } = new List<ModerbolagModel>();
            public List<StyrelseMedlemModel> StyrelseMedlemmar { get; set; } = new List<StyrelseMedlemModel>();
            public string RiskklassForetag { get; set; }
            public decimal? RiskprognosForetagProcent { get; set; }
            public decimal? BranschRiskprognosForetagProcent { get; set; }
            public string Snikod { get; set; }
            public string Bolagsform { get; set; }
            public string Bolagstatus { get; set; }
            public DateTime? RegDatumForetag { get; set; }
            public DateTime? BestalldDatum { get; set; }
            public bool? FinnsStyrelseKonkursengagemang { get; set; }
            public bool? FinnsStyrelseBetAnmarkningar { get; set; }
            public bool? FinnsStyrelseKonkursansokningar { get; set; }
            public int? AntalAnmarkningar { get; set; }
            public string CompanyPhone { get; set; }

            public class StyrelseMedlemModel
            {
                public CivicRegNumberSe CivicRegNumber { get; set; }
                public DateTime? RegDatum { get; set; }
                public string Befattning { get; set; }
                public string BefattningKod { get; set; }
            }

            public class ModerbolagModel
            {
                public decimal? AgarandelProcent { get; set; }
                public string RiskklassForetag { get; set; }
                public string Orgnr { get; set; }
                public string Namn { get; set; }
            }

            public class BokslutModel
            {
                public bool? IsHelAr { get; set; }
                public DateTime? FranDatum { get; set; }
                public DateTime? TillDatum { get; set; }
                public int? NettoOmsattning { get; set; }
                public decimal? AvkastningTotKapProcent { get; set; }
                public decimal? KassalikviditetProcent { get; set; }
                public decimal? SoliditetProcent { get; set; }
                public int? SummaEgetKapital { get; set; }
                public int? SummaObeskattadeReserver { get; set; }
                public int? SummaImmateriellaTillgangar { get; set; }
            }

            public BokslutModel GetBokslut(DateTime from, DateTime to, Action<BokslutModel, bool> updateNewOrExisting = null)
            {
                if (Bokslut == null)
                    Bokslut = new List<BokslutModel>();

                var b = Bokslut?.Where(x => x.FranDatum == from && x.TillDatum == to).FirstOrDefault();
                if (b == null)
                {
                    b = new BokslutModel { FranDatum = from, TillDatum = to };
                    Bokslut.Add(b);
                    updateNewOrExisting?.Invoke(b, true);
                }
                else
                    updateNewOrExisting?.Invoke(b, false);

                return b;
            }
        }

        private CompanyUcModel ParseCompanyUcModel(UcSeService2.report r)
        {
            var model = new CompanyUcModel();

            var h = new UcReplyHelper(r);

            h.MapSingleOptionalGroup("W010", x =>
            {
                model.CompanyName = x.OptSingleValue("W01080") ?? x.OptSingleValue("W01017");
                model.CompanyStreetAddress = x.OptSingleValue("W01081") ?? x.OptSingleValue("W01018");
                model.CompanyZipCode = x.OptSingleValue("W01003");
                model.CompanyPostalArea = x.OptSingleValue("W01082") ?? x.OptSingleValue("W01019");
                model.CompanyPhone = x.OptSingleValue("W01016");
            });

            h.MapSingleOptionalGroup("W030", x =>
             {
                 DateTime? regDatumForetag = null;
                 if (x.OptSingleValue("W03020") == "1") //Means registered before 1976
                     regDatumForetag = new DateTime(1976, 1, 1);
                 else
                     regDatumForetag = x.OptLongDate("W03005");

                 model.Bolagstatus = x.OptSingleValue("W03010") ?? "Ok";

                 model.RegDatumForetag = regDatumForetag;
             });

            h.MapSingleOptionalGroup("W110", x =>
            {
                model.RiskklassForetag = x.OptSingleValue("W11005");
                model.RiskprognosForetagProcent = x.OptSinglePercentValue("W11029");
                model.BranschRiskprognosForetagProcent = x.OptSinglePercentValue("W11006");
                model.Snikod = x.OptSingleValue("W11011");
                model.Bolagsform = x.OptSingleValue("W11021");
            });

            h.MapSingleOptionalGroup("W400", x =>
            {
                Func<string, bool?> getIntOneOptBool = y =>
                {
                    var v = x.OptSingleValue(y)?.Trim();
                    if (string.IsNullOrWhiteSpace(v))
                        return new bool?();
                    return v == "1";
                };
                model.FinnsStyrelseKonkursengagemang = getIntOneOptBool("W40032");
                model.FinnsStyrelseBetAnmarkningar = getIntOneOptBool("W40033");
                model.FinnsStyrelseKonkursansokningar = getIntOneOptBool("W40034");
            });

            h.HandleRepeatingGroup("W410", x =>
            {
                var m = new CompanyUcModel.StyrelseMedlemModel
                {
                    Befattning = x.OptSingleValue("W41002"),
                    BefattningKod = x.OptSingleValue("W41036"),
                    RegDatum = x.OptLongDate("W41012")
                };

                //W41001 civicregnr or sometimes just född xxxx so may be invalid
                var civicRegNr = x.OptSingleValue("W41001");
                CivicRegNumberSe civicRegNrParsed;
                if (string.IsNullOrWhiteSpace(civicRegNr) && CivicRegNumberSe.TryParse(civicRegNr, out civicRegNrParsed))
                {
                    m.CivicRegNumber = civicRegNrParsed;
                }

                model.StyrelseMedlemmar.Add(m);
            });

            h.HandleRepeatingGroup("W530", x =>
            {
                model.Moderbolag.Add(new CompanyUcModel.ModerbolagModel
                {
                    Orgnr = x.OptSingleValue("W53001"),
                    Namn = x.OptSingleValue("W53002"),
                    RiskklassForetag = x.OptSingleValue("W53015"),
                    AgarandelProcent = x.OptSinglePercentValue("W53008")
                });
            });

            h.MapSingleOptionalGroup("W611", x =>
            {
                model.AntalAnmarkningar = x.OptSingleIntValue("W61111");
            });

            model.Bokslut = new List<CompanyUcModel.BokslutModel>();
            h.HandleRepeatingGroup("W914", x =>
            {
                var franDatum = x.OptLongDate("W91402");
                var tillDatum = x.OptLongDate("W91403");
                if (franDatum.HasValue && tillDatum.HasValue)
                {
                    var b = model.GetBokslut(franDatum.Value, tillDatum.Value);
                    b.IsHelAr = x.OptSingleValue("W91401")?.Trim() == "0";
                }
            });

            h.HandleRepeatingGroup("W911", x =>
            {
                var franDatum = x.OptLongDate("W91102");
                var tillDatum = x.OptLongDate("W91103");
                if (franDatum.HasValue && tillDatum.HasValue)
                {
                    var b = model.GetBokslut(franDatum.Value, tillDatum.Value);
                    b.IsHelAr = x.OptSingleValue("W91101")?.Trim() == "0";
                    b.NettoOmsattning = x.OptSingleIntValue("W91106");
                }
            });

            h.HandleRepeatingGroup("W920", x =>
            {
                var franDatum = x.OptLongDate("W92050");
                var tillDatum = x.OptLongDate("W92051");
                if (franDatum.HasValue && tillDatum.HasValue)
                {
                    var b = model.GetBokslut(franDatum.Value, tillDatum.Value);
                    b.IsHelAr = x.OptSingleValue("W92049")?.Trim() == "0";
                    b.AvkastningTotKapProcent = x.OptSinglePercentValue("W92004");
                    b.KassalikviditetProcent = x.OptSinglePercentValue("W92002");
                    b.SoliditetProcent = x.OptSinglePercentValue("W92001");
                }
            });

            h.HandleRepeatingGroup("W912", x =>
            {
                var franDatum = x.OptLongDate("W91202");
                var tillDatum = x.OptLongDate("W91203");
                if (franDatum.HasValue && tillDatum.HasValue)
                {
                    var b = model.GetBokslut(franDatum.Value, tillDatum.Value);
                    b.IsHelAr = x.OptSingleValue("W91201")?.Trim() == "0";
                    b.SummaEgetKapital = x.OptSingleIntValue("W91243");
                    b.SummaObeskattadeReserver = x.OptSingleIntValue("W91244");
                    b.SummaImmateriellaTillgangar = x.OptSingleIntValue("W91210");
                }
            });
            h.MapSingleOptionalGroup("W980", x =>
            {
                model.BestalldDatum = x.OptLongDate("W98042");
            });

            return model;
        }

        public Result Parse(UcSeService2.ucReply r, IOrganisationNumber orgnr, Func<DateTime> getToday)
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

            var allXmlReports = r?.ucReport?.SelectMany(ucReport => ucReport?.xmlReply?.reports.SelectMany(xmlReport => xmlReport?.report));
            var report = allXmlReports.Where(x => x.id == orgnr.NormalizedValue).FirstOrDefault();

            if (report == null)
                return createError("Missing report");

            var companyModel = ParseCompanyUcModel(report);

            var items = new List<Item>();
            Action<string, string> add = (a, b) => items.Add(new Item { Name = a, Value = b });

            var ageBaseDate = (companyModel.BestalldDatum ?? getToday()).Date;

            add("upplysningDatum", companyModel.BestalldDatum.HasValue ? companyModel.BestalldDatum.Value.ToString("yyyy-MM-dd") : "missing");
            add("riskklassForetag", string.IsNullOrWhiteSpace(companyModel.RiskklassForetag) ? "missing" : companyModel.RiskklassForetag);
            add("riskprognosForetagProcent", !companyModel.RiskprognosForetagProcent.HasValue ? "missing" : companyModel.RiskprognosForetagProcent.Value.ToString(CultureInfo.InvariantCulture));
            add("antalModerbolag", companyModel.Moderbolag.Count.ToString());
            add("branschRiskprognosForetagProcent", !companyModel.BranschRiskprognosForetagProcent.HasValue ? "missing" : companyModel.BranschRiskprognosForetagProcent.Value.ToString(CultureInfo.InvariantCulture));
            add("snikod", string.IsNullOrWhiteSpace(companyModel.Snikod) ? "missing" : companyModel.Snikod);
            add("bolagsform", string.IsNullOrWhiteSpace(companyModel.Bolagsform) ? "missing" : companyModel.Bolagsform);
            add("bolagsstatus", string.IsNullOrWhiteSpace(companyModel.Bolagstatus) ? "missing" : companyModel.Bolagstatus);
            add("regDatumForetag", !companyModel.RegDatumForetag.HasValue ? "missing" : companyModel.RegDatumForetag.Value.ToString("yyyy-MM-dd"));

            var biggestModerbolag = companyModel.Moderbolag.OrderByDescending(x => x.AgarandelProcent ?? 0).FirstOrDefault();
            add("moderbolagRiskklassForetag", string.IsNullOrWhiteSpace(biggestModerbolag?.RiskklassForetag) ? "missing" : biggestModerbolag?.RiskklassForetag);

            Func<bool?, string> optBoolTrueFalseMissing = y => y.HasValue ? (y.Value ? "true" : "false") : "missing";
            add("finnsStyrelseKonkursengagemang", optBoolTrueFalseMissing(companyModel.FinnsStyrelseKonkursengagemang));
            add("finnsStyrelseBetAnmarkningar", optBoolTrueFalseMissing(companyModel.FinnsStyrelseBetAnmarkningar));
            add("finnsStyrelseKonkursansokningar", optBoolTrueFalseMissing(companyModel.FinnsStyrelseKonkursansokningar));


            //Auktoriserad revisor, Godkänd revisor, <Befattning på ledamot med betfattningskod 150>
            var styrelseKod = "Ingen";
            if (companyModel.StyrelseMedlemmar?.Any(x => x.Befattning == "Auktoriserad revisor") ?? false)
                styrelseKod = "Auktoriserad revisor";
            else if (companyModel.StyrelseMedlemmar?.Any(x => x.Befattning == "Godkänd revisor") ?? false)
                styrelseKod = "Godkänd revisor";
            else
            {
                var b = companyModel.StyrelseMedlemmar?.Where(x => x.BefattningKod?.TrimStart('0') == "150").FirstOrDefault()?.Befattning;
                if (!string.IsNullOrWhiteSpace(b))
                    styrelseKod = b;
            }
            add("styrelseRevisorKod", styrelseKod);

            var bokslutOrdered = companyModel?.Bokslut?.Where(x => x.TillDatum.HasValue)?.OrderByDescending(x => x.TillDatum.Value);

            {
                string bokslutDatum = "missing";
                string nettoOmsattning = "missing";
                string avkastningTotKapProcent = "missing";
                string kassalikviditetProcent = "missing";
                string soliditetProcent = "missing";
                string summaEgetKapital = "missing";
                string summaObeskattadeReserver = "missing";
                string summaImmateriellaTillgangar = "missing";

                var b1 = bokslutOrdered?.FirstOrDefault();
                if (b1 != null)
                {
                    bokslutDatum = b1.TillDatum.Value.ToString("yyyy-MM-dd");
                    if (b1.NettoOmsattning.HasValue)
                        nettoOmsattning = b1.NettoOmsattning.Value.ToString();
                    if (b1.AvkastningTotKapProcent.HasValue)
                        avkastningTotKapProcent = b1.AvkastningTotKapProcent.Value.ToString(CultureInfo.InvariantCulture);
                    if (b1.KassalikviditetProcent.HasValue)
                        kassalikviditetProcent = b1.KassalikviditetProcent.Value.ToString(CultureInfo.InvariantCulture);
                    if (b1.SoliditetProcent.HasValue)
                        soliditetProcent = b1.SoliditetProcent.Value.ToString(CultureInfo.InvariantCulture);
                    if (b1.SummaEgetKapital.HasValue)
                        summaEgetKapital = b1.SummaEgetKapital.Value.ToString(CultureInfo.InvariantCulture);
                    if (b1.SummaObeskattadeReserver.HasValue)
                        summaObeskattadeReserver = b1.SummaObeskattadeReserver.Value.ToString(CultureInfo.InvariantCulture);
                    if (b1.SummaImmateriellaTillgangar.HasValue)
                        summaImmateriellaTillgangar = b1.SummaImmateriellaTillgangar.Value.ToString(CultureInfo.InvariantCulture);
                }
                add("bokslutDatum", bokslutDatum);
                add("nettoOmsattning", nettoOmsattning);
                add("avkastningTotKapProcent", avkastningTotKapProcent);
                add("kassalikviditetProcent", kassalikviditetProcent);
                add("soliditetProcent", soliditetProcent);
                add("summaEgetKapital", summaEgetKapital);
                add("summaObeskattadeReserver", summaObeskattadeReserver);
                add("summaImmateriellaTillgangar", summaImmateriellaTillgangar);
            }

            {
                string bokslutDatumFg = "missing";
                string nettoOmsattningFg = "missing";
                string avkastningTotKapProcentFg = "missing";
                string kassalikviditetProcentFg = "missing";
                string soliditetProcentFg = "missing";
                string summaEgetKapitalFg = "missing";
                string summaObeskattadeReserverFg = "missing";
                string summaImmateriellaTillgangarFg = "missing";

                var b2 = bokslutOrdered.Skip(1).FirstOrDefault();
                if (b2 != null)
                {
                    bokslutDatumFg = b2.TillDatum.Value.ToString("yyyy-MM-dd");
                    if (b2.NettoOmsattning.HasValue)
                        nettoOmsattningFg = b2.NettoOmsattning.Value.ToString();
                    if (b2.AvkastningTotKapProcent.HasValue)
                        avkastningTotKapProcentFg = b2.AvkastningTotKapProcent.Value.ToString(CultureInfo.InvariantCulture);
                    if (b2.KassalikviditetProcent.HasValue)
                        kassalikviditetProcentFg = b2.KassalikviditetProcent.Value.ToString(CultureInfo.InvariantCulture);
                    if (b2.SoliditetProcent.HasValue)
                        soliditetProcentFg = b2.SoliditetProcent.Value.ToString(CultureInfo.InvariantCulture);
                    if (b2.SummaEgetKapital.HasValue)
                        summaEgetKapitalFg = b2.SummaEgetKapital.Value.ToString(CultureInfo.InvariantCulture);
                    if (b2.SummaObeskattadeReserver.HasValue)
                        summaObeskattadeReserverFg = b2.SummaObeskattadeReserver.Value.ToString(CultureInfo.InvariantCulture);
                    if (b2.SummaImmateriellaTillgangar.HasValue)
                        summaImmateriellaTillgangarFg = b2.SummaImmateriellaTillgangar.Value.ToString(CultureInfo.InvariantCulture);
                }

                add("bokslutDatumFg", bokslutDatumFg);
                add("nettoOmsattningFg", nettoOmsattningFg);
                add("avkastningTotKapProcentFg", avkastningTotKapProcentFg);
                add("kassalikviditetProcentFg", kassalikviditetProcentFg);
                add("soliditetProcentFg", soliditetProcentFg);
                add("summaEgetKapitalFg", summaEgetKapitalFg);
                add("summaObeskattadeReserverFg", summaObeskattadeReserverFg);
                add("summaImmateriellaTillgangarFg", summaImmateriellaTillgangarFg);
            }

            var ledamotsBas = companyModel
                .StyrelseMedlemmar
                .Where(x => x.RegDatum.HasValue)
                .ToList();

            if (ledamotsBas.Any())
            {
                var antalStyrelseLedamotsManaderPre = ledamotsBas
                                    .Select(x => Dates.GetAbsoluteNrOfMonthsBetweenDates(x.RegDatum.Value, ageBaseDate));
                add("antalStyrelseLedamotsManader", antalStyrelseLedamotsManaderPre.Aggregate(0, (x, y) => x + y).ToString());
                add("styrelseLedamotMaxMander", antalStyrelseLedamotsManaderPre.Max().ToString());
            }
            else
            {
                add("antalStyrelseLedamotsManader", "missing");
                add("styrelseLedamotMaxMander", "missing");
            }

            add("foretagAlderIManader", companyModel.RegDatumForetag.HasValue
                ? Dates.GetAbsoluteNrOfMonthsBetweenDates(companyModel.RegDatumForetag.Value, ageBaseDate).ToString()
                : "missing");

            add("antalAnmarkningar", companyModel.AntalAnmarkningar.HasValue ? companyModel.AntalAnmarkningar.ToString() : "missing");


            //Name and address
            Action<string, string> addIfPresent = (x, y) =>
            {
                if (!string.IsNullOrWhiteSpace(y))
                    add(x, y);
            };

            addIfPresent("companyName", companyModel.CompanyName);
            addIfPresent("addressStreet", companyModel.CompanyStreetAddress);
            addIfPresent("addressZipcode", companyModel.CompanyZipCode);
            addIfPresent("addressCity", companyModel.CompanyPostalArea);
            addIfPresent("phone", companyModel.CompanyPhone);

            return new Result
            {
                IsError = false,
                IsInvalidCredentialsError = false,
                ErrorMessage = null,
                SuccessItems = items,
                CompanyModel = ParseCompanyUcModel(report)
            };
        }
    }
}