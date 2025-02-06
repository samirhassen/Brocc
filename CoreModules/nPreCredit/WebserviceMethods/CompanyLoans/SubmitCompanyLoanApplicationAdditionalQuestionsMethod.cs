using Newtonsoft.Json;
using nPreCredit.Code.Email;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.CompanyLoans;
using NTech.Banking.Conversion;
using NTech.Core.Module.Shared.Clients;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class SubmitCompanyLoanApplicationAdditionalQuestionsMethod : TypedWebserviceMethod<SubmitCompanyLoanApplicationAdditionalQuestionsMethod.Request, SubmitCompanyLoanApplicationAdditionalQuestionsMethod.Response>
    {
        public override string Path => "CompanyLoan/Submit-AdditionalQuestions";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();

            var clientCfg = resolver.Resolve<NTech.Services.Infrastructure.IClientConfiguration>();
            var bankAccountNrParser = new NTech.Banking.BankAccounts.BankAccountNumberParser(clientCfg.Country.BaseCountry);
            var bankAccountNrTypeParsed = Enums.Parse<NTech.Banking.BankAccounts.BankAccountNumberTypeCode>(request.BankAccountNrType, ignoreCase: true);
            if (!bankAccountNrTypeParsed.HasValue)
                return Error("Invalid bank account nr type", errorCode: "invalidBankAccountNrType");
            if (!bankAccountNrParser.TryParseBankAccount(request.BankAccountNr, bankAccountNrTypeParsed.Value, out var bankAccountNrParsed))
                return Error("Invalid bank account nr", errorCode: "invalidBankAccountNr");

            var repo = resolver.Resolve<IPartialCreditApplicationModelRepository>();
            var app = repo.Get(request.ApplicationNr, new PartialCreditApplicationModelRequest
            {
                ApplicationFields = new List<string> { "applicantCustomerId", "applicantEmail" }
            });

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                context.BeginTransaction();
                try
                {
                    var header = context.CreditApplicationHeaders.Include("Items").SingleOrDefault(x => x.ApplicationNr == request.ApplicationNr);
                    if (header == null)
                        return Error("No such application exists", errorCode: "notFound");

                    var wFlowService = resolver.Resolve<ICompanyLoanWorkflowService>();

                    var evt = context.CreateAndAddEvent(CreditApplicationEventCode.CompanyLoanAnsweredAdditionalQuestions, creditApplicationHeader: header);

                    wFlowService.ChangeStepStatusComposable(context, "AdditionalQuestions", "Accepted", application: header);

                    var questions = new AdditionalQuestionsDocumentModel
                    {
                        AnswerDate = requestContext.Clock().Now,
                        Items = new List<AdditionalQuestionsDocumentModel.Item>()
                    };

                    void AddKycQuestions(PersonBaseModel personModel, int customerId)
                    {
                        if (personModel.AnsweredYesOnPepQuestion.HasValue)
                        {
                            questions.Items.Add(new AdditionalQuestionsDocumentModel.Item
                            {
                                AnswerCode = personModel.AnsweredYesOnPepQuestion.Value ? "true" : "false",
                                QuestionCode = "isPep",
                                IsCustomerQuestion = true,
                                QuestionGroup = "customer",
                                CustomerId = customerId
                            });

                            if (personModel.AnsweredYesOnPepQuestion.Value && !string.IsNullOrWhiteSpace(personModel.PepRole))
                                questions.Items.Add(new AdditionalQuestionsDocumentModel.Item
                                {
                                    AnswerText = personModel.PepRole,
                                    QuestionCode = "pepWho",
                                    IsCustomerQuestion = true,
                                    QuestionGroup = "customer",
                                    CustomerId = customerId
                                });
                        }

                        if (personModel.AnsweredYesOnIsUSPersonQuestion.HasValue)
                        {
                            questions.Items.Add(new AdditionalQuestionsDocumentModel.Item
                            {
                                AnswerCode = personModel.AnsweredYesOnIsUSPersonQuestion.Value ? "true" : "false",
                                QuestionCode = "answeredYesOnIsUSPersonQuestion",
                                IsCustomerQuestion = true,
                                QuestionGroup = "customer",
                                CustomerId = customerId
                            });
                        }
                    }

                    void AddQuestion(string questionCode, string answerTextOrCode, string groupName, int? customerId, bool answerIsCode)
                    {
                        if (string.IsNullOrWhiteSpace(answerTextOrCode)) return;
                        questions.Items.Add(new AdditionalQuestionsDocumentModel.Item
                        {
                            QuestionCode = questionCode,
                            AnswerCode = answerIsCode ? answerTextOrCode : null,
                            AnswerText = answerIsCode ? null : answerTextOrCode,
                            QuestionGroup = groupName,
                            CustomerId = customerId
                        });
                    }

                    //--------------------------------------
                    //-- Bank account ----------------------
                    //--------------------------------------

                    context.AddOrUpdateCreditApplicationItems(
                        header,
                        new List<PreCreditContextExtended.CreditApplicationItemModel>
                        {
                        new PreCreditContextExtended.CreditApplicationItemModel { GroupName = "application", Name = "bankAccountNrType", Value = bankAccountNrParsed.AccountType.ToString(), IsEncrypted = false },
                        new PreCreditContextExtended.CreditApplicationItemModel { GroupName = "application", Name = "bankAccountNr", Value = bankAccountNrParsed.FormatFor(null), IsEncrypted = true },
                        new PreCreditContextExtended.CreditApplicationItemModel { GroupName = "application", Name = "additionalQuestionsAnswerDate", Value = requestContext.Clock().Now.ToString("o"), IsEncrypted = false },
                        }
                        , "additionalQuestions");

                    AddQuestion("bankAccountNrType", bankAccountNrParsed.AccountType.ToString(), "bankAccount", null, true);
                    AddQuestion("bankAccountNr", bankAccountNrParsed.FormatFor("display"), "bankAccount", null, false);

                    //--------------------------------------
                    //-- Collateral ------------------------
                    //--------------------------------------
                    int collateralCustomerId;

                    var customerClient = new Lazy<ICustomerClient>(resolver.Resolve<ICustomerClient>);
                    AddQuestion("isApplicantCollateral", request.Collateral.IsApplicant.GetValueOrDefault() ? "true" : "false", "collateral", null, true);
                    if (request.Collateral.IsApplicant.GetValueOrDefault())
                    {
                        collateralCustomerId = app.Application.Get("applicantCustomerId").IntValue.Required;
                    }
                    else
                    {
                        var p = request.Collateral.NonApplicantPerson;
                        collateralCustomerId = CreateOrUpdateCustomer("AdditionalQuestionsCollateral", request.ApplicationNr, customerClient.Value, p, withAddProperty: add =>
                        {
                            add("email", p.Email, true);
                            add("phone", p.Phone, true);
                        });
                        AddKycQuestions(p, collateralCustomerId);
                        AddQuestion("collateralFirstName", p.FirstName, "collateral", collateralCustomerId, false);
                        AddQuestion("collateralLastName", p.LastName, "collateral", collateralCustomerId, false);
                        AddQuestion("collateralEmail", p.Email, "collateral", collateralCustomerId, false);
                        AddQuestion("collateralPhone", p.Phone, "collateral", collateralCustomerId, false);
                    }

                    var customerListService = resolver.Resolve<CreditApplicationCustomerListService>();
                    customerListService.SetMemberStatusComposable(context, "companyLoanCollateral", true, collateralCustomerId, applicationNr: request.ApplicationNr, evt: evt);

                    //--------------------------------------
                    //-- Beneficial owners -----------------
                    //--------------------------------------
                    foreach (var bOwner in request.BeneficialOwners)
                    {
                        var customerId = CreateOrUpdateCustomer("AdditionalQuestionsBeneficialOwner", request.ApplicationNr, customerClient.Value, bOwner);
                        var customerQuestionsSet = PopulateCustomerQuestionsSet(bOwner, customerId, requestContext.Clock().Now);
                        if (customerQuestionsSet != null)
                        {
                            customerClient.Value.AddCustomerQuestionsSet(customerQuestionsSet, "CompanyLoanApplication", request.ApplicationNr);
                        }

                        customerListService.SetMemberStatusComposable(context, "companyLoanBeneficialOwner", true, customerId, applicationNr: request.ApplicationNr, evt: evt);
                        if (!string.IsNullOrWhiteSpace(bOwner.Connection))
                            KeyValueStoreService.SetValueComposable(context, customerId.ToString(), $"companyLoanConnection_{request.ApplicationNr}", bOwner.Connection?.Trim());
                        if (bOwner.OwnershipPercent.HasValue)
                            KeyValueStoreService.SetValueComposable(context, customerId.ToString(), $"companyLoanOwnershipPercent_{request.ApplicationNr}", bOwner.OwnershipPercent.Value.ToString(CultureInfo.InvariantCulture));
                        AddKycQuestions(bOwner, customerId);

                        AddQuestion("beneficialOwnerFirstName", bOwner.FirstName, "beneficialOwner", customerId, false);
                        AddQuestion("beneficialOwnerLastName", bOwner.LastName, "beneficialOwner", customerId, false);
                        if (!string.IsNullOrWhiteSpace(bOwner.Connection))
                        {
                            AddQuestion("beneficialOwnerConnection", bOwner.Connection, "beneficialOwner", customerId, false);
                        }
                        AddQuestion("beneficialOwnerOwnershipPercent", bOwner.OwnershipPercent?.ToString(CultureInfo.InvariantCulture), "beneficialOwner", customerId, false);
                    }

                    AddQuestion("beneficialOwnerPercentCount", request.BeneficialOwners.Count(x => x.OwnershipPercent.HasValue).ToString(), "beneficialOwner", null, true);

                    //--------------------------------------
                    //-- Additional questions --------------
                    //--------------------------------------
                    foreach (var q in request.ProductQuestions)
                    {
                        questions.Items.Add(new AdditionalQuestionsDocumentModel.Item
                        {
                            AnswerCode = q.AnswerCode,
                            AnswerText = q.AnswerText,
                            QuestionCode = q.QuestionCode,
                            QuestionText = q.QuestionText,
                            IsCustomerQuestion = false,
                            QuestionGroup = "product"
                        });
                    }

                    KeyValueStoreService.SetValueComposable(context, request.ApplicationNr, "additionalQuestionsDocument", JsonConvert.SerializeObject(questions));

                    context.CreateAndAddComment("Additional questions answered", "additionalQuestionsAnswered", creditApplicationHeader: header);

                    context.SaveChanges();
                    context.CommitTransaction();
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }
            }

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {

                    var s = EmailServiceFactory.CreateEmailService();
                    var customerClient = new CustomerClient(LegacyHttpServiceSystemUser.SharedInstance, LegacyServiceClientFactory.CreateClientFactory(NEnv.ServiceRegistry));

                    var applicantEmail = app.Application.Get("applicantEmail").StringValue.Optional;
                    if (string.IsNullOrWhiteSpace(applicantEmail))
                    {
                        using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
                        {
                            var h = context.CreditApplicationHeaders.Include("Items").SingleOrDefault(x => x.ApplicationNr == request.ApplicationNr);
                            context.CreateAndAddComment("no additional questions email will be sent since no applicant email was found", "additionalQuestionsAnswered", creditApplicationHeader: h);
                        }
                    }

                    s.SendTemplateEmail(new List<string> { applicantEmail }, "companyloan-document-request", null, $"ApplicationNr={request.ApplicationNr}");
                }
                catch (Exception ex)
                {
                    try
                    {
                        using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
                        {
                            var h = context.CreditApplicationHeaders.Include("Items").SingleOrDefault(x => x.ApplicationNr == request.ApplicationNr);
                            context.CreateAndAddComment("Document request email failed with errror:" + ex.Message, "additionalQuestionsAnswered", creditApplicationHeader: h);
                        }
                    }
                    catch (Exception)
                    {


                    }
                }
            });
            return new Response
            {
            };
        }

        private CustomerQuestionsSet PopulateCustomerQuestionsSet(BeneficialOwnerModel bOwner, int customerId, DateTimeOffset clock)
        {
            var questionItems = new List<CustomerQuestionsSetItem>();

            if (bOwner.AnsweredYesOnPepQuestion.HasValue)
            {
                questionItems.Add(
                    new CustomerQuestionsSetItem
                    {
                        AnswerCode = bOwner.AnsweredYesOnPepQuestion.ToString(),
                        AnswerText = bOwner.AnsweredYesOnPepQuestion.Value ? "Ja" : "Nej",
                        QuestionCode = "isPep",
                        QuestionText = "Är någon av angivna ägare eller firmatecknare, " +
                                       "en person i politiskt utsatt ställning? Har någon, " +
                                       "eller tidigare haft, en hög politisk post eller hög statlig " +
                                       "befattning eller är nära familjemedlem eller medarbetare till en sådan person?"
                    });
            }

            if (bOwner.AnsweredYesOnIsUSPersonQuestion.HasValue)
            {
                questionItems.Add(
                    new CustomerQuestionsSetItem
                    {
                        AnswerCode = bOwner.AnsweredYesOnIsUSPersonQuestion.ToString(),
                        AnswerText = bOwner.AnsweredYesOnIsUSPersonQuestion.Value ? "Ja" : "Nej",
                        QuestionCode = "answeredYesOnIsUSPersonQuestion",
                        QuestionText = "Är någon av nedanstående ägare deklarations- eller skatteskyldiga i USA?"
                    });
            }

            if (questionItems.Count > 0)
            {
                return new CustomerQuestionsSet
                {
                    AnswerDate = clock.DateTime,
                    CustomerId = customerId,
                    Source = "AdditionalQuestions",
                    Items = questionItems
                };
            }
            else
            {
                return null;
            }
        }

        private int CreateOrUpdateCustomer<T>(string eventType, string applicationNr, ICustomerClient customerClient, T p, Action<Action<string, string, bool>> withAddProperty = null) where T : PersonBaseModel
        {
            var request = new CreateOrUpdatePersonRequest
            {
                CivicRegNr = p.CivicNr,
                EventSourceId = applicationNr,
                EventType = eventType,
                Properties = new List<CreateOrUpdatePersonRequest.Property>(),
                AdditionalSensitiveProperties = new List<string>()
            };

            void AddProperty(string name, string value, bool forceUpdate)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;

                request.Properties.Add(new CreateOrUpdatePersonRequest.Property { Name = name, Value = value });

                if (forceUpdate)
                    request.AdditionalSensitiveProperties.Add(name);
            }

            AddProperty("firstName", p.FirstName, true);
            AddProperty("lastName", p.LastName, true);

            withAddProperty?.Invoke(AddProperty);

            return customerClient.CreateOrUpdatePerson(request);
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            [Required]
            public string BankAccountNr { get; set; }

            [Required]
            public string BankAccountNrType { get; set; }

            [Required]
            public CollateralModel Collateral { get; set; }

            [Required]
            public List<BeneficialOwnerModel> BeneficialOwners { get; set; }

            [Required]
            public List<QuestionModel> ProductQuestions { get; set; }
        }

        public class BeneficialOwnerModel : PersonBaseModel
        {
            public string Connection { get; set; }
            public decimal? OwnershipPercent { get; set; }
        }

        public class CollateralModel
        {
            [Required]
            public bool? IsApplicant { get; set; }

            public CollateralPersonModel NonApplicantPerson { get; set; }
        }

        public class CollateralPersonModel : PersonBaseModel
        {
            [Required]
            public string Email { get; set; }

            [Required]
            public string Phone { get; set; }
        }

        public class PersonBaseModel
        {
            [Required]
            public string CivicNr { get; set; }

            [Required]
            public string FirstName { get; set; }

            [Required]
            public string LastName { get; set; }

            public bool? AnsweredYesOnPepQuestion { get; set; }
            public string PepRole { get; set; }
            public bool? AnsweredYesOnIsUSPersonQuestion { get; set; }
        }

        public class QuestionModel
        {
            public string QuestionCode { get; set; }
            public string AnswerCode { get; set; }
            public string QuestionText { get; set; }
            public string AnswerText { get; set; }
        }

        public class Response
        {

        }
    }
}