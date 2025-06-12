using NTech.Banking.CivicRegNumbers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace nSavings.Code.Riksgalden
{
    public class RiksgaldenFileBuilder<T>
    {
        private readonly List<Column> columns = new List<Column>();

        private static System.Globalization.CultureInfo FormattingCulture =
            System.Globalization.CultureInfo.GetCultureInfo("sv-SE");

        private readonly IList<T> items;

        private RiksgaldenFileBuilder(IList<T> items)
        {
            this.items = items;
        }

        public static RiksgaldenFileBuilder<T> Begin(IList<T> items)
        {
            return new RiksgaldenFileBuilder<T>(items);
        }

        public RiksgaldenFileBuilder<T> AddStringColumn(string name, bool isRequired, int maxLength,
            Func<T, string> getValue)
        {
            columns.Add(new Column
            {
                Name = name,
                IsRequired = isRequired,
                MaxLength = maxLength,
                GetValue = x =>
                {
                    var v = getValue(x);
                    return string.IsNullOrWhiteSpace(v) ? null : v?.Trim();
                }
            });
            return this;
        }

        public RiksgaldenFileBuilder<T> AddDecimalColumn(string name, bool isRequired, int maxLength,
            Func<T, decimal?> getValue, int? fixedDecimals = null)
        {
            return AddStringColumn(name, isRequired, maxLength,
                x => fixedDecimals.HasValue
                    ? getValue(x)?.ToString($"f{fixedDecimals.Value}", FormattingCulture)
                    : getValue(x)?.ToString(FormattingCulture)
            );
        }

        public RiksgaldenFileBuilder<T> AddDateAndTimeColumn(string name, bool isRequired, Func<T, DateTime?> getValue)
        {
            return AddStringColumn(name, isRequired, 20, x => getValue(x)?.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        public RiksgaldenFileBuilder<T> AddCivicRegNrColumn(string name, bool isRequired,
            Func<T, ICivicRegNumber> getValue)
        {
            return AddStringColumn(name, isRequired, 50, x =>
            {
                var v = getValue(x);
                if (v == null)
                    return null;
                return v.Country == "SE" ? v.NormalizedValue : $"{v.Country}{v.NormalizedValue}";
            });
        }

        private class Column
        {
            public Func<T, string> GetValue { get; set; }
            public bool IsRequired { get; set; }
            public int MaxLength { get; set; }
            public string Name { get; set; }
        }

        private void WriteItem(StreamWriter w, T item)
        {
            var values = columns.Select(c =>
            {
                var v = c.GetValue(item);
                if (c.IsRequired && string.IsNullOrWhiteSpace(v))
                    throw new Exception($"Missing required value {c.Name}");
                if (c.MaxLength > 0 && (v?.Length ?? 0) > c.MaxLength)
                    throw new Exception($"{c.Name} has maxlength {c.MaxLength} but a give value had length {v.Length}");
                return v;
            });
            w.WriteLine(string.Join("|", values));
        }

        public void WriteFileToStream(DateTime writeTime, string instituteName, string instituteOrgnr, Stream target,
            bool? isFirstFilialBlock = null)
        {
            if (string.IsNullOrWhiteSpace(instituteName))
                throw new Exception("Missing instituteName");
            if (string.IsNullOrWhiteSpace(instituteOrgnr))
                throw new Exception("Missing instituteOrgnr");

            using (var w = new StreamWriter(target, Encoding.GetEncoding("Windows-1252"), 4096, true))
            {
                if (!isFirstFilialBlock.HasValue || isFirstFilialBlock.GetValueOrDefault())
                {
                    var firstLinePrefix = $"#!3|ÅÄÖåäö|{writeTime:yyyy-MM-dd HH:mm:ss}|{instituteOrgnr}|";
                    if (firstLinePrefix.Length + instituteName.Length > 160)
                        instituteName = instituteName.Substring(0, 160 - firstLinePrefix.Length);
                    w.WriteLine(firstLinePrefix + instituteName);
                }

                if (!isFirstFilialBlock.HasValue)
                {
                    w.WriteLine(string.Join("|", columns.Select(x => x.Name)));
                }

                foreach (var item in items)
                {
                    WriteItem(w, item);
                }

                w.Flush();
            }
        }
    }
}