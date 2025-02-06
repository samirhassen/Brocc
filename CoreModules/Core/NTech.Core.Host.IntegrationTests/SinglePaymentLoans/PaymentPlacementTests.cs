using nCredit.DbModel.BusinessEvents;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.SinglePaymentLoans.Utilities;

namespace NTech.Core.Host.IntegrationTests.SinglePaymentLoans
{
    internal class PaymentPlacementTests
    {
        const string creditNr = "L10001";
        const decimal ReminderFee = 60m;        

        /*
         * Payment order: initialFeeNotification, NotificationFee (which we dont use on this client), Interest, Capital, ReminderFee
         */
        [Test]
        public void OldestNotification_InitialFee_IsPaidFirst()
        {
            const decimal PaidAmount = 1m;
            RunPayAfterTwoMonthsTest(PaidAmount, x => x
                .ExpectNotificationPartiallyPaid(28, creditNr, dueDay: 28, fromMonthNr: 1,
                    balanceAfterAmount: 419.64m - PaidAmount + ReminderFee, notificationCostAfter: (Code: "initialFeeNotification", Amount: 148m))
                .ExpectNotificationNotPaid(28, creditNr, dueDay: 28, fromMonthNr: 2)
                .ExpectedUnplacedBalanceAmount(28, expectedAmount: 0m));
                
        }

        [Test]
        public void OldestNotification_Interest_IsPaidSecond()
        {
            const decimal PaidAmount = 149m + 1m;
            RunPayAfterTwoMonthsTest(PaidAmount, x => x
                .ExpectNotificationPartiallyPaid(28, creditNr, dueDay: 28, fromMonthNr: 1, balanceAfterAmount: 419.64m - PaidAmount + ReminderFee, 
                    notificationCostAfter: (Code: "initialFeeNotification", Amount: 0m), interestAfterAmount: 28.90m)
                .ExpectNotificationNotPaid(28, creditNr, dueDay: 28, fromMonthNr: 2)
                .ExpectedUnplacedBalanceAmount(28, expectedAmount: 0m));
        }

        [Test]
        public void OldestNotification_Capital_IsPaidThird()
        {
            const decimal PaidAmount = 149m + 29.90m + 1m;
            RunPayAfterTwoMonthsTest(PaidAmount, x => x
                .ExpectNotificationPartiallyPaid(28, creditNr, dueDay: 28, fromMonthNr: 1, balanceAfterAmount: 419.64m - PaidAmount + ReminderFee,
                    notificationCostAfter: (Code: "initialFeeNotification", Amount: 0m), interestAfterAmount: 0m, capitalAfterAmount: 239.74m)
                .ExpectNotificationNotPaid(28, creditNr, dueDay: 28, fromMonthNr: 2)
                .ExpectedUnplacedBalanceAmount(28, expectedAmount: 0m));
        }

        [Test]
        public void OldestNotification_ReminderFee_IsPaidLast()
        {
            const decimal PaidAmount = 419.64m + 1m;
            RunPayAfterTwoMonthsTest(PaidAmount, x => x
                .ExpectNotificationPartiallyPaid(28, creditNr, dueDay: 28, fromMonthNr: 1, balanceAfterAmount: 59m,
                    notificationCostAfter: (Code: "initialFeeNotification", Amount: 0m), interestAfterAmount: 0m, capitalAfterAmount: 0m, reminderFeeAfterAmount: 59m)
                .ExpectNotificationNotPaid(28, creditNr, dueDay: 28, fromMonthNr: 2)
                .ExpectedUnplacedBalanceAmount(28, expectedAmount: 0m));
        }

        [Test]
        public void MinorOverpayment_PlacedAsCapital_WhenFromUnplaced()
        {
            const decimal PaidAmount = 419.64m + 270.64m + ReminderFee + 1m;
            RunPayAfterTwoMonthsTest(PaidAmount, x => x
                .ExpectNotificationFullyPaid(28, creditNr, dueDay: 28, fromMonthNr: 1)
                .ExpectNotificationFullyPaid(28, creditNr, dueDay: 28, fromMonthNr: 2)
                .ExpectExtraAmortization(28, creditNr, amount: 1m)
                .ExpectedUnplacedBalanceAmount(28, expectedAmount: 0m), usePaymentFile: false);
        }

        [Test]
        public void MinorOverpayment_LeftUnplaced_WhenFileImport()
        {
            const decimal PaidAmount = 419.64m + 270.64m + ReminderFee + 1m;
            RunPayAfterTwoMonthsTest(PaidAmount, x => x
                .ExpectedUnplacedBalanceAmount(28, expectedAmount: PaidAmount), usePaymentFile: true);
        }

        [Test]
        public void MajorOverpaymentFromUnplaced_SettlesLoanAndLeftUnplaced()
        {
            const decimal PaidAmount = 50000m;

            RunPayAfterTwoMonthsTest(PaidAmount, x => x
                .ExpectNotificationFullyPaid(28, creditNr, dueDay: 28, fromMonthNr: 1)
                .ExpectNotificationFullyPaid(28, creditNr, dueDay: 28, fromMonthNr: 2)
                .ExpectExtraAmortization(28, creditNr, amount: 520.65m)
                .ExpectedUnplacedBalanceAmount(28, expectedAmount: 48729.07m)
                .ExpectCreditSettled(28, creditNr), usePaymentFile: false);
        }

        [Test]
        public void MajorOverpaymentFromFileNoSettlementOffer_AllUnplaced()
        {
            const decimal PaidAmount = 50000m;

            RunPayAfterTwoMonthsTest(PaidAmount, x => x                
                .ExpectedUnplacedBalanceAmount(28, 50000m), usePaymentFile: true);
        }

        [Test]
        public void ExactPaymentWithSettlementOffer_IsNotAutoPlaced()
        {
            const decimal PaidAmount = 1270.93m;

            RunPayAfterTwoMonthsTest(PaidAmount, x => x
                .ExpectedUnplacedBalanceAmount(28, 1270.93m), 
                    usePaymentFile: true, createSettlementOfferDayNr: 23, placeFromUnplaced: false);
        }

        [Test]
        public void ExactPaymentWithSettlementOffer_SuggestsSettleOnManualPlace()
        {
            const decimal PaidAmount = 1270.93m;

            RunPayAfterTwoMonthsTest(PaidAmount, x => x
                .ExpectCreditSettled(28, creditNr)
                .AssertCustom(28, x =>
                {
                    var hasWriteOffs = x.Context.TransactionsQueryable.Any(x => x.WriteoffId != null);
                    Assert.IsFalse(hasWriteOffs, $"{x.Prefix}Writeoffs not expected");
                }),
                usePaymentFile: true, createSettlementOfferDayNr: 23, placeFromUnplaced: true);
        }

        [Test]
        public void MassiveUnderPaymentWithSettlementOffer_IsNotAutoPlaced()
        {
            const decimal PaidAmount = 1m;

            RunPayAfterTwoMonthsTest(PaidAmount, x => x
                .ExpectedUnplacedBalanceAmount(28, 1m),
                    usePaymentFile: true, createSettlementOfferDayNr: 23, placeFromUnplaced: false);
        }

        [Test]
        public void MassiveUnderPaymentWithSettlementOffer_SuggestsNormalPaymentPlacement()
        {
            const decimal PaidAmount = 1m;

            RunPayAfterTwoMonthsTest(PaidAmount, x => x
                .ExpectNotificationPartiallyPaid(28, creditNr, dueDay: 28, fromMonthNr: 1, balanceAfterAmount: 419.64m - 1m + ReminderFee,  notificationCostAfter: (Code: "initialFeeNotification", Amount: 148m))
                .AssertCustom(28, x =>
                {
                    var hasWriteOffs = x.Context.TransactionsQueryable.Any(x => x.WriteoffId != null);
                    Assert.IsFalse(hasWriteOffs, $"{x.Prefix}Writeoffs not expected");
                }),
                usePaymentFile: true, createSettlementOfferDayNr: 23, placeFromUnplaced: true);
        }

        [Test]
        public void MassiveOverPaymentWithSettlementOffer_SuggestsSettleAndUnplaced()
        {
            const decimal PaidAmount = 1270.93m + 1000000m;

            RunPayAfterTwoMonthsTest(PaidAmount, x => x
                .ExpectCreditSettled(28, creditNr)
                .AssertCustom(28, x =>
                {
                    var hasWriteOffs = x.Context.TransactionsQueryable.Any(x => x.WriteoffId != null);
                    Assert.IsFalse(hasWriteOffs, $"{x.Prefix}Writeoffs not expected");
                })
                .ExpectedUnplacedBalanceAmount(28, 1000000m),
                usePaymentFile: true, createSettlementOfferDayNr: 23, placeFromUnplaced: true);
        }

        private void RunPayAfterTwoMonthsTest(decimal paidAmount, Action<CreditCycleAssertionBuilder> addPaymentAssertion, bool? usePaymentFile = null, int? createSettlementOfferDayNr = null, bool placeFromUnplaced = false)
        {
            string creditNr = "L10001";
            var builder = CreditCycleAssertionBuilder
                .Begin()

                .ForMonth(1)
                .ExpectNotification(14, creditNr: creditNr, dueDay: 28, initialAmount: 419.64m, capitalAmount: 240.74m, interestAmount: 29.90m,
                    notificationCost: (Code: "initialFeeNotification", Amount: 149m))

                .ForMonth(2)
                .ExpectNotification(14, creditNr: creditNr, dueDay: 28, initialAmount: 270.64m, capitalAmount: 238.61m, interestAmount: 32.03m);

            addPaymentAssertion(builder);
            
            foreach (var usePaymentFileActual in usePaymentFile.HasValue ? new[] { usePaymentFile.Value  } : new[] { false, true } )
            {
                TestContext.WriteLine($"usePaymentFile={usePaymentFile}");
                new HappyFlowTest(builder.End(), paidAmount, usePaymentFileActual, createSettlementOfferDayNr, placeFromUnplaced).RunTest();
            }            
        }

        private class HappyFlowTest : SinglePaymentLoansTestRunner
        {
            private readonly CreditCycleAssertionBuilder.CreditCycleAssertion assertion;
            private readonly decimal paidAmount;
            private readonly bool usePaymentFile;
            private readonly int? createSettlementOfferDayNr;
            private readonly bool placeFromUnplaced;

            public HappyFlowTest(CreditCycleAssertionBuilder.CreditCycleAssertion assertion, decimal paidAmount, bool usePaymentFile, int? createSettlementOfferDayNr = null, bool placeFromUnplaced = false)
            {
                this.assertion = assertion;
                this.paidAmount = paidAmount;
                this.usePaymentFile = usePaymentFile;
                this.createSettlementOfferDayNr = createSettlementOfferDayNr;
                this.placeFromUnplaced = placeFromUnplaced;
            }

            protected override void DoTest()
            {
                Support.MoveToNextDayOfMonth(1);
                var maxMonthNr = assertion.MaxMonthNr;
                foreach (var monthNr in Enumerable.Range(1, 2))
                {
                    ShortTimeCredits.RunOneMonth(Support, creditCycleAssertion: (Assertion: assertion, MonthNr: monthNr), afterDay: dayNr =>
                    {
                        if (monthNr == 1 && dayNr == 1)
                            ShortTimeCredits.CreateCredit(Support, creditIndex: 1, repaymentTime: 4, isRepaymentTimeDays: false,
                                        loanAmount: 1000m, initialFeeOnFirstNotification: 149m, marginInterestRatePercent: 39m);

                        if (monthNr == 2 && createSettlementOfferDayNr == dayNr)
                        {
                            Credits.CreateSettlementOffer(Support, creditNr, Support.CurrentMonth.GetDayDate(28), null, null);
                        }

                        if (monthNr == 2 && dayNr == 28)
                        {
                            PayUsingMultiCreditPaymentManager(Support, paidAmount, "L10001", "1111111108", usePaymentFile: usePaymentFile, placeFromUnplaced: placeFromUnplaced);
                        }
                    }, payDirectWhenScheduled: false);
                }
            }

            private static void PayUsingMultiCreditPaymentManager(CreditSupportShared support, decimal paymentAmount, string creditNr, string ocr, bool usePaymentFile, bool placeFromUnplaced)
            {
                var multiMgr = support.GetRequiredService<MultiCreditPlacePaymentBusinessEventManager>();
                if (usePaymentFile)
                {
                    var file = Credits.CreateIncomingPaymentFile(support, new List<(string Ocr, string CustomerName, decimal Amount, string NoteText, string ExternalId)>
                    {
                        (Ocr: ocr, CustomerName: "Customer name", Amount: paymentAmount, NoteText: creditNr, ExternalId: creditNr)
                    });
                    var isOk = multiMgr.TryImportFile(file, false, false, out var failedMessage, out var _);
                    Assert.That(isOk, Is.True, $"TryPlaceFromUnplaced failed: {failedMessage}");
                    if(placeFromUnplaced)
                    {
                        var paymentId = support.WithCreditDb(x => x.IncomingPaymentHeaders.Single().Id);
                        Credits.PlaceUnplacedPaymentUsingSuggestion(support, paymentId, creditNr);
                    }
                }
                else
                {
                    Credits.AddUnplacedPayments(support, support.CreditEnvSettings, (Amount: paymentAmount, NoteText: "whatever"));

                    int paymentId;
                    using (var context = support.CreateCreditContextFactory().CreateContext())
                    {
                        paymentId = context.IncomingPaymentHeadersQueryable.Single().Id;
                    }
                    var instruction = multiMgr.ComputeMultiCreditPlacementInstruction(new PaymentPlacementSuggestionRequest { CreditNrs = new List<string> { creditNr }, PaymentId = paymentId }).Instruction;
                    multiMgr.PlaceFromUnplaced(new PaymentPlacementRequest
                    {
                        Instruction = instruction,
                        PaymentId = paymentId
                    });
                }
            }
        }
    }
}
