using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using nScheduler;
using nScheduler.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace TestsnPreCredit
{
    [TestClass]
    public class SchedulerModelParserTests
    {
        [TestMethod]
        public void ScheduleFileMorning_WithWhitespaceEverywhere_IsStillParsedCorrectly()
        {
            var model = CreateTestModel();

            var slot = model.Timeslots.SingleOrDefault(x => x.Name == "Morning");
            Assert.IsNotNull(slot);

            var call = slot.Items.SingleOrDefault(x => x.ServiceCall.ServiceUrl == new Uri("https://credit.example.org/Api/Credit/CreateTerminationLetters"));
            Assert.IsNotNull(call);

            var description = call.TriggerLimitation.GetDescription();
            Assert.IsTrue(description.Contains("Success")
                && description.Contains("Warning")
                && description.Contains("CreateCreditNotifications")
                && description.Contains("CreateCreditReminders")
                && description.Contains("14"));
        }

        [TestMethod]
        public void ScheduleFileEvening_WithWhitespaceEverywhere_IsStillParsedCorrectly()
        {
            var model = CreateTestModel();

            var slot = model.Timeslots.SingleOrDefault(x => x.Name == "Evening");
            Assert.IsNotNull(slot);

            var call = slot.Items.SingleOrDefault(x => x.ServiceCall.ServiceUrl == new Uri("https://customer.example.org/Customer/RunPeriodicMaintenance"));
            Assert.IsNotNull(call);

            Assert.IsTrue(call.ServiceCall.IsManualTriggerAllowed);
        }

        [TestMethod]
        public void Merge_EmptyClient_ResultsInSharedOnly()
        {
            var clientJobs = new SchedulerModel();
            var sharedJobs = CreateTestModel();

            new SchedulerModelCombinator().MergeSharedJobsIntoClientJobs(sharedJobs, clientJobs);

            Assert.AreEqual(JsonConvert.SerializeObject(clientJobs), JsonConvert.SerializeObject(CreateTestModel()));
        }

        [TestMethod]
        public void Merge_MissingShared_ResultsInClientOnly()
        {
            var clientJobs = CreateTestModel();
            SchedulerModel sharedJobs = null;

            new SchedulerModelCombinator().MergeSharedJobsIntoClientJobs(sharedJobs, clientJobs);

            Assert.AreEqual(JsonConvert.SerializeObject(clientJobs), JsonConvert.SerializeObject(CreateTestModel()));
        }

        [TestMethod]
        public void Merge_ServiceCalls_Are_Merged()
        {
            var clientJobs = CreateTestModel();
            var sharedJobs = CreateTestModel();
            clientJobs.ServiceCalls.Remove(clientJobs.ServiceCalls.Keys.First());

            new SchedulerModelCombinator().MergeSharedJobsIntoClientJobs(sharedJobs, clientJobs);

            Assert.AreEqual(JsonConvert.SerializeObject(clientJobs), JsonConvert.SerializeObject(CreateTestModel()));
        }

        [TestMethod]
        public void Merge_Timeslots_Are_Merged()
        {
            var clientJobs = CreateTestModel();
            var sharedJobs = CreateTestModel();
            var morningSlot = clientJobs.Timeslots.Single(x => x.Name == "Morning");
            morningSlot.Items.Remove(morningSlot.Items.First());

            new SchedulerModelCombinator().MergeSharedJobsIntoClientJobs(sharedJobs, clientJobs);

            Assert.AreEqual(JsonConvert.SerializeObject(clientJobs), JsonConvert.SerializeObject(CreateTestModel()));
        }

        private SchedulerModel CreateTestModel()
        {
            Func<string, bool> isFeatureEnabled = _ => true;
            var sr = ServiceRegistry.CreateFromDict(new Dictionary<string, string> { { "nCredit", "https://credit.example.org" }, { "ncustomer", "https://customer.example.org" } });

            return SchedulerModel.Parse(XDocument.Parse(ScheduleWithWhitespaceEverywhere), sr, isFeatureEnabled, "SE");
        }

        private static string ScheduleWithWhitespaceEverywhere =
@"<SchedulerSetup>

  <ScheduleRunner> ModuleDbSqlAgent</ScheduleRunner>
  <ServiceCalls>
    <ServiceCall name='  CreateCreditTerminationLetters'>
      <ServiceUrl service='nCredit '> Api/Credit/CreateTerminationLetters </ServiceUrl>
    </ServiceCall>

    <ServiceCall name=' CustomerPeriodicMaintenance' allowManualTrigger='true  '>
      <ServiceUrl service=' nCustomer'>Customer/RunPeriodicMaintenance</ServiceUrl>

    </ServiceCall> 
  </ServiceCalls>
  <TimeSlots>
    <TimeSlot name=' Morning'>  
      <TimeSlotItem serviceCallName='  CreateCreditTerminationLetters'>
        <TriggerRule>
          <OnlyTheseDays> 14 </OnlyTheseDays>  


          <OnlyOnOtherCallStatus serviceCallName=' CreateCreditNotifications' allowedStatuses=' Success, Warning' />
          <OnlyOnOtherCallStatus serviceCallName='CreateCreditReminders ' allowedStatuses='Success,Warning ' />
        </TriggerRule>
      </TimeSlotItem>  
    </TimeSlot>
    <TimeSlot name='Evening  '>
      <TimeSlotItem serviceCallName=' CustomerPeriodicMaintenance'>   
        <TriggerRule> AlwaysRun</TriggerRule>
      </TimeSlotItem>      

    </TimeSlot>
  </TimeSlots>
</SchedulerSetup>".Replace("'", "\"");
    }
}
