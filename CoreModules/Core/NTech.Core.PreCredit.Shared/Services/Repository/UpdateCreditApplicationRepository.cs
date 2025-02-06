using nPreCredit.Code.Services;
using NTech.Core;
using NTech.Core.Module.Shared.Services;
using NTech.Core.PreCredit.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit
{
    public class UpdateCreditApplicationRepository
    {
        private ICoreClock clock;
        private readonly IPreCreditContextFactoryService preCreditContextFactory;
        private readonly EncryptionService encryptionService;

        public UpdateCreditApplicationRepository(ICoreClock clock, IPreCreditContextFactoryService preCreditContextFactory, EncryptionService encryptionService)
        {
            this.clock = clock;
            this.preCreditContextFactory = preCreditContextFactory;
            this.encryptionService = encryptionService;
        }

        public class CreditApplicationUpdateRequest
        {
            public int UpdatedByUserId { get; set; }
            public string InformationMetadata { get; set; }
            public List<CreditApplicationItem> Items { get; set; }
            public string StepName { get; set; }

            public class ApplicationSearchTerm
            {
                public string Name { get; set; }
                public string Value { get; set; }
            }

            public class CreditApplicationItem
            {
                public string GroupName { get; set; }
                public string Name { get; set; }
                public string Value { get; set; }
                public bool IsSensitive { get; set; } //Will cause encryption
            }
        }

        //Adds to or updates an application.
        public void UpdateApplication(string applicationNr, CreditApplicationUpdateRequest request, Action<IPreCreditContextExtended> also = null)
        {
            var changeDate = clock.Now;
            using (var context = preCreditContextFactory.CreateExtended())
            {
                context.DoUsingTransaction(() =>
                {
                    var applicationWIthStuff = context
                        .CreditApplicationHeadersQueryable
                        .Where(x => x.ApplicationNr == applicationNr)
                        .Select(x => new
                        {
                            Application = x,
                            x.Items
                        })
                        .Single();

                    if (request.InformationMetadata == null || request.UpdatedByUserId == 0)
                    {
                        request.InformationMetadata = context.InformationMetadata;
                        request.UpdatedByUserId = context.CurrentUserId;
                    }

                    List<Tuple<string, CreditApplicationItem>> itemsToEncrypt = new List<Tuple<string, CreditApplicationItem>>();
                    foreach (var g in request.Items.GroupBy(x => new { x.GroupName, x.Name }))
                    {
                        var i = g.Single();
                        var existingItem = applicationWIthStuff.Items.SingleOrDefault(x => x.Name == i.Name && x.GroupName == i.GroupName);
                        if (existingItem != null)
                        {
                            if (i.IsSensitive)
                            {
                                //Add a new encrypted item and then set the id on i
                                existingItem.IsEncrypted = true;
                                existingItem.Value = null;
                                itemsToEncrypt.Add(Tuple.Create(i.Value, existingItem));
                            }
                            else
                            {
                                existingItem.Value = i.Value;
                                existingItem.IsEncrypted = false;
                            }
                            existingItem.ChangedById = request.UpdatedByUserId;
                            existingItem.ChangedDate = changeDate;
                            existingItem.AddedInStepName = request.StepName;
                        }
                        else
                        {
                            var newItem = new CreditApplicationItem
                            {
                                AddedInStepName = request.StepName,
                                ApplicationNr = applicationNr,
                                CreditApplication = applicationWIthStuff.Application,
                                GroupName = i.GroupName,
                                IsEncrypted = i.IsSensitive,
                                InformationMetaData = request.InformationMetadata,
                                ChangedById = request.UpdatedByUserId,
                                ChangedDate = changeDate,
                                Name = i.Name
                            };
                            if (i.IsSensitive)
                            {
                                itemsToEncrypt.Add(Tuple.Create(i.Value, newItem));
                            }
                            else
                            {
                                newItem.Value = i.Value;
                            }

                            context.AddCreditApplicationItems(newItem);
                        }
                    }

                    encryptionService.SaveEncryptItems(
                        itemsToEncrypt.ToArray(),
                        x => x.Item1,
                        (x, y) => x.Item2.Value = y.ToString(),
                        context);

                    also?.Invoke(context);

                    context.SaveChanges();
                });
            }
        }
    }
}