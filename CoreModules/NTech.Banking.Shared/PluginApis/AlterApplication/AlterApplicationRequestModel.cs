using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.PluginApis.AlterApplication
{
    public class AlterApplicationRequestModel
    {
        public string ApplicationNr { get; set; }
        public HashSet<int> EnsureConnectedCustomerIds { get; set; } = new HashSet<int>();
        public List<Item> ApplicationItems { get; set; } = new List<Item>();
        public List<ComplexItem> ComplexApplicationItems { get; set; } = new List<ComplexItem>();
        public ApplicationCommentModel ApplicationComment { get; set; }
        public AdditionalQuestionsDocumentModel AdditionalQuestionsDocument { get; set; }
        public DateTimeOffset? ChangeDate { get; set; }

        public void AddApplicationItem(string name, string value, string groupName = null, int? applicantNr = null, bool isEncrypted = false)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            string gn;
            if (groupName != null && applicantNr.HasValue)
            {
                if (Char.IsDigit(groupName.Last()))
                    throw new Exception("Groupname cannot end with a digit when combined with applicantNr");
                gn = $"{groupName}{applicantNr.Value}";
            }
            else if (applicantNr.HasValue)
                gn = $"applicant{applicantNr.Value}";
            else
                gn = groupName ?? "application";

            if (ApplicationItems.Any(x => x.GroupName.Equals(gn, StringComparison.OrdinalIgnoreCase) && x.ItemName.Equals(name, StringComparison.OrdinalIgnoreCase)))
                throw new Exception($"Duplicate application item: {gn}.{name}");

            ApplicationItems.Add(new Item { GroupName = gn, ItemName = name, Value = value, IsEncrypted = isEncrypted });
        }

        public void AddComplexApplicationItem(string listName, int nr, Dictionary<string, string> uniqueValues, Dictionary<string, List<string>> repeatingValues)
        {
            ComplexApplicationItems.Add(new ComplexItem
            {
                ListName = listName,
                Nr = nr,
                RepeatingValues = repeatingValues?.ToDictionary(
                    x => x.Key,
                    x => x.Value?.Where(y => !string.IsNullOrWhiteSpace(y))?.Select(y => y?.Trim())?.ToList() ?? new List<string>()),
                UniqueValues = uniqueValues?.Where(x => !string.IsNullOrWhiteSpace(x.Value))?.ToDictionary(x => x.Key, x => x.Value?.Trim())
            });
        }

        public void SetComment(string text, string customerIpAddress = null)
        {
            ApplicationComment = new ApplicationCommentModel
            {
                CustomerIpAddress = customerIpAddress,
                Text = text?.Trim()
            };
        }

        public class Item
        {
            public string GroupName { get; set; }
            public string ItemName { get; set; }
            public string Value { get; set; }
            public bool IsEncrypted { get; set; }
        }

        public class ComplexItem
        {
            public string ListName { get; set; }
            public int Nr { get; set; }
            public Dictionary<string, string> UniqueValues { get; set; }
            public Dictionary<string, List<string>> RepeatingValues { get; set; }
        }

        public class ApplicationCommentModel
        {
            public string Text { get; set; }
            public string CustomerIpAddress { get; set; }
        }
    }
}