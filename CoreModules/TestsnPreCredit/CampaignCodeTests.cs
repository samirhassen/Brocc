using Microsoft.VisualStudio.TestTools.UnitTesting;
using nPreCredit.Code.Services;
using nPreCredit.DbModel;
using NTech;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestsnPreCredit
{
    [TestClass]
    public class CampaignCodeTests
    {
        [TestMethod]
        public void FilterLogic()
        {
            var d = new Dictionary<string, string>();
            var codes = new List<CampaignCode>();
            var today = new DateTime(2020, 03, 07);
            var clock = new StrictMock<IClock>();
            clock.Setup(x => x.Today).Returns(today);

            Action<int, string> test = (expectedCount, caseDescrption) =>
                Assert.AreEqual(expectedCount, CampaignCodeService.FindMatchedCampaignCodes(d, codes.AsQueryable(), clock.Object).Count(), caseDescrption);

            test(0, "no matches when no codes and no input");

            /*
             * Google campaign code match
             */
            d["utm_campaign"] = "summer1";
            var theCode = new CampaignCode { Code = "summer1" };
            codes.Add(theCode);
            test(0, "no match on google campaign with matching utm_campaign when not flagged as google campaign");
            theCode.IsGoogleCampaign = true;
            test(1, "match on google campaign with matching utm_campaign when flagged as google campaign");

            /*
             * Deleted
             */
            theCode.DeletedByUserId = 1;
            test(0, "no match on deleted campaign");
            theCode.DeletedByUserId = null;

            /*
             * Start date
             */
            theCode.StartDate = today;
            test(1, "match on google campaign with matching utm_campaign that started today");

            theCode.StartDate = today.AddDays(1);
            test(0, "no match on google campaign with matching utm_campaign that starts tomorrow");
            theCode.StartDate = null;

            /*
             * End date
             */
            theCode.EndDate = today;
            test(1, "match on google campaign with matching utm_campaign that ends today");

            theCode.EndDate = today.AddDays(-1);
            test(0, "no match on google campaign with matching utm_campaign that ended yesterday");
            theCode.EndDate = null;
        }
    }
}
