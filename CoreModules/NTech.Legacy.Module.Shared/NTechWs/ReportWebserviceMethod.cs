namespace NTech.Services.Infrastructure.NTechWs
{
    public abstract class ReportWebserviceMethod<TRequest> : FileStreamWebserviceMethod<TRequest>
        where TRequest : class, new()
    {
        public ReportWebserviceMethod(bool usePost = false, bool allowDirectFormPost = false) : base(usePost: usePost, allowDirectFormPost: allowDirectFormPost)
        {

        }

        public override string Path => $"Reports/{ReportName}";

        public abstract string ReportName { get; }
    }
}
