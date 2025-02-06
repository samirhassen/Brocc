using Newtonsoft.Json;
using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class MortgageLoanApplicationBasisService : IMortgageLoanApplicationBasisService
    {
        private readonly IPartialCreditApplicationModelService partialCreditApplicationModelService;
        private readonly IClock clock;
        private readonly KeyValueStore householdIncomeStore;

        public MortgageLoanApplicationBasisService(IPartialCreditApplicationModelService partialCreditApplicationModelService, IKeyValueStoreService keyValueStoreService, IClock clock)
        {
            this.partialCreditApplicationModelService = partialCreditApplicationModelService;
            this.householdIncomeStore = new KeyValueStore(KeyValueStoreKeySpaceCode.HouseholdIncomeModelV1, keyValueStoreService);
            this.clock = clock;
        }

        public MortgageLoanApplicationBasisCurrentValuesModel GetCurrentValues(string applicationNr)
        {
            if (string.IsNullOrWhiteSpace(applicationNr))
                throw new ArgumentNullException("applicationNr");

            var app = this.partialCreditApplicationModelService.Get(applicationNr, new PartialCreditApplicationModelRequest
            {
                ApplicantFields = new List<string> { "incomePerMonthAmount" },
                ErrorIfGetNonLoadedField = true,
                LoadChangedBy = true
            });

            var result = new MortgageLoanApplicationBasisCurrentValuesModel
            {
                CombinedGrossMonthlyIncome = 0m
            };

            app.DoForEachApplicant(applicantNr =>
            {
                result.CombinedGrossMonthlyIncome += (app.Applicant(applicantNr).Get("incomePerMonthAmount").DecimalValue.Optional ?? 0m);
            });

            return result;
        }

        public HouseholdIncomeModel GetHouseholdIncomeModel(string applicationNr)
        {
            if (string.IsNullOrWhiteSpace(applicationNr))
                throw new ArgumentNullException("applicationNr");

            var v = householdIncomeStore.GetValue(applicationNr);

            if (v != null)
                return JsonConvert.DeserializeObject<HouseholdIncomeModel>(v);

            //We default to guessing that the income is currently all from employment or all from services depending on employment type
            var r = new HouseholdIncomeModel
            {
                ApplicantIncomes = new List<HouseholdIncomeModel.Applicant>()
            };

            var app = this.partialCreditApplicationModelService.Get(applicationNr, new PartialCreditApplicationModelRequest
            {
                ApplicantFields = new List<string> { "incomePerMonthAmount", "employment2" },
                ErrorIfGetNonLoadedField = true,
                LoadChangedBy = true
            });

            app.DoForEachApplicant(applicantNr =>
            {
                var incomeItem = app.Applicant(applicantNr).Get("incomePerMonthAmount");
                var combinedGrossMonthlyIncome = app.Applicant(applicantNr).Get("incomePerMonthAmount").DecimalValue.Optional ?? 0m;
                var employment = app.Applicant(applicantNr).Get("employment2").StringValue.Optional;
                r.ApplicantIncomes.Add(new HouseholdIncomeModel.Applicant
                {
                    ApplicantNr = applicantNr,
                    EmploymentGrossMonthlyIncome = employment == "own" ? 0m : combinedGrossMonthlyIncome,
                    ServiceGrossMonthlyIncome = employment == "own" ? combinedGrossMonthlyIncome : 0m,
                    CapitalGrossMonthlyIncome = 0m,
                    ChangedByUserId = incomeItem.ChangedByUserId,
                    ChangedDate = incomeItem.ChangedDate?.DateTime
                });
            });

            return r;
        }

        private bool HasIncomeChange(HouseholdIncomeModel.Applicant i1, HouseholdIncomeModel.Applicant i2)
        {
            return i1.EmploymentGrossMonthlyIncome != i2.EmploymentGrossMonthlyIncome
                || i1.CapitalGrossMonthlyIncome != i2.CapitalGrossMonthlyIncome
                || i1.ServiceGrossMonthlyIncome != i2.ServiceGrossMonthlyIncome;
        }

        public void SetHouseholdIncomeModel(string applicationNr, HouseholdIncomeModel model, INTechCurrentUserMetadata user)
        {
            if (string.IsNullOrWhiteSpace(applicationNr))
                throw new ArgumentNullException("applicationNr");
            if (model == null)
                throw new ArgumentNullException("model");
            if (user == null)
                throw new ArgumentNullException("user");

            var currentModel = this.GetHouseholdIncomeModel(applicationNr);

            var updateItems = new List<PartialCreditApplicationModelService.ApplicantUpdateItem>();

            foreach (var newApplicant in model.ApplicantIncomes)
            {
                var currentApplicant = currentModel.ApplicantIncomes.SingleOrDefault(x => x.ApplicantNr == newApplicant.ApplicantNr);
                if (HasIncomeChange(currentApplicant, newApplicant))
                {
                    newApplicant.ChangedByUserId = user.UserId;
                    newApplicant.ChangedDate = clock.Now.DateTime;
                    if (currentApplicant.GetGrossMonthlyIncome() != newApplicant.GetGrossMonthlyIncome())
                    { //NOTE: Not the same as HasIncomeChange since you can just move income between fields without changing the total
                        updateItems.Add(new PartialCreditApplicationModelService.ApplicantUpdateItem
                        {
                            ApplicantNr = newApplicant.ApplicantNr,
                            IsSensitive = false,
                            Name = "incomePerMonthAmount",
                            Value = newApplicant.GetGrossMonthlyIncome().ToString(CultureInfo.InvariantCulture)
                        });
                    }
                }
                else
                {
                    newApplicant.ChangedByUserId = currentApplicant.ChangedByUserId;
                    newApplicant.ChangedDate = currentApplicant.ChangedDate;
                }
            }

            householdIncomeStore.SetValue(applicationNr, JsonConvert.SerializeObject(model));

            if (updateItems.Any())
                this.partialCreditApplicationModelService.Update(applicationNr, user, "UpdateHouseholdIncome", applicantItems: updateItems);
        }
    }

    public interface IMortgageLoanApplicationBasisService
    {
        MortgageLoanApplicationBasisCurrentValuesModel GetCurrentValues(string applicationNr);
        HouseholdIncomeModel GetHouseholdIncomeModel(string applicationNr);
        void SetHouseholdIncomeModel(string applicationNr, HouseholdIncomeModel model, INTechCurrentUserMetadata user);
    }

    public class MortgageLoanApplicationBasisCurrentValuesModel
    {
        public decimal? CombinedGrossMonthlyIncome { get; set; }
    }

    public class HouseholdIncomeModel
    {
        public decimal GetCombinedGrossMonthlyIncome()
        {
            return ApplicantIncomes?.Sum(x => x.GetGrossMonthlyIncome()) ?? 0m;
        }

        public List<Applicant> ApplicantIncomes { get; set; }
        public class Applicant
        {
            public int ApplicantNr { get; set; }
            public decimal EmploymentGrossMonthlyIncome { get; set; }
            public decimal CapitalGrossMonthlyIncome { get; set; }
            public decimal ServiceGrossMonthlyIncome { get; set; }

            public decimal GetGrossMonthlyIncome()
            {
                return EmploymentGrossMonthlyIncome + CapitalGrossMonthlyIncome + ServiceGrossMonthlyIncome;
            }
            public int? ChangedByUserId { get; set; }
            public DateTime? ChangedDate { get; set; }
        }
    }
}