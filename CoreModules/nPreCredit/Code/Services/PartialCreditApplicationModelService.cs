using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;

namespace nPreCredit.Code.Services
{
    public class PartialCreditApplicationModelService : IPartialCreditApplicationModelService
    {
        private readonly IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;
        private readonly UpdateCreditApplicationRepository updateCreditApplicationRepository;

        public PartialCreditApplicationModelService(IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository, UpdateCreditApplicationRepository updateCreditApplicationRepository)
        {

            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
            this.updateCreditApplicationRepository = updateCreditApplicationRepository;
        }

        public PartialCreditApplicationModel Get(string applicationNr, PartialCreditApplicationModelRequest request)
        {
            return partialCreditApplicationModelRepository.Get(applicationNr, request);
        }

        public void Update(string applicationNr, INTechCurrentUserMetadata user, string stepName, List<ApplicationUpdateItem> applicationItems = null, List<ApplicantUpdateItem> applicantItems = null)
        {
            if (string.IsNullOrWhiteSpace(applicationNr))
                throw new ArgumentNullException("applicationNr");
            if (string.IsNullOrWhiteSpace(stepName))
                throw new ArgumentNullException("stepName");
            if (user == null)
                throw new ArgumentNullException("user");
            if (((applicationItems?.Count ?? 0) + (applicantItems?.Count ?? 0)) == 0)
                return;

            var items = new List<UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem>();
            foreach (var a in (applicationItems ?? new List<ApplicationUpdateItem>()))
            {
                items.Add(new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem
                {
                    GroupName = "application",
                    IsSensitive = a.IsSensitive,
                    Name = a.Name,
                    Value = a.Value
                });
            }
            foreach (var a in (applicantItems ?? new List<ApplicantUpdateItem>()))
            {
                items.Add(new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem
                {
                    GroupName = $"applicant{a.ApplicantNr}",
                    IsSensitive = a.IsSensitive,
                    Name = a.Name,
                    Value = a.Value
                });
            }

            updateCreditApplicationRepository.UpdateApplication(applicationNr, new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest
            {
                StepName = stepName,
                Items = items,
                InformationMetadata = user.InformationMetadata,
                UpdatedByUserId = user.UserId
            });
        }

        public class ApplicationUpdateItem
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public bool IsSensitive { get; set; }
        }

        public class ApplicantUpdateItem : ApplicationUpdateItem
        {
            public int? ApplicantNr { get; set; }
        }
    }

    public interface IPartialCreditApplicationModelService
    {
        PartialCreditApplicationModel Get(string applicationNr, PartialCreditApplicationModelRequest request);
        void Update(string applicationNr, INTechCurrentUserMetadata user, string stepName, List<PartialCreditApplicationModelService.ApplicationUpdateItem> applicationItems = null, List<PartialCreditApplicationModelService.ApplicantUpdateItem> applicantItems = null);
    }
}