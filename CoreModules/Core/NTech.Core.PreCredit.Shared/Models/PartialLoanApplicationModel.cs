using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace nPreCredit
{
    public class PartialCreditApplicationModel
    {
        public class ApplicationItem
        {
            public string GroupName { get; set; }
            public string ItemName { get; set; }

            public string ItemValue { get; set; }

            public DateTimeOffset? ChangedDate { get; set; }
            public int? ChangedById { get; set; }
        }

        public class GroupSource
        {
            private readonly ISet<string> requestedFieldNames;
            private readonly PartialCreditApplicationModel model;
            private readonly string groupName;

            public GroupSource(PartialCreditApplicationModel model, string groupName, ISet<string> requestedFieldNames)
            {
                this.model = model;
                this.requestedFieldNames = requestedFieldNames;
                this.groupName = groupName;
            }

            public StringItem Get(string itemName)
            {
                if (requestedFieldNames != null && !requestedFieldNames.Contains(itemName))
                    throw new Exception($"{groupName}.{itemName} that field was never requested");
                return model.Get(groupName, itemName);
            }
        }

        private readonly List<ApplicationItem> items;
        private int nrOfApplicants;

        public int NrOfApplicants
        {
            get
            {
                return this.nrOfApplicants;
            }
        }

        public void RemoveIfExists(string groupName, string itemName)
        {
            var index = items.FindIndex(x => x.GroupName == groupName && x.ItemName == itemName);
            if (index >= 0)
                items.RemoveAt(index);
        }

        private ISet<string> requestedApplicationFields = null;
        private ISet<string> requestedApplicantFields = null;
        private ISet<string> requestedDocumentFields = null;
        private ISet<string> requestedQuestionFields = null;
        private ISet<string> requestedCreditreportFields = null;
        private ISet<string> requestedExternalFields = null;
        private readonly bool wasChangeDataLoaded;

        public PartialCreditApplicationModel(int nrOfApplicants,
            List<ApplicationItem> items = null,
            ISet<string> requestedApplicationFields = null,
            ISet<string> requestedApplicantFields = null,
            ISet<string> requestedDocumentFields = null,
            ISet<string> requestedQuestionFields = null,
            ISet<string> requestedCreditreportFields = null,
            ISet<string> requestedExternalFields = null,
            bool wasChangeDataLoaded = false)
        {
            this.nrOfApplicants = nrOfApplicants;
            this.items = items ?? new List<ApplicationItem>();
            this.requestedApplicationFields = requestedApplicationFields;
            this.requestedApplicantFields = requestedApplicantFields;
            this.requestedDocumentFields = requestedDocumentFields;
            this.requestedQuestionFields = requestedQuestionFields;
            this.requestedCreditreportFields = requestedCreditreportFields;
            this.requestedExternalFields = requestedExternalFields;
            this.wasChangeDataLoaded = wasChangeDataLoaded;
        }

        public GroupSource Applicant(int applicantNr)
        {
            if (applicantNr < 1 || applicantNr > nrOfApplicants)
                throw new ArgumentException("Invalid applicantNr", "applicantNr");

            return new GroupSource(this, $"applicant{applicantNr}", this.requestedApplicantFields);
        }

        public void DoForEachApplicant(Action<int> doUsingApplicantNr)
        {
            foreach (var applicantNr in Enumerable.Range(1, NrOfApplicants))
            {
                doUsingApplicantNr(applicantNr);
            }
        }

        public GroupSource Question(int applicantNr)
        {
            if (applicantNr < 1 || applicantNr > nrOfApplicants)
                throw new ArgumentException("Invalid applicantNr", "applicantNr");

            return new GroupSource(this, $"question{applicantNr}", this.requestedQuestionFields);
        }

        public GroupSource Document(int applicantNr)
        {
            if (applicantNr < 1 || applicantNr > nrOfApplicants)
                throw new ArgumentException("Invalid applicantNr", "applicantNr");

            return new GroupSource(this, $"document{applicantNr}", this.requestedDocumentFields);
        }

        public GroupSource Creditreport(int applicantNr)
        {
            if (applicantNr < 1 || applicantNr > nrOfApplicants)
                throw new ArgumentException("Invalid applicantNr", "applicantNr");

            return new GroupSource(this, $"creditreport{applicantNr}", this.requestedCreditreportFields);
        }

        public GroupSource Application
        {
            get
            {
                return new GroupSource(this, "application", this.requestedApplicationFields);
            }
        }

        public GroupSource External
        {
            get
            {
                return new GroupSource(this, "external", this.requestedExternalFields);
            }
        }

        private StringItem Get(string groupName, string itemName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                throw new ArgumentException("GroupName cannot be empty", "groupName");
            if (string.IsNullOrWhiteSpace(itemName))
                throw new ArgumentException("ItemName cannot be empty", "itemName");

            try
            {
                var v = items.SingleOrDefault(x =>
                    x?.GroupName?.ToLowerInvariant() == groupName?.ToLowerInvariant()
                    && x?.ItemName?.ToLowerInvariant() == itemName?.ToLowerInvariant());

                Action<string> set = newValue =>
                {
                    var item = items.SingleOrDefault(x =>
                        x?.GroupName?.ToLowerInvariant() == groupName?.ToLowerInvariant()
                        && x?.ItemName?.ToLowerInvariant() == itemName?.ToLowerInvariant());

                    if (item != null)
                        item.ItemValue = newValue;
                    else
                        items.Add(new ApplicationItem { GroupName = groupName, ItemName = itemName, ItemValue = newValue });
                };

                Tuple<DateTimeOffset?, int?> changedByAndWhen = null;
                if (wasChangeDataLoaded)
                    changedByAndWhen = Tuple.Create(v?.ChangedDate, v?.ChangedById);
                return new StringItem(v?.ItemValue, $"{groupName}.{itemName}", set, changedByAndWhen);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in PartialLoanModel.Get({groupName}.{itemName})", ex);
            }
        }

        public string ToJson()
        {
            var r = ToGroupedItems();
            var e = new ExpandoObject();
            IDictionary<string, object> ed = e;

            ed["nrOfApplicants"] = r.NrOfApplicants;

            foreach (var groupName in r.GroupedItems.Keys)
            {
                var g = new ExpandoObject();
                IDictionary<string, object> gd = g;
                foreach (var item in r.GroupedItems[groupName])
                {
                    gd[item.Key] = item.Value;
                }
                ed[groupName] = g;
            }

            return JsonConvert.SerializeObject(e);
        }

        public GroupedItemPartialCreditApplicationModel ToGroupedItems()
        {
            var groupedItems = new Dictionary<string, Dictionary<string, string>>();

            List<string> groupNames = Enumerable
                .Range(1, NrOfApplicants)
                .Select(x => $"applicant{x}")
                .Concat(new string[] { "application" })
                .ToList();

            foreach (var groupName in groupNames)
            {
                var g = new Dictionary<string, string>();
                foreach (var item in items.Where(x => x.GroupName == groupName))
                {
                    g[item.ItemName] = item.ItemValue;
                }
                groupedItems[groupName] = g;
            }

            return new GroupedItemPartialCreditApplicationModel
            {
                NrOfApplicants = NrOfApplicants,
                GroupedItems = groupedItems
            };
        }

        public static PartialCreditApplicationModel FromJson(string json)
        {
            var items = new List<ApplicationItem>();

            var e = JsonConvert.DeserializeObject<ExpandoObject>(json, new Newtonsoft.Json.Converters.ExpandoObjectConverter());

            IDictionary<string, object> ed = e;
            int nrOfApplicants = Convert.ToInt32(ed["nrOfApplicants"]);
            foreach (var groupName in Enumerable.Range(1, nrOfApplicants).Select(x => $"applicant{x}").Concat(new string[] { "application" }))
            {
                ExpandoObject applicant = ed[groupName] as ExpandoObject;
                IDictionary<string, object> applicantd = applicant;
                foreach (var kvp in applicantd)
                {
                    items.Add(new ApplicationItem
                    {
                        GroupName = groupName,
                        ItemName = kvp.Key,
                        ItemValue = kvp.Value?.ToString()
                    });
                }
            }

            return new PartialCreditApplicationModel(nrOfApplicants, items);
        }
    }
    public class GroupedItemPartialCreditApplicationModel
    {
        public int NrOfApplicants { get; set; }
        public Dictionary<string, Dictionary<string, string>> GroupedItems { get; set; }
    }

    public class PartialCreditApplicationModelExtended<TCustomerApplicationDataType> : PartialCreditApplicationModel where TCustomerApplicationDataType : PartialCreditApplicationModelExtendedCustomDataBase
    {
        public PartialCreditApplicationModelExtended(TCustomerApplicationDataType customData,
             bool wasChangeDataLoaded,
            List<ApplicationItem> items,
            ISet<string> requestedApplicationFields = null,
            ISet<string> requestedApplicantFields = null,
            ISet<string> requestedDocumentFields = null,
            ISet<string> requestedQuestionFields = null,
            ISet<string> requestedCreditreportFields = null,
            ISet<string> requestedExternalFields = null) : base(
            customData?.NrOfApplicants ?? 1,
            items: items,
            requestedApplicationFields: requestedApplicationFields,
            requestedApplicantFields: requestedApplicantFields,
            requestedDocumentFields: requestedDocumentFields,
            requestedQuestionFields: requestedQuestionFields,
            requestedCreditreportFields: requestedCreditreportFields,
            requestedExternalFields: requestedExternalFields,
            wasChangeDataLoaded: wasChangeDataLoaded)
        {
            CustomData = customData;
        }

        public TCustomerApplicationDataType CustomData { get; private set; }
    }

    public class PartialCreditApplicationModelExtendedCustomDataBase
    {
        public int NrOfApplicants { get; set; }
    }
}
