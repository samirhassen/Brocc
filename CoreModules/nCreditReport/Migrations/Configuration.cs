namespace nCreditReport.Migrations
{
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<nCreditReport.CreditReportContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(nCreditReport.CreditReportContext context)
        {
            //  This method will be called after migrating to the latest version.
        }
    }
}
