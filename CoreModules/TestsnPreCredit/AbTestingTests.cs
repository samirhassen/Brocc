using Microsoft.VisualStudio.TestTools.UnitTesting;
using nPreCredit;
using nPreCredit.Code.Services;
using nPreCredit.DbModel;
using NTech;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestsnPreCredit
{
    [TestClass]
    public class AbTestingTests
    {
        [TestMethod]
        public void TestFilterActiveExperiments()
        {
            var today = new DateTime(2020, 11, 01);
            var clock = new StrictMock<IClock>();
            clock.Setup(x => x.Today).Returns(today);

            List<AbTestingExperiment> experiments = new List<AbTestingExperiment>();
            List<ComplexApplicationListItem> items = new List<ComplexApplicationListItem>();

            Action<int, string> test = (expectedCount, desc) =>
            {
                var f = AbTestingService.FilterActiveExperiments(experiments.AsQueryable(), items.AsQueryable(), clock.Object);
                Assert.AreEqual(expectedCount, f.Count(), desc);
            };

            test(0, "no rows at all should produce no active experiments");
            /*

        public DateTime? EndDate { get; set; }
        public int? MaxCount { get; set; }
        public bool IsActive { get; set; }

             */
            var e = new AbTestingExperiment
            {
                Id = 42,
                IsActive = false
            };
            Action addApplicationToExperiment = () =>
                items.Add(new ComplexApplicationListItem { ListName = "AbTestingExperiment", Nr = 1, ItemName = "ExperimentId", ItemValue = e.Id.ToString() });

            experiments.Add(e);
            test(0, "inactive");

            e.IsActive = true;
            test(1, "active");

            e.EndDate = today.AddDays(-1);
            test(0, "end date: yesterday");
            e.EndDate = today;
            test(1, "end date: today");
            e.EndDate = today.AddDays(1);
            test(1, "end date: tomorrow");
            e.EndDate = null;

            e.StartDate = today.AddDays(-1);
            test(1, "start date: yesterday");
            e.StartDate = today;
            test(1, "start date: today");
            e.StartDate = today.AddDays(1);
            test(0, "start date: tomorrow");
            e.StartDate = null;

            e.MaxCount = 2;
            test(1, "max count: 2, 0 applications");
            addApplicationToExperiment();
            test(1, "max count: 2, 1 applications");
            addApplicationToExperiment();
            test(0, "max count: 2, 2 applications");
            addApplicationToExperiment();
            test(0, "max count: 2, 3 applications");
        }
    }
}
