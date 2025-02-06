using NTech.Banking.BankAccounts.Se;
using NTech.Banking.IncomingPaymentFiles;
using NTech.Banking.OrganisationNumbers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace NTech.Banking.IncomingPaymentFiles
{
    public class IncomingPaymentFileFormat_BgMax : IIncomingPaymentFileFormat
    {
        private readonly bool isProduction;
        private readonly bool isStrict;

        public IncomingPaymentFileFormat_BgMax(bool isProduction, bool isStrict)
        {
            this.isProduction = isProduction;
            this.isStrict = isStrict;
        }

        public string FileFormatName
        {
            get
            {
                return "bgmax";
            }
        }

        public bool MightBeAValidFile(byte[] filedata)
        {
            RawFile _; string __;
            return RawFile.TryParse(new MemoryStream(filedata), out _, out __);
        }

        public bool TryParse<TIncomingPaymentFile>(byte[] filedata, out TIncomingPaymentFile paymentFile, out string errorMessage) where TIncomingPaymentFile : IncomingPaymentFile, new()
        {
            using (var stream = new MemoryStream(filedata))
            {
                RawFile rawFile;
                if (!RawFile.TryParse(stream, out rawFile, out errorMessage))
                {
                    paymentFile = null;
                    return false;
                }

                //NOTE: Uses the file specs notation there for instance 1-2 means the first two characters
                Func<string, int, int, string> getPos = (line, fromPos, toPos) =>
                    line.Length < toPos ? null : line.Substring(fromPos - 1, toPos - fromPos + 1);

                if (getPos(rawFile.tk01, 45, 45) == "T" && isProduction)
                    throw new Exception("This file is test flagged and cannot be imported to production!");

                var pf = new TIncomingPaymentFile
                {
                    Format = "BgMax",
                    ExternalCreationDate = DateTime.ParseExact(getPos(rawFile.tk01, 25, 38), "yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                    ExternalId = getPos(rawFile.tk01, 25, 44), //They dont have an id but this should be fine
                    Accounts = new List<IncomingPaymentFile.Account>()
                };

                //Our structure is (accountnr -> date - > payment while theirs is (accountnr, date) -> payment hence some remapping
                Func<IncomingPaymentFile.Account, IncomingPaymentFile.AccountDateBatch, IncomingPaymentFile.AccountDateBatch> getOrCreateAccountDateBatch = (newAccount, newDateBatch) =>
                {
                    var existingAccount = pf.Accounts.SingleOrDefault(x => x.AccountNr.NormalizedValue == newAccount.AccountNr.NormalizedValue);
                    if (existingAccount == null)
                    {
                        pf.Accounts.Add(newAccount);
                        existingAccount = newAccount;
                    }
                    var existingDateBatch = existingAccount.DateBatches.SingleOrDefault(x => x.BookKeepingDate == newDateBatch.BookKeepingDate);
                    if (existingDateBatch == null)
                    {
                        existingAccount.DateBatches.Add(newDateBatch);
                        existingDateBatch = newDateBatch;
                    }
                    return existingDateBatch;
                };

                foreach (var section in rawFile.sections)
                {
                    BankGiroNumberSe bg;
                    if (!BankGiroNumberSe.TryParse(getPos(section.tk05, 3, 12), out bg))
                    {
                        errorMessage = "Invalid bankgironumber";
                        paymentFile = null;
                        return false;
                    }

                    var dateBatch = getOrCreateAccountDateBatch(new IncomingPaymentFile.Account
                    {
                        AccountNr = new IncomingPaymentFile.BankAccountNr(bg),
                        Currency = getPos(section.tk05, 23, 25),
                        DateBatches = new List<IncomingPaymentFile.AccountDateBatch>()
                    }, new IncomingPaymentFile.AccountDateBatch
                    {
                        BookKeepingDate = DateTime.ParseExact(getPos(section.tk15, 38, 45), "yyyyMMdd", CultureInfo.InvariantCulture),
                        Payments = new List<IncomingPaymentFile.AccountDateBatchPayment>()
                    });

                    foreach (var payment in section.payments)
                    {
                        if (payment.tk20 != null)
                        {
                            BankGiroNumberSe payerBankgiroNr = null;
                            var payerBankgiroNrRaw = getPos(payment.tk20, 3, 12)?.TrimEnd();
                            if (payerBankgiroNrRaw.Length > 0 && !payerBankgiroNrRaw.All(x => x == '0'))
                            {
                                if (!BankGiroNumberSe.TryParse(payerBankgiroNrRaw, out payerBankgiroNr))
                                {
                                    errorMessage = "Invalid payer (tk20) bankgironumber";
                                    paymentFile = null;
                                    return false;
                                }
                            }

                            var ocr = getPos(payment.tk20, 13, 37)?.Trim()?.TrimStart('0');

                            var p = new IncomingPaymentFile.AccountDateBatchPayment
                            {
                                Amount = decimal.Parse(getPos(payment.tk20, 38, 53) + "." + getPos(payment.tk20, 54, 55), CultureInfo.InvariantCulture),
                                ExternalId = getPos(payment.tk20, 58, 69)?.Trim(),
                                OcrReference = string.IsNullOrWhiteSpace(ocr) ? null : ocr,
                                CustomerAccountNr = payerBankgiroNr != null ? new IncomingPaymentFile.BankAccountNr(payerBankgiroNr) : null
                            };

                            //Info texts
                            if (payment.tk25 != null && payment.tk25.Count > 0)
                            {
                                p.InformationText = string.Join(Environment.NewLine, payment.tk25.Select(x => getPos(x, 3, 52).Trim()));
                            }

                            //Payer name
                            if (payment.tk26 != null)
                            {
                                var payerName = (getPos(payment.tk26, 3, 37)?.Trim() + " " + getPos(payment.tk26, 38, 72)).Trim();
                                if (!string.IsNullOrWhiteSpace(payerName))
                                    p.CustomerName = payerName;
                            }

                            if (payment.tk27 != null)
                            {
                                var adr = getPos(payment.tk27, 3, 37)?.Trim();
                                var zip = getPos(payment.tk27, 38, 46)?.Trim();
                                if (!string.IsNullOrWhiteSpace(adr))
                                    p.CustomerAddressStreetName = adr;
                                if (!string.IsNullOrWhiteSpace(zip))
                                    p.CustomerAddressPostalCode = zip;
                            }

                            if (payment.tk28 != null)
                            {
                                var area = getPos(payment.tk28, 3, 37)?.Trim();
                                var countryCode = getPos(payment.tk28, 73, 74)?.Trim(); //Only if not SE
                                if (!string.IsNullOrWhiteSpace(area))
                                    p.CustomerAddressTownName = area;
                                if (!string.IsNullOrWhiteSpace(countryCode))
                                    p.CustomerAddressCountry = countryCode;
                            }

                            if (payment.tk29 != null)
                            {
                                var orgnr = getPos(payment.tk29, 3, 14)?.Trim()?.TrimStart('0');
                                if (!string.IsNullOrWhiteSpace(orgnr))
                                {
                                    OrganisationNumberSe orgnrParsed;
                                    if (!OrganisationNumberSe.TryParse(orgnr, out orgnrParsed))
                                    {
                                        errorMessage = "Invalid (tk29) orgnr";
                                        paymentFile = null;
                                        return false;
                                    }
                                    p.CustomerOrgnr = orgnrParsed;
                                }
                            }

                            dateBatch.Payments.Add(p);
                        }
                        else if (payment.tk21 != null)
                        {
                            //Avdrag. Not implemented
                            if (this.isStrict)
                            {
                                errorMessage = "tk21 is not supported";
                                paymentFile = null;
                                return false;
                            }
                        }
                    }
                }

                if (this.isStrict)
                {
                    var actualTotalCount = 0;
                    var actualSectionCount = 0;
                    var actualSectionSum = 0m;
                    foreach (var section in rawFile.sections)
                    {
                        actualSectionSum = 0m;
                        actualSectionCount = 0;
                        foreach (var payment in section.payments)
                        {
                            var paymentAmount = decimal.Parse(getPos(payment.tk20, 38, 53) + "." + getPos(payment.tk20, 54, 55), CultureInfo.InvariantCulture);
                            actualSectionCount += 1;
                            actualSectionSum += paymentAmount;
                            actualTotalCount += 1;
                        }
                        var sectionAmount = decimal.Parse(getPos(section.tk15, 51, 66) + "." + getPos(section.tk15, 67, 68), CultureInfo.InvariantCulture);
                        if (actualSectionSum != sectionAmount)
                        {
                            errorMessage = $"Section actually sums to {actualSectionSum.ToString(CultureInfo.InvariantCulture)} but tk15 says it should sum to {sectionAmount.ToString(CultureInfo.InvariantCulture)} which is not allowed in strict mode.";
                            paymentFile = null;
                            return false;
                        }
                        var sectionCount = int.Parse(getPos(section.tk15, 72, 79));
                        if (actualSectionCount != sectionCount)
                        {
                            errorMessage = $"Section actually has {actualSectionCount} payments but tk15 says it should have {sectionCount} which is not allowed in strict mode.";
                            paymentFile = null;
                            return false;
                        }
                    }
                    var totalCount = int.Parse(getPos(rawFile.tk70, 3, 10));
                    if (totalCount != actualTotalCount)
                    {
                        errorMessage = $"File actually has {actualTotalCount} payments but tk70 says it should have {totalCount} which is not allowed in strict mode.";
                        paymentFile = null;
                        return false;
                    }
                }
                errorMessage = null;
                paymentFile = pf;
                return true;
            }
        }

        public bool IsSupportedInCountry(string countryIsoCode)
        {
            return countryIsoCode == "SE";
        }

        public class RawFile
        {
            public string tk01; //Start
            public List<RawSection> sections = new List<RawSection>();
            public string tk70; //Slut

            public static bool TryParse(Stream file, out RawFile parsedFile, out string errorMessage)
            {
                /*
                 tk01
                 section+
                 tk70

                section
                   tk05
                   payment+
                   tk15

                payment
                   tk20 | tk21
                   (tk22|tk23)*
                   tk25*
                   tk26 (opt)
                   tk27 (opt)
                   tk28 (opt)
                   tk29 (opt)
                 */
                using (var sr = new StreamReader(file, Encoding.GetEncoding("iso-8859-1")))
                {
                    parsedFile = new RawFile();
                    RawSection s = null;
                    RawPayment p = null;

                    string line;
                    int lineNr = 0;
                    Func<string, string> err = msg => $"Problem on line {lineNr}: {msg}";

                    while ((line = sr.ReadLine()) != null)
                    {
                        lineNr++;

                        if (line.Trim().Length == 0)
                            continue;

                        if (line.Length < 2)
                        {
                            errorMessage = err("Line must be at least 2 chars long");
                            return false;
                        }

                        var prefix = line.Substring(0, 2);

                        if (prefix == "01")
                        {
                            if (parsedFile.tk01 != null)
                            {
                                errorMessage = err("tk01 occurs more than once");
                                return false;
                            }
                            parsedFile.tk01 = line;
                        }
                        else if (prefix == "70")
                        {
                            if (parsedFile.tk01 == null)
                            {
                                errorMessage = err("tk70 occurs out of order");
                                return false;
                            }
                            if (parsedFile.tk70 != null)
                            {
                                errorMessage = err("tk70 occurs more than once");
                                return false;
                            }
                            parsedFile.tk70 = line;
                        }
                        else if (prefix == "05")
                        {
                            if (s != null)
                            {
                                errorMessage = err("tk05 occurs in open section");
                                return false;
                            }
                            s = new RawSection
                            {
                                tk05 = line
                            };
                        }
                        else if (prefix == "15")
                        {
                            if (s == null)
                            {
                                errorMessage = err("tk15 occurs without an open section");
                                return false;
                            }
                            if (p != null)
                                s.payments.Add(p);
                            p = null;
                            s.tk15 = line;
                            parsedFile.sections.Add(s);
                            s = null;
                        }
                        else if (prefix == "20" || prefix == "21")
                        {
                            if (s == null)
                            {
                                errorMessage = err($"tk{prefix} occurs without and open section");
                                return false;
                            }
                            if (p != null)
                                s.payments.Add(p);
                            p = new RawPayment
                            {
                                tk20 = prefix == "20" ? line : null,
                                tk21 = prefix == "21" ? line : null,
                            };
                        }
                        else if (prefix == "26" || prefix == "27" || prefix == "28" || prefix == "29")
                        {
                            if (p == null)
                            {
                                errorMessage = err($"tk{prefix} occurs without and open payment");
                                return false;
                            }
                            if (prefix == "26")
                                p.tk26 = line;
                            if (prefix == "27")
                                p.tk27 = line;
                            if (prefix == "28")
                                p.tk28 = line;
                            if (prefix == "29")
                                p.tk29 = line;
                        }
                        else if (prefix == "22" || prefix == "23" || prefix == "25")
                        {
                            if (p == null)
                            {
                                errorMessage = err($"tk{prefix} occurs without and open payment");
                                return false;
                            }
                            if (prefix == "22")
                                p.tk22.Add(line);
                            if (prefix == "23")
                                p.tk23.Add(line);
                            if (prefix == "25")
                                p.tk25.Add(line);
                        }
                    }
                    errorMessage = null;
                    return true;
                }
            }
        }

        public class RawSection
        {
            public string tk05; //Öppning
            public List<RawPayment> payments = new List<RawPayment>();
            public string tk15; //Insättning
        }

        public class RawPayment
        {
            public string tk20; //betalning
            public string tk21; //avdrag
            public List<string> tk22 = new List<string>(); //extra referens
            public List<string> tk23 = new List<string>(); //extra referens
            public List<string> tk25 = new List<string>(); //information
            public string tk26; //namn
            public string tk27; //address 1
            public string tk28; //address 2
            public string tk29; //orgnr
        }
    }
}