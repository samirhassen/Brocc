using NTech.Core.Credit.Shared.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.DbModel.Repository
{
    public class PartialCreditModelRepository
    {
        private List<PartialCreditModel> GetModels(PartialCreditModelRequestSet requestSet, ICreditContextExtended context, Func<IQueryable<CreditInterimDataWrapper>, IQueryable<CreditInterimDataWrapper>> sortFilterAndTransform)
        {
            return GetModelsExtended(requestSet,
                context,
                x => sortFilterAndTransform(x).Select(y => new CreditFinalDataWrapper<string> //This type is a hack. string has no special meaning here. could be any type
                {
                    BasicCreditData = y.BasicCreditData,
                    ExtraCreditData = null
                })).Cast<PartialCreditModel>().ToList();
        }

        public Query NewQuery(DateTime transactionDate)
        {
            return new Query(this, PartialCreditModelRequestSet.Create(transactionDate));
        }


        public class QueryBuilder<TExtraCreditData>
        {
            public PartialCreditModelRequestSet RequestSet { get; set; }
            public Func<IQueryable<CreditInterimDataWrapper>, IQueryable<CreditFinalDataWrapper<TExtraCreditData>>> SortFilterAndTransform { get; set; }
            public QueryBuilder<TExtraCreditData> WithFilterAndTransform(Func<IQueryable<CreditInterimDataWrapper>, IQueryable<CreditFinalDataWrapper<TExtraCreditData>>> sortFilterAndTransform)
            {
                this.SortFilterAndTransform = sortFilterAndTransform;
                return this;
            }

            public List<PartialCreditModelExtended<TExtraCreditData>> Execute(ICreditContextExtended context)
            {
                var repo = new PartialCreditModelRepository();
                return repo.GetModelsExtended(this.RequestSet, context, SortFilterAndTransform);
            }
        }

        public class CreditInterimDataWrapper
        {
            public CreditHeader Credit { get; set; }
            public PartialCreditModelBasicCreditData BasicCreditData { get; set; }
        }

        public class CreditFinalDataWrapper<TExtraCreditData>
        {
            public PartialCreditModelBasicCreditData BasicCreditData { get; set; }
            public TExtraCreditData ExtraCreditData { get; set; }
        }

        private List<PartialCreditModelExtended<TExtraCreditData>> GetModelsExtended<TExtraCreditData>(
            PartialCreditModelRequestSet requestSet,
            ICreditContextExtended context,
            Func<IQueryable<CreditInterimDataWrapper>, IQueryable<CreditFinalDataWrapper<TExtraCreditData>>> sortFilterAndTransform)
        {
            var d = requestSet.TransactionDate;
            List<string> dateCodes = requestSet.Dates.Select(x => x.ToString()).ToList();
            List<string> valueCodes = requestSet.Values.Select(x => x.ToString()).ToList();
            List<string> stringCodes = requestSet.Strings.Select(x => x.ToString()).ToList();

            var interimResult = context
                .CreditHeadersQueryableNoTracking
                .Select(x => new CreditInterimDataWrapper
                {
                    Credit = x,
                    BasicCreditData = new PartialCreditModelBasicCreditData
                    {
                        CreditNr = x.CreditNr,
                        Values = x
                            .DatedCreditValues
                            .Where(y => y.TransactionDate <= d && valueCodes.Contains(y.Name))
                            .GroupBy(y => y.Name)
                            .Select(y => y.OrderByDescending(z => z.TransactionDate)
                            .ThenByDescending(z => z.Timestamp)
                            .Select(z => new PartialCreditModelBasicCreditData.ValueItem<decimal?> { Name = z.Name, TransactionDate = z.TransactionDate, Value = (decimal?)z.Value })
                            .FirstOrDefault()),
                        Strings = x
                            .DatedCreditStrings
                            .Where(y => y.TransactionDate <= d && stringCodes.Contains(y.Name))
                            .GroupBy(y => y.Name)
                            .Select(y => y.OrderByDescending(z => z.TransactionDate)
                            .ThenByDescending(z => z.Timestamp)
                            .Select(z => new PartialCreditModelBasicCreditData.ValueItem<string> { Name = z.Name, TransactionDate = z.TransactionDate, Value = z.Value })
                            .FirstOrDefault()),
                        Dates = x
                            .DatedCreditDates
                            .Where(y => y.TransactionDate <= d && !y.RemovedByBusinessEventId.HasValue && dateCodes.Contains(y.Name))
                            .GroupBy(y => y.Name)
                            .Select(y => y.OrderByDescending(z => z.TransactionDate)
                            .ThenByDescending(z => z.Timestamp)
                            .Select(z => new PartialCreditModelBasicCreditData.ValueItem<DateTime?> { Name = z.Name, TransactionDate = z.TransactionDate, Value = (DateTime?)z.Value })
                            .FirstOrDefault())
                    }
                });

            var finalData = sortFilterAndTransform(interimResult);

            var result = sortFilterAndTransform(interimResult).ToList();

            return result
                .Select(x => new PartialCreditModelExtended<TExtraCreditData>(requestSet, x.BasicCreditData, x.ExtraCreditData))
                .ToList();
        }

        public class Query
        {
            private PartialCreditModelRequestSet RequestSet { get; set; }
            private PartialCreditModelRepository Repository { get; set; }

            public Query(PartialCreditModelRepository repo, PartialCreditModelRequestSet requestSet)
            {
                this.Repository = repo;
                this.RequestSet = requestSet;
            }

            public Query WithDates(params DatedCreditDateCode[] dates)
            {
                foreach (var d in dates)
                    RequestSet.Dates.Add(d);

                return this;
            }

            public Query WithValues(params DatedCreditValueCode[] values)
            {
                foreach (var d in values)
                    RequestSet.Values.Add(d);

                return this;
            }

            public Query WithStrings(params DatedCreditStringCode[] strings)
            {
                foreach (var d in strings)
                    RequestSet.Strings.Add(d);

                return this;
            }

            public List<PartialCreditModelExtended<TExtraCreditData>> ExecuteExtended<TExtraCreditData>(ICreditContextExtended context, Func<IQueryable<PartialCreditModelRepository.CreditInterimDataWrapper>, IQueryable<PartialCreditModelRepository.CreditFinalDataWrapper<TExtraCreditData>>> sortFilterAndTransform)
            {
                return Repository.GetModelsExtended(RequestSet, context, sortFilterAndTransform);
            }

            public List<PartialCreditModel> Execute(ICreditContextExtended context, Func<IQueryable<PartialCreditModelRepository.CreditInterimDataWrapper>, IQueryable<PartialCreditModelRepository.CreditInterimDataWrapper>> sortFilterAndTransform)
            {
                return Repository.GetModels(RequestSet, context, sortFilterAndTransform);
            }
        }
    }
}