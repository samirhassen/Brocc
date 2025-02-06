namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<nPreCredit.PreCreditContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            this.CommandTimeout = this.CommandTimeout.HasValue ? this.CommandTimeout.Value * 30 : 300;
        }

        protected override void Seed(nPreCredit.PreCreditContext context)
        {
        }
    }
}
