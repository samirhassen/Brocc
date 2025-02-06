using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;

namespace nPreCredit.Code
{
    public class UcBvCreditReportClient : AbstractServiceClient, IUcBvCreditReportClient
    {
        public UcBvCreditReportClient()
        {
        }
        protected override string ServiceName => "nCreditReport";

        private Tuple<bool, T, string> HandleUcbvResult<T>(Func<NHttp.NHttpCallResult> call)
        {
            var result = call();
            if (result.StatusCode == 400)
            {
                return Tuple.Create(false, default(T), result.ReasonPhrase);
            }
            else
            {
                return Tuple.Create(true, result.ParseJsonAs<T>(), (string)null);
            }
        }

        public Tuple<bool, List<UcbvSokAdressHit>, string> UcbvSokAddress(string adress, string postnr, string postort, string kommun)
        {
            return HandleUcbvResult<List<UcbvSokAdressHit>>(() => Begin()
                .PostJson("Ucbv/SokAddress", new { adress, postnr, postort, kommun }));
        }

        public Tuple<bool, UcbvObjectInfo, string> UcbvHamtaObjekt(string id)
        {
            return HandleUcbvResult<UcbvObjectInfo>(() => Begin()
                .PostJson("Ucbv/HamtaObjekt", new { id }));
        }

        public Tuple<bool, VarderaBostadsrattResult, string> UcbvVarderaBostadsratt(UcbvVarderaBostadsrattRequest request)
        {
            return HandleUcbvResult<VarderaBostadsrattResult>(() => Begin()
                .PostJson("Ucbv/VarderaBostadsratt", request));
        }

    }
}