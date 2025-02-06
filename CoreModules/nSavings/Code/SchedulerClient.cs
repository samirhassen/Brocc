namespace nSavings.Code
{
    public class SchedulerClient : AbstractServiceClient
    {
        protected override string ServiceName => "nScheduler";

        public int? FetchLastSuccessAgeInDaysByTag(string tag)
        {
            return Begin()
                .PostJson("Api/ServiceRun/FetchLastSuccessAgeInDaysByTag", new { tag })
                .ParseJsonAs<int?>();
        }
    }
}