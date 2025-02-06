using nCredit.DbModel.BusinessEvents;
using Newtonsoft.Json;
using NTech.Core.Credit.Shared.Models;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.MlStandard.Utilities;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests.MlStandard
{
    public class MultiLoanCreationTests
    {
        [Test]
        public void CreateLoans()
        {
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                support.Now = new DateTimeOffset(2022, 1, 1, support.Now.Hour, support.Now.Minute, support.Now.Second, support.Now.Offset);
                const decimal OneMillion = 1000000m;
                var service = support.GetRequiredService<SwedishMortageLoanCreationService>();

                var loanRequest = new SwedishMortgageLoanCreationRequest
                {
                    NewCollateral = new SwedishMortgageLoanCreationRequest.CollateralModel
                    {
                        IsBrfApartment = true,
                        BrfOrgNr = "5590406483", //Nakergals orgnr
                        BrfName = "Nakter gallant AB",
                        BrfApartmentNr = "S42",
                        TaxOfficeApartmentNr = "1105",
                        AddressStreet = "High mountain way 12",
                        AddressZipcode = "111 11",
                        AddressCity = "Le town",
                        AddressMunicipality = "Le city"
                    },
                    Loans = new List<SwedishMortgageLoanCreationRequest.SeMortgageLoanModel>
                    {
                        new SwedishMortgageLoanCreationRequest.SeMortgageLoanModel
                        {
                            MonthlyFeeAmount = 20m,
                            NominalInterestRatePercent = 1.5m,
                            Applicants = new List<MortgageLoanRequest.Applicant>
                            {
                                new MortgageLoanRequest.Applicant
                                {
                                    ApplicantNr = 1,
                                    CustomerId = TestPersons.EnsureTestPerson(support, 1),
                                    OwnershipPercent = 50m
                                },
                                new MortgageLoanRequest.Applicant
                                {
                                    ApplicantNr = 2,
                                    CustomerId = TestPersons.EnsureTestPerson(support, 2),
                                    OwnershipPercent = 50m
                                },
                            },
                            ProviderName = "self",
                            LoanAmount = OneMillion,
                            EndDate = support.Clock.Today.AddYears(40),
                            CreditNr = "L10001",
                            NextInterestRebindDate = support.Clock.Today.AddMonths(3),
                            InterestRebindMounthCount = 3,
                            ReferenceInterestRate = 0.2m,
                            ConsentingPartyCustomerIds = new List<int>
                            {
                                TestPersons.EnsureTestPerson(support, 1), TestPersons.EnsureTestPerson(support, 2)
                            },
                            PropertyOwnerCustomerIds = new List<int>
                            {
                                TestPersons.EnsureTestPerson(support, 1), TestPersons.EnsureTestPerson(support, 2)
                            }
                        },
                        new SwedishMortgageLoanCreationRequest.SeMortgageLoanModel
                        {
                            MonthlyFeeAmount = 20m,
                            NominalInterestRatePercent = 2.5m,
                            Applicants = new List<MortgageLoanRequest.Applicant>
                            {
                                new MortgageLoanRequest.Applicant
                                {
                                    ApplicantNr = 1,
                                    CustomerId = TestPersons.EnsureTestPerson(support, 1),
                                    OwnershipPercent = 50m
                                },
                                new MortgageLoanRequest.Applicant
                                {
                                    ApplicantNr = 2,
                                    CustomerId = TestPersons.EnsureTestPerson(support, 2),
                                    OwnershipPercent = 50m
                                },
                            },
                            ProviderName = "self",
                            LoanAmount = OneMillion / 2m,
                            EndDate = support.Clock.Today.AddYears(40),
                            CreditNr = "L10002",
                            NextInterestRebindDate = support.Clock.Today.AddMonths(12),
                            InterestRebindMounthCount = 12,
                            ReferenceInterestRate = 0.2m,
                            ConsentingPartyCustomerIds = new List<int>
                            {
                                TestPersons.EnsureTestPerson(support, 1), TestPersons.EnsureTestPerson(support, 2)
                            },
                            PropertyOwnerCustomerIds = new List<int>
                            {
                                TestPersons.EnsureTestPerson(support, 1), TestPersons.EnsureTestPerson(support, 2)
                            }
                        }
                    }
                };

                loanRequest.AmortizationBasis = SwedishMortgageLoanAmortizationBasisService.CalculateSuggestedAmortizationBasis(
                    new CalculateMortgageLoanAmortizationBasisRequest
                    {
                        CombinedYearlyIncomeAmount = OneMillion * 0.70m,
                        ObjectValueAmount = 1.7m * loanRequest.Loans.Sum(x => x.LoanAmount ?? 0m),
                        OtherMortageLoansBalanceAmount = OneMillion * 0.15m,
                        NewLoans = loanRequest.Loans.Select(x => new CalculateMortgageLoanAmortizationBasisRequest.MlAmortizationBasisRequestNewLoan
                        {
                            CreditNr = x.CreditNr,
                            CurrentBalanceAmount = x.LoanAmount ?? 0m
                        }).ToList()
                    },
                    support.Clock.Today);

                loanRequest.Loans.ForEach(x =>
                {
                    var basisLoan = loanRequest.AmortizationBasis.Loans.Single(y => y.CreditNr == x.CreditNr);
                    x.FixedMonthlyAmortizationAmount = loanRequest.AmortizationBasis.Loans.Single(y => y.CreditNr == x.CreditNr).MonthlyAmortizationAmount;
                });

                service.CreateLoans(loanRequest);

                Credits.NotificationRenderer.ObservePrintContext = x =>
                {
                    TestContext.WriteLine($"Notification({x.ArchiveFilename}):{Environment.NewLine}{JsonConvert.SerializeObject(x.Context, Formatting.Indented)}");
                };
                try
                {
                    CreditsMlStandard.RunOneMonth(support);
                    CreditsMlStandard.RunOneMonth(support);
                    CreditsMlStandard.RunOneMonth(support);
                    CreditsMlStandard.RunOneMonth(support);
                }
                finally
                {
                    Credits.NotificationRenderer.ObservePrintContext = null;
                }
            });
        }
    }
}
