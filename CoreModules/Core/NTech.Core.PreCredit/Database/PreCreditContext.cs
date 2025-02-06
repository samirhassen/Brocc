using Microsoft.EntityFrameworkCore;
using nPreCredit;
using nPreCredit.DbModel;
using NTech.Core.Module.Database;
using System.Linq.Expressions;

namespace NTech.Core.PreCredit.Database
{
    public class PreCreditContext : NTechDbContext
    {
        public virtual DbSet<FraudControlProperty> FraudControlProperties { get; set; }
        public virtual DbSet<CreditApplicationKeySequence> CreditApplicationKeySequences { get; set; }
        public virtual DbSet<CreditApplicationHeader> CreditApplicationHeaders { get; set; }
        public virtual DbSet<CreditApplicationSearchTerm> CreditApplicationSearchTerms { get; set; }
        public virtual DbSet<CreditApplicationItem> CreditApplicationItems { get; set; }
        public virtual DbSet<CreditApplicationChangeLogItem> CreditApplicationChangeLogItems { get; set; }
        public virtual DbSet<CreditApplicationComment> CreditApplicationComments { get; set; }
        public virtual DbSet<EncryptedValue> EncryptedValues { get; set; }
        public virtual DbSet<CreditDecision> CreditDecisions { get; set; }
        public virtual DbSet<FraudControl> FraudControls { get; set; }
        public virtual DbSet<FraudControlItem> FraudControlItems { get; set; }
        public virtual DbSet<CreditApplicationOneTimeToken> CreditApplicationOneTimeTokens { get; set; }
        public virtual DbSet<SystemItem> SystemItems { get; set; }
        public virtual DbSet<HandlerLimitLevel> HandlerLimitLevels { get; set; }
        public virtual DbSet<CreditApprovalBatchHeader> CreditApprovalBatchHeaders { get; set; }
        public virtual DbSet<CreditApprovalBatchItem> CreditApprovalBatchItems { get; set; }
        public virtual DbSet<CreditApprovalBatchItemOverride> CreditApprovalBatchItemOverrides { get; set; }
        public virtual DbSet<CustomerCheckpoint> CustomerCheckpoints { get; set; }
        public virtual DbSet<CreditApplicationCancellation> CreditApplicationCancellations { get; set; }
        public virtual DbSet<CreditDecisionPauseItem> CreditDecisionPauseItems { get; set; }
        public virtual DbSet<CreditDecisionSearchTerm> CreditDecisionSearchTerms { get; set; }
        public virtual DbSet<MortgageLoanCreditApplicationHeaderExtension> MortgageLoanCreditApplicationHeaderExtensions { get; set; }
        public virtual DbSet<CreditApplicationEvent> CreditApplicationEvents { get; set; }
        public virtual DbSet<CreditApplicationDocumentHeader> CreditApplicationDocumentHeaders { get; set; }
        public virtual DbSet<KeyValueItem> KeyValueItems { get; set; }
        public virtual DbSet<TemporaryExternallyEncryptedItem> TemporaryExternallyEncryptedItems { get; set; }
        public virtual DbSet<CreditApplicationPauseItem> CreditApplicationPauseItems { get; set; }
        public virtual DbSet<AffiliateReportingEvent> AffiliateReportingEvents { get; set; }
        public virtual DbSet<AffiliateReportingLogItem> AffiliateReportingLogItems { get; set; }
        public virtual DbSet<CreditApplicationListMember> CreditApplicationListMembers { get; set; }
        public virtual DbSet<CreditApplicationListOperation> CreditApplicationListOperations { get; set; }
        public virtual DbSet<CreditApplicationCustomerListMember> CreditApplicationCustomerListMembers { get; set; }
        public virtual DbSet<CreditApplicationCustomerListOperation> CreditApplicationCustomerListOperations { get; set; }
        public virtual DbSet<CreditDecisionItem> CreditDecisionItems { get; set; }
        public virtual DbSet<ComplexApplicationListItem> ComplexApplicationListItems { get; set; }
        public virtual DbSet<HComplexApplicationListItem> HComplexApplicationListItems { get; set; }
        public virtual DbSet<ApplicationReportCasheRow> ApplicationReportCasheRows { get; set; }
        public virtual DbSet<ManualSignature> ManualSignatures { get; set; }
        public virtual DbSet<WorkListHeader> WorkListHeaders { get; set; }
        public virtual DbSet<WorkListItem> WorkListItems { get; set; }
        public virtual DbSet<WorkListItemProperty> WorkListItemProperties { get; set; }
        public virtual DbSet<WorkListFilterItem> WorkListFilterItems { get; set; }
        public virtual DbSet<AbTestingExperiment> AbTestingExperiments { get; set; }
        public virtual DbSet<Campaign> Campaigns { get; set; }
        public virtual DbSet<CampaignCode> CampaignCodes { get; set; }
        public virtual DbSet<StandardPolicyFilterRuleSet> StandardPolicyFilterRuleSets { get; set; }

        public override string ConnectionStringName => "PreCreditContext";

        protected override void HandleCreate(ModelBuilder modelBuilder, LegacyEntityFrameworkHelper legacyHelper)
        {
            modelBuilder.Entity<CreditApplicationKeySequence>().HasKey(x => x.Id);

            Cfg<FraudControlProperty>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.CustomerId).IsRequired();
                ch.Property(x => x.Value).IsRequired();
                ch.Property(x => x.Name).IsRequired().HasMaxLength(128);
                ch.HasOne(x => x.ReplacesFraudControlProperty).WithMany(x => x.ReplacedByFraudControlProperties).HasForeignKey(x => x.ReplacesFraudControlProperty_Id);
                ch.Property(x => x.IsCurrentData).IsRequired();
            });

            Cfg<CreditApplicationHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.ApplicationNr);
                ch.Property(x => x.ApplicationNr).HasMaxLength(128);
                ch.Property(x => x.NrOfApplicants).IsRequired();
                ch.Property(x => x.ProviderName).IsRequired().HasMaxLength(100);
                ch.Property(x => x.ApplicationType).HasMaxLength(50);
                ch.HasIndex(x => x.ApplicationType);

                ch.HasMany(x => x.SearchTerms);
                ch.Property(x => x.ApplicationDate).IsRequired();
                ch.HasIndex(x => x.ApplicationDate);
                ch.Property(x => x.IsActive).IsRequired();
                ch.HasIndex(x => x.IsActive);
                ch.Property(x => x.CreditCheckStatus).IsRequired().HasMaxLength(100).HasDefaultValueSql("''");
                ch.HasIndex(x => x.CreditCheckStatus);
                ch.HasMany(x => x.CreditDecisions).WithOne(x => x.CreditApplication).HasForeignKey(x => x.ApplicationNr).IsRequired();
                ch.HasOne(x => x.CurrentCreditDecision).WithMany().HasForeignKey(x => x.CurrentCreditDecisionId);
                ch.HasMany(x => x.FraudControls).WithOne(x => x.CreditApplication).HasForeignKey(x => x.ApplicationNr).IsRequired();
                ch.HasMany(x => x.Cancellations).WithOne(x => x.CreditApplication).HasForeignKey(x => x.ApplicationNr).IsRequired();

                //The compiler cant seem to resolve the correct version of HasForeignKey
                Expression<Func<MortgageLoanCreditApplicationHeaderExtension, object>> mlExtensionApplicationNr = x => x.ApplicationNr;
                ch.HasOne(x => x.MortgageLoanExtension).WithOne(x => x.Application).HasForeignKey(mlExtensionApplicationNr).IsRequired();

                ch.Property(x => x.CustomerCheckStatus).IsRequired().HasMaxLength(100).HasDefaultValueSql("''");
                ch.HasIndex(x => x.CustomerCheckStatus);

                ch.Property(x => x.AgreementStatus).IsRequired().HasMaxLength(100).HasDefaultValueSql("''");
                ch.HasIndex(x => x.AgreementStatus);
                ch.Property(x => x.FraudCheckStatus).IsRequired().HasMaxLength(100).HasDefaultValueSql("''");
                ch.HasIndex(x => x.FraudCheckStatus);
                ch.Property(x => x.IsFinalDecisionMade).IsRequired();
                ch.HasIndex(x => x.IsFinalDecisionMade);

                ch.Property(x => x.FinalDecisionById);
                ch.Property(x => x.FinalDecisionDate);
                ch.Property(x => x.IsPartiallyApproved).IsRequired();
                ch.HasIndex(x => x.IsPartiallyApproved);

                ch.Property(x => x.PartiallyApprovedById);
                ch.Property(x => x.PartiallyApprovedDate);
                ch.Property(x => x.IsRejected).IsRequired().HasDefaultValueSql("0");
                ch.Property(x => x.RejectedDate);
                ch.Property(x => x.RejectedById);
                ch.Property(x => x.IsCancelled).IsRequired().HasDefaultValueSql("0");
                ch.HasIndex(x => x.IsCancelled);
                ch.Property(x => x.CancelledDate);
                ch.Property(x => x.CancelledBy);
                ch.Property(x => x.CancelledState).HasMaxLength(100);
                ch.Property(x => x.ArchivedDate);
                ch.HasMany(x => x.Approvals).WithOne(x => x.Application).HasForeignKey(x => x.ApplicationNr).IsRequired();
                ch.Property(x => x.WaitingForAdditionalInformationDate);
                ch.HasIndex(x => x.WaitingForAdditionalInformationDate);
                ch.Property(x => x.HideFromManualListsUntilDate);
                ch.HasIndex(x => x.HideFromManualListsUntilDate);
                ch.Property(x => x.CanSkipAdditionalQuestions).HasDefaultValueSql("0");
                ch.HasIndex(x => x.CanSkipAdditionalQuestions);
                //NOTE: This has cascading deletes in legacy ef but ef core refuses to create this citing possible cycles.
                ch.HasMany(x => x.Events).WithOne(x => x.Application).HasForeignKey(x => x.ApplicationNr).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.Documents).WithOne(x => x.CreditApplication).HasForeignKey(x => x.ApplicationNr).IsRequired();
                ch.HasMany(x => x.PauseItems).WithOne(x => x.CreditApplication).HasForeignKey(x => x.ApplicationNr).IsRequired();
                ch.HasMany(x => x.ListMemberships).WithOne(x => x.CreditApplication).HasForeignKey(x => x.ApplicationNr).IsRequired();
                ch.HasMany(x => x.ListMembershipOperations).WithOne(x => x.CreditApplication).HasForeignKey(x => x.ApplicationNr).IsRequired();
                ch.HasMany(x => x.CustomerListMemberships).WithOne(x => x.CreditApplication).HasForeignKey(x => x.ApplicationNr).IsRequired();
                ch.HasMany(x => x.CustomerListMembershipOperations).WithOne(x => x.CreditApplication).HasForeignKey(x => x.ApplicationNr).IsRequired();
                ch.HasMany(x => x.ComplexApplicationListItems).WithOne(x => x.Application).HasForeignKey(x => x.ApplicationNr).IsRequired();

                ch.HasIndex(x => new { x.Timestamp, x.ArchivedDate }).HasDatabaseName("ArchiveCustomerPerfIdx1");
                ch
                    .HasIndex(x => new { x.IsActive, x.IsFinalDecisionMade, x.HideFromManualListsUntilDate })
                    .IncludeProperties(x => new { x.ApplicationNr, x.IsPartiallyApproved, x.CreditCheckStatus, x.CustomerCheckStatus, x.AgreementStatus, x.FraudCheckStatus, x.WaitingForAdditionalInformationDate })
                    .HasDatabaseName("CategoryCountIdx2");
                ch
                    .HasIndex(x => new { x.IsActive, x.CreditCheckStatus, x.CustomerCheckStatus, x.AgreementStatus, x.FraudCheckStatus, x.IsPartiallyApproved })
                    .IncludeProperties(x => new { x.ApplicationNr, x.ProviderName, x.ApplicationDate, x.IsFinalDecisionMade })
                    .HasDatabaseName("CreditApplicationHeader_PerfIdx1");
                ch
                    .HasIndex(x => new { x.ApplicationDate, x.HideFromManualListsUntilDate })
                    .IncludeProperties(x => new { x.ApplicationNr, x.CurrentCreditDecisionId, x.IsCancelled })
                    .HasDatabaseName("CreditManagementMonitorIdx1");
            });

            var cs = modelBuilder.Entity<CreditApplicationSearchTerm>();
            ConfigureInfrastructureFields(cs);
            cs.HasOne(x => x.CreditApplication).WithMany(x => x.SearchTerms).HasForeignKey(x => x.ApplicationNr).IsRequired();
            cs.Property(x => x.ApplicationNr);

            //NOTE: The below two are a bug preserved from ef standard to preserve migrations
            //       It should be one index CreditApplicationSearchTermCoveringIndex(Name, Value, ApplicationNr)
            //       but the extra B split it in two ... but we need migrations to fix this so leaving it for now
            cs.HasIndex(x => new { x.ApplicationNr }).HasDatabaseName("CreditApplicationSearchTeBrmCoveringIndex");
            cs.HasIndex(x => new { x.Name, x.Value }).HasDatabaseName("CreditApplicationSearchTermCoveringIndex");

            cs.HasIndex(x => x.Name).HasDatabaseName("CreditApplicationSearchTermNameIndex");
            cs.HasIndex(x => x.Value).HasDatabaseName("CreditApplicationSearchTermValueIndex");

            cs.HasKey(x => x.Id);
            cs.Property(x => x.Name).IsRequired().HasMaxLength(100);
            cs.Property(x => x.Value).IsRequired().HasMaxLength(100);

            var ci = modelBuilder.Entity<CreditApplicationItem>();
            ConfigureInfrastructureFields(ci);
            ci.HasOne(x => x.CreditApplication).WithMany(x => x.Items).HasForeignKey(x => x.ApplicationNr).IsRequired();
            ci.HasKey(x => x.Id);
            ci.Property(x => x.AddedInStepName);
            ci.Property(x => x.GroupName).IsRequired().HasMaxLength(100);

            ci.HasIndex(x => new { x.ApplicationNr });
            ci.HasIndex(x => new { x.GroupName }).HasDatabaseName("CreditApplicationItemGroupNameIndex");
            ci.HasIndex(x => new { x.Name }).HasDatabaseName("CreditApplicationItemNameIndex");
            ci.HasIndex(x => new { x.GroupName, x.Name }).HasDatabaseName("CreditApplicationItemNamesIndex");
            ci
                .HasIndex(x => new { x.ApplicationNr, x.Id })
                .IncludeProperties(x => new { x.AddedInStepName })
                .HasDatabaseName("CreditApplicationItem_PerfIdx1");
            ci
                .HasIndex(x => new { x.Name, x.IsEncrypted })
                .IncludeProperties(x => new { x.ApplicationNr, x.Value })
                .HasDatabaseName("CreditApplicationItemIdx1");
            ci
                .HasIndex(x => new { x.Timestamp })
                .IncludeProperties(x => new { x.ApplicationNr })
                .HasDatabaseName("CreditApplicationItemReplIdx1");
            ci
                .HasIndex(x => new { x.ApplicationNr, x.GroupName, x.Name })
                .HasDatabaseName("UIX_ItemCompositeName")
                .IsUnique();
            ci.Property(x => x.Name).IsRequired().HasMaxLength(100);
            ci.Property(x => x.Value).IsRequired();
            ci.Property(x => x.IsEncrypted).HasDefaultValueSql("0");

            Cfg<EncryptedValue>(modelBuilder, ev =>
            {
                ev.HasKey(x => x.Id);
                ev.Property(x => x.EncryptionKeyName).IsRequired().HasMaxLength(100);
                ev.Property(e => e.Timestamp).IsRequired().IsRowVersion();
                ev.Property(e => e.CreatedById).IsRequired();
                ev.Property(e => e.CreatedDate).IsRequired();
                ev.Property(x => x.Value).IsRequired();
            });

            Cfg<AcceptedCreditDecision>(modelBuilder, e =>
            {
                e.Property(x => x.AcceptedDecisionModel).IsRequired();
            });

            Cfg<RejectedCreditDecision>(modelBuilder, e =>
            {
                e.Property(x => x.RejectedDecisionModel).IsRequired();
            });

            Cfg<CreditDecision>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);
                e.HasKey(x => x.Id);
                e.Property(x => x.DecisionDate).IsRequired();
                e.Property(x => x.DecisionType).HasMaxLength(20);
                e.Property(x => x.DecisionById).IsRequired();
                e.Property(x => x.WasAutomated).IsRequired().HasDefaultValueSql("0");
                e.HasMany(x => x.PauseItems).WithOne(x => x.Decision).HasForeignKey(x => x.CreditDecisionId).IsRequired();
                e.HasMany(x => x.SearchTerms).WithOne(x => x.Decision).HasForeignKey(x => x.CreditDecisionId).IsRequired();
                e.HasMany(x => x.DecisionItems).WithOne(x => x.Decision).HasForeignKey(x => x.CreditDecisionId).IsRequired();
                e
                    .HasDiscriminator<string>("Discriminator")
                    .HasValue<AcceptedCreditDecision>("AcceptedCreditDecision")
                    .HasValue<RejectedCreditDecision>("RejectedCreditDecision");
                e.Property<string>("Discriminator").HasMaxLength(128);
                e.HasIndex(x => new { x.DecisionDate });
                e.HasIndex(x => new { x.DecisionDate });
                e.HasIndex(x => new { x.Timestamp }).HasDatabaseName("TsIdx1");
                e.HasIndex("Discriminator").IncludeProperties(x => new { x.Id, x.WasAutomated }).HasDatabaseName("CreditDecisionPerfIdx1");
            });

            Cfg<CreditDecisionPauseItem>(modelBuilder, e =>
            {
                e.HasKey(x => x.Id);
                ConfigureInfrastructureFields(e);
                e.Property(x => x.CustomerId).IsRequired();
                e.Property(x => x.PausedUntilDate).IsRequired().HasColumnType("datetime");
                e.Property(x => x.RejectionReasonName).HasMaxLength(128);
            });

            Cfg<CreditDecisionItem>(modelBuilder, e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.IsRepeatable).IsRequired();
                e.Property(x => x.ItemName).IsRequired().HasMaxLength(128);
                e.Property(x => x.Value).IsRequired().HasMaxLength(256);
            });

            Cfg<CreditApplicationPauseItem>(modelBuilder, e =>
            {
                e.HasKey(x => x.Id);
                ConfigureInfrastructureFields(e);
                e.Property(x => x.CustomerId).IsRequired();
                e.Property(x => x.PausedUntilDate).IsRequired().HasColumnType("datetime");
                e.Property(x => x.PauseReasonName).HasMaxLength(128).IsRequired();
                e.Property(x => x.RemovedDate);
                e.Property(x => x.RemovedBy);
            });

            Cfg<FraudControl>(modelBuilder, fc =>
            {
                fc.HasKey(x => x.Id);
                ConfigureInfrastructureFields(fc);
                fc.HasMany(x => x.FraudControlItems);
                fc.Property(x => x.Status).IsRequired();
                fc.Property(x => x.RejectionReasons);
                fc.HasOne(x => x.ReplacesFraudControl).WithMany(x => x.ReplacedByFraudControls).HasForeignKey(x => x.ReplacesFraudControl_Id);
                fc.Property(x => x.IsCurrentData).IsRequired();
            });

            Cfg<FraudControlItem>(modelBuilder, item =>
            {
                item.HasKey(x => x.Id);
                item.HasOne(x => x.FraudControl).WithMany(x => x.FraudControlItems).HasForeignKey(x => x.FraudControl_Id);
                ConfigureInfrastructureFields(item);
            });

            Cfg<CreditApplicationComment>(modelBuilder, e =>
            {
                e.HasKey(x => x.Id);
                ConfigureInfrastructureFields(e);
                e.HasOne(x => x.CreditApplication).WithMany(x => x.Comments).HasForeignKey(x => x.ApplicationNr).IsRequired();
                e.Property(x => x.CommentById).IsRequired();
                e.Property(x => x.CommentDate).IsRequired();
                e.Property(x => x.CommentText);
                e.Property(x => x.Attachment);
                e.Property(x => x.EventType).HasMaxLength(100);
                e.HasIndex(x => new { x.ApplicationNr });
                e.HasIndex(x => new { x.ApplicationNr, x.EventType }).HasDatabaseName("CreditApplicationComment_PerfIdx1");
                e.HasIndex(x => new { x.EventType, x.ApplicationNr }).HasDatabaseName("CreditApplicationComment_PerfIdx2").IncludeProperties(x => new { x.CommentDate });
            });

            Cfg<CreditApplicationOneTimeToken>(modelBuilder, e =>
            {
                e.HasKey(x => x.Token);
                e.Property(x => x.Token).HasMaxLength(60);
                e.HasOne(x => x.CreditApplication).WithMany(x => x.OneTimeTokens).HasForeignKey(x => x.ApplicationNr).IsRequired();
                ConfigureInfrastructureFields(e);
                e.Property(x => x.TokenType).IsRequired().HasMaxLength(100);
                e.Property(x => x.TokenExtraData);
                e.Property(x => x.ValidUntilDate);
                e.Property(x => x.RemovedDate);
                e.Property(x => x.RemovedBy);
                e.Property(x => x.CreationDate).IsRequired();
                e.HasIndex(x => new { x.ApplicationNr, x.Token, x.TokenType }).HasDatabaseName("CreditApplicationOneTimeToken_PerfIdx1");
                e.HasIndex(x => new { x.ApplicationNr });
            });

            Cfg<SystemItem>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.Id);
                e.Property(x => x.Key).IsRequired().HasMaxLength(100);
                e.Property(x => x.Value);
                e.HasIndex(x => x.Key);
            });

            Cfg<HandlerLimitLevel>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.HandlerUserId);
                e.Property(x => x.HandlerUserId).ValueGeneratedNever();
                e.Property(x => x.IsOverrideAllowed).IsRequired();
                e.Property(x => x.LimitLevel).IsRequired();
            });

            Cfg<ApplicationReportCasheRow>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.ApplicationNr);
                e.Property(x => x.ApplicationNr).HasMaxLength(128);
                e.Property(x => x.ApplicationDate).HasColumnType("datetime");
                e.Property(x => x.ChangedById);
                e.Property(x => x.ChangedDate);
                e.Property(x => x.Decision);
                e.Property(x => x.DecisionAmount);
                e.Property(x => x.DecisionInterestRate);
                e.Property(x => x.DecisionNotificationFee);
                e.Property(x => x.DecisionRejectionReasons);
                e.Property(x => x.DecisionRepaymentTime);
                e.Property(x => x.Handler);
                e.Property(x => x.Overrided);
                e.Property(x => x.SysRecomendation);
                e.Property(x => x.SysRecomendationInterestRate);
                e.Property(x => x.SysRecomendationMaxAmount);
                e.Property(x => x.SysRecomendationNotificationFee);
                e.Property(x => x.SysRecomendationomendationAmount);
                e.Property(x => x.SysRecomendationomendationRepaymentTime);
                e.Property(x => x.SysRecomendationRejectionReasons);
            });

            Cfg<CreditApprovalBatchHeader>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.Id);
                e.HasMany(x => x.Items).WithOne(x => x.CreditApprovalBatch).HasForeignKey(x => x.CreditApprovalBatchHeaderId).IsRequired();
                e.Property(x => x.ApprovedById).IsRequired();
                e.Property(x => x.ApprovedDate).IsRequired();
            });

            Cfg<CreditApprovalBatchItem>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.Id);
                e.Property(x => x.CreditNr).HasMaxLength(128);
                e.Property(x => x.ApprovalType).IsRequired().HasMaxLength(128);
                e.Property(x => x.ApprovedAmount).IsRequired();
                e.Property(x => x.DecisionById); //Not required since this may be automated in the future in which case there really isnt a person who made the decision
                e.Property(x => x.ApprovedById).IsRequired();
                e.HasMany(x => x.Overrides).WithOne(x => x.BatchItem).HasForeignKey(x => x.CreditApprovalBatchItemId).IsRequired();
            });

            Cfg<CreditApprovalBatchItemOverride>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.Id);
                e.Property(x => x.ContextData);
                e.Property(x => x.CodeName).IsRequired().HasMaxLength(128);
            });

            Cfg<CreditApplicationChangeLogItem>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.Id);
                e.Property(x => x.ApplicationNr).IsRequired().HasMaxLength(100);
                e.Property(x => x.Name).IsRequired().HasMaxLength(100);
                e.Property(x => x.GroupName).IsRequired().HasMaxLength(100);

                e.Property(x => x.OldValue).IsRequired().HasMaxLength(100);
                e.Property(x => x.TransactionType).IsRequired().HasMaxLength(100);
                //NOTE: These three indexes are preseverd from ef but it looks from that code it was intended to be one index
                //      that covered ApplicationNr, Name, GroupName
                e.HasIndex(x => x.ApplicationNr).HasDatabaseName("CreditApplicationChangeLogItemApplicationNrsIndex");
                e.HasIndex(x => x.Name).HasDatabaseName("CreditApplicationChangeLogItemNamesIndex");
                e.HasIndex(x => x.GroupName).HasDatabaseName("CreditApplicationChangeLogItemGroupNamesIndex");
            });

            Cfg<CustomerCheckpoint>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.Id);
                e.Property(x => x.CustomerId).IsRequired();
                e.Property(x => x.IsCheckpointActive).IsRequired();
                e.Property(x => x.IsReasonTextEncrypted).IsRequired();
                e.Property(x => x.ReasonText).HasMaxLength(2000);
                e.Property(x => x.StateBy).IsRequired();
                e.Property(x => x.StateDate).IsRequired().HasColumnType("datetime");
                e.HasIndex(x => x.CustomerId);
                e.HasIndex(x => new { x.CustomerId }).HasDatabaseName("IX_CustomerCheckpointOnlyOneDefault").IsUnique().HasFilter("[IsCurrentState]=(1)");
            });

            Cfg<CreditApplicationCancellation>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.Id);
                e.Property(x => x.WasAutomated).IsRequired();
                e.Property(x => x.CancelledDate).IsRequired();
                e.Property(x => x.CancelledBy).IsRequired();
                e.Property(x => x.CancelledState).HasMaxLength(100);
            });

            Cfg<CreditDecisionSearchTerm>(modelBuilder, e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.TermName).IsRequired().HasMaxLength(128);
                e.Property(x => x.TermValue).IsRequired().HasMaxLength(128);
                e.HasIndex(x => new { x.TermName, x.TermValue }).HasDatabaseName("CreditDecisionSearchTermSearchIdx1");
            });

            Cfg<MortgageLoanCreditApplicationHeaderExtension>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.ApplicationNr);
                ch.Property(x => x.CustomerOfferStatus).IsRequired().HasMaxLength(100);
                ch.Property(x => x.AdditionalQuestionsStatus).HasMaxLength(100);
                ch.Property(x => x.DocumentCheckStatus).HasMaxLength(100);
                ch.Property(x => x.InitialCreditCheckStatus).HasMaxLength(100);
                ch.Property(x => x.FinalCreditCheckStatus).HasMaxLength(100);
                ch.Property(x => x.DirectDebitCheckStatus).HasMaxLength(100);

                ch.HasIndex(x => x.CustomerOfferStatus);
                ch.HasIndex(x => x.AdditionalQuestionsStatus);
                ch.HasIndex(x => x.InitialCreditCheckStatus);
                ch.HasIndex(x => x.FinalCreditCheckStatus);
                ch.HasIndex(x => x.DirectDebitCheckStatus);
                ch.HasIndex(x => x.DocumentCheckStatus);
            });

            Cfg<CreditApplicationEvent>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.EventType).IsRequired().HasMaxLength(100);
                ch.Property(x => x.EventDate).IsRequired();
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.HasMany(x => x.CreatedExtensions).WithOne(x => x.CreatedByBusinessEvent).HasForeignKey(x => x.CreatedByBusinessEventId).IsRequired();
                ch.HasMany(x => x.CreatedCreditApplicationListOperations).WithOne(x => x.ByEvent).HasForeignKey(x => x.CreditApplicationEventId);
                ch.HasMany(x => x.CreatedCreditApplicationCustomerListOperations).WithOne(x => x.ByEvent).HasForeignKey(x => x.CreditApplicationEventId);
                ch.HasMany(x => x.CreatedCreditApplicationChangeLogItems).WithOne(x => x.EditEvent).HasForeignKey(x => x.EditEventId);

                ch.HasMany(x => x.CreatedComplexApplicationListItems).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByEventId).IsRequired().OnDelete(DeleteBehavior.ClientNoAction);
                ch.HasMany(x => x.ChangedComplexApplicationListItems).WithOne(x => x.LatestChangeEvent).HasForeignKey(x => x.LatestChangeEventId).IsRequired().OnDelete(DeleteBehavior.ClientNoAction);
                ch.HasMany(x => x.CreatedHComplexApplicationListItems).WithOne(x => x.ChangeEvent).HasForeignKey(x => x.ChangeEventId).OnDelete(DeleteBehavior.ClientSetNull);
            });

            Cfg<CreditApplicationDocumentHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.ApplicantNr);
                ch.Property(x => x.CustomerId);
                ch.Property(x => x.DocumentType).IsRequired().HasMaxLength(100);
                ch.Property(x => x.AddedDate).IsRequired();
                ch.Property(x => x.AddedByUserId).IsRequired();
                ch.Property(x => x.RemovedDate);
                ch.Property(x => x.RemovedByUserId);
                ch.Property(x => x.DocumentArchiveKey);
                ch.Property(x => x.DocumentFileName);
                ch.Property(x => x.DocumentSubType).HasMaxLength(256);
            });

            Cfg<KeyValueItem>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => new { x.Key, x.KeySpace });
                ch.Property(x => x.Key).IsRequired().HasMaxLength(128);
                ch.Property(x => x.KeySpace).IsRequired().HasMaxLength(128);
                ch.Property(x => x.Value);
            });

            Cfg<TemporaryExternallyEncryptedItem>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.Id);
                ch.Property(x => x.Id).HasMaxLength(128);
                ch.Property(x => x.CipherText).IsRequired();
                ch.Property(x => x.ProtocolVersionName).IsRequired().HasMaxLength(100);
                ch.Property(x => x.DeleteAfterDate).IsRequired().HasColumnType("datetime");
                ch.Property(x => x.AddedDate).IsRequired().HasColumnType("datetime");
            });

            Cfg<AffiliateReportingEvent>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.Id);
                ch.Property(x => x.CreationDate).IsRequired().HasColumnType("datetime");
                ch.Property(x => x.WaitUntilDate).IsRequired().HasColumnType("datetime");
                ch.Property(x => x.DeleteAfterDate).IsRequired().HasColumnType("datetime");
                ch.Property(x => x.EventData).IsRequired();
                ch.Property(x => x.EventType).IsRequired().HasMaxLength(128);
                ch.Property(x => x.ApplicationNr).IsRequired().HasMaxLength(128);
                ch.Property(x => x.ProviderName).IsRequired().HasMaxLength(128);
                ch.Property(x => x.ProcessedStatus).HasMaxLength(128).IsRequired();
                ch.Property(x => x.ProcessedDate).HasColumnType("datetime");
            });

            Cfg<AffiliateReportingLogItem>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.Id);
                ch.Property(x => x.ProviderName).IsRequired().HasMaxLength(128);
                ch.Property(x => x.LogDate).IsRequired().HasColumnType("datetime");
                ch.Property(x => x.DeleteAfterDate).IsRequired().HasColumnType("datetime");
                ch.Property(x => x.IncomingApplicationEventId).IsRequired();
                ch.Property(x => x.ProcessedStatus).IsRequired().HasMaxLength(128);
                ch.Property(x => x.ThrottlingCount);
                ch.Property(x => x.ThrottlingContext).HasMaxLength(128);
                ch.Property(x => x.MessageText).HasMaxLength(1024);
                ch.Property(x => x.ExceptionText).HasMaxLength(1024);
                ch.Property(x => x.OutgoingRequestBody);
                ch.Property(x => x.OutgoingResponseBody);
            });

            Cfg<CreditApplicationListMember>(modelBuilder, ch =>
            {
                ch.HasKey(x => new { x.ApplicationNr, x.ListName });
                ch.Property(x => x.ApplicationNr).HasMaxLength(128).IsRequired();
                ch.Property(x => x.ListName).HasMaxLength(128).IsRequired();
                ch.HasIndex(x => new { x.ApplicationNr });
            });

            Cfg<CreditApplicationListOperation>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.Id);
                ch.Property(x => x.ApplicationNr).HasMaxLength(128).IsRequired();
                ch.Property(x => x.ListName).HasMaxLength(128).IsRequired();
                ch.Property(x => x.IsAdd).IsRequired();
                ch.Property(x => x.OperationDate).IsRequired();
                ch.Property(x => x.ByUserId).IsRequired();
            });

            Cfg<CreditApplicationCustomerListMember>(modelBuilder, ch =>
            {
                ch.HasKey(x => new { x.ApplicationNr, x.CustomerId, x.ListName });
                ch.Property(x => x.CustomerId);
                ch.Property(x => x.ApplicationNr).HasMaxLength(128).IsRequired();
                ch.Property(x => x.ListName).HasMaxLength(128).IsRequired();
                ch.HasIndex(x => new { x.ApplicationNr });
            });

            Cfg<CreditApplicationCustomerListOperation>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.Id);
                ch.Property(x => x.ApplicationNr).HasMaxLength(128).IsRequired();
                ch.Property(x => x.CustomerId).IsRequired();
                ch.Property(x => x.ListName).HasMaxLength(128).IsRequired();
                ch.Property(x => x.IsAdd).IsRequired();
                ch.Property(x => x.OperationDate).IsRequired();
                ch.Property(x => x.ByUserId).IsRequired();
            });

            Cfg<ComplexApplicationListItem>(modelBuilder, e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.ListName).IsRequired().HasMaxLength(128);
                e.Property(x => x.ItemName).IsRequired().HasMaxLength(128);
                e.Property(x => x.ItemValue).IsRequired().HasMaxLength(512);
                e.Property(x => x.IsRepeatable).IsRequired();
            });

            Cfg<HComplexApplicationListItem>(modelBuilder, e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.ListName).IsRequired().HasMaxLength(128);
                e.Property(x => x.ItemName).IsRequired().HasMaxLength(128);
                e.Property(x => x.IsRepeatable).IsRequired();
                e.Property(x => x.ItemValue).HasMaxLength(512);
            });

            Cfg<ManualSignature>(modelBuilder, e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.SignatureSessionId);
                e.Property(x => x.CreationDate).IsRequired().HasColumnType("datetime");
                e.Property(x => x.CommentText).IsRequired();
                e.Property(x => x.UnsignedDocumentArchiveKey);
                e.Property(x => x.IsRemoved);
                e.Property(x => x.RemovedDate).HasColumnType("datetime");
                e.Property(x => x.IsHandled);
                e.Property(x => x.HandledDate).HasColumnType("datetime");
                e.Property(x => x.SignedDocumentArchiveKey);
                e.Property(x => x.SignedDate).HasColumnType("datetime");
                e.Property(x => x.HandleByUser);
            });

            Cfg<WorkListHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);

                ch.HasKey(x => x.Id);
                ch.Property(x => x.ListType).IsRequired().HasMaxLength(128);
                ch.Property(x => x.CreationDate).HasColumnType("datetime").IsRequired();
                ch.Property(x => x.CreatedByUserId).IsRequired();
                ch.Property(x => x.ClosedByUserId);
                ch.Property(x => x.ClosedDate).HasColumnType("datetime");
                ch.Property(x => x.CustomData);
                ch.Property(x => x.IsUnderConstruction).IsRequired();
                ch.HasMany(x => x.Items).WithOne(x => x.WorkList).HasForeignKey(x => x.WorkListHeaderId).IsRequired();
                ch.HasMany(x => x.FilterItems).WithOne(x => x.WorkList).HasForeignKey(x => x.WorkListHeaderId).IsRequired();
            });

            Cfg<WorkListItem>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);

                ch.HasKey(x => new { x.WorkListHeaderId, x.ItemId });
                ch.Property(x => x.ItemId).IsRequired().HasMaxLength(128);
                ch.Property(x => x.WorkListHeaderId).IsRequired();
                ch.Property(x => x.OrderNr).IsRequired();
                ch.Property(x => x.TakenByUserId);
                ch.Property(x => x.TakenDate).HasColumnType("datetime");
                ch.Property(x => x.CompletedDate).HasColumnType("datetime");
                ch.HasMany(x => x.Properties).WithOne(x => x.Item).HasForeignKey(x => new { x.WorkListHeaderId, x.ItemId }).IsRequired();

                ch.HasIndex(x => new { x.WorkListHeaderId, x.OrderNr }).HasDatabaseName("OrderUIdx").IsUnique();
            });

            Cfg<WorkListItemProperty>(modelBuilder, ch =>
            {
                ch.HasKey(x => new { x.WorkListHeaderId, x.ItemId, x.Name });
                ch.Property(x => x.ItemId).IsRequired().HasMaxLength(128);
                ch.Property(x => x.WorkListHeaderId).IsRequired();
                ch.Property(x => x.IsEncrypted).IsRequired();
                ch.Property(x => x.Name).IsRequired().HasMaxLength(128);
                ch.Property(x => x.DataTypeName).IsRequired().HasMaxLength(128);
                ch.Property(x => x.Value).HasMaxLength(128); //Not required since null is a reasonable value to want to store
                ch.HasIndex(x => new { x.WorkListHeaderId, x.ItemId }).HasDatabaseName("IX_WorkListHeaderId_ItemId");
            });

            Cfg<WorkListFilterItem>(modelBuilder, ch =>
            {
                ch.HasKey(x => new { x.WorkListHeaderId, x.Name });
                ch.Property(x => x.WorkListHeaderId).IsRequired();
                ch.Property(x => x.Name).IsRequired().HasMaxLength(128);
                ch.Property(x => x.Value).IsRequired().HasMaxLength(128);
                ch.HasIndex(x => x.WorkListHeaderId);
            });

            Cfg<AbTestingExperiment>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.Id);
                ch.Property(x => x.ExperimentName).IsRequired().HasMaxLength(256);
                ch.Property(x => x.IsActive).IsRequired();
                ch.Property(x => x.CreatedById).IsRequired();
                ch.Property(x => x.CreatedDate).IsRequired();
                ch.Property(x => x.VariationName).HasMaxLength(128).IsRequired();
                ch.Property(x => x.StartDate).HasColumnType("date");
                ch.Property(x => x.EndDate).HasColumnType("date");
            });

            Cfg<Campaign>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.Id);
                ch.Property(x => x.Id).HasMaxLength(128);
                ch.Property(x => x.Name).IsRequired().HasMaxLength(256);
                ch.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime");
                ch.Property(x => x.CreatedByUserId).IsRequired();
                ch.Property(x => x.IsActive).IsRequired();
                ch.Property(x => x.InactivatedOrDeletedDate).HasColumnType("datetime");
                ch.HasMany(x => x.CampaignCodes).WithOne(x => x.Campaign).HasForeignKey(x => x.CampaignId).IsRequired();
            });

            Cfg<CampaignCode>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.Id);
                ch.Property(x => x.Id).HasMaxLength(128);
                ch.Property(x => x.Code).IsRequired().HasMaxLength(256);
                ch.Property(x => x.StartDate).HasColumnType("date");
                ch.Property(x => x.EndDate).HasColumnType("date");
                ch.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime");
                ch.Property(x => x.DelatedDate).HasColumnType("datetime");
                ch.Property(x => x.CreatedByUserId).IsRequired();
                ch.Property(x => x.IsGoogleCampaign).IsRequired();
            });

            Cfg<StandardPolicyFilterRuleSet>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);

                ch.HasKey(x => x.Id);
                ch.Property(x => x.RuleSetName).HasMaxLength(256).IsRequired();
                ch.Property(x => x.RuleSetModelData).IsRequired();
                ch.Property(x => x.SlotName).HasMaxLength(128); //Not required since we use null to represent inactive so we can have a normal unique index
                ch.HasIndex(x => new { x.SlotName }).HasFilter("[SlotName] IS NOT NULL").IsUnique().HasDatabaseName("Idx_StandardPolicyFilterRuleSet_UniqueSlotNames");
            });
        }
    }
}
