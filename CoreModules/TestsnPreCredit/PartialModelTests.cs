using Microsoft.VisualStudio.TestTools.UnitTesting;
using nPreCredit;
using System;
using System.Collections.Generic;

namespace TestsnPreCredit
{
    [TestClass]
    public class PartialModelTests
    {
        [TestMethod]
        public void ApplicationSerializationRoundtrip()
        {
            Func<string, string, string, PartialCreditApplicationModel.ApplicationItem> i = (g, n, v) => new PartialCreditApplicationModel.ApplicationItem
            {
                GroupName = g,
                ItemName = n,
                ItemValue = v
            };
            var model = new PartialCreditApplicationModel(
                2,
                new List<PartialCreditApplicationModel.ApplicationItem>()
                {
                    i("application", "loansToSettleAmount", "15000"),

                    i("applicant1", "marriage", "marriage_gift"),
                    i("applicant1", "nrOfChildren", "3"),
                    i("applicant1", "incomePerMonthAmount", "2500"),
                    i("applicant1", "housingCostPerMonthAmount", "1200"),
                    i("applicant1", "otherLoanCostPerMonthAmount", "500"),

                    i("applicant2", "marriage", "marriage_gift"),
                    i("applicant2", "nrOfChildren", "3"),
                    i("applicant2", "incomePerMonthAmount", "3500"),
                    i("applicant2", "housingCostPerMonthAmount", "0"),
                    i("applicant2", "studentLoanCostPerMonthAmount", "300"),
                    i("applicant2", "creditCardCostPerMonthAmount", "250")
                });

            var json = model.ToJson();
            var newModel = PartialCreditApplicationModel.FromJson(json);
            Assert.AreEqual("marriage_gift", newModel.Applicant(1).Get("marriage").StringValue.Required);
            Assert.AreEqual("3", newModel.Applicant(2).Get("nrOfChildren").StringValue.Required);
            Assert.AreEqual("15000", newModel.Application.Get("loansToSettleAmount").StringValue.Required);
            Assert.AreEqual(json, newModel.ToJson());
        }

        [TestMethod]
        public void CreditReportSerializationRoundtrip()
        {
            Func<string, string, PartialCreditReportModel.Item> i = (n, v) => new PartialCreditReportModel.Item
            {
                Name = n,
                Value = v
            };
            var model = new PartialCreditReportModel(
                new List<PartialCreditReportModel.Item>()
                {
                    i("test", "254234ä4"),
                    i("fnu", "b")
                });

            var json = model.ToJson();
            var newModel = PartialCreditReportModel.FromJson(json);
            Assert.AreEqual("254234ä4", newModel.Get("test").StringValue.Required);
            Assert.AreEqual(json, newModel.ToJson());
        }
    }
}
