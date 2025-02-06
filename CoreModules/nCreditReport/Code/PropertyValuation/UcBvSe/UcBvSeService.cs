using Newtonsoft.Json.Linq;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace nCreditReport.Code.PropertyValuation.UcBvSe
{
    public class UcBvSeService : UcBvSeServiceBase
    {
        public UcBvSeService(NTechSimpleSettings settings) : base(settings) { }

        public UcBvSeService(Uri serviceEndpoint, string username, string password, string clientIp) : base(serviceEndpoint, username, password, clientIp) { }

        public async Task<ServiceResponse<List<SokAdressResult>>> SokAdress(string adress, string postnr, string postort, string kommun)
        {
            var request = new
            {
                adress,
                postnr,
                postort,
                kommun
            };

            var result = await PostJsonRequestAndResponse<List<SokAdressResult>>(request, "Vardering11/SokAdress");

            if (result.Felkod == 10) //Means no such address
            {
                return new ServiceResponse<List<SokAdressResult>>
                {
                    Data = new List<SokAdressResult>(),
                    Felkod = 0,
                    Felmeddelande = null,
                    TransId = result.RawFullJson
                };
            }
            return result;
        }

        public async Task<ServiceResponse<HamtaObjektInfoResult>> HamtaObjektInfo(string id)
        {
            var request = new
            {
                Id = id
            };

            var result = await PostJsonRequestAndResponse<HamtaObjektInfoResult>(request, "Vardering11/HamtaObjektInfo2");

            if (result.IsError() && result.Felkod == 1000)
            {
                //NOTE: This api is very strange. An unknown id causes an error so we need to reinterpret back to something sane
                return new ServiceResponse<HamtaObjektInfoResult>
                {
                    Data = null,
                    Felkod = 0,
                    Felmeddelande = null,
                    TransId = null,
                    RawFullJson = result.RawFullJson
                };
            }
            return result;
        }

        /// <summary>
        /// lghnr is the tax office one so for "Gatan 1 LGH 1102" we would use lghNr = 1102
        /// </summary>
        public async Task<ServiceResponse<HamtaLagenhetResult>> HamtaLagenhet(string id, string lghNr)
        {
            return await PostJsonRequestAndResponse<HamtaLagenhetResult>(new { id, lghNr }, "Vardering11/HamtaLagenhet");
        }

        public async Task<ServiceResponse<VarderaBostadsrattResult>> VarderaBostadsratt(string id, string lghNr, int? area)
        {
            return await PostJsonRequestAndResponse<VarderaBostadsrattResult>(new
            {
                ObjektID = id,
                Yta = area?.ToString(),
                Skvlghnr = lghNr
            }, "Vardering11/VarderaBostadsratt");
        }

        public async Task<ServiceResponse<VarderaBostadsrattResult>> VarderaSmahus(string id)
        {
            return await PostJsonRequestAndResponse<VarderaBostadsrattResult>(new
            {
                ObjektID = id
            }, "Vardering11/VarderaSmahus");
        }

        public async Task<ServiceResponse<JObject>> Inskrivning(string id)
        {
            return await PostJsonRequestAndResponse<JObject>(new
            {
                Id = id
            }, "Fastighetsdata/Inskrivning");
        }

        public async Task<(bool IsOk, byte[] Result, int? HttpErrorCode, string HttpErrorStatusText)> HamtaVarderingsPdf2(string transId)
        {
            return await base.PostJsonRequestAndOctetStreamResponse(new { transId }, "Vardering11/HamtaVarderingsPdf2");
        }

        public async Task<(bool IsOk, byte[] Result, int? HttpErrorCode, string HttpErrorStatusText)> HamtaArsredovisningsPDF2(string transId)
        {
            return await base.PostJsonRequestAndOctetStreamResponse(new { transId }, "Vardering11/HamtaArsredovisningsPDF2");
        }
    }
}