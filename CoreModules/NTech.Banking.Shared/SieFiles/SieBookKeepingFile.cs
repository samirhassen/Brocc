using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace NTech.Banking.SieFiles
{
    public class SieBookKeepingFile
    {
        private readonly Func<DateTime> now;

        public SieBookKeepingFile(Func<DateTime> now)
        {
            Verifications = new List<Verification>();
            Objects = new List<AccountingObject>();
            Dimensions = new List<Dimension>();
            this.now = now;
        }

        //Reserverade dimensioner
        //1 = Kostnadsställe / resultatenhet.
        //2 = Kostnadsbärare (skall vara underdimension till 1).
        //3-5 = Reserverade för framtida utökning av standarden.
        //6 = Projekt.
        //7 = Anställd.
        //8 = Kund.
        //9 = Leverantör.
        //10 = Faktura.
        //11-19 = Reserverade för framtida utökning av standarden.
        //20- = Fritt disponibla.

        public TransactionPairBuilder WithTransactionPair(decimal amount, Tuple<string, string> accountNrs)
        {
            return new TransactionPairBuilder
            {
                Debet = new Transaction
                {
                    Account = accountNrs.Item1,
                    Amount = amount,
                    ObjectNrsByDimensionNr = new Dictionary<int, string>()
                },
                Credit = new Transaction
                {
                    Account = accountNrs.Item2,
                    Amount = -amount,
                    ObjectNrsByDimensionNr = new Dictionary<int, string>()
                },
                File = this
            };
        }

        public class Verification
        {
            public Verification()
            {
                Transactions = new List<Transaction>();
            }

            public string Text { get; set; }

            public DateTime? Date { get; set; }
            public DateTime? RegistrationDate { get; set; }

            public IList<Transaction> Transactions { get; set; }

            internal SieBookKeepingFile File { get; set; }

            public Transaction MergeTransaction(Transaction t)
            {
                if (Transactions == null)
                    Transactions = new List<Transaction>();
                var mergeTargetTransaction = Transactions.Where(x => x.CanBeMergedWith(t)).FirstOrDefault();
                if (mergeTargetTransaction != null)
                {
                    mergeTargetTransaction.Merge(t);
                    return mergeTargetTransaction;
                }
                else
                {
                    Transactions.Add(t);
                    return t;
                }
            }
        }

        public class TransactionPairBuilder
        {
            public Transaction Debet { get; set; }
            public Transaction Credit { get; set; }

            internal SieBookKeepingFile File { get; set; }

            /// <param name="dimensionText">Something like '{ 1 "40" 2 "114" }'</param>
            /// <returns></returns>
            public TransactionPairBuilder HavingDimensionRaw(string dimensionText)
            {
                Debet.DimensionRaw = dimensionText;
                Credit.DimensionRaw = dimensionText;
                return this;
            }

            public TransactionPairBuilder HavingCostPlaceDimension(string objectNr, string objectName)
            {
                return HavingDimension(1, "Kostnadsställe", objectNr, objectName);
            }

            public TransactionPairBuilder HavingDimension(int dimensionNr, string dimensionName, string objectNr, string objectName)
            {
                if (File.Dimensions == null)
                    File.Dimensions = new List<Dimension>();
                if (File.Objects == null)
                    File.Objects = new List<AccountingObject>();

                if (!File.Dimensions.Any(x => x.Nr == dimensionNr))
                    File.Dimensions.Add(new Dimension { Nr = dimensionNr, Name = dimensionName });

                if (!File.Objects.Any(x => x.ObjectNr == objectNr))
                    File.Objects.Add(new AccountingObject { DimensionNr = dimensionNr, ObjectNr = objectNr, Name = objectName });

                Debet.ObjectNrsByDimensionNr[dimensionNr] = objectNr;
                Credit.ObjectNrsByDimensionNr[dimensionNr] = objectNr;

                return this;
            }

            public void MergeIntoVerification(Verification v)
            {
                v.MergeTransaction(Debet);
                v.MergeTransaction(Credit);
            }
        }

        public class Transaction
        {
            public Transaction()
            {
                ObjectNrsByDimensionNr = new Dictionary<int, string>();
            }

            public decimal Amount { get; set; }
            public string Account { get; set; }
            public string DimensionRaw { get; set; }
            public Dictionary<int, string> ObjectNrsByDimensionNr { get; set; }

            public bool CanBeMergedWith(Transaction tr)
            {
                if (tr.Account != Account)
                    return false;
                if (DimensionRaw != null)
                {
                    if (tr.ObjectNrsByDimensionNr != null && tr.ObjectNrsByDimensionNr.Count > 0)
                        return false;
                    else if (tr.DimensionRaw != DimensionRaw)
                        return false;
                }
                else
                {
                    foreach (var k1 in ObjectNrsByDimensionNr.Keys)
                    {
                        if (!tr.ObjectNrsByDimensionNr.ContainsKey(k1) || tr.ObjectNrsByDimensionNr[k1] != ObjectNrsByDimensionNr[k1])
                            return false;
                    }
                    foreach (var k2 in tr.ObjectNrsByDimensionNr.Keys)
                    {
                        if (!ObjectNrsByDimensionNr.ContainsKey(k2) || tr.ObjectNrsByDimensionNr[k2] != ObjectNrsByDimensionNr[k2])
                            return false;
                    }
                }

                return true;
            }

            public void Merge(Transaction t)
            {
                Amount += t.Amount;
            }
        }

        public string ProgramName { get; set; }
        public Tuple<int, int> ProgramVersion { get; set; }
        public string ExportedCompanyName { get; set; }
        public IList<Verification> Verifications { get; set; }
        public IList<AccountingObject> Objects { get; set; }

        public IList<Dimension> Dimensions { get; set; }
        public string DimensionsDeclarationRaw { get; set; }

        public class Dimension
        {
            public int Nr { get; set; }

            public string Name { get; set; }
        }

        public class AccountingObject
        {
            public string ObjectNr { get; set; }
            public int DimensionNr { get; set; }
            public string Name { get; set; }
        }

        private string Clean(string s)
        {
            if (s == null)
                return null;
            return s.Replace("\n", "").Replace("\"", " ");
        }

        private void CheckIntegrity()
        {
            if (ExportedCompanyName == null)
                throw new Exception("Missing ExportedCompanyName");
            if (Verifications == null || Verifications.Count == 0)
                throw new Exception("Missing verifications"); //The SIE standard requires at least one

            if (ProgramName == null || ProgramVersion == null)
                throw new Exception("ProgramName or ProgramVersion missing");

            if (DimensionsDeclarationRaw != null)
            {
                if ((Dimensions != null && Dimensions.Count > 0) || (Objects != null && Objects.Count > 0))
                    throw new Exception("DimensionsDeclarationRaw cannot be combined with Dimensions or Objects");
                if (this.Verifications.Any(x => x.Transactions.Any(y => y.ObjectNrsByDimensionNr != null && y.ObjectNrsByDimensionNr.Count > 0)))
                {
                    throw new Exception("ObjectNrsByDimensionNr cannot be used when using RawDimensions");
                }
            }
            else
            {
                var ds = Dimensions == null ? new List<Dimension>() : Dimensions;
                var undeclaredDimensions = Verifications
                    .SelectMany(x => x
                        .Transactions
                        .Where(y => y.ObjectNrsByDimensionNr != null)
                        .SelectMany(y => y.ObjectNrsByDimensionNr))
                    .Select(x => x.Key).ToList().Except(ds.Select(x => x.Nr));
                if (undeclaredDimensions.Any())
                {
                    throw new Exception("Undeclared dimensions: " + string.Join(", ", undeclaredDimensions.Select(x => x.ToString())));
                }

                var os = Objects == null ? new List<AccountingObject>() : Objects;
                var undeclaredObjects = Verifications
                    .SelectMany(x => x
                        .Transactions
                        .Where(y => y.ObjectNrsByDimensionNr != null)
                        .SelectMany(y => y.ObjectNrsByDimensionNr))
                    .Select(x => x.Value).ToList().Except(os.Select(x => x.ObjectNr));
                if (undeclaredObjects.Any())
                {
                    throw new Exception("Undeclared objects: " + string.Join(", ", undeclaredObjects.Select(x => x.ToString())));
                }
            }

            foreach (var ver in Verifications)
            {
                if (ver.Text == null || !ver.Date.HasValue)
                    throw new Exception("Text or Date missing on verification");
                if (ver.Transactions == null || ver.Transactions.Count == 0)
                    throw new Exception("The verification has no transactions");

                foreach (var t in ver.Transactions)
                {
                    if (t.Account == null)
                        throw new Exception("Missing Account on transaction");
                }
                if (ver.Transactions.Sum(x => x.Amount) != 0m)
                    throw new Exception("The verification is not balanced");
            }
        }

        private StringBuilder CreateSieContent()
        {
            CheckIntegrity();

            var b = new StringBuilder();

            Action<string, string> addLine = (name, content) =>
                {
                    b.Append($"#{name} {content}").AppendLine();
                };

            addLine("FLAGGA", "0");

            addLine("PROGRAM", $"\"{Clean(ProgramName)}\" {ProgramVersion.Item1}.{ProgramVersion.Item2}");

            addLine("FORMAT", "PC8");

            addLine("GEN", $"{now().ToString("yyyyMMdd")}");

            addLine("SIETYP", "4");

            addLine("FNAMN", $"\"{Clean(ExportedCompanyName)}\"");

            if (DimensionsDeclarationRaw != null)
            {
                b.AppendLine(DimensionsDeclarationRaw);
            }

            if (Dimensions != null)
            {
                foreach (var d in Dimensions)
                {
                    addLine("DIM", $"{d.Nr.ToString()} \"{Clean(d.Name)}\"");
                }
            }

            if (Objects != null)
            {
                foreach (var o in Objects)
                {
                    addLine("OBJEKT", $"{o.DimensionNr.ToString()} \"{Clean(o.ObjectNr)}\" \"{Clean(o.Name)}\"");
                }
            }

            foreach (var ver in Verifications)
            {
                addLine("VER", $"\"\" \"\" {FormatDate(ver.Date.Value)} \"{ver.Text}\"{(ver.RegistrationDate.HasValue ? " " + FormatDate(ver.RegistrationDate.Value) : "")}");
                b.Append("{").AppendLine();
                foreach (var t in ver.Transactions)
                {
                    string dims;
                    if (t.DimensionRaw != null)
                    {
                        dims = $" {t.DimensionRaw} ";
                    }
                    else
                    {
                        dims = t.ObjectNrsByDimensionNr == null || t.ObjectNrsByDimensionNr.Count == 0
                            ? " {} "
                            : " {" + string.Join(" ", t.ObjectNrsByDimensionNr.Select(x => $"\"{x.Key.ToString()}\" \"{Clean(x.Value)}\"")) + "} ";
                    }

                    b.AppendLine($"    #TRANS {t.Account}{dims}{t.Amount.ToString(CultureInfo.InvariantCulture)}");
                }
                b.Append("}").AppendLine();
            }

            return b;
        }

        public Verification CreateVerification(DateTime date, string text = null, DateTime? registrationDate = null)
        {
            var v = new Verification
            {
                Date = date,
                Text = text,
                File = this,
                RegistrationDate = registrationDate
            };
            return v;
        }

        public void AddVerification(Verification v)
        {
            if (Verifications == null)
                Verifications = new List<Verification>();
            Verifications.Add(v);
        }

        public void Save(Stream target)
        {
            var b = CreateSieContent();
            var bytes = Encoding.GetEncoding(437).GetBytes(b.ToString());
            target.Write(bytes, 0, bytes.Length);
        }

        public void Save(string filename, bool allowOverwrite = false)
        {
            using (var fs = new FileStream(filename, allowOverwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                Save(fs);
            }
        }

        public static string FormatDate(DateTime d)
        {
            return d.ToString("yyyyMMdd");
        }
    }
}