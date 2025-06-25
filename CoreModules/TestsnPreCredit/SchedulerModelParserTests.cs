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
            SchedulerModel model = CreateTestModel();

            SchedulerModel.TimeSlot slot = model.Timeslots.SingleOrDefault(x => x.Name == "Morning");
            Assert.IsNotNull(slot);

            SchedulerModel.TimeSlotItem call = slot.Items.SingleOrDefault(x =>
                x.ServiceCall.Name == "CreateCreditTerminationLetters");
            Assert.IsNotNull(call);
            Assert.AreEqual(new Uri("https://credit.example.org/Api/Credit/CreateTerminationLetters"),
                call.ServiceCall.ServiceUrl);

            SchedulerModel.TimeSlot evening = model.Timeslots.SingleOrDefault(x => x.Name == "Evening");
            Assert.IsNotNull(evening);

            SchedulerModel.TimeSlotItem callWithParm =
                evening.Items.SingleOrDefault(x => x.ServiceCall.Name == "CustomerPeriodicMaintenance");
            Assert.IsNotNull(callWithParm);
            
            Assert.AreEqual(new Uri("https://customer.example.org/Customer/RunPeriodicMaintenance?test=SomeVal"),
                callWithParm.ServiceCall.ServiceUrl);

            string description = call.TriggerLimitation.GetDescription();
            Assert.IsTrue(description.Contains("Success")
                          && description.Contains("Warning")
                          && description.Contains("CreateCreditNotifications")
                          && description.Contains("CreateCreditReminders")
                          && description.Contains("14"));
        }

        [TestMethod]
        public void ScheduleFileEvening_WithWhitespaceEverywhere_IsStillParsedCorrectly()
        {
            SchedulerModel model = CreateTestModel();

            SchedulerModel.TimeSlot slot = model.Timeslots.SingleOrDefault(x => x.Name == "Evening");
            Assert.IsNotNull(slot);

            SchedulerModel.TimeSlotItem call = slot.Items.SingleOrDefault(x =>
                x.ServiceCall.ServiceUrl == new Uri("https://customer.example.org/Customer/RunPeriodicMaintenance?test=SomeVal"));
            Assert.IsNotNull(call);

            Assert.IsTrue(call.ServiceCall.IsManualTriggerAllowed);
        }

        [TestMethod]
        public void Merge_EmptyClient_ResultsInSharedOnly()
        {
            SchedulerModel clientJobs = new SchedulerModel();
            SchedulerModel sharedJobs = CreateTestModel();

            SchedulerModelCombinator.MergeSharedJobsIntoClientJobs(sharedJobs, clientJobs);

            Assert.AreEqual(JsonConvert.SerializeObject(clientJobs), JsonConvert.SerializeObject(CreateTestModel()));
        }

        [TestMethod]
        public void Merge_MissingShared_ResultsInClientOnly()
        {
            SchedulerModel clientJobs = CreateTestModel();

            SchedulerModelCombinator.MergeSharedJobsIntoClientJobs(null, clientJobs);

            Assert.AreEqual(JsonConvert.SerializeObject(clientJobs), JsonConvert.SerializeObject(CreateTestModel()));
        }

        [TestMethod]
        public void Merge_ServiceCalls_Are_Merged()
        {
            SchedulerModel clientJobs = CreateTestModel();
            SchedulerModel sharedJobs = CreateTestModel();
            clientJobs.ServiceCalls.Remove(clientJobs.ServiceCalls.Keys.First());

            SchedulerModelCombinator.MergeSharedJobsIntoClientJobs(sharedJobs, clientJobs);

            Assert.AreEqual(JsonConvert.SerializeObject(clientJobs), JsonConvert.SerializeObject(CreateTestModel()));
        }

        [TestMethod]
        public void Merge_Timeslots_Are_Merged()
        {
            SchedulerModel clientJobs = CreateTestModel();
            SchedulerModel sharedJobs = CreateTestModel();
            SchedulerModel.TimeSlot morningSlot = clientJobs.Timeslots.Single(x => x.Name == "Morning");
            morningSlot.Items.Remove(morningSlot.Items.First());

            SchedulerModelCombinator.MergeSharedJobsIntoClientJobs(sharedJobs, clientJobs);

            Assert.AreEqual(JsonConvert.SerializeObject(clientJobs), JsonConvert.SerializeObject(CreateTestModel()));
        }

        private static SchedulerModel CreateTestModel()
        {
            ServiceRegistry sr = ServiceRegistry.CreateFromDict(new Dictionary<string, string>
            {
                { "nCredit", "https://credit.example.org" }, { "ncustomer", "https://customer.example.org" }
            });

            return SchedulerModel.Parse(XDocument.Parse(ScheduleWithWhitespaceEverywhere), sr, IsFeatureEnabled, "SE");
            bool IsFeatureEnabled(string _) => true;
        }

        private static readonly string ScheduleWithWhitespaceEverywhere =
            @"<SchedulerSetup>

  <ScheduleRunner> ModuleDbSqlAgent</ScheduleRunner>
  <ServiceCalls>
    <ServiceCall name='  CreateCreditTerminationLetters'>
      <ServiceUrl service='nCredit '> Api/Credit/CreateTerminationLetters </ServiceUrl>
    </ServiceCall>

    <ServiceCall name=' CustomerPeriodicMaintenance' allowManualTrigger='true  '>
      <ServiceUrl service=' nCustomer'>Customer/RunPeriodicMaintenance</ServiceUrl>

      <ServiceParameter name='test   '>SomeVal</ServiceParameter>

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