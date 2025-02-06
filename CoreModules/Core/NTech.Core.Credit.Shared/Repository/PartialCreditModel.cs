using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.DbModel.Repository
{
    public class PartialCreditModel
    {
        private PartialCreditModelRequestSet requestSet;
        private PartialCreditModelBasicCreditData basicCreditData;
        private Func<DatedCreditDateCode, Tuple<DateTime?, DateTime>> getDate;
        private Func<DatedCreditValueCode, Tuple<decimal?, DateTime>> getValue;
        private Func<DatedCreditStringCode, Tuple<string, DateTime>> getString;

        public PartialCreditModel(PartialCreditModelRequestSet requestSet,
            PartialCreditModelBasicCreditData basicCreditData)
        {
            this.requestSet = requestSet;
            this.basicCreditData = basicCreditData;

            var datesLookup = basicCreditData.Dates.ToLookup(y => y.Name);
            var valuesLookup = basicCreditData.Values.ToLookup(y => y.Name);
            var stringLookup = basicCreditData.Strings.ToLookup(y => y.Name);
            this.getDate = n => datesLookup[n.ToString()].Select(y => Tuple.Create(y.Value, y.TransactionDate)).SingleOrDefault();
            this.getValue = n => valuesLookup[n.ToString()].Select(y => Tuple.Create(y.Value, y.TransactionDate)).SingleOrDefault();
            this.getString = n => stringLookup[n.ToString()].Select(y => Tuple.Create(y.Value, y.TransactionDate)).SingleOrDefault();
        }

        public string CreditNr
        {
            get
            {
                return this.basicCreditData.CreditNr;
            }
        }

        public DateTime ForTransactionDate
        {
            get
            {
                return requestSet.TransactionDate;
            }
        }

        private TResult Get<TEnum, TResult>(
            TEnum code,
            Func<TEnum, Tuple<TResult, DateTime>> getItem,
            ISet<TEnum> requestedItems,
            Action<DateTime> observeTransactionDate)
        {
            if (!requestedItems.Contains(code))
                throw new Exception($"{code} was not requested");
            var v = getItem(code);
            if (v == null)
                return default(TResult);
            observeTransactionDate?.Invoke(v.Item2);
            return v.Item1;
        }

        public DateTime? GetDate(DatedCreditDateCode code, Action<DateTime> observeTransactionDate = null)
        {
            return Get(code, getDate, requestSet.Dates, observeTransactionDate);
        }

        public decimal? GetValue(DatedCreditValueCode code, Action<DateTime> observeTransactionDate = null)
        {
            return Get(code, getValue, requestSet.Values, observeTransactionDate);
        }

        public string GetString(DatedCreditStringCode code, Action<DateTime> observeTransactionDate = null)
        {
            return Get(code, getString, requestSet.Strings, observeTransactionDate);
        }
    }
}