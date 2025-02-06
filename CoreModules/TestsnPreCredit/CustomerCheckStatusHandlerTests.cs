using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using nPreCredit;
using nPreCredit.Code;
using NTech;
using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using ICustomerClient = NTech.Core.Module.Shared.Clients.ICustomerClient;

namespace TestsnPreCredit
{
    [TestClass]
    public class CustomerCheckStatusHandlerTests
    {
        [TestMethod]
        public void AcceptedCustomer_ChangeToAccepted_ShouldNotChangeThusReturnNull()
        {
            WithTestContext(t =>
            {
                var changeTo = t.Handler.GetUpdateCustomerCheckStatusUpdateOrNull(t.ApplicationData);

                Assert.IsNull(changeTo);
            });
        }

        [TestMethod]
        public void Inactive_DontChangeStatus()
        {
            WithTestContext(t =>
            {
                var changeTo = t.Handler.GetUpdateCustomerCheckStatusUpdateOrNull(t.ApplicationData);

                Assert.IsNull(changeTo);
            }, alterApplicationData: a => a.IsActive = false);
        }

        [TestMethod]
        public void FinalDecisionMade_DontChangeStatus()
        {
            WithTestContext(t =>
            {
                var changeTo = t.Handler.GetUpdateCustomerCheckStatusUpdateOrNull(t.ApplicationData);

                Assert.IsNull(changeTo);
            }, alterApplicationData: a => a.IsFinalDecisionMade = true);
        }

        [TestMethod]
        public void PartiallyApproved_DontChangeStatus()
        {
            WithTestContext(t =>
            {
                var changeTo = t.Handler.GetUpdateCustomerCheckStatusUpdateOrNull(t.ApplicationData);

                Assert.IsNull(changeTo);
            }, alterApplicationData: a => a.IsPartiallyApproved = true);
        }

        [TestMethod]
        public void SanctionedCustomer_ChangeToRejected()
        {
            WithTestContext(t =>
            {
                var changeTo = t.Handler.GetUpdateCustomerCheckStatusUpdateOrNull(t.ApplicationData);

                Assert.AreEqual("Rejected", changeTo);
            }, isCustomerSanctionFlagged: true);
        }

        [TestMethod]
        public void MissingProperty_ResetsBackToInitial()
        {
            foreach (var n in new List<string> { "sanction", "email", "firstName", "addressZipcode" })
            {
                WithTestContext(t =>
                {
                    var changeTo = t.Handler.GetUpdateCustomerCheckStatusUpdateOrNull(t.ApplicationData);

                    if (n != "someRandomProperty")
                        Assert.AreEqual("Initial", changeTo, $"{n} did not reset status");
                    else
                        Assert.IsNull(changeTo, $"{n} changed status but should not have");
                },
                alterApplicationData: x => x.CustomerCheckStatus = "Accepted",
                missingCustomerProperties: new List<string> { n });
            }
        }

        [TestMethod]
        public void MissingNameOrAddress_AfterAgreementSignedAccepted_ResetsToRejected()
        {
            foreach (var n in new List<string> { "firstName", "addressZipcode" })
            {
                WithTestContext(t =>
                {
                    var changeTo = t.Handler.GetUpdateCustomerCheckStatusUpdateOrNull(t.ApplicationData);

                    Assert.AreEqual("Rejected", changeTo, $"{n} did not reset status");
                },
                alterApplicationData: x =>
                {
                    x.AgreementStatus = "Accepted";
                    x.CustomerCheckStatus = "Accepted";
                    x.CreditCheckStatus = "Accepted";
                },
                missingCustomerProperties: new List<string> { n });
            }
        }

        [TestMethod]
        public void KycScreenNotCompleted_DontChangeStatus()
        {
            WithTestContext(t =>
            {
                var changeTo = t.Handler.GetUpdateCustomerCheckStatusUpdateOrNull(t.ApplicationData);

                Assert.IsNull(changeTo);
            }, isKycScreenComplete: false);
        }

        [TestMethod]
        public void KycScreenAndSanctionMissingAndWasOnboardedExternally_ChangeToAccepted()
        {
            WithTestContext(t =>
            {
                var changeTo = t.Handler.GetUpdateCustomerCheckStatusUpdateOrNull(t.ApplicationData);

                Assert.AreEqual("Accepted", changeTo);
            },
            isKycScreenComplete: false,
            missingCustomerProperties: new List<string> { "sanction" },
            customerWasOnboardedExternallyValue: "true",
            isCustomerPepFlagged: false);
        }

        #region "Setup"
        private void WithTestContext(Action<TestContext> withContext,
            Action<CustomerCheckStatusHandler.ApplicationData> alterApplicationData = null,
            bool isCustomerSanctionFlagged = false,
            List<string> missingCustomerProperties = null,
            bool isKycScreenComplete = true,
            string customerWasOnboardedExternallyValue = null,
            bool? isCustomerPepFlagged = null)
        {
            var clock = new Mock<IClock>();
            var customerClient = new Mock<ICustomerClient>();
            var repo = new Mock<nPreCredit.IPartialCreditApplicationModelRepository>();
            var handler = new CustomerCheckStatusHandler(customerClient.Object, repo.Object);

            var app = new CustomerCheckStatusHandler.ApplicationData
            {
                ApplicationNr = "A1",
                CreditCheckStatus = "Initial",
                CustomerCheckStatus = "Initial",
                IsPartiallyApproved = false,
                IsActive = true,
                IsFinalDecisionMade = false
            };
            alterApplicationData?.Invoke(app);

            repo
                .Setup(x => x.Get(It.IsAny<string>(), It.Is<PartialCreditApplicationModelRequest>(y => y.ApplicantFields != null && y.ApplicantFields.Contains("customerId"))))
                .Returns(new nPreCredit.PartialCreditApplicationModel(1, items: new List<nPreCredit.PartialCreditApplicationModel.ApplicationItem>
                {
                    new PartialCreditApplicationModel.ApplicationItem
                    {
                        GroupName = "applicant1",
                        ItemName = "customerId",
                        ItemValue = "1"
                    }
                }));
            //
            var defaultProperties = new HashSet<string> { "sanction", "civicRegNr", "email", "firstName", "addressZipcode", "includeInFatcaExport" };
            var propsOnCustomer = defaultProperties.Except(missingCustomerProperties ?? new List<string>());

            customerClient
                .Setup(x => x.FetchCustomerOnboardingStatuses(new HashSet<int>() { 1 }, null, null, false))
                .Returns(new Dictionary<int, KycCustomerOnboardingStatusModel>
                {
                    { 1, new KycCustomerOnboardingStatusModel
                    {
                        IsSanction = isCustomerSanctionFlagged,
                        LatestScreeningDate = isKycScreenComplete ? clock.Object.Now.DateTime : (DateTime?) null,
                        IsPep = isCustomerPepFlagged
                    } }
                });

            var propsForBulkFetch = new Dictionary<string, string>
            {
                {"wasOnboardedExternally", customerWasOnboardedExternallyValue},
            };
            foreach (var prop in propsOnCustomer)
            {
                propsForBulkFetch.Add(prop, "thiscanbeanyvalue");
            }

            customerClient
                .Setup(x => x.BulkFetchPropertiesByCustomerIdsD(It.IsAny<ISet<int>>(), It.IsAny<string[]>()))
                .Returns(new Dictionary<int, Dictionary<string, string>>
                    {
                        { 1, propsForBulkFetch }
                    }
                );

            withContext(new TestContext
            {
                ApplicationData = app,
                PartialCreditApplicationModelRepository = repo,
                Clock = clock,
                CustomerClient = customerClient,
                Handler = handler
            });
        }

        private class TestContext
        {
            public Mock<IClock> Clock { get; set; }
            public Mock<ICustomerClient> CustomerClient { get; set; }
            public Mock<IPartialCreditApplicationModelRepository> PartialCreditApplicationModelRepository { get; set; }
            public CustomerCheckStatusHandler.ApplicationData ApplicationData { get; set; }
            public CustomerCheckStatusHandler Handler { get; set; }
        }
        #endregion       
    }
}
