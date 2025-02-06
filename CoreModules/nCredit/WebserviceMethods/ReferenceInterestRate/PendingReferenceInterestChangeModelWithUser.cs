using AutoMapper;
using nCredit.Code.Services;

namespace nCredit.WebserviceMethods
{

    public class PendingReferenceInterestChangeModelWithUser : PendingReferenceInterestChangeModel
    {
        public string InitiatedByUserName { get; set; }

        public class ReferenceInterestChangeAutoMapperProfile : Profile
        {
            public ReferenceInterestChangeAutoMapperProfile()
            {
                CreateMap<PendingReferenceInterestChangeModel, PendingReferenceInterestChangeModelWithUser>();
            }
        }
    }
}