using Moq;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using nCredit.DomainModel;
using Newtonsoft.Json;
using NTech.Banking.SieFiles;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.MlStandard.Utilities;
using NTech.Core.Host.IntegrationTests.UlLegacy;
using System.Text;

namespace NTech.Core.Host.IntegrationTests.MlStandard
{
    internal class BookKeepingTests
    {
        private const string? WriteSieFilesToFolder = null;//@"c:\temp\siefiles"; //NOTE: Used to generate files for testing in bookkeeping systems
        private const decimal OneMillion = 1000000m;
        private const decimal AmortizationAmount = 1000m;

        [Test]
        public void NotificationsAndWriteoffs()
        {
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                decimal? secondNotificationInterestBalance = null;
                decimal? secondNotificationDebtBalance = null;

                ClearSieFiles();

                support.MoveToNextDayOfMonth(1);

                var paymentOrderService = support.GetRequiredService<PaymentOrderService>();

                for (var monthIndex = 0; monthIndex < 3; monthIndex++)
                {
                    CreditsMlStandard.RunOneMonth(support, afterDay: dayNr =>
                    {
                        if (dayNr == 1 && monthIndex == 0)
                        {
                            CreditsMlStandard.CreateCredit(support, 1, notificationFeeAmount: 10, loanAmount: OneMillion, monthlyAmoritzationAmount: AmortizationAmount);
                        }
    
                        if (monthIndex == 1 && dayNr == 23)
                        {
                            var mm = support.GetRequiredService< CreditTerminationLettersInactivationBusinessEventManager>();
                            var m = new NotificationWriteOffBusinessEventManager(support.CurrentUser, true, support.CreateCreditContextFactory(),
                                support.Clock, support.ClientConfiguration, support.CreditEnvSettings, paymentOrderService);

                            int notificationId;
                            using (var context = support.CreateCreditContextFactory().CreateContext())
                            {
                                notificationId = context.CreditNotificationHeadersQueryable.OrderByDescending(x => x.Id).Select(x => x.Id).First();
                            }
                            Assert.IsTrue(m.TryWriteOffNotifications(new List<NotificationWriteOffBusinessEventManager.WriteOffInstruction>
                            {
                                new NotificationWriteOffBusinessEventManager.WriteOffInstruction
                                {
                                    NotificationId = notificationId,
                                    PartialWriteOffAmountTypeUniqueIdsAndAmounts = new Dictionary<string, decimal>
                                    {
                                        [PaymentOrderItem.GetUniqueId(CreditDomainModel.AmountType.Interest)] = 1m
                                    }
                                }
                            }, mm, out var errors, out var evt));
                        }

                        if (monthIndex == 2 && dayNr == 17)
                        {
                            var customerRelationMergeService = new Mock<ICustomerRelationsMergeService>();
                            var writeOffMgr = new CreditCorrectAndCloseBusinessEventManager(support.CurrentUser, support.CreateCreditContextFactory(), support.Clock,
                                support.ClientConfiguration, support.CreditEnvSettings, customerRelationMergeService.Object,
                                support.GetRequiredService<PaymentOrderService>());
                            var creditNr = CreditsMlStandard.GetCreatedCredit(support, 1).CreditNr;
                            Assert.IsTrue(writeOffMgr.TryCorrectAndCloseCredit(creditNr, false, out var _, out var __, out var ___));
                        }
                    }, payNotificationsOnDueDate: true,
                    observerBookKeeping: x =>
                    {
                        const decimal NotificationFee = 10m;
                        const decimal Interest = 1686.52m;

                        if (monthIndex == 0 && x.DayNr == 2)
                        {
                            WriteSieFile("loan_created", support, x.File);
                            Assert.That(x.BalancePerAccount.OptS("1608"), Is.EqualTo(OneMillion), DebugMessageBookKeeping(support, x.File));
                        }
                        else if(monthIndex == 0 && x.DayNr == 15)
                        {
                            WriteSieFile("first_notification_created", support, x.File);
                            Assert.That(x.BalancePerAccount.OptS("1510"), Is.EqualTo(NotificationFee + Interest), DebugMessageBookKeeping(support, x.File));
                            Assert.That(x.BalancePerAccount.OptS("3024"), Is.EqualTo(-Interest), DebugMessageBookKeeping(support, x.File));
                            Assert.That(x.BalancePerAccount.OptS("3661"), Is.EqualTo(-NotificationFee), DebugMessageBookKeeping(support, x.File));
                        }
                        else if(monthIndex == 0 && x.DayNr == 29)
                        {
                            WriteSieFile("first_notification_paid", support, x.File);
                            Assert.That(x.BalancePerAccount.OptS("1608"), Is.EqualTo(OneMillion - AmortizationAmount), DebugMessageBookKeeping(support, x.File));
                            Assert.That(x.BalancePerAccount.OptS("1510"), Is.EqualTo(0m), DebugMessageBookKeeping(support, x.File));
                        }
                        else if(monthIndex == 1 && x.DayNr == 15)
                        {
                            WriteSieFile("second_notification_created", support, x.File);
                            secondNotificationDebtBalance = x.BalancePerAccount.OptS("1510");
                            secondNotificationInterestBalance = x.BalancePerAccount.OptS("3024");
                            Assert.That(secondNotificationDebtBalance, Is.GreaterThan(0m), DebugMessageBookKeeping(support, x.File));
                        }
                        else if (monthIndex == 1 && x.DayNr == 24)
                        {
                            WriteSieFile("second_notification_partial_writeoff", support, x.File);//Writeoff one 1 interest
                            Assert.That(x.BalancePerAccount.OptS("1510"), Is.EqualTo(secondNotificationDebtBalance - 1m), DebugMessageBookKeeping(support, x.File));
                            Assert.That(x.BalancePerAccount.OptS("3024"), Is.EqualTo(secondNotificationInterestBalance + 1m), DebugMessageBookKeeping(support, x.File));
                        }
                        else if (monthIndex == 1 && x.DayNr == 29)
                        {
                            WriteSieFile("second_notification_paid", support, x.File);
                            Assert.That(x.BalancePerAccount.OptS("1510"), Is.EqualTo(0m), DebugMessageBookKeeping(support, x.File));
                            Assert.That(x.BalancePerAccount.OptS("3661"), Is.EqualTo(-2m*NotificationFee), DebugMessageBookKeeping(support, x.File));
                        }
                        else if (monthIndex == 2 && x.DayNr == 15)
                        {
                            WriteSieFile("third_notification_created", support, x.File);
                            Assert.That(x.BalancePerAccount.OptS("1510"), Is.GreaterThan(0m), DebugMessageBookKeeping(support, x.File));
                        }
                        else if (monthIndex == 2 && x.DayNr == 18)
                        {
                            WriteSieFile("correct_and_close", support, x.File);
                            Assert.That(x.BalancePerAccount.OptS("1510"), Is.EqualTo(0m), DebugMessageBookKeeping(support, x.File));
                            Assert.That(x.BalancePerAccount.OptS("1608"), Is.EqualTo(0m), DebugMessageBookKeeping(support, x.File));
                            Assert.That(x.BalancePerAccount.OptS("6351"), Is.EqualTo(OneMillion - 2m * AmortizationAmount), DebugMessageBookKeeping(support, x.File));
                        }
                        else
                            Assert.That(x.File.Verifications.Count, Is.EqualTo(0), $"monthIndex={monthIndex}, dayNr={x.DayNr}{Environment.NewLine}" + DebugMessageBookKeeping(support, x.File));
                    });
                }
            });
        }

        [Test]
        public void DebtCollection()
        {
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                ClearSieFiles();

                support.MoveToNextDayOfMonth(1);

                for (var monthIndex = 0; monthIndex < 5; monthIndex++)
                {
                    CreditsMlStandard.RunOneMonth(support, afterDay: dayNr =>
                    {
                        if (dayNr == 1 && monthIndex == 0)
                        {
                            CreditsMlStandard.CreateCredit(support, 1, notificationFeeAmount: 10, loanAmount: OneMillion, monthlyAmoritzationAmount: AmortizationAmount);
                        }
                    }, payNotificationsOnDueDate: false,
                    observerBookKeeping: x =>
                    {
                        if(monthIndex == 0 && x.DayNr == 2)
                        {
                            WriteSieFile($"loan_created", support, x.File);
                        }
                        else if(monthIndex < 4 && x.DayNr == 15)
                        {
                            WriteSieFile($"notification_{monthIndex + 1}_created", support, x.File);
                        }
                        else if(monthIndex == 4 && x.DayNr == 21)
                        {
                            //Sent to debt collection
                            WriteSieFile($"debtcollection_export", support, x.File);
                            Assert.That(x.BalancePerAccount.OptS("1975"), Is.EqualTo(-OneMillion), DebugMessageBookKeeping(support, x.File));
                            Assert.That(x.BalancePerAccount.OptS("6351"), Is.EqualTo(OneMillion), DebugMessageBookKeeping(support, x.File));

                            foreach(var otherAccount in x.BalancePerAccount.Keys.Except(new [] { "1975", "6351" }))
                                Assert.That(x.BalancePerAccount.OptS(otherAccount), Is.EqualTo(0m), DebugMessageBookKeeping(support, x.File));
                        }
                        else
                            Assert.That(x.File.Verifications.Count, Is.EqualTo(0), $"monthIndex={monthIndex}, dayNr={x.DayNr}{Environment.NewLine}" + DebugMessageBookKeeping(support, x.File));
                    });
                }
            });
        }

        [Test]
        public void Settlement()
        {
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                ClearSieFiles();

                support.MoveToNextDayOfMonth(1);
                
                CreditsMlStandard.RunOneMonth(support, afterDay: dayNr =>
                {
                    if (dayNr == 1)
                    {
                        CreditsMlStandard.CreateCredit(support, 1, notificationFeeAmount: 10, loanAmount: OneMillion, monthlyAmoritzationAmount: AmortizationAmount);
                    }
                    if(dayNr == 18)
                    {
                        //Settle loan
                        var creditNr = CreditsMlStandard.GetCreatedCredit(support, 1).CreditNr;
                        Credits.CreateSettlementOffer(support, creditNr, support.Clock.Today, null, null);
                        Credits.CreateAndPlaceUnplacedPayment(support, creditNr, 50000000);
                        Credits.AssertIsSettled(support, creditNr);
                    }
                }, payNotificationsOnDueDate: false,
                observerBookKeeping: x =>
                {
                    if (x.DayNr == 2)
                    {
                        WriteSieFile($"loan_created", support, x.File);
                    }
                    else if (x.DayNr == 15)
                    {
                        WriteSieFile($"notification_created", support, x.File);
                        Assert.That(x.BalancePerAccount.OptS("3024"), Is.EqualTo(-1686.52m), DebugMessageBookKeeping(support, x.File));
                    }
                    else if (x.DayNr == 19)
                    {
                        WriteSieFile($"loan_settled", support, x.File);
                        Assert.That(x.BalancePerAccount.OptS("1608"), Is.EqualTo(0m), DebugMessageBookKeeping(support, x.File));
                        Assert.That(x.BalancePerAccount.OptS("1510"), Is.EqualTo(0m), DebugMessageBookKeeping(support, x.File));
                        Assert.That(x.BalancePerAccount.OptS("3024"), Is.EqualTo(-1084.19m), DebugMessageBookKeeping(support, x.File)); //NOTE: Lower than notified since partially written off due to early settlement
                        Assert.That(x.BalancePerAccount.OptS("3661"), Is.EqualTo(-10m), DebugMessageBookKeeping(support, x.File));
                    }
                    else
                        Assert.That(x.File.Verifications.Count, Is.EqualTo(0), $"dayNr={x.DayNr}{Environment.NewLine}" + DebugMessageBookKeeping(support, x.File));
                });
            });
        }
#pragma warning disable CS0162 // WriteSieFilesToFolder is changed by hand locally to test sie files so we dont care about this compiler warning
        private void WriteSieFile(string scenario, MlStandardTestRunner.TestSupport support, SieBookKeepingFile sieFile)
        {
            if(WriteSieFilesToFolder == null)
                return;

            Directory.CreateDirectory(WriteSieFilesToFolder!);
            sieFile.Save(Path.Combine(WriteSieFilesToFolder!, $"{support.Clock.Today:yyyyMMdd}_{scenario}.si"), allowOverwrite: true);

        }
        private void ClearSieFiles()
        {
            if (WriteSieFilesToFolder == null)
                return;

            if (!Directory.Exists(WriteSieFilesToFolder))
                return;

            foreach (var file in Directory.GetFiles(WriteSieFilesToFolder, "*.si"))
                File.Delete(file);
        }
#pragma warning restore CS0162
        private string DebugMessageBookKeeping(MlStandardTestRunner.TestSupport support, SieBookKeepingFile sieFile)
        {
            StringBuilder b = new();
            b.AppendLine("Verifications in file:");
            foreach (var ver in sieFile.Verifications)
            {
                b.AppendLine(ver.Text);
                foreach (var transaction in ver.Transactions)
                {
                    var accountNr = transaction.Account;                    
                    b.AppendLine($" {transaction.Account}: {transaction.Amount}  ({support.BookKeepingBalancePerAccount.OptS(accountNr)}) [{support.BookKeepingNamePerAccount.Opt(accountNr)}]");
                }
            }
            b.AppendLine("Balance:");
            var balances = support.BookKeepingBalancePerAccount.ToDictionary(x => support.BookKeepingNamePerAccount[x.Key], x => x.Value);
            b.AppendLine(JsonConvert.SerializeObject(balances, Formatting.Indented));
            return b.ToString();
        }
    }
}
