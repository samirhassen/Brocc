
using nPreCredit.Code.Services;
using NTech.Core.Module.Shared.Services;
using NTech.Core.PreCredit.Shared;
using NTech.Core.PreCredit.Shared.Services.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace nPreCredit
{
    public class PartialCreditApplicationModelRepository : IPartialCreditApplicationModelRepositoryExtended
    {
        private readonly EncryptionService encryptionService;
        private readonly IPreCreditContextFactoryService preCreditContextFactoryService;
        private readonly ILinqQueryExpander linqQueryExpander;

        public PartialCreditApplicationModelRepository(EncryptionService encryptionService, IPreCreditContextFactoryService preCreditContextFactoryService, ILinqQueryExpander linqQueryExpander)
        {
            this.encryptionService = encryptionService;
            this.preCreditContextFactoryService = preCreditContextFactoryService;
            this.linqQueryExpander = linqQueryExpander;
        }

        public bool ExistsAll(string applicationNr, out string missingFieldsMessage,
            List<string> applicationFields = null,
            List<string> applicantFields = null,
            List<string> documentFields = null,
            List<string> questionFields = null,
            List<string> externalFields = null)
        {
            var missing = new List<string>();
            var model = Get(applicationNr, applicationFields: applicationFields, applicantFields: applicantFields, documentFields: documentFields, questionFields: questionFields, externalFields: externalFields);

            if (applicationFields != null)
            {
                foreach (var af in applicationFields)
                {
                    if (!model.Application.Get(af).Exists)
                        missing.Add(af);
                }
            }

            if (externalFields != null)
            {
                foreach (var af in externalFields)
                {
                    if (!model.External.Get(af).Exists)
                        missing.Add(af);
                }
            }

            model.DoForEachApplicant(applicantNr =>
            {
                var tag = $"applicant{applicantNr}:";
                if (applicantFields != null)
                {
                    foreach (var ap in applicantFields)
                    {
                        if (!model.Applicant(applicantNr).Get(ap).Exists)
                            missing.Add(tag + ap);
                    }
                }
                if (documentFields != null)
                {
                    foreach (var ap in documentFields)
                    {
                        if (!model.Document(applicantNr).Get(ap).Exists)
                            missing.Add(tag + ap);
                    }
                }
                if (questionFields != null)
                {
                    foreach (var ap in questionFields)
                    {
                        if (!model.Question(applicantNr).Get(ap).Exists)
                            missing.Add(tag + ap);
                    }
                }
            });

            if (missing.Any())
            {
                missingFieldsMessage = string.Join(", ", missing.Distinct());
                return false;
            }
            else
            {
                missingFieldsMessage = null;
                return true;
            }
        }

        public PartialCreditApplicationModelExtended<TCustom> GetExtended<TCustom>(string applicationNr, PartialCreditApplicationModelRequest request, Func<string, IPreCreditContextExtended, TCustom> loadCustomDataByApplicationNr) where TCustom : PartialCreditApplicationModelExtendedCustomDataBase
        {
            if (string.IsNullOrWhiteSpace(applicationNr))
                throw new ArgumentException("Missing value", "applicationNr");
            if (request == null)
                throw new ArgumentNullException("request");

            var applicantFields = (request.ApplicantFields ?? new List<string>()).Distinct().ToList();
            var applicationFields = (request.ApplicationFields ?? new List<string>()).Distinct().ToList();
            var documentFields = (request.DocumentFields ?? new List<string>()).Distinct().ToList();
            var questionFields = (request.QuestionFields ?? new List<string>()).Distinct().ToList();
            var creditreportFields = (request.CreditreportFields ?? new List<string>()).Distinct().ToList();
            var externalFields = (request.ExternalFields ?? new List<string>()).Distinct().ToList();

            var wasCustomerIdRequested = applicantFields.Contains("customerId");

            var localApplicantFields = wasCustomerIdRequested ? applicantFields : applicantFields.Concat(new[] { "customerId" }).ToList();

            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                //Non encrypted fields
                var predicate = PredicateBuilder.False<CreditApplicationItem>();

                foreach (var af in applicationFields)
                {
                    var localAf = af;
                    predicate = predicate.Or(x => x.GroupName == "application" && x.Name == localAf);
                }
                foreach (var af in localApplicantFields)
                {
                    var localAf = af;
                    predicate = predicate.Or(x => x.GroupName.StartsWith("applicant") && x.Name == localAf);
                }
                foreach (var df in documentFields)
                {
                    var localDf = df;
                    predicate = predicate.Or(x => x.GroupName.StartsWith("document") && x.Name == localDf);
                }
                foreach (var qf in questionFields)
                {
                    var localQf = qf;
                    predicate = predicate.Or(x => x.GroupName.StartsWith("question") && x.Name == localQf);
                }
                foreach (var crf in creditreportFields)
                {
                    var localCrf = crf;
                    predicate = predicate.Or(x => x.GroupName.StartsWith("creditreport") && x.Name == localCrf);
                }
                foreach (var ef in externalFields)
                {
                    var localEf = ef;
                    predicate = predicate.Or(x => x.GroupName == "external" && x.Name == localEf);
                }
                var creditApplicationItemsPre = context
                    .CreditApplicationItemsQueryable
                    .Where(x => x.ApplicationNr == applicationNr);

                if (linqQueryExpander.IsExpansionNeeded)
                {
                    creditApplicationItemsPre = linqQueryExpander.AsExpandable(creditApplicationItemsPre);
                }

                var creditApplicationItems = creditApplicationItemsPre
                    .Where(predicate)
                    .ToList();

                var decryptedCreditApplicationItems = Decrypt(context, creditApplicationItems);

                var items = !request.LoadChangedBy
                        ? creditApplicationItems
                            .Select(x => Tuple.Create(new PartialCreditApplicationModel.ApplicationItem
                            {
                                GroupName = x.GroupName,
                                ItemName = x.Name,
                                ItemValue = decryptedCreditApplicationItems[x.Id]
                            }, x.IsEncrypted))
                            .ToList()
                    :
                        creditApplicationItems
                            .Select(x => Tuple.Create(new PartialCreditApplicationModel.ApplicationItem
                            {
                                GroupName = x.GroupName,
                                ItemName = x.Name,
                                ItemValue = decryptedCreditApplicationItems[x.Id],
                                ChangedById = x.ChangedById,
                                ChangedDate = x.ChangedDate
                            }, x.IsEncrypted))
                            .ToList();

                var customerIdItems = items.Where(x => x.Item1.ItemName == "customerId").ToList();
                if (!wasCustomerIdRequested)
                    items = items.Where(x => x.Item1.ItemName != "customerId").ToList();

                var customData = loadCustomDataByApplicationNr(applicationNr, context);

                Func<List<string>, ISet<string>> gl = x => (request.ErrorIfGetNonLoadedField && x != null)
                            ? new HashSet<string>(x)
                            : null;

                return new PartialCreditApplicationModelExtended<TCustom>(customData, request.LoadChangedBy,
                    items.Select(x => x.Item1).ToList(),
                    requestedApplicantFields: gl(applicantFields),
                    requestedApplicationFields: gl(applicationFields),
                    requestedDocumentFields: gl(documentFields),
                    requestedQuestionFields: gl(questionFields),
                    requestedCreditreportFields: gl(creditreportFields));
            }
        }

        private Dictionary<int, string> Decrypt(IPreCreditContextExtended context, IEnumerable<CreditApplicationItem> items)
        {
            var result = new Dictionary<int, string>();

            var itemsList = items.ToList();

            //Decrypt encrypted
            var encryptedItems = itemsList.Where(x => x.IsEncrypted).Select(x => new
            {
                Item = x,
                EncryptedValueId = long.Parse(x.Value)
            });
            if (encryptedItems.Any())
            {
                var decryptedValues = encryptionService.DecryptEncryptedValues(context, encryptedItems.Select(x => x.EncryptedValueId).ToArray());

                foreach (var i in encryptedItems)
                {
                    result[i.Item.Id] = decryptedValues[i.EncryptedValueId];
                }
            }

            //Just return non encrypted
            foreach (var item in itemsList.Where(x => !x.IsEncrypted))
            {
                result[item.Id] = item.Value;
            }

            return result;
        }

        public PartialCreditApplicationModel Get(string applicationNr, PartialCreditApplicationModelRequest request)
        {
            return GetExtended(applicationNr, request, (an, context) => context
                    .CreditApplicationHeadersQueryable
                    .Where(x => x.ApplicationNr == an)
                    .Select(x => new PartialCreditApplicationModelExtendedCustomDataBase
                    {
                        NrOfApplicants = x.NrOfApplicants
                    })
                    .Single()
            );
        }

        public List<string> S(params string[] args)
        {
            return args.Where(x => x != null).ToList();
        }

        public PartialCreditApplicationModel Get(string applicationNr,
            List<string> applicationFields = null,
            List<string> applicantFields = null,
            List<string> documentFields = null,
            List<string> questionFields = null,
            List<string> creditreportFields = null,
            List<string> externalFields = null,
            bool errorIfGetNonLoadedField = false)
        {
            return Get(applicationNr, new PartialCreditApplicationModelRequest
            {
                ApplicantFields = applicantFields,
                ApplicationFields = applicationFields,
                CreditreportFields = creditreportFields,
                DocumentFields = documentFields,
                QuestionFields = questionFields,
                ExternalFields = externalFields,
                ErrorIfGetNonLoadedField = errorIfGetNonLoadedField
            });
        }
    }

    public static class PredicateBuilder
    {
        public static Expression<Func<T, bool>> True<T>() { return f => true; }
        public static Expression<Func<T, bool>> False<T>() { return f => false; }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1,
                                                            Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>
                  (Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1,
                                                             Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>
                  (Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
        }
    }
}