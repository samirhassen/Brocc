namespace nUser.Migrations
{
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<nUser.DbModel.UsersContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            ContextKey = "nUser.DbModel.UsersContext";
        }

        protected override void Seed(nUser.DbModel.UsersContext context)
        {
            //  This method will be called after migrating to the latest version.
        }
    }
}
