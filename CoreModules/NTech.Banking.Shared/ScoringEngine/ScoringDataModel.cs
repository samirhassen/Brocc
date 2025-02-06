using System;
using System.Collections.Generic;
using System.Globalization;

namespace NTech.Banking.ScoringEngine
{
    public interface IScoringDataModelPopulator
    {
        ScoringDataModel Set(string name, string value, int? applicantNr);
        ScoringDataModel Set(string name, decimal? value, int? applicantNr);
        ScoringDataModel Set(string name, DateTime? value, int? applicantNr);
        ScoringDataModel Set(string name, int? value, int? applicantNr);
        ScoringDataModel Set(string name, bool? value, int? applicantNr);
    }

    public class ScoringDataModel : IScoringDataModelPopulator
    {
        //NOTE: These are public to make serialization less brittle since it works even if you dont do anything special
        public Dictionary<string, string> ApplicationItems = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<int, Dictionary<string, string>> ApplicantItems = new Dictionary<int, Dictionary<string, string>>();

        private Dictionary<string, string> GetDict(int? applicantNr)
        {
            if (applicantNr.HasValue)
            {
                if (!ApplicantItems.ContainsKey(applicantNr.Value))
                    ApplicantItems[applicantNr.Value] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                return ApplicantItems[applicantNr.Value];
            }
            else
                return ApplicationItems;
        }

        public ScoringDataModel Set(string name, string value, int? applicantNr)
        {
            var d = GetDict(applicantNr);
            if (string.IsNullOrWhiteSpace(value))
            {
                if (d.ContainsKey(name))
                    d.Remove(name);
            }
            else
                d[name] = value;

            return this;
        }

        public ScoringDataModel Set(string name, decimal? value, int? applicantNr)
        {
            return Set(name, value?.ToString(CultureInfo.InvariantCulture), applicantNr);
        }

        public ScoringDataModel Set(string name, DateTime? value, int? applicantNr)
        {
            return Set(name, value?.ToString("yyyy-MM-dd"), applicantNr);
        }

        public ScoringDataModel Set(string name, int? value, int? applicantNr)
        {
            return Set(name, value?.ToString(CultureInfo.InvariantCulture), applicantNr);
        }

        public ScoringDataModel Set(string name, bool? value, int? applicantNr)
        {
            return Set(name, value.HasValue ? (value.Value ? "true" : "false") : null, applicantNr);
        }

        public void Remove(string name, int? applicantNr = null)
        {
            var d = GetDict(applicantNr);
            if (d.ContainsKey(name))
                d.Remove(name);
        }

        public string GetString(string name, int? applicantNr)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", "name");
            var d = GetDict(applicantNr);
            string v = null;
            if (d.ContainsKey(name))
                v = d[name];
            if (string.IsNullOrWhiteSpace(v))
                return null;
            return v?.Trim();
        }

        public decimal? GetDecimal(string name, int? applicantNr)
        {
            return ParseDecimal(GetString(name, applicantNr));
        }

        public int? GetInt(string name, int? applicantNr)
        {
            return ParseInt(GetString(name, applicantNr));
        }

        public bool? GetBool(string name, int? applicantNr)
        {
            return ParseBool(GetString(name, applicantNr));
        }


        private static decimal? ParseDecimal(string v)
        {
            decimal d;
            if (string.IsNullOrWhiteSpace(v))
                return null;
            if (decimal.TryParse(v, NumberStyles.Integer | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out d))
                return d;
            else
                return null;
        }

        private static int? ParseInt(string v)
        {
            int d;
            if (string.IsNullOrWhiteSpace(v))
                return null;
            if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out d))
                return d;
            else
                return null;
        }

        private static bool? ParseBool(string v)
        {
            if (string.IsNullOrWhiteSpace(v))
                return null;
            return (v ?? "").ToLowerInvariant() == "true";
        }

        private static string MissingMsg(string name, int? applicantNr)
        {
            return $"Missing '{name}'" + (applicantNr.HasValue ? $" for applicant {applicantNr.Value}" : "");
        }

        public string GetStringRequired(string name, int? applicantNr)
        {
            var v = GetString(name, applicantNr);
            if (v == null)
                throw new ScoringDataModelException(MissingMsg(name, applicantNr)) { IsMissingItemError = true, MissingItemName = name };
            return v;
        }

        private T Req<T>(Func<string, int?, T?> f, string name, int? applicantNr) where T : struct
        {
            var value = f(name, applicantNr);
            if (!value.HasValue)
                throw new ScoringDataModelException(MissingMsg(name, applicantNr)) { IsMissingItemError = true, MissingItemName = name };
            return value.Value;
        }

        public decimal GetDecimalRequired(string name, int? applicantNr)
        {
            return Req(GetDecimal, name, applicantNr);
        }

        public int GetIntRequired(string name, int? applicantNr)
        {
            return Req(GetInt, name, applicantNr);
        }

        public bool GetBoolRequired(string name, int? applicantNr)
        {
            return Req(GetBool, name, applicantNr);
        }

        public bool Exists(string name, int? applicantNr)
        {
            return GetString(name, applicantNr) != null;
        }

        /// <summary>
        /// Merge another model into this one
        /// </summary>
        public void AddDataFromOtherModel(ScoringDataModel other)
        {
            if (other == null)
                return;
            foreach (var k1 in other.ApplicationItems)
                ApplicationItems[k1.Key] = k1.Value;
            foreach (var k2 in other.ApplicantItems)
            {
                if (!ApplicantItems.ContainsKey(k2.Key))
                {
                    ApplicantItems.Add(k2.Key, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
                }
                foreach (var k2i in k2.Value)
                {
                    ApplicantItems[k2.Key][k2i.Key] = k2i.Value;
                }
            }
        }

        public ScoringDataModel Copy()
        {
            var m = new ScoringDataModel();
            m.AddDataFromOtherModel(this);
            return m;
        }
    }

    public class ScoringDataModelException : Exception
    {
        public ScoringDataModelException() : base()
        {
        }

        public ScoringDataModelException(string message) : base(message)
        {
        }

        public ScoringDataModelException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public bool IsMissingItemError { get; set; }
        public string MissingItemName { get; set; }
    }

    /// <summary>
    /// ScoringDataModel with a simpler serialization format that is easier to work with in js and works better with automatic service documentation
    /// </summary>
    public class ScoringDataModelFlat
    {
        public List<Item> Items { get; set; }

        public static ScoringDataModelFlat FromModel(ScoringDataModel m)
        {
            if (m == null)
                return null;

            var f = new ScoringDataModelFlat();
            f.Items = new List<Item>();
            
            if(m.ApplicationItems != null)
            {
                foreach (var i in m.ApplicationItems)
                {
                    f.Items.Add(new Item { Name = i.Key, Value = i.Value, ApplicantNr = null });
                }
            }
            if(m.ApplicantItems != null)
            {
                foreach(var applicantNr in m.ApplicantItems.Keys)
                {
                    foreach(var i in m.ApplicantItems[applicantNr])
                    {
                        f.Items.Add(new Item { ApplicantNr = applicantNr, Name = i.Key, Value = i.Value });
                    }
                }
            }
            return f;
        }

        public ScoringDataModel ToModel()
        {
            var m = new ScoringDataModel();
            if (Items == null)
                return m;

            foreach(var i in Items)
            {
                m.Set(i.Name, i.Value, i.ApplicantNr);
            }

            return m;
        }

        public class Item
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public int? ApplicantNr { get; set; }
        }
    }
}