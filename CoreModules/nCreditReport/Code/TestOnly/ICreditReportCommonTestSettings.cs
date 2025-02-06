namespace nCreditReport.Code.TestOnly
{
    public interface ICreditReportCommonTestSettings
    {
        /// <summary>
        /// only: Uses the test module only, never call the real service
        /// preferred: If the customer already exist in the test module use that. Otherwise call the real service.
        /// fallback: Call the real service first. If the customer doesnt exist there call the test module and have it create a customer if one does not exist.
        /// </summary>
        string TestModuleMode { get; set; }
    }
}