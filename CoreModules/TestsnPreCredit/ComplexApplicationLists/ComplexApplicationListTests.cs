using Microsoft.VisualStudio.TestTools.UnitTesting;
using nPreCredit;
using nPreCredit.Code.Datasources;
using nPreCredit.Code.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TestsnPreCredit.ComplexApplicationLists
{
    [TestClass]
    public class ComplexApplicationListTests
    {
        private static int idGenerator = 1;

        private static int NextId()
        {
            return Interlocked.Increment(ref idGenerator);
        }

        private static ComplexApplicationListItem CreateUnique(string applicationNr, string listName, int nr, string name, string value)
        {
            return new ComplexApplicationListItem
            {
                Id = NextId(),
                ApplicationNr = applicationNr,
                ListName = listName,
                Nr = nr,
                IsRepeatable = false,
                ItemName = name,
                ItemValue = value
            };
        }

        private static List<ComplexApplicationListItem> CreateRepeated(string applicationNr, string listName, int nr, string name, List<string> value)
        {
            var items = value.Select(x => CreateUnique(applicationNr, listName, nr, name, x)).ToList();
            items.ForEach(x => x.IsRepeatable = true);
            return items;
        }

        public static List<ComplexApplicationListItem> CreateRow(string applicationNr, string listName, int nr, Dictionary<string, string> uniqueItems, Dictionary<string, List<string>> repeatedItems)
        {
            var items = new List<ComplexApplicationListItem>();
            if (uniqueItems != null)
            {
                foreach (var uv in uniqueItems)
                {
                    items.Add(CreateUnique(applicationNr, listName, nr, uv.Key, uv.Value));
                }
            }
            if (repeatedItems != null)
            {
                foreach (var rv in repeatedItems)
                {
                    items.AddRange(CreateRepeated(applicationNr, listName, nr, rv.Key, rv.Value));
                }
            }
            return items;
        }

        /*
        /// Row
        /// {
        ///    ApplicationNr = A1
        ///    ListName = MortageObjectCollateral
        ///    Nr = 1
        ///    UniqueItems = { "isObject" : false, "priceAmount" : "100000" }
        ///    RepeatedItems = { "customerIds: ["42", "43", "44"] }
        /// }
        /// Row
        /// {
        ///    ApplicationNr = A1
        ///    ListName = MortageObjectCollateral
        ///    Nr = 2
        ///    UniqueItems = { "isObject" : true, "priceAmount" : "150000" }
        ///    RepeatedItems = { "customerIds: ["45"] }
        /// }
        */

        private static List<ComplexApplicationListItem> CreateTestSet1()
        {
            var items = new List<ComplexApplicationListItem>();
            items.AddRange(CreateRow("A1", "MortageObjectCollateral", 1, new Dictionary<string, string>
            {
                { "isObject", "false" },
                { "priceAmount", "100000" }
            }, new Dictionary<string, List<string>>
            {
                { "customerIds", new List<string> { "42", "43", "44" } }
            }));
            items.AddRange(CreateRow("A1", "MortageObjectCollateral", 2, new Dictionary<string, string>
            {
                { "isObject", "true" },
                { "priceAmount", "150000" }
            }, new Dictionary<string, List<string>>
            {
                { "customerIds", new List<string> { "45" } }
            }));
            return items;
        }

        [TestMethod]
        public void AddCustomer46ToSecondRow()
        {
            var t = CreateTestSet1();

            var r = ComplexApplicationListService.ChangeListTestable(t, new List<ComplexApplicationListOperation>
            {
                new ComplexApplicationListOperation
                {
                    ApplicationNr = "A1",
                    ListName = "MortageObjectCollateral",
                    Nr = 2,
                    ItemName = "customerIds",
                    RepeatedValue = new List<string> { "45", "46" }
                }
            });

            Assert.AreEqual(0, r.UpdatedExistingItems.Count);
            Assert.AreEqual(0, r.DeletedExistingItems.Count);
            Assert.AreEqual(1, r.AddedNewItems.Count);

            var i = r.AddedNewItems.Single();
            Assert.AreEqual("46", i.ItemValue);
        }

        [TestMethod]
        public void AddCustomer46ToSecondRowAndReOrder()
        {
            var t = CreateTestSet1();

            var r = ComplexApplicationListService.ChangeListTestable(t, new List<ComplexApplicationListOperation>
            {
                new ComplexApplicationListOperation
                {
                    ApplicationNr = "A1",
                    ListName = "MortageObjectCollateral",
                    Nr = 2,
                    ItemName = "customerIds",
                    RepeatedValue = new List<string> { "46", "45" }
                }
            });

            Assert.AreEqual(0, r.UpdatedExistingItems.Count);
            Assert.AreEqual(1, r.DeletedExistingItems.Count);
            Assert.AreEqual(2, r.AddedNewItems.Count);
            Assert.AreEqual("46", r.AddedNewItems.First().ItemValue);
        }

        [TestMethod]
        public void RemoveCustomer43FromFirstRow()
        {
            var t = CreateTestSet1();

            var r = ComplexApplicationListService.ChangeListTestable(t, new List<ComplexApplicationListOperation>
            {
                new ComplexApplicationListOperation
                {
                    ApplicationNr = "A1",
                    ListName = "MortageObjectCollateral",
                    Nr = 1,
                    ItemName = "customerIds",
                    RepeatedValue = new List<string> { "42", "44" }
                }
            });

            Assert.AreEqual(0, r.UpdatedExistingItems.Count);

            //"43", "44" removed
            Assert.AreEqual(2, r.DeletedExistingItems.Count);
            Assert.AreEqual("43", r.DeletedExistingItems[0].ItemValue);
            Assert.AreEqual("44", r.DeletedExistingItems[1].ItemValue);

            //"44" readded
            Assert.AreEqual(1, r.AddedNewItems.Count);
            Assert.AreEqual("44", r.AddedNewItems[0].ItemValue);

            //NOTE: That this case could be optimized to not require the delete since the list is not reordered.
        }

        [TestMethod]
        public void UpdatePriceAmountOnFirstRow()
        {
            var t = CreateTestSet1();

            var r = ComplexApplicationListService.ChangeListTestable(t, new List<ComplexApplicationListOperation>
            {
                new ComplexApplicationListOperation
                {
                    ApplicationNr = "A1",
                    ListName = "MortageObjectCollateral",
                    Nr = 1,
                    ItemName = "priceAmount",
                    UniqueValue = "155000"
                }
            });

            Assert.AreEqual(1, r.UpdatedExistingItems.Count);
            Assert.AreEqual(0, r.DeletedExistingItems.Count);
            Assert.AreEqual(0, r.AddedNewItems.Count);

            var i = r.UpdatedExistingItems.Single();
            Assert.AreEqual("priceAmount", i.ItemName);
            Assert.AreEqual("155000", i.ItemValue);
        }

        [TestMethod]
        public void RemovePriceAmountOnFirstRow()
        {
            var t = CreateTestSet1();

            var r = ComplexApplicationListService.ChangeListTestable(t, new List<ComplexApplicationListOperation>
            {
                new ComplexApplicationListOperation
                {
                    ApplicationNr = "A1",
                    ListName = "MortageObjectCollateral",
                    Nr = 1,
                    ItemName = "priceAmount",
                    IsDelete = true
                }
            });

            Assert.AreEqual(0, r.UpdatedExistingItems.Count);
            Assert.AreEqual(1, r.DeletedExistingItems.Count);
            Assert.AreEqual(0, r.AddedNewItems.Count);

            var i = r.DeletedExistingItems.Single();
            Assert.AreEqual("priceAmount", i.ItemName);
        }

        private static ComplexApplicationListDataSource.ComplexListRow CreateRowWithSingleRepeatedItem(Dictionary<string, List<string>> value)
        {
            return new ComplexApplicationListDataSource.ComplexListRow
            {
                RepeatedItems = value
            };
        }

        [TestMethod]
        public void ComplexListRowGetRepeatedItem_ShouldHandle_NullDictionary()
        {
            var c = CreateRowWithSingleRepeatedItem(null);

            var customerIds = c.GetRepeatedItem("customerIds", int.Parse);

            Assert.AreEqual(null, customerIds);
        }

        [TestMethod]
        public void ComplexListRowGetRepeatedItem_ShouldHandle_ItemMissingFromDictionary()
        {
            var c = CreateRowWithSingleRepeatedItem(new Dictionary<string, List<string>>());

            var customerIds = c.GetRepeatedItem("customerIds", int.Parse);

            Assert.AreEqual(null, customerIds);
        }

        [TestMethod]
        public void ComplexListRowGetRepeatedItem_ShouldHandle_EmptyListInDictionary()
        {
            var c = CreateRowWithSingleRepeatedItem(new Dictionary<string, List<string>>
            {
                {"customerIds", new List<string>( )}
            });

            var customerIds = c.GetRepeatedItem("customerIds", int.Parse);

            Assert.AreEqual(0, customerIds.Count);
        }

        [TestMethod]
        public void ComplexListRowGetRepeatedItem_ShouldHandle_NullItemValue()
        {
            var c = CreateRowWithSingleRepeatedItem(new Dictionary<string, List<string>>
                {
                    {"customerIds", new List<string> { null }}
                });

            var customerIds = c.GetRepeatedItem("customerIds", int.Parse);

            Assert.AreEqual(0, customerIds.Count);
        }

        [TestMethod]
        public void ComplexListRowGetRepeatedItem_ShouldHandle_ActualItemValues()
        {
            var c = CreateRowWithSingleRepeatedItem(new Dictionary<string, List<string>>
            {
                {"customerIds", new List<string> { "42", "43" }}
            });

            var customerIds = c.GetRepeatedItem("customerIds", int.Parse);

            Assert.AreEqual(2, customerIds.Count);
            Assert.AreEqual(42, customerIds[0]);
            Assert.AreEqual(43, customerIds[1]);
        }

        [TestMethod]
        public void ReplaceRow_Handled_InsertUpdateDelete()
        {
            var existingItems = CreateRow("A1", "L", 1, new Dictionary<string, string> {
                { "updated", "updated1" }, { "preserved", "preserved1" }, { "removed", "removed1" }
            }, null);

            var changes = ComplexApplicationListService.CreateReplaceRowOperations(existingItems, "A1", "L", 1, new Dictionary<string, string>
            {
                { "updated", "updated2" },
                { "preserved", "preserved1" },
                { "new", "new1" }
            });

            Assert.AreEqual(4, changes.Count);
            Assert.IsTrue(changes.Any(x => x.ItemName == "updated" && x.UniqueValue == "updated2" && !x.IsDelete));
            Assert.IsTrue(changes.Any(x => x.ItemName == "preserved" && x.UniqueValue == "preserved1" && !x.IsDelete));
            Assert.IsTrue(changes.Any(x => x.ItemName == "new" && x.UniqueValue == "new1" && !x.IsDelete));
            Assert.IsTrue(changes.Any(x => x.ItemName == "removed" && x.IsDelete));

            var r = ComplexApplicationListService.ChangeListTestable(existingItems, changes);
            Assert.AreEqual(1, r.AddedNewItems.Count);
            Assert.AreEqual(1, r.UpdatedExistingItems.Count); //Not 2 since one is not changed
            Assert.AreEqual(1, r.DeletedExistingItems.Count);
        }
    }
}