namespace nSavings.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddedWithdrawalAccountChangeDualitySupport : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.SavingsAccountWithdrawalAccountChange", "CreatedByBusinessEventId", "dbo.BusinessEvent");
            DropIndex("dbo.SavingsAccountWithdrawalAccountChange", new[] { "CreatedByBusinessEventId" });
            RenameColumn(table: "dbo.SavingsAccountWithdrawalAccountChange", name: "CreatedByBusinessEventId", newName: "CommitedOrCancelledByEventId");
            AddColumn("dbo.SavingsAccountWithdrawalAccountChange", "NewWithdrawalIban", c => c.String(nullable: false, maxLength: 100));
            AddColumn("dbo.SavingsAccountWithdrawalAccountChange", "InitiatedByBusinessEventId", c => c.Int(nullable: false));
            AlterColumn("dbo.SavingsAccountWithdrawalAccountChange", "CommitedOrCancelledByEventId", c => c.Int());
            CreateIndex("dbo.SavingsAccountWithdrawalAccountChange", "InitiatedByBusinessEventId");
            CreateIndex("dbo.SavingsAccountWithdrawalAccountChange", "CommitedOrCancelledByEventId");
            AddForeignKey("dbo.SavingsAccountWithdrawalAccountChange", "InitiatedByBusinessEventId", "dbo.BusinessEvent", "Id", cascadeDelete: true);
            AddForeignKey("dbo.SavingsAccountWithdrawalAccountChange", "CommitedOrCancelledByEventId", "dbo.BusinessEvent", "Id");
        }

        public override void Down()
        {
            DropForeignKey("dbo.SavingsAccountWithdrawalAccountChange", "CommitedOrCancelledByEventId", "dbo.BusinessEvent");
            DropForeignKey("dbo.SavingsAccountWithdrawalAccountChange", "InitiatedByBusinessEventId", "dbo.BusinessEvent");
            DropIndex("dbo.SavingsAccountWithdrawalAccountChange", new[] { "CommitedOrCancelledByEventId" });
            DropIndex("dbo.SavingsAccountWithdrawalAccountChange", new[] { "InitiatedByBusinessEventId" });
            AlterColumn("dbo.SavingsAccountWithdrawalAccountChange", "CommitedOrCancelledByEventId", c => c.Int(nullable: false));
            DropColumn("dbo.SavingsAccountWithdrawalAccountChange", "InitiatedByBusinessEventId");
            DropColumn("dbo.SavingsAccountWithdrawalAccountChange", "NewWithdrawalIban");
            RenameColumn(table: "dbo.SavingsAccountWithdrawalAccountChange", name: "CommitedOrCancelledByEventId", newName: "CreatedByBusinessEventId");
            CreateIndex("dbo.SavingsAccountWithdrawalAccountChange", "CreatedByBusinessEventId");
            AddForeignKey("dbo.SavingsAccountWithdrawalAccountChange", "CreatedByBusinessEventId", "dbo.BusinessEvent", "Id", cascadeDelete: true);
        }
    }
}
