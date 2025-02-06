using Microsoft.VisualStudio.TestTools.UnitTesting;
using nCredit;
using nCredit.Code.EInvoiceFi;
using nCredit.DbModel.BusinessEvents;
using System;

namespace TestsnPreCredit.Credit
{
    [TestClass]
    public class EInvoiceFiTests
    {
        [TestMethod]
        public void Start_WithNoIdentifiers_ErrorList()
        {
            var h = new EInvoiceFiMessageHandler();
            var repo = new MockMatchingRepository();
            var clock = new MockClock();
            var m = MockMessage.Create("start", "1", clock, TimeSpan.FromHours(1));

            var result = h.MatchMessageToCredit(m, repo);

            Assert.AreEqual(EInvoiceFiMessageHandler.MessageAction.ErrorList, result.Item2.Action);
        }

        [TestMethod]
        public void Start_SingleLoanMatchingEmail_CreditNrMatched()
        {
            var h = new EInvoiceFiMessageHandler();
            var repo = new MockMatchingRepository();
            var clock = new MockClock();
            var m = MockMessage.Create("start", "1", clock, TimeSpan.FromHours(1), Tuple.Create(EInvoiceFiItemCode.CustomerIdentification1, "test@example.org"));
            repo.AddCreditNrsMatchingEmail("test@example.org", "L4242");
            var result = h.MatchMessageToCredit(m, repo);

            Assert.AreEqual("L4242", result.Item1);
        }

        [TestMethod]
        public void Start_SingleInactiveLoanMatchingEmail_Skipped()
        {
            var h = new EInvoiceFiMessageHandler();
            var repo = new MockMatchingRepository();
            var clock = new MockClock();
            var m = MockMessage.Create("start", "1", clock, TimeSpan.FromHours(1), Tuple.Create(EInvoiceFiItemCode.CustomerIdentification1, "test@example.org"));
            repo.AddCreditNrsMatchingEmail("test@example.org", "L4242");
            repo.AddInactiveCreditNrs("L4242");
            var result = h.MatchMessageToCredit(m, repo);

            Assert.AreEqual(EInvoiceFiMessageHandler.MessageAction.SkipMessage, result.Item2.Action);
            Assert.AreEqual("L4242", result.Item2.MatchedCreditNr);
        }

        [TestMethod]
        public void Start_MultipleInactiveLoanMatchingEmail_ErrorList()
        {
            var h = new EInvoiceFiMessageHandler();
            var repo = new MockMatchingRepository();
            var clock = new MockClock();
            var m = MockMessage.Create("start", "1", clock, TimeSpan.FromHours(1), Tuple.Create(EInvoiceFiItemCode.CustomerIdentification1, "test@example.org"));
            repo.AddCreditNrsMatchingEmail("test@example.org", "L4242", "L4243");
            repo.AddInactiveCreditNrs("L4242", "L4243");
            var result = h.MatchMessageToCredit(m, repo);

            Assert.AreEqual(EInvoiceFiMessageHandler.MessageAction.ErrorList, result.Item2.Action);
            Assert.IsNull(result.Item2.MatchedCreditNr);
        }

        [TestMethod]
        public void Start_DifferentLoansMatchingEmailAndOcr_ErrorList()
        {
            var h = new EInvoiceFiMessageHandler();
            var repo = new MockMatchingRepository();
            var clock = new MockClock();
            var m = MockMessage.Create("start", "1", clock, TimeSpan.FromHours(1),
                Tuple.Create(EInvoiceFiItemCode.CustomerIdentification1, "test@example.org"),
                Tuple.Create(EInvoiceFiItemCode.CustomerIdentification2, "123456"));
            repo.AddCreditNrsMatchingEmail("test@example.org", "L4242");
            repo.AddCreditNrsMatchingOcr("123456", "L4243");
            var result = h.MatchMessageToCredit(m, repo);

            Assert.AreEqual(EInvoiceFiMessageHandler.MessageAction.ErrorList, result.Item2.Action);
            Assert.IsNull(result.Item2.MatchedCreditNr);
        }

        [TestMethod]
        public void Start_SingleLoanMatchingEmailOnly_CreditNrMatched()
        {
            var h = new EInvoiceFiMessageHandler();
            var repo = new MockMatchingRepository();
            var clock = new MockClock();
            var m = MockMessage.Create("start", "1", clock, TimeSpan.FromHours(1),
                Tuple.Create(EInvoiceFiItemCode.CustomerIdentification1, "test@example.org"),
                Tuple.Create(EInvoiceFiItemCode.CustomerIdentification2, "123456"));
            repo.AddCreditNrsMatchingEmail("test@example.org", "L4242");
            var result = h.MatchMessageToCredit(m, repo);

            Assert.IsNull(result.Item2);
            Assert.AreEqual("L4242", result.Item1);
        }

        [TestMethod]
        public void Start_SingleLoanMatchingOcrOnly_CreditNrMatched()
        {
            var h = new EInvoiceFiMessageHandler();
            var repo = new MockMatchingRepository();
            var clock = new MockClock();
            var m = MockMessage.Create("start", "1", clock, TimeSpan.FromHours(1),
                Tuple.Create(EInvoiceFiItemCode.CustomerIdentification1, "test@example.org"),
                Tuple.Create(EInvoiceFiItemCode.CustomerIdentification2, "123456"));
            repo.AddCreditNrsMatchingOcr("123456", "L4243");
            var result = h.MatchMessageToCredit(m, repo);

            Assert.IsNull(result.Item2);
            Assert.AreEqual("L4243", result.Item1);
        }

        [TestMethod]
        public void Start_SingleLoanMatchingEmailAndOcr_CreditNrMatched()
        {
            var h = new EInvoiceFiMessageHandler();
            var repo = new MockMatchingRepository();
            var clock = new MockClock();
            var m = MockMessage.Create("start", "1", clock, TimeSpan.FromHours(1),
                Tuple.Create(EInvoiceFiItemCode.CustomerIdentification1, "test@example.org"),
                Tuple.Create(EInvoiceFiItemCode.CustomerIdentification2, "123456"));
            repo.AddCreditNrsMatchingOcr("123456", "L4244");
            repo.AddCreditNrsMatchingEmail("test@example.org", "L4244");
            var result = h.MatchMessageToCredit(m, repo);

            Assert.IsNull(result.Item2);
            Assert.AreEqual("L4244", result.Item1);
        }

        [TestMethod]
        public void Stop_MatchingCurrentEInvoiceExists_OnlyThatCreditNrMatched()
        {
            var h = new EInvoiceFiMessageHandler();
            var repo = new MockMatchingRepository();
            var clock = new MockClock();
            var m = MockMessage.Create("stop", "1", clock, TimeSpan.FromHours(1),
                Tuple.Create(EInvoiceFiItemCode.CustomerIdentification1, "test@example.org"),
                Tuple.Create(EInvoiceFiItemCode.CustomerIdentification2, "123456"),
                Tuple.Create(EInvoiceFiItemCode.EInvoiceAddress, "a1"),
                Tuple.Create(EInvoiceFiItemCode.EInvoiceBankCode, "b1"));
            repo.AddCreditNrsMatchingOcr("123456", "L1");
            repo.AddCreditNrsMatchingEmail("test@example.org", "L2");
            repo.AddCreditNrsMatchingEInvoiceIdentifier("a1", "b1", "L3");
            var result = h.MatchMessageToCredit(m, repo);

            Assert.IsNull(result.Item2);
            Assert.AreEqual("L3", result.Item1);
        }

        [TestMethod]
        public void Change_SingleLoanMatchingEmailAndOcr_CreditNrMatched()
        {
            var h = new EInvoiceFiMessageHandler();
            var repo = new MockMatchingRepository();
            var clock = new MockClock();
            var m = MockMessage.Create("change", "1", clock, TimeSpan.FromHours(1),
                Tuple.Create(EInvoiceFiItemCode.CustomerIdentification1, "test@example.org"),
                Tuple.Create(EInvoiceFiItemCode.CustomerIdentification2, "123456"));
            repo.AddCreditNrsMatchingOcr("123456", "L4244");
            repo.AddCreditNrsMatchingEmail("test@example.org", "L4244");
            var result = h.MatchMessageToCredit(m, repo);

            Assert.IsNull(result.Item2);
            Assert.AreEqual("L4244", result.Item1);
        }

        private EInvoiceFiBusinessEventManager.EInvoiceState CreateMockState(DateTime? startDate, string eInvoiceAddress = "a1", string eInvoiceBankCode = "b1")
        {
            return new EInvoiceFiBusinessEventManager.EInvoiceState
            {
                EInvoiceAddress = eInvoiceAddress,
                EInvoiceBankCode = eInvoiceBankCode,
                IsStarted = startDate.HasValue,
                StartedDate = startDate
            };
        }

        [TestMethod]
        public void Start_AlreadyStartedRecent_LeaveInQueue()
        {
            var h = new EInvoiceFiMessageHandler();
            var clock = new MockClock();
            var m = MockMessage.Create("start", "1", clock, TimeSpan.FromHours(1));
            var currentState = CreateMockState(clock.HistoricalDate(TimeSpan.FromDays(60)));

            var result = h.ProcessMatchedMessage(m, "L1", currentState, clock);
            Assert.AreEqual(EInvoiceFiMessageHandler.MessageAction.LeaveInQueue, result.Action);
        }

        [TestMethod]
        public void Start_AlreadyStartedOld_ErrorList()
        {
            var h = new EInvoiceFiMessageHandler();
            var clock = new MockClock();
            var m = MockMessage.Create("start", "1", clock, TimeSpan.FromDays(30));
            var currentState = CreateMockState(clock.HistoricalDate(TimeSpan.FromDays(60)));
            var result = h.ProcessMatchedMessage(m, "L1", currentState, clock);
            Assert.AreEqual(EInvoiceFiMessageHandler.MessageAction.ErrorList, result.Action);
        }

        [TestMethod]
        public void Start_Stopped_Started()
        {
            var h = new EInvoiceFiMessageHandler();
            var clock = new MockClock();
            var m = MockMessage.Create("start", "1", clock, TimeSpan.FromDays(30),
                Tuple.Create(EInvoiceFiItemCode.EInvoiceAddress, "a1"),
                Tuple.Create(EInvoiceFiItemCode.EInvoiceBankCode, "b1"));
            var currentState = CreateMockState(null);
            var result = h.ProcessMatchedMessage(m, "L1", currentState, clock);
            Assert.AreEqual(EInvoiceFiMessageHandler.MessageAction.Start, result.Action);
            Assert.AreEqual("a1", result.EInvoiceAddress);
            Assert.AreEqual("b1", result.EInvoiceBankCode);
        }

        [TestMethod]
        public void Stop_AlreadyStopped_Skipped()
        {
            var h = new EInvoiceFiMessageHandler();
            var clock = new MockClock();
            var m = MockMessage.Create("stop", "1", clock, TimeSpan.FromMinutes(1));
            var currentState = CreateMockState(null);
            var result = h.ProcessMatchedMessage(m, "L1", currentState, clock);
            Assert.AreEqual(EInvoiceFiMessageHandler.MessageAction.SkipMessage, result.Action);
        }

        [TestMethod]
        public void Stop_Started_Stopped()
        {
            var h = new EInvoiceFiMessageHandler();
            var clock = new MockClock();
            var m = MockMessage.Create("stop", "1", clock, TimeSpan.FromMinutes(1));
            var currentState = CreateMockState(clock.Today);
            var result = h.ProcessMatchedMessage(m, "L1", currentState, clock);
            Assert.AreEqual(EInvoiceFiMessageHandler.MessageAction.Stop, result.Action);
        }

        [TestMethod]
        public void Change_Started_Changed()
        {
            var h = new EInvoiceFiMessageHandler();
            var clock = new MockClock();
            var m = MockMessage.Create("change", "1", clock, TimeSpan.FromDays(30),
                Tuple.Create(EInvoiceFiItemCode.EInvoiceAddress, "a1"),
                Tuple.Create(EInvoiceFiItemCode.EInvoiceBankCode, "b1"));
            var currentState = CreateMockState(clock.Today);
            var result = h.ProcessMatchedMessage(m, "L1", currentState, clock);
            Assert.AreEqual(EInvoiceFiMessageHandler.MessageAction.Change, result.Action);
            Assert.AreEqual("a1", result.EInvoiceAddress);
            Assert.AreEqual("b1", result.EInvoiceBankCode);
        }

        [TestMethod]
        public void Change_Stopped_Started()
        {
            var h = new EInvoiceFiMessageHandler();
            var clock = new MockClock();
            var m = MockMessage.Create("change", "1", clock, TimeSpan.FromDays(30),
                Tuple.Create(EInvoiceFiItemCode.EInvoiceAddress, "a1"),
                Tuple.Create(EInvoiceFiItemCode.EInvoiceBankCode, "b1"));
            var currentState = CreateMockState(null);
            var result = h.ProcessMatchedMessage(m, "L1", currentState, clock);
            Assert.AreEqual(EInvoiceFiMessageHandler.MessageAction.Start, result.Action);
            Assert.AreEqual("a1", result.EInvoiceAddress);
            Assert.AreEqual("b1", result.EInvoiceBankCode);
        }

        //TODO: Parse files
        //TODO: Validate bankccode and bank address
    }
}
