namespace nCredit.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedSieFile : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SieFileVerification",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Text = c.String(nullable: false, maxLength: 256),
                        Date = c.DateTime(nullable: false, storeType: "date"),
                        RegistrationDate = c.DateTime(nullable: false, storeType: "date"),
                        OutgoingBookkeepingFileHeaderId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.OutgoingBookkeepingFileHeader", t => t.OutgoingBookkeepingFileHeaderId)
                .Index(t => t.OutgoingBookkeepingFileHeaderId);
            
            CreateTable(
                "dbo.SieFileConnection",
                c => new
                    {
                        VerificationId = c.Int(nullable: false),
                        ConnectionType = c.String(nullable: false, maxLength: 128),
                        ConnectionId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.VerificationId, t.ConnectionType })
                .ForeignKey("dbo.SieFileVerification", t => t.VerificationId, cascadeDelete: true)
                .Index(t => t.VerificationId);
            
            CreateTable(
                "dbo.SieFileTransaction",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        AccountNr = c.String(nullable: false, maxLength: 128),
                        VerificationId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.SieFileVerification", t => t.VerificationId, cascadeDelete: true)
                .Index(t => t.VerificationId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.SieFileTransaction", "VerificationId", "dbo.SieFileVerification");
            DropForeignKey("dbo.SieFileVerification", "OutgoingBookkeepingFileHeaderId", "dbo.OutgoingBookkeepingFileHeader");
            DropForeignKey("dbo.SieFileConnection", "VerificationId", "dbo.SieFileVerification");
            DropIndex("dbo.SieFileTransaction", new[] { "VerificationId" });
            DropIndex("dbo.SieFileConnection", new[] { "VerificationId" });
            DropIndex("dbo.SieFileVerification", new[] { "OutgoingBookkeepingFileHeaderId" });
            DropTable("dbo.SieFileTransaction");
            DropTable("dbo.SieFileConnection");
            DropTable("dbo.SieFileVerification");
        }
    }
}
