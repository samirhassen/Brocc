using System;

namespace NTech.Banking.PluginApis.CreateApplication
{
    public abstract class CreateApplicationPluginBase
    {
        public IApplicationCreationContext Context { get; set; }
        //NOTE: This is just to make reflection easier
        public abstract Type RequestType { get; }
        protected CreateApplicationRequestModel CreateNewApplication(int nrOfApplicants, string providerName)
        {
            return new CreateApplicationRequestModel
            {
                NrOfApplicants = nrOfApplicants,
                ProviderName = providerName,
                ApplicationNr = Context.GenerateNewApplicationNr(),
                ApplicationDate = Context.Now,
                HideFromManualListsUntilDate = new DateTimeOffset?(Context.Now.AddMinutes(5))
            };
        }

        protected Tuple<bool, CreateApplicationRequestModel, Tuple<string, string>> Error(string errorCode, string errorMessage)
        {
            return Tuple.Create(false, (CreateApplicationRequestModel)null, Tuple.Create(errorCode, errorMessage));
        }

        protected Tuple<bool, CreateApplicationRequestModel, Tuple<string, string>> Success(CreateApplicationRequestModel r)
        {
            return Tuple.Create(true, r, (Tuple<string, string>)null);
        }
    }
}
