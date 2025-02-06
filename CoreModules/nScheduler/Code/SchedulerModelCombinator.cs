using System.Collections.Generic;
using System.Linq;

namespace nScheduler.Code
{
    public class SchedulerModelCombinator
    {
        /// <summary>
        /// Merge service calls, timeslots and time slots items between from shared into the clients config.
        /// If it exists in both:
        /// - Service call: The client version is used.
        /// - TimeSlot: They are merged where shared only items are added after the client items.
        /// - Timeslot item: The client item is used.
        /// </summary>
        public void MergeSharedJobsIntoClientJobs(SchedulerModel sharedJobs, SchedulerModel clientJobs)
        {
            if (sharedJobs == null)
                return;

            foreach (var sharedServiceCall in sharedJobs.ServiceCalls)
            {
                clientJobs.AddServiceCallIfNotExists(sharedServiceCall.Value);
            }

            if (clientJobs.Timeslots == null)
            {
                clientJobs.Timeslots = new List<SchedulerModel.TimeSlot>();
            }
            var clientTimeslots = clientJobs.Timeslots.ToDictionary(x => x.Name, x => x);
            foreach (var sharedTimeSlot in sharedJobs.Timeslots)
            {
                if (!clientTimeslots.ContainsKey(sharedTimeSlot.Name))
                {
                    clientJobs.Timeslots.Add(sharedTimeSlot);
                }
                else
                {
                    AddSharedItemsToClientTimeslot(clientTimeslots[sharedTimeSlot.Name], sharedTimeSlot);
                }
            }

            return;
        }

        private void AddSharedItemsToClientTimeslot(SchedulerModel.TimeSlot clientTimeslot, SchedulerModel.TimeSlot sharedTimeslot)
        {
            var clientServiceCallNames = clientTimeslot.Items.Select(x => x.ServiceCall.Name).ToHashSetShared();
            foreach (var sharedItem in sharedTimeslot.Items)
            {
                if (!clientServiceCallNames.Contains(sharedItem.ServiceCall.Name))
                {
                    clientTimeslot.Items.Add(sharedItem);
                }
            }
        }
    }
}