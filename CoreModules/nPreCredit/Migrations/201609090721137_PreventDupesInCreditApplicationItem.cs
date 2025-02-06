namespace nPreCredit.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class PreventDupesInCreditApplicationItem : DbMigration
    {
        public override void Up()
        {
            Sql("delete from CreditApplicationItem where id in(select min(i.Id) from CreditApplicationItem i group by i.ApplicationNr, i.GroupName, i.Name having count(*) > 1)");
            Sql("create unique index UIX_ItemCompositeName on CreditApplicationItem (ApplicationNr, GroupName, Name)");
        }

        public override void Down()
        {
            Sql("drop index UIX_ItemCompositeName on CreditApplicationItem");
        }
    }
}
