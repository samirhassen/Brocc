using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Credit.Shared.Services;

namespace NTech.Core.Host.IntegrationTests.UlLegacy
{
    public partial class AlternatePaymentPlanTests
    {
        /// <summary>
        /// Customer pays exact amount on due date every month
        /// </summary>
        [Test]
        public void CustomerPaysAllOnDueDateWithFirstMonthPartialEarlyPayment()
        {            
            var messages = SetupMessageObserver();
            try
            {
                var t = new Tester();
                t.RunTestOverMonths(Enumerable.Range(1, 7).Select(monthNr =>
                {         
                    Action<int> d = dayNr =>
                    {
                        var month = Month.ContainingDate(t.Support.Clock.Today);
                        if(monthNr == 1 && dayNr == 1)
                        {
                            ChangeTemplateText(t.Support, "onCreatedTemplateText",
                                @"{{lastDueDate}}
                                {{#months}}{{dueDate}} {{monthlyAmount}}{{/months}}
                                {{totalAmountToPay}}
                                {{creditNr}}");
                            ChangeTemplateText(t.Support, "onNotificationTemplateText", "{{remainingMonthlyAmount}}");
                        }

                        if (monthNr == 1 && dayNr == 17)
                        {
                            //Partially pay first month to see if remainingMonthlyAmount works
                            Credits.CreateAndImportPaymentFileWithOcr(t.Support, new Dictionary<string, decimal> { { "111111118", 1m } });
                        }

                        if (monthNr == 1 && dayNr == 18)
                        {
                            //Plan created on the 17th of month 1 with 6 payments of 100, ..., 101
                            using (var context = t.CreateCreditContext())
                            {
                                var plan = context.AlternatePaymentPlanHeadersQueryable.Select(x => new { Header = x, Months = x.Months }).Single();
                                var planMonths = plan.Months;
                                Assert.That(planMonths.First().MonthAmount, Is.EqualTo(100m));
                                Assert.That(planMonths.Last().MonthAmount, Is.EqualTo(102.31m));
                            }
                        }

                        if (monthNr > 1 && dayNr == 15)
                        {
                            using (var context = t.CreateCreditContext())
                            {
                                var wasNotified = context.CreditNotificationHeadersQueryable.Any(x => x.TransactionDate > month.FirstDate);
                                Assert.That(wasNotified, monthNr > 6 ? Is.True : Is.False, "Notification");
                                if (wasNotified) t.PrintRepaymentTime("notified after plan");
                            }
                        }

                        if (monthNr <= 6 && dayNr == 28)
                        {
                            Credits.CreateAndImportPaymentFileWithOcr(t.Support, new Dictionary<string, decimal> { { "111111118", 
                                    monthNr == 1 ? 99m : (monthNr == 6 ? 102.31m : 100m) } });
                            using (var context = t.CreateCreditContext())
                            {
                                var plan = context.AlternatePaymentPlanHeadersQueryable.Single();
                                //Fully paid month 6
                                Assert.That(plan.FullyPaidByEventId, monthNr == 6 ? Is.Not.Null : Is.Null, "Payment plan fully paid");
                                var futurePaymentCount = Credits.GetCurrentPaymentPlan(t.Support, t.CreditNr).Payments.Count;
                                if (monthNr == 6)
                                    Assert.That(futurePaymentCount, Is.EqualTo(plan.FuturePaymentPlanMonthCount), "Expected repayment months after plan");
                            }
                        }
                    };
                    return d;
                }).ToArray());

                var onCreatedMessages = messages.Where(x => x.TemplateName == "onCreatedTemplateText").ToList();
                var onCreatedMessageLines = onCreatedMessages.Single().MessageText.Split('\r').Select(x => x.Trim()).ToList();
                Assert.Multiple(() =>
                {
                    Assert.That(onCreatedMessageLines.FirstOrDefault(), Is.EqualTo("28.10.2022"));
                    Assert.That(onCreatedMessageLines.Skip(1).FirstOrDefault(), Is.EqualTo("28.5.2022 100,0028.6.2022 100,0028.7.2022 100,0028.8.2022 100,0028.9.2022 100,0028.10.2022 102,31"));
                    Assert.That(onCreatedMessageLines.Skip(2).FirstOrDefault(), Is.EqualTo("602,31"));
                    Assert.That(onCreatedMessageLines.Skip(3).FirstOrDefault(), Is.EqualTo("C9871"));

                    Assert.That(onCreatedMessages, Has.Count.EqualTo(1));                    
                    Assert.That(messages.Where(x => x.TemplateName == "onMissedPayment").Count(), Is.EqualTo(0));
                });
                var onNotificationTemplateTextMessages = messages.Where(x => x.TemplateName == "onNotificationTemplateText").ToList();
                Assert.Multiple(() =>
                {
                    Assert.That(onNotificationTemplateTextMessages.Count, Is.EqualTo(6));
                    if(onNotificationTemplateTextMessages.Count > 0)
                        Assert.That(onNotificationTemplateTextMessages[0].MessageText, Is.EqualTo("99,00"), "remainingMonthlyAmount - Expected partially paid first notification"); //Partially paid 1
                });
            }
            finally
            {
                TeardownMessageObserver();
            }            
        }

        /// <summary>
        /// Customer pays all of it right after starting the plan.
        /// Make sure we starting up normal payments early.
        /// </summary>
        [Test]
        public void CustomerPaysAllAtOnce()
        {
            var t = new Tester();
            t.RunTestOverMonths(Enumerable.Range(1, 2).Select(monthNr =>
            {
                Action<int> d = dayNr =>
                {
                    var month = Month.ContainingDate(t.Support.Clock.Today);

                    if (monthNr > 1 && dayNr == 15)
                    {
                        using (var context = t.CreateCreditContext())
                        {
                            var wasNotified = context.CreditNotificationHeadersQueryable.Any(x => x.TransactionDate > month.FirstDate);
                            Assert.That(wasNotified, monthNr > 1 ? Is.True : Is.False, "Notification");
                            if (wasNotified) t.PrintRepaymentTime("notified after plan");
                        }
                    }

                    if (monthNr == 1 && dayNr == 18)
                    {
                        using (var context = t.CreateCreditContext())
                        {
                            var plan = context.AlternatePaymentPlanHeadersQueryable.Single();
                            //Fully paid month 6 when by the notification job
                            Assert.That(plan.FullyPaidByEventId, Is.Null, "Payment plan fully paid");
                        }
                    }

                    if (monthNr == 1 && dayNr == 19)
                    {
                        Credits.CreateAndImportPaymentFileWithOcr(t.Support, new Dictionary<string, decimal> { { "111111118", 102.31m + (100m * 5) } });
                    }
                };
                return d;
            }).ToArray());
        }

        /// <summary>
        /// Customer pays 3 times on due date and then stops paying
        /// </summary>
        [Test]
        public void CustomerStopsPayingAfterThreeMonths()
        {
            var t = new Tester();
            t.RunTestOverMonths(Enumerable.Range(1, 7).Select(monthNr =>
            {
                Action<int> d = dayNr =>
                {
                    var month = Month.ContainingDate(t.Support.Clock.Today);
                    if (dayNr == 1) Console.WriteLine($"[monthNr={monthNr}]: Month={t.Support.Clock.Today.ToString("yyyy-MM-dd")}");

                    if (monthNr == 1 && dayNr == 18)
                    {
                        //Plan created on the 17th of month 1 with 6 payments of 100, ..., 101
                        using (var context = t.CreateCreditContext())
                        {
                            var plan = context.AlternatePaymentPlanHeadersQueryable.Select(x => new { Header = x, Months = x.Months }).Single();
                            var planMonths = plan.Months;
                            Assert.That(planMonths.First().MonthAmount, Is.EqualTo(100m));
                            Assert.That(planMonths.Last().MonthAmount, Is.EqualTo(102.31m));
                        }
                    }

                    if (monthNr > 1 && dayNr == 15)
                    {
                        using (var context = t.CreateCreditContext())
                        {
                            var wasNotified = context.CreditNotificationHeadersQueryable.Any(x => x.TransactionDate > month.FirstDate);
                            Assert.That(wasNotified, monthNr >= 5 ? Is.True : Is.False, $"[month={monthNr}]: Notification");
                            if (wasNotified) t.PrintRepaymentTime("notified after plan");
                        }
                    }

                    if (monthNr <= 6 && dayNr == 28)
                    {
                        if(monthNr <= 3) Credits.CreateAndImportPaymentFile(t.Support, new Dictionary<string, decimal> { { t.CreditNr, 100m } });

                        using (var context = t.CreateCreditContext())
                        {
                            var plan = context.AlternatePaymentPlanHeadersQueryable.Single();
                            
                            Assert.That(plan.CancelledByEventId, monthNr >= 5 ? Is.Not.Null : Is.Null,  $"[month={monthNr}]: Payment plan cancelled");
                            var futurePaymentCount = Credits.GetCurrentPaymentPlan(t.Support, t.CreditNr).Payments.Count;
                            if (monthNr == 5)
                                Assert.That(futurePaymentCount, Is.EqualTo(plan.FuturePaymentPlanMonthCount), "Expected repayment months after plan");
                        }
                    }
                };
                return d;
            }).ToArray());
            t.AssertMessagesSent("onNotificationTemplateText", 
                new DateTime(2022, 5, 18), new DateTime(2022, 6, 14), 
                new DateTime(2022, 7, 14), new DateTime(2022, 8, 14));
        }

        /// <summary>
        /// Customer does not pay any months
        /// </summary>
        [Test]
        public void CustomerNeverPays_PlanStartsSameMonth()
        {
            var messages = SetupMessageObserver();
            try
            {
                var t = new Tester();
                t.RunTestOverMonths(Enumerable.Range(1, 7).Select(monthNr =>
                {
                    Action<int> d = dayNr =>
                    {
                        var month = Month.ContainingDate(t.Support.Clock.Today);
                        if (dayNr == 1) Console.WriteLine($"[monthNr={monthNr}]: Month={t.Support.Clock.Today.ToString("yyyy-MM-dd")}");

                        if (monthNr == 1 && dayNr == 18)
                        {
                            //Plan created on the 17th of month 1 with 6 payments of 100, ..., 101
                            using (var context = t.CreateCreditContext())
                            {
                                var plan = context.AlternatePaymentPlanHeadersQueryable.Select(x => new { Header = x, Months = x.Months }).Single();
                                var planMonths = plan.Months;
                                Assert.That(planMonths.First().MonthAmount, Is.EqualTo(100m));
                                Assert.That(planMonths.Last().MonthAmount, Is.EqualTo(102.31m));
                            }
                        }

                        if (monthNr > 1 && dayNr == 15)
                        {
                            using (var context = t.CreateCreditContext())
                            {
                                var wasNotified = context.CreditNotificationHeadersQueryable.Any(x => x.TransactionDate > month.FirstDate);
                                //This will be sent to debt collection so no month 6
                                Assert.That(wasNotified, monthNr <= 5 ? Is.True : Is.False, $"[month={monthNr}]: Notification");
                                if (wasNotified) t.PrintRepaymentTime("notified after plan");
                                if (monthNr == 6)
                                {
                                    Assert.That(context.OutgoingDebtCollectionFileHeadersQueryable.Any(), Is.True, "Debt collection");
                                }
                            }
                        }

                        if (monthNr <= 6 && dayNr == 28)
                        {
                            using (var context = t.CreateCreditContext())
                            {
                                var plan = context.AlternatePaymentPlanHeadersQueryable.Single();

                                Assert.That(plan.CancelledByEventId, monthNr > 1 ? Is.Not.Null : Is.Null, $"[month={monthNr}]: Payment plan cancelled");
                                var futurePaymentCount = Credits.GetCurrentPaymentPlan(t.Support, t.CreditNr).Payments.Count;
                                if (monthNr == 2) //+1 since a notification has happened on the 14th from what we recalculated to
                                    Assert.That(futurePaymentCount + 1, Is.EqualTo(plan.FuturePaymentPlanMonthCount));
                            }
                        }
                    };
                    return d;
                }).ToArray());

                Assert.Multiple(() =>
                {
                    Assert.That(messages.Where(x => x.TemplateName == "onCreatedTemplateText").Count(), Is.EqualTo(1));
                    Assert.That(messages.Where(x => x.TemplateName == "onNotificationTemplateText").Count(), Is.EqualTo(1));
                    Assert.That(messages.Where(x => x.TemplateName == "onMissedPaymentTemplateText").Count(), Is.EqualTo(1));
                });
            }
            finally
            {
                TeardownMessageObserver();
            }            
        }

        /// <summary>
        /// Customer does not pay any months
        /// </summary>
        [Test]
        public void CustomerNeverPays_PlanStartsNextMonth()
        {
            var messages = SetupMessageObserver();
            try
            {
                var t = new Tester();
                t.PlanStartDayNr = 27;
                t.RunTestOverMonths(Enumerable.Range(1, 7).Select(monthNr =>
                {
                    Action<int> d = dayNr =>
                    {
                        var month = Month.ContainingDate(t.Support.Clock.Today);
                        if (dayNr == 1) Console.WriteLine($"[monthNr={monthNr}]: Month={t.Support.Clock.Today.ToString("yyyy-MM-dd")}");

                        if (monthNr == 1 && dayNr == 28)
                        {
                            //Plan created on the 27th of month 1 with 6 payments of 100, ..., 101                        
                            using (var context = t.CreateCreditContext())
                            {
                                var plan = context.AlternatePaymentPlanHeadersQueryable.Select(x => new { Header = x, Months = x.Months }).Single();
                                var planMonths = plan.Months;
                                Assert.That(planMonths.First().MonthAmount, Is.EqualTo(100m));
                                Assert.That(planMonths.Last().MonthAmount, Is.EqualTo(102.31m));
                            }
                        }

                        if (monthNr > 1 && dayNr == 15)
                        {
                            using (var context = t.CreateCreditContext())
                            {
                                var wasNotified = context.CreditNotificationHeadersQueryable.Any(x => x.TransactionDate > month.FirstDate);
                                //This will be sent to debt collection so no month 6
                                Assert.That(wasNotified, monthNr != 2 && monthNr < 7 ? Is.True : Is.False, $"[month={monthNr}]: Notification");
                                if (wasNotified) t.PrintRepaymentTime("notified after plan");
                                if (monthNr == 7)
                                {
                                    Assert.That(context.OutgoingDebtCollectionFileHeadersQueryable.Any(), Is.True, "Debt collection");
                                }
                            }
                        }

                        if (monthNr <= 6 && dayNr == 28)
                        {
                            using (var context = t.CreateCreditContext())
                            {
                                var plan = context.AlternatePaymentPlanHeadersQueryable.Single();

                                Assert.That(plan.CancelledByEventId, monthNr > 2 ? Is.Not.Null : Is.Null, $"[month={monthNr}]: Payment plan cancelled");
                                var futurePaymentCount = Credits.GetCurrentPaymentPlan(t.Support, t.CreditNr).Payments.Count;
                                if (monthNr == 3)
                                    Assert.That(futurePaymentCount, Is.EqualTo(plan.FuturePaymentPlanMonthCount)); //TODO: Is this really right?
                            }
                        }
                    };
                    return d;
                }).ToArray());

                Assert.Multiple(() =>
                {
                    Assert.That(messages.Where(x => x.TemplateName == "onCreatedTemplateText").Count(), Is.EqualTo(1));
                    Assert.That(messages.Where(x => x.TemplateName == "onNotificationTemplateText").Count(), Is.EqualTo(1));
                    Assert.That(messages.Where(x => x.TemplateName == "onMissedPaymentTemplateText").Count(), Is.EqualTo(1));
                });
            }
            finally
            {
                TeardownMessageObserver();
            }            
        }

        /// <summary>
        /// Customer pays all on due date, gets monthly "onNotification" secure messages
        /// </summary>
        [Test]
        public void CustomerPaysAllOnDueDate_GetsOnNotificationMessages()
        {
            var messages = SetupMessageObserver();
            try
            {
                var t = new Tester();
                t.RunTestOverMonths(Enumerable.Range(1, 7).Select(monthNr =>
                {
                    Action<int> d = dayNr =>
                    {
                        ChangeTemplateText(t.Support, "onNotificationTemplateText",
                              @"{{monthlyAmount}}
                                {{dueDate}}");

                        var month = Month.ContainingDate(t.Support.Clock.Today);
                        if (monthNr <= 6 && dayNr == 28)
                        {
                            Credits.CreateAndPlaceUnplacedPayment(t.Support, t.CreditNr, monthNr == 6 ? 102.31m : 100m);
                        }
                    };
                    return d;
                }).ToArray());

                var onNotificationMessages = messages.Where(x => x.TemplateName == "onNotificationTemplateText").ToList();
                for (int i = 0; i < onNotificationMessages.Count; i++)
                {
                    var notificationMessageLines = onNotificationMessages[i].MessageText.Split('\r').Select(x => x.Trim()).ToList();
                    Assert.Multiple(() =>
                    {
                        Assert.That(notificationMessageLines.FirstOrDefault(), Is.EqualTo(i == 5 ? "102,31" : "100,00"));
                        Assert.That(notificationMessageLines.Skip(1).FirstOrDefault(), Is.EqualTo($"28.{i + 5}.2022"));
                    });
                }

                Assert.That(onNotificationMessages, Has.Count.EqualTo(6));
            }
            finally
            {
                TeardownMessageObserver();
            }
        }

        /// <summary>
        /// Customer does not pay any months, gets "onMissedPayment" secure message
        /// </summary>
        [Test]
        public void CustomerNeverPays_GetsOnMissedPaymentMessages()
        {
            var messages = SetupMessageObserver();
            try
            {
                var t = new Tester();
                t.RunTestOverMonths(Enumerable.Range(1, 7).Select(monthNr =>
                {
                    Action<int> d = dayNr =>
                    {
                        ChangeTemplateText(t.Support, "onMissedPaymentTemplateText",
                              @"{{creditNr}}
                                {{dueDate}}
                                {{minimumAmountToPay}}");
                    };
                    return d;
                }).ToArray());

                var onMissedPaymentMessages = messages.Where(x => x.TemplateName == "onMissedPaymentTemplateText").ToList();
                var onMissedPaymentMessageLines = onMissedPaymentMessages.Single().MessageText.Split('\r').Select(x => x.Trim()).ToList();

                Assert.Multiple(() =>
                {
                    Assert.That(onMissedPaymentMessageLines.FirstOrDefault(), Is.EqualTo("C9871"));
                    Assert.That(onMissedPaymentMessageLines.Skip(1).FirstOrDefault(), Is.EqualTo("28.5.2022"));
                    Assert.That(onMissedPaymentMessageLines.Skip(2).FirstOrDefault(), Is.EqualTo("100,00"));
                    Assert.That(onMissedPaymentMessages, Has.Count.EqualTo(1));
                });
            }
            finally
            {
                TeardownMessageObserver();
            }
        }

        /// <summary>
        /// Customer pays 2 times on due date then makes partial payment, gets "onMissedPayment" secure message
        /// </summary>
        [Test]
        public void CustomerPartiallyPays_GetsOnMissedPaymentMessages()
        {
            var messages = SetupMessageObserver();
            try
            {
                var t = new Tester();
                t.RunTestOverMonths(Enumerable.Range(1, 7).Select(monthNr =>
                {
                    Action<int> d = dayNr =>
                    {
                        ChangeTemplateText(t.Support, "onMissedPaymentTemplateText",
                           @"{{creditNr}}
                                {{dueDate}}
                                {{minimumAmountToPay}}");

                        if (monthNr <= 6 && dayNr == 28)
                        {
                            if (monthNr <= 2) 
                                Credits.CreateAndImportPaymentFile(t.Support, new Dictionary<string, decimal> { { t.CreditNr, 100m } });
                            if (monthNr == 3) 
                                Credits.CreateAndImportPaymentFile(t.Support, new Dictionary<string, decimal> { { t.CreditNr, 58m } });
                        }
                    };
                    return d;
                }).ToArray());

                var onMissedPaymentMessages = messages.Where(x => x.TemplateName == "onMissedPaymentTemplateText").ToList();
                var onMissedPaymentMessageLines = onMissedPaymentMessages.Single().MessageText.Split('\r').Select(x => x.Trim()).ToList();

                Assert.Multiple(() =>
                {
                    Assert.That(onMissedPaymentMessageLines.FirstOrDefault(), Is.EqualTo("C9871"));
                    Assert.That(onMissedPaymentMessageLines.Skip(1).FirstOrDefault(), Is.EqualTo("28.7.2022"));
                    Assert.That(onMissedPaymentMessageLines.Skip(2).FirstOrDefault(), Is.EqualTo("42,00"));
                    Assert.That(onMissedPaymentMessages, Has.Count.EqualTo(1));
                });
            }
            finally
            {
                TeardownMessageObserver();
            }
        }

        private static void ChangeTemplateText(SupportShared support, string templateName, string newTemplateText)
        {
            var settingsService = support.CreateSettingsService();
            var settingValues = settingsService.LoadSettingsValues("altPaymentPlanSecureMessageTemplates");
            settingValues[templateName] = newTemplateText;
            settingsService.SaveSettingsValues("altPaymentPlanSecureMessageTemplates", settingValues, (IsSystemUser: true, GroupMemberships: new HashSet<string>()));
        }

        private List<(string TemplateName, Dictionary<string, object> TemplateDataMines, string MessageText)> SetupMessageObserver()
        {
            var messages = new List<(string TemplateName, Dictionary<string, object> TemplateDataMines, string MessageText)>();
            Action<(string TemplateName, Dictionary<string, object> TemplateDataMines, string MessageText)> observeSendSecureMessage = x => messages.Add(x);
            AlternatePaymentPlanSecureMessagesService.ObserveSendSecureMessage = observeSendSecureMessage;
            return messages;
        }

        private void TeardownMessageObserver() => AlternatePaymentPlanSecureMessagesService.ObserveSendSecureMessage = null;
    }
}
