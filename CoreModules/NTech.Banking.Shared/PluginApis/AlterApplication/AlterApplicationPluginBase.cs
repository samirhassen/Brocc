using System;

namespace NTech.Banking.PluginApis.AlterApplication
{
    public abstract class AlterApplicationPluginBase
    {
        public IApplicationAlterationContext Context { get; set; }

        //NOTE: This is just to make reflection easier
        public abstract Type RequestType { get; }

        protected AlterApplicationRequestModel BeginAlterApplication(string applicationNr)
        {
            return new AlterApplicationRequestModel
            {
                ApplicationNr = applicationNr,
                ChangeDate = Context.Now
            };
        }

        protected Tuple<bool, AlterApplicationRequestModel, Tuple<string, string>> Error(string errorCode, string errorMessage)
        {
            return Tuple.Create(false, (AlterApplicationRequestModel)null, Tuple.Create(errorCode, errorMessage));
        }

        protected Tuple<bool, AlterApplicationRequestModel, Tuple<string, string>> Success(AlterApplicationRequestModel r)
        {
            return Tuple.Create(true, r, (Tuple<string, string>)null);
        }
    }
}