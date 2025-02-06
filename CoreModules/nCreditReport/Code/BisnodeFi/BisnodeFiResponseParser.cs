using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace nCreditReport.Code.BisnodeFi
{
    public class BisnodeFiResponseParser
    {
        const string countryNumbers = "[{\"a\":\"AF\",\"n\":4},{\"a\":\"AX\",\"n\":248},{\"a\":\"AL\",\"n\":8},{\"a\":\"DZ\",\"n\":12},{\"a\":\"AS\",\"n\":16},{\"a\":\"AD\",\"n\":20},{\"a\":\"AO\",\"n\":24},{\"a\":\"AI\",\"n\":660},{\"a\":\"AQ\",\"n\":10},{\"a\":\"AG\",\"n\":28},{\"a\":\"AR\",\"n\":32},{\"a\":\"AM\",\"n\":51},{\"a\":\"AW\",\"n\":533},{\"a\":\"AU\",\"n\":36},{\"a\":\"AT\",\"n\":40},{\"a\":\"AZ\",\"n\":31},{\"a\":\"BS\",\"n\":44},{\"a\":\"BH\",\"n\":48},{\"a\":\"BD\",\"n\":50},{\"a\":\"BB\",\"n\":52},{\"a\":\"BY\",\"n\":112},{\"a\":\"BE\",\"n\":56},{\"a\":\"BZ\",\"n\":84},{\"a\":\"BJ\",\"n\":204},{\"a\":\"BM\",\"n\":60},{\"a\":\"BT\",\"n\":64},{\"a\":\"BO\",\"n\":68},{\"a\":\"BQ\",\"n\":535},{\"a\":\"BA\",\"n\":70},{\"a\":\"BW\",\"n\":72},{\"a\":\"BV\",\"n\":74},{\"a\":\"BR\",\"n\":76},{\"a\":\"IO\",\"n\":86},{\"a\":\"BN\",\"n\":96},{\"a\":\"BG\",\"n\":100},{\"a\":\"BF\",\"n\":854},{\"a\":\"BI\",\"n\":108},{\"a\":\"KH\",\"n\":116},{\"a\":\"CM\",\"n\":120},{\"a\":\"CA\",\"n\":124},{\"a\":\"CV\",\"n\":132},{\"a\":\"KY\",\"n\":136},{\"a\":\"CF\",\"n\":140},{\"a\":\"TD\",\"n\":148},{\"a\":\"CL\",\"n\":152},{\"a\":\"CN\",\"n\":156},{\"a\":\"CX\",\"n\":162},{\"a\":\"CC\",\"n\":166},{\"a\":\"CO\",\"n\":170},{\"a\":\"KM\",\"n\":174},{\"a\":\"CG\",\"n\":178},{\"a\":\"CD\",\"n\":180},{\"a\":\"CK\",\"n\":184},{\"a\":\"CR\",\"n\":188},{\"a\":\"CI\",\"n\":384},{\"a\":\"HR\",\"n\":191},{\"a\":\"CU\",\"n\":192},{\"a\":\"CW\",\"n\":531},{\"a\":\"CY\",\"n\":196},{\"a\":\"CZ\",\"n\":203},{\"a\":\"DK\",\"n\":208},{\"a\":\"DJ\",\"n\":262},{\"a\":\"DM\",\"n\":212},{\"a\":\"DO\",\"n\":214},{\"a\":\"EC\",\"n\":218},{\"a\":\"EG\",\"n\":818},{\"a\":\"SV\",\"n\":222},{\"a\":\"GQ\",\"n\":226},{\"a\":\"ER\",\"n\":232},{\"a\":\"EE\",\"n\":233},{\"a\":\"ET\",\"n\":231},{\"a\":\"FK\",\"n\":238},{\"a\":\"FO\",\"n\":234},{\"a\":\"FJ\",\"n\":242},{\"a\":\"FI\",\"n\":246},{\"a\":\"FR\",\"n\":250},{\"a\":\"GF\",\"n\":254},{\"a\":\"PF\",\"n\":258},{\"a\":\"TF\",\"n\":260},{\"a\":\"GA\",\"n\":266},{\"a\":\"GM\",\"n\":270},{\"a\":\"GE\",\"n\":268},{\"a\":\"DE\",\"n\":276},{\"a\":\"GH\",\"n\":288},{\"a\":\"GI\",\"n\":292},{\"a\":\"GR\",\"n\":300},{\"a\":\"GL\",\"n\":304},{\"a\":\"GD\",\"n\":308},{\"a\":\"GP\",\"n\":312},{\"a\":\"GU\",\"n\":316},{\"a\":\"GT\",\"n\":320},{\"a\":\"GG\",\"n\":831},{\"a\":\"GN\",\"n\":324},{\"a\":\"GW\",\"n\":624},{\"a\":\"GY\",\"n\":328},{\"a\":\"HT\",\"n\":332},{\"a\":\"HM\",\"n\":334},{\"a\":\"VA\",\"n\":336},{\"a\":\"HN\",\"n\":340},{\"a\":\"HK\",\"n\":344},{\"a\":\"HU\",\"n\":348},{\"a\":\"IS\",\"n\":352},{\"a\":\"IN\",\"n\":356},{\"a\":\"ID\",\"n\":360},{\"a\":\"IR\",\"n\":364},{\"a\":\"IQ\",\"n\":368},{\"a\":\"IE\",\"n\":372},{\"a\":\"IM\",\"n\":833},{\"a\":\"IL\",\"n\":376},{\"a\":\"IT\",\"n\":380},{\"a\":\"JM\",\"n\":388},{\"a\":\"JP\",\"n\":392},{\"a\":\"JE\",\"n\":832},{\"a\":\"JO\",\"n\":400},{\"a\":\"KZ\",\"n\":398},{\"a\":\"KE\",\"n\":404},{\"a\":\"KI\",\"n\":296},{\"a\":\"KP\",\"n\":408},{\"a\":\"KR\",\"n\":410},{\"a\":\"KW\",\"n\":414},{\"a\":\"KG\",\"n\":417},{\"a\":\"LA\",\"n\":418},{\"a\":\"LV\",\"n\":428},{\"a\":\"LB\",\"n\":422},{\"a\":\"LS\",\"n\":426},{\"a\":\"LR\",\"n\":430},{\"a\":\"LY\",\"n\":434},{\"a\":\"LI\",\"n\":438},{\"a\":\"LT\",\"n\":440},{\"a\":\"LU\",\"n\":442},{\"a\":\"MO\",\"n\":446},{\"a\":\"MK\",\"n\":807},{\"a\":\"MG\",\"n\":450},{\"a\":\"MW\",\"n\":454},{\"a\":\"MY\",\"n\":458},{\"a\":\"MV\",\"n\":462},{\"a\":\"ML\",\"n\":466},{\"a\":\"MT\",\"n\":470},{\"a\":\"MH\",\"n\":584},{\"a\":\"MQ\",\"n\":474},{\"a\":\"MR\",\"n\":478},{\"a\":\"MU\",\"n\":480},{\"a\":\"YT\",\"n\":175},{\"a\":\"MX\",\"n\":484},{\"a\":\"FM\",\"n\":583},{\"a\":\"MD\",\"n\":498},{\"a\":\"MC\",\"n\":492},{\"a\":\"MN\",\"n\":496},{\"a\":\"ME\",\"n\":499},{\"a\":\"MS\",\"n\":500},{\"a\":\"MA\",\"n\":504},{\"a\":\"MZ\",\"n\":508},{\"a\":\"MM\",\"n\":104},{\"a\":\"NA\",\"n\":516},{\"a\":\"NR\",\"n\":520},{\"a\":\"NP\",\"n\":524},{\"a\":\"NL\",\"n\":528},{\"a\":\"NC\",\"n\":540},{\"a\":\"NZ\",\"n\":554},{\"a\":\"NI\",\"n\":558},{\"a\":\"NE\",\"n\":562},{\"a\":\"NG\",\"n\":566},{\"a\":\"NU\",\"n\":570},{\"a\":\"NF\",\"n\":574},{\"a\":\"MP\",\"n\":580},{\"a\":\"NO\",\"n\":578},{\"a\":\"OM\",\"n\":512},{\"a\":\"PK\",\"n\":586},{\"a\":\"PW\",\"n\":585},{\"a\":\"PS\",\"n\":275},{\"a\":\"PA\",\"n\":591},{\"a\":\"PG\",\"n\":598},{\"a\":\"PY\",\"n\":600},{\"a\":\"PE\",\"n\":604},{\"a\":\"PH\",\"n\":608},{\"a\":\"PN\",\"n\":612},{\"a\":\"PL\",\"n\":616},{\"a\":\"PT\",\"n\":620},{\"a\":\"PR\",\"n\":630},{\"a\":\"QA\",\"n\":634},{\"a\":\"RE\",\"n\":638},{\"a\":\"RO\",\"n\":642},{\"a\":\"RU\",\"n\":643},{\"a\":\"RW\",\"n\":646},{\"a\":\"BL\",\"n\":652},{\"a\":\"SH\",\"n\":654},{\"a\":\"KN\",\"n\":659},{\"a\":\"LC\",\"n\":662},{\"a\":\"MF\",\"n\":663},{\"a\":\"PM\",\"n\":666},{\"a\":\"VC\",\"n\":670},{\"a\":\"WS\",\"n\":882},{\"a\":\"SM\",\"n\":674},{\"a\":\"ST\",\"n\":678},{\"a\":\"SA\",\"n\":682},{\"a\":\"SN\",\"n\":686},{\"a\":\"RS\",\"n\":688},{\"a\":\"SC\",\"n\":690},{\"a\":\"SL\",\"n\":694},{\"a\":\"SG\",\"n\":702},{\"a\":\"SX\",\"n\":534},{\"a\":\"SK\",\"n\":703},{\"a\":\"SI\",\"n\":705},{\"a\":\"SB\",\"n\":90},{\"a\":\"SO\",\"n\":706},{\"a\":\"ZA\",\"n\":710},{\"a\":\"GS\",\"n\":239},{\"a\":\"SS\",\"n\":728},{\"a\":\"ES\",\"n\":724},{\"a\":\"LK\",\"n\":144},{\"a\":\"SD\",\"n\":729},{\"a\":\"SR\",\"n\":740},{\"a\":\"SJ\",\"n\":744},{\"a\":\"SZ\",\"n\":748},{\"a\":\"SE\",\"n\":752},{\"a\":\"CH\",\"n\":756},{\"a\":\"SY\",\"n\":760},{\"a\":\"TW\",\"n\":158},{\"a\":\"TJ\",\"n\":762},{\"a\":\"TZ\",\"n\":834},{\"a\":\"TH\",\"n\":764},{\"a\":\"TL\",\"n\":626},{\"a\":\"TG\",\"n\":768},{\"a\":\"TK\",\"n\":772},{\"a\":\"TO\",\"n\":776},{\"a\":\"TT\",\"n\":780},{\"a\":\"TN\",\"n\":788},{\"a\":\"TR\",\"n\":792},{\"a\":\"TM\",\"n\":795},{\"a\":\"TC\",\"n\":796},{\"a\":\"TV\",\"n\":798},{\"a\":\"UG\",\"n\":800},{\"a\":\"UA\",\"n\":804},{\"a\":\"AE\",\"n\":784},{\"a\":\"GB\",\"n\":826},{\"a\":\"US\",\"n\":840},{\"a\":\"UM\",\"n\":581},{\"a\":\"UY\",\"n\":858},{\"a\":\"UZ\",\"n\":860},{\"a\":\"VU\",\"n\":548},{\"a\":\"VE\",\"n\":862},{\"a\":\"VN\",\"n\":704},{\"a\":\"VG\",\"n\":92},{\"a\":\"VI\",\"n\":850},{\"a\":\"WF\",\"n\":876},{\"a\":\"EH\",\"n\":732},{\"a\":\"YE\",\"n\":887},{\"a\":\"ZM\",\"n\":894},{\"a\":\"ZW\",\"n\":716}]";
        private static Lazy<Dictionary<int, string>> countryCodeByCountryNumber = new Lazy<Dictionary<int, string>>(() =>
        {
            return JsonConvert
                .DeserializeAnonymousType(countryNumbers, new[] { new { n = 0, a = "" } })
                .ToDictionary(x => x.n, x => x.a);
        });
        private string IsoNumberToCode(int? nr)
        {
            if (!nr.HasValue)
                return null;

            if (!countryCodeByCountryNumber.Value.ContainsKey(nr.Value))
                return "Other_" + nr.Value;
            else
                return countryCodeByCountryNumber.Value[nr.Value];
        }
        private string TranslateBricPaymentRemark(string b)
        {
            //We translate from numbers since their docs use both 1 -> 4 and 0 -> 3 while the wsdl uses 0 -> 3 with the same meaning making it super easy to introduce off by one bugs.
            //Risk for payment remarks: 0 = Small(less than 0, 15 %); 1 = Minor(less than 5 %); 2 = Normal(5 – 10 %); 3 = High(greater than 10 %)
            if (string.IsNullOrWhiteSpace(b))
                return null;

            switch (b[0])
            {
                case '0': return "Small";
                case '1': return "Minor";
                case '2': return "Normal";
                case '3': return "High";
                default: return "Unknown_" + b;
            }
        }

        public class Item
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        private static DateTime? ParseBisnodeDate(string date)
        {
            if (date == null)
                return null;
            //Bisnode has this superwierd idea where for some old addresses they will put dates like 19650700. Why they wouldn't just put 01 at the end instead is beyond me but anyway....
            if (date.EndsWith("00"))
                date = date.Substring(0, date.Length - 2) + "01";
            DateTime d;
            if (DateTime.TryParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return d;
            else
                return null;
        }

        public class Result
        {
            public bool IsError { get; set; }
            public bool IsInvalidCredentialsError { get; set; }
            public string ErrorMessage { get; set; }
            public List<Item> SuccessItems { get; set; }
        }

        private bool TryParseErrorableSection<T>(object section, StringBuilder errorMessage, Action<T> parseSection) where T : class
        {
            var errorSection = section as SoliditetFiWs.Virhe;
            if (errorSection != null)
            {
                errorMessage.AppendFormat($";{errorSection.Kuvaus ?? ""};{errorSection.Tyyppi ?? ""}");
                return false;
            }
            T sectionT = section as T;
            if (sectionT == null)
            {
                errorMessage.AppendFormat($"Section missing: {typeof(T).Name}");
                return false;
            }
            parseSection(sectionT);
            return true;
        }

        private bool IsPosteRestante(string streetAddress)
        {
            if (streetAddress == null)
                return false;
            return (streetAddress.Equals("Poste restante", StringComparison.OrdinalIgnoreCase));
        }

        public Result Parse(SoliditetFiWs.SoliditetHenkiloLuottoTiedotResponse r, bool isAddressOnlyRequest)
        {
            var items = new List<Item>();
            Action<string, string> add = (name, value) =>
            {
                if (!string.IsNullOrWhiteSpace(value))
                    items.Add(new Item { Name = name, Value = value });
            };
            Action<string, Func<string>> add2 = (name, valueFactory) =>
            {
                var value = valueFactory();
                if (!string.IsNullOrWhiteSpace(value))
                    items.Add(new Item { Name = name, Value = value });
            };

            //Check for errors
            bool isError = false;
            var errorMessage = new StringBuilder();
            if (r?.VastausLoki?.SyyKoodi != "1")
            {
                isError = true;
                errorMessage.Append(r?.VastausLoki?.PaluuKoodi?.Value);
            }

            isError = isError || !TryParseErrorableSection<SoliditetFiWs.HenkiloTiedot>(r?.HenkiloTiedotResponse?.Item, errorMessage, ht =>
            {
                add("firstName", ht?.Henkilo?.NykyisetEtunimet);
                add("lastName", ht?.Henkilo?.NykyinenSukunimi);
                add("bricRiskOfPaymentRemark", TranslateBricPaymentRemark(ht?.BRIC?.MaksuHairioRiski));

                var zipcode = ht?.Henkilo?.VakinainenOsoite?.Postinumero;
                var streetAddress = ht?.Henkilo?.VakinainenOsoite?.LahiosoiteS;

                var isBrokenAddress = string.IsNullOrWhiteSpace(zipcode);
                if (!isBrokenAddress)
                {
                    add("addressStreet", streetAddress);
                    add("addressZipcode", zipcode);
                    add("addressCity", ht?.Henkilo?.VakinainenOsoite?.PostitoimipaikkaS);
                    add("addressCountry", IsoNumberToCode(ht?.Henkilo?.VakinainenOsoite?.Valtiokoodi));
                    bool? hasDomesticAddress = null;
                    var cn = IsoNumberToCode(ht?.Henkilo?.VakinainenOsoite?.Valtiokoodi);

                    if (cn != null)
                        hasDomesticAddress = cn == "FI";
                    add("hasDomesticAddress", hasDomesticAddress.HasValue ? (hasDomesticAddress.Value ? "true" : "false") : "false");
                    add2("domesticAddressSinceDate", () =>
                    {
                        if (!hasDomesticAddress.HasValue || !hasDomesticAddress.Value)
                            return null;

                        var startDate = ht?.Henkilo?.VakinainenOsoite?.AsuminenAlkupvm;
                        var endDate = ht?.Henkilo?.VakinainenOsoite?.AsuminenLoppupvm;
                        if (string.IsNullOrWhiteSpace(startDate) || !string.IsNullOrWhiteSpace(endDate))
                            return null;

                        var d = ParseBisnodeDate(startDate);
                        if (d.HasValue)
                            return d.Value.ToString("yyyy-MM-dd");
                        else
                            return null;
                    });
                    var suomeenMuuttopvmRaw = ht?.Henkilo?.SuomeenMuuttopvm;
                    if (!string.IsNullOrWhiteSpace(suomeenMuuttopvmRaw))
                    {
                        DateTime d;
                        if (DateTime.TryParseExact(suomeenMuuttopvmRaw, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                        {
                            add("immigrationDate", d.ToString("yyyy-MM-dd"));
                        }
                    }
                }
                else
                {
                    add("hasDomesticAddress", "false");
                }

                var hasPosteRestanteAddress = (ht?.Henkilo?.PostiOsoite?.Any(x => IsPosteRestante(x.PostiosoiteS)) ?? false) || IsPosteRestante(streetAddress);
                add("hasPosteRestanteAddress", hasPosteRestanteAddress ? "true" : "false");

                var hasPostBoxAddress = ((zipcode ?? "").Trim().EndsWith("1"));
                add("hasPostBoxAddress", hasPostBoxAddress ? "true" : "false");

                if (!(ht?.Hakutiedot?.OnnistuikoHaku?.Koodi ?? "0").EndsWith("0"))
                {
                    add("personStatus", (ht?.Hakutiedot?.OnnistuikoHaku?.Koodi ?? "0").TrimStart('0') == "2" ? "deactivated" : "nodata");
                }
                else if (!string.IsNullOrWhiteSpace(ht?.Henkilo?.Kuolinpvm) || !string.IsNullOrWhiteSpace(ht?.Henkilo?.Kuolleeksijulistamispvm))
                {
                    add("personStatus", "dead");
                }
                else if (ht?.Henkilo?.Edunvalvonta == SoliditetFiWs.KyllaEiType1.k || ht?.Henkilo?.Edunvalvonta == SoliditetFiWs.KyllaEiType1.K)
                {
                    add("personStatus", "hasguardian");
                }
                else
                {
                    add("personStatus", "normal");
                }
            });

            if (!isAddressOnlyRequest)
            {

                isError = isError || !TryParseErrorableSection<SoliditetFiWs.LuottoTietoMerkinnat>(r?.LuottoTietoMerkinnatResponse?.Item, errorMessage, remarkSection =>
                {
                    int? nrOfPaymentRemarks = remarkSection?.MerkintojenLkm;
                    add("nrOfPaymentRemarks", nrOfPaymentRemarks.HasValue ? nrOfPaymentRemarks.Value.ToString() : null);
                    add("hasPaymentRemark", nrOfPaymentRemarks.HasValue ? (nrOfPaymentRemarks.Value > 0 ? "true" : "false") : null);
                });

                isError = isError || !TryParseErrorableSection<SoliditetFiWs.YritysYhteysTiedot>(r?.YritysYhteydetResponse?.Item, errorMessage, businessSection =>
                {
                    bool hasBusinessConnection = false;
                    if (businessSection.OnVastuuHenkilo == SoliditetFiWs.KyllaEiType1.k || businessSection.OnVastuuHenkilo == SoliditetFiWs.KyllaEiType1.K)
                        hasBusinessConnection = true;
                    else if (businessSection.VastuuHenkiloTieto != null && businessSection.VastuuHenkiloTieto.Length > 0)
                        hasBusinessConnection = true;
                    add("hasBusinessConnection", hasBusinessConnection ? "true" : "false");
                });
            }

            //TODO: This is not doable with encrypt by passphrase since it does max 8k. Find another solution
            //add("rawReport", SerializationUtil.Serialize(r).ToString());

            //TODO: isCitizen (replace with has immigrated. maybe from SuomeenMuuttopvm)

            return new Result
            {
                IsError = isError,
                IsInvalidCredentialsError = isError && (errorMessage?.ToString() ?? "").Contains("Tietoa ei voida toimittaa. Syy: ei palvelun käyttöoikeutta"),
                ErrorMessage = isError ? errorMessage?.ToString() : null,
                SuccessItems = isError ? null : items
            };
        }
    }
}