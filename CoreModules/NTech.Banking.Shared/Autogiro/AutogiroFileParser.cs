using NTech.Banking.BankAccounts.Se;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.Autogiro
{
    public class AutogiroFileParser
    {
        public Dictionary<string, Row> Rows { get; set; } = new Dictionary<string, Row>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> IgnoredTransactionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public bool SkipUnknownTransactionCodes { get; set; }

        public static AutogiroFileParser NewParser(bool skipUnknownTransactionCodes)
        {
            return new AutogiroFileParser
            {
                SkipUnknownTransactionCodes = skipUnknownTransactionCodes,
                IgnoredTransactionCodes = new HashSet<string>()
            };
        }

        private Row NewRow(string transactionCode, bool isStartRow, bool isEndRow, List<string> nestedTransactionCodes = null)
        {
            var r = new Row
            {
                TransactionCode = transactionCode,
                Builder = this,
                IsStartRow = isStartRow,
                IsEndRow = isEndRow,
            };
            if (nestedTransactionCodes != null)
                nestedTransactionCodes.ForEach(x => r.NestedTransactionCodes.Add(x));                
            Rows[transactionCode] = r;
            return r.NewField("Transaktionskod", 1, 2);
        }

        public AutogiroFileParser IgnoreTransactionCodes(params string[] transactionCodes)
        {
            foreach (var t in transactionCodes)
                this.IgnoredTransactionCodes.Add(t);
            return this;
        }

        public Row NewHeaderRow(string transactionCode)
        {
            return NewRow(transactionCode, true, false);
        }
        
        public Row NewFooterRow(string transactionCode)
        {
            return NewRow(transactionCode, false, true);
        }

        public Row NewRepeatingRow(string transactionCode, List<string> nestedTransactionCodes = null)
        {
            return NewRow(transactionCode, false, false, nestedTransactionCodes: nestedTransactionCodes);
        }

        public ParsedFile Parse(Stream stream)
        {
            List<ParsedLine> items = new List<ParsedLine>();
            ParsedLine headerLine = null;
            ParsedLine footerLine = null;

            using (var r = new StreamReader(stream, Encoding.GetEncoding("iso-8859-1")))
            {
                string line;
                ParsedLine parentLine = null;
                while((line = r.ReadLine()) != null)
                {
                    if (line.Trim().Length == 0)
                        continue;

                    Func<int, int, string> get = (startPos, length) => line.Substring(startPos - 1, length);
                    line = (line ?? "").PadRight(80, ' ');
                    var transactionCode = get(1, 2);
                    if(!Rows.ContainsKey(transactionCode))
                    {
                        if (SkipUnknownTransactionCodes || IgnoredTransactionCodes.Contains(transactionCode))
                            continue;
                        else
                            throw new AutogiroParserException($"Encountered unknown transactioncode TK'{transactionCode}'");
                    }
                    var row = Rows[transactionCode];

                    var parsedLine = new ParsedLine
                    {
                        RawLine = line,
                        Row = row
                    };

                    if (row.NestedTransactionCodes.Any())
                    {
                        parentLine = parsedLine;
                    }

                    if (row.IsStartRow)
                    {
                        if (headerLine != null)
                            throw new AutogiroParserException("Encountered more than one header");
                        else
                            headerLine = parsedLine;
                    }
                    else if (row.IsEndRow)
                    {
                        if (footerLine != null)
                            throw new AutogiroParserException("Encountered more than one footer");
                        else
                            footerLine = parsedLine;
                    }
                    else
                    {
                        if (!parsedLine.Row.NestedTransactionCodes.Any() && parentLine != null)
                        {
                            if (parentLine.Row.NestedTransactionCodes.Contains(row.TransactionCode))
                                parentLine.NestedLines.Add(parsedLine);
                            else
                                throw new AutogiroParserException($"Found {row.TransactionCode} nested in {parentLine.Row.TransactionCode} which is not allowed");
                        }
                        else
                            items.Add(parsedLine);
                    }                        
                }
                if (Rows.Values.Any(x => x.IsStartRow) && headerLine == null)
                    throw new AutogiroParserException("Missing header");
                if (Rows.Values.Any(x => x.IsEndRow) && footerLine == null)
                    throw new AutogiroParserException("Missing footer");
            }
            
            return new ParsedFile
            {
                Header = headerLine,
                Items = items,
                Footer = footerLine
            };
        }

        public class ParsedFile
        {
            public ParsedLine Header { get; set; }
            public List<ParsedLine> Items{ get; set; }
            public ParsedLine Footer { get; set; }

        }
        
        public class ParsedLine
        {
            public string RawLine { get; set; }
            public Row Row { get; set; }
            public List<ParsedLine> NestedLines { get; set; } = new List<ParsedLine>();

            public string GetString(string name)
            {
                if (!Row.Fields.ContainsKey(name))
                    throw new AutogiroParserException($"No such field '{name}' in TK'{Row.TransactionCode}'");
                var f = Row.Fields[name];
                return RawLine.Substring(f.StartPosition - 1, f.Length)?.Trim();
            }

            public string GetNumberString(string name)
            {
                return GetString(name).TrimStart('0');
            }

            public decimal GetDecimal(string name)
            {
                var s = GetString(name);
                if (s.Length < 3)
                    throw new AutogiroParserException($"{name}: Decimal with length < 3 is impossible since the last two are decimals");
                var prefix = s.Substring(0, s.Length - 2).TrimStart('0');
                if (prefix.Length == 0)
                    prefix = "0";
                var suffix = s.Substring(s.Length - 2, 2);

                return decimal.Parse($"{prefix}.{suffix}", NumberStyles.Number, CultureInfo.InvariantCulture);
            }

            public int GetInt(string name)
            {
                var s = GetString(name).TrimStart('0');
                if (s.Length == 0)
                    s = "0";

                return int.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
            }

            public BankGiroNumberSe GetBankgiroNr(string name)
            {
                var n = GetString(name).TrimStart('0');
                BankGiroNumberSe b;
                string msg;
                if (!BankGiroNumberSe.TryParseWithErrorMessage(n, out b, out msg))
                    throw new AutogiroParserException($"{name}: Encountered invalid bankgironr");
                return b;
            }

            public DateTime GetDate(string name, bool isPrefix = false)
            {
                var n = GetString(name);
                if(n.Length == 8 || (isPrefix && n.Length > 8))
                {
                    DateTime d;
                    if (DateTime.TryParseExact(n.Substring(0, 8), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                        return d;
                }
                throw new AutogiroParserException($"{name}: Invalid date");
            }
        }

        public class Row
        {
            public AutogiroFileParser Builder { get; set; }
            public string TransactionCode { get; set; }
            public Dictionary<string, Field> Fields { get; set; } = new Dictionary<string, Field>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> NestedTransactionCodes { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            public bool IsStartRow { get; set; }
            public bool IsEndRow { get; set; }

            public Row NewField(string name, int startPosition, int length)
            {
                var f = new Field
                {
                    Name = name,
                    Length = length,
                    StartPosition = startPosition
                };
                Fields.Add(name, f);
                return this;
            }

            public AutogiroFileParser EndRow()
            {
                return Builder;
            }            
        }

        public class Field
        {
            public string Name { get; set; }
            public int StartPosition { get; set; }
            public int Length { get; set; }
        }

        public enum FieldTypeCode
        {
            Date,
            String
        }
    }

    public class AutogiroParserException : Exception
    {
        public AutogiroParserException() : base()
        {
        }

        public AutogiroParserException(string message) : base(message)
        {
        }

        public AutogiroParserException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AutogiroParserException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}
