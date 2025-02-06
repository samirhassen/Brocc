using Dapper;
using nCustomer.Code.Services.EidAuthentication;
using nCustomer.Code.Services.Kyc;
using nCustomer.DbModel;
using NTech.Core.Customer.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCustomer.Controllers
{
    public partial class CustomerController : NController
    {
        [HttpPost()]
        public ActionResult RunPeriodicMaintenance(IDictionary<string, string> schedulerData = null)
        {
            Func<string, string> getSchedulerData = s => (schedulerData != null && schedulerData.ContainsKey(s)) ? schedulerData[s] : null;

            return CustomersContext.RunWithExclusiveLock("ntech.scheduledjobs.customerperiodicmaintenance",
                    RunPeriodicMaintenanceI,
                    () => new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Job is already running"));
        }

        private ActionResult RunPeriodicMaintenanceI()
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            var w = Stopwatch.StartNew();
            try
            {
                OneTimeMigration_IncludeInFatcaExport();
                PeriodicMaintenance_RepairSearchTerms();
                new AuthenticationSessionService(CoreClock.SharedInstance).ArchiveOldSessions();
                new KycQuestionsSessionArchiveOnlyService(Service.CustomerContextFactory, CoreClock.SharedInstance).ArchiveOldSessions();
                OneTimeMigration_LegacyCompanyLoanQuestionSets();
                w.Stop();
                NLog.Information($"Customer PeriodicMaintenance finished TotalMilliseconds={w.ElapsedMilliseconds}");
            }
            catch (Exception ex)
            {
                NLog.Error(ex, $"Customer PeriodicMaintenance crashed");
                errors.Add($"Customer PeriodicMaintenance crashed, see error log for details");
            }
            finally
            {
                w.Stop();
            }
            return Json2(new { errors, totalMilliseconds = w.ElapsedMilliseconds, warnings = warnings });
        }

        private void PeriodicMaintenance_RepairSearchTerms()
        {
            //Name, email and phoneNr
            var repository = new CustomerSearchTermRepository(() => new CustomersContext(), GetCurrentUserMetadata(), Clock);
            repository.RepairSearchTerms();

            //Company name
            this.Service.CompanyLoanNameSearch.PopulateSearchTerms(this.GetCurrentUserMetadata());

            //Remove inactive search terms
            using (var db = new CustomersContext())
            {
                var countInactive = db.CustomerSearchTerms.Count(x => !x.IsActive);

                var nrOfLoops = (countInactive / 5000) + 1;
                for (var i = 0; i < nrOfLoops; i++)
                {
                    var count = db.Database.Connection.Execute("delete from CustomerSearchTerm where IsActive = 0 and Id in(select top 5000 d.Id from CustomerSearchTerm d where d.IsActive = 0 order by Id asc)", commandTimeout: 60);
                    if (count == 0)
                        break;
                }
            }
        }

        private void OneTimeMigration_IncludeInFatcaExport()
        {
            const string DoneKey = "OneTimeMigration_IncludeInFatcaExport";

            RunOneTimeMigration(DoneKey, () =>
            {

                var sql =
    @"with Tmp1
as
(
	select	c.CustomerId,
			c.Value as taxcountries
	from	CustomerProperty c
	where	c.IsCurrentData = 1
	and		c.Name = 'taxcountries'
),
Tmp2
as
(
	select	t.*,
			(
				select	c.Value
				from	CustomerProperty c
				where	c.IsCurrentData = 1
				and		c.Name = 'includeInFatcaExport'	
				and		c.CustomerId = t.CustomerId
			) as includeInFatcaExport
	from	Tmp1 t
)
select	t2.CustomerId
from	Tmp2 t2
where	t2.taxcountries = '[{""countryIsoCode"":""[[[country]]]""}]'
and     t2.includeInFatcaExport is null".Replace("[[[country]]]", NEnv.ClientCfg.Country.BaseCountry);

                int[] customerIds;
                using (var context = new CustomersContext())
                {
                    customerIds = context.Database.SqlQuery<int>(sql).ToArray();
                }

                if (customerIds.Length > 0)
                {
                    foreach (var g in customerIds.SplitIntoGroupsOfN(500))
                    {
                        using (var context = new CustomersContext())
                        {
                            using (var tr = context.Database.BeginTransaction())
                            {
                                var repository = CreateWriteRepo(context);
                                repository.UpdateProperties(
                                    g.Select(x => new CustomerPropertyModel
                                    {
                                        CustomerId = x,
                                        IsSensitive = false,
                                        Group = "fatca",
                                        Name = "includeInFatcaExport",
                                        Value = "false"
                                    }).ToList(),
                                    true);

                                context.SaveChanges();
                                tr.Commit();
                            }
                        }
                    }
                }
                return $"{customerIds.Length} updated";
            }, keySpace: DoneKey);
        }

        private void OneTimeMigration_LegacyCompanyLoanQuestionSets()
        {
            RunOneTimeMigration("OneTimeMigration_LegacyCompanyLoanQuestionSets", () =>
            {
                var countMigrated = KycQuestionsBalanziaSeMigrator.MigrateCompanyLoanQuestionSets(this.GetCurrentUserMetadata().CoreUser, this.Clock);
                return $"{countMigrated} migrated";
            });
        }

        private void RunOneTimeMigration(string doneKey, Func<string> migrateReturningMigratonMessage, string keySpace = "OnetimeMigration")
        {
            if (!string.IsNullOrWhiteSpace(Service.KeyValueStore.GetValue(doneKey, keySpace)))
                return;

            var message = migrateReturningMigratonMessage();

            Service.KeyValueStore.SetValue(doneKey, keySpace, message, GetCurrentUserMetadata().CoreUser);
        }
    }
}