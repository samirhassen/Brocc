using nPreCredit.DbModel;
using NTech.Core.Module.Shared.Database;
using NTech.Services.Infrastructure;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;

namespace nPreCredit
{
    public class PreCreditContext : ChangeTrackingDbContext, IPreCreditContext
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

        private const string ConnectionStringName = "PreCreditContext";

        public PreCreditContext() : base($"name={ConnectionStringName}")
        {
            this.Database.CommandTimeout = 3600;
            this.Configuration.AutoDetectChangesEnabled = false;
        }

        private static System.Data.Entity.ModelConfiguration.EntityTypeConfiguration<T> ConfigureInfrastructureFields<T>(System.Data.Entity.ModelConfiguration.EntityTypeConfiguration<T> t) where T : InfrastructureBaseItem
        {
            t.Property(e => e.Timestamp).IsRequired().IsRowVersion();
            t.Property(e => e.ChangedById).IsRequired();
            t.Property(e => e.ChangedDate).IsRequired();
            t.Property(e => e.InformationMetaData);
            return t;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            Func<bool, IndexAnnotation> index = isUnique => new IndexAnnotation(new IndexAttribute() { IsUnique = isUnique });

            modelBuilder.Entity<CreditApplicationKeySequence>().HasKey(x => x.Id);

            Cfg<FraudControlProperty>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.CustomerId).IsRequired();
                ch.Property(x => x.Value).IsRequired();
                ch.Property(x => x.Name).IsRequired().HasMaxLength(128);
                ch.HasOptional(x => x.ReplacesFraudControlProperty).WithMany(x => x.ReplacedByFraudControlProperties).HasForeignKey(x => x.ReplacesFraudControlProperty_Id);
                ch.Property(x => x.IsCurrentData).IsRequired();
            });

            Cfg<CreditApplicationHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.ApplicationNr);
                ch.Property(x => x.NrOfApplicants).IsRequired();
                ch.Property(x => x.ProviderName).IsRequired().HasMaxLength(100);
                ch.Property(x => x.ApplicationType).HasMaxLength(50).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.HasMany(x => x.SearchTerms);
                ch.Property(x => x.ApplicationDate).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.Property(x => x.IsActive).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.Property(x => x.CreditCheckStatus).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.HasMany(x => x.CreditDecisions).WithRequired(x => x.CreditApplication).HasForeignKey(x => x.ApplicationNr);
                ch.HasOptional(x => x.CurrentCreditDecision).WithMany().HasForeignKey(x => x.CurrentCreditDecisionId);
                ch.HasMany(x => x.FraudControls).WithRequired(x => x.CreditApplication).HasForeignKey(x => x.ApplicationNr);
                ch.HasMany(x => x.Cancellations).WithRequired(x => x.CreditApplication).HasForeignKey(x => x.ApplicationNr);
                ch.HasOptional(x => x.MortgageLoanExtension).WithRequired(x => x.Application);

                ch.Property(x => x.CustomerCheckStatus).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.Property(x => x.AgreementStatus).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.Property(x => x.FraudCheckStatus).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.Property(x => x.IsFinalDecisionMade).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.Property(x => x.FinalDecisionById);
                ch.Property(x => x.FinalDecisionDate);
                ch.Property(x => x.IsPartiallyApproved).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.Property(x => x.PartiallyApprovedById);
                ch.Property(x => x.PartiallyApprovedDate);
                ch.Property(x => x.IsRejected).IsRequired();
                ch.Property(x => x.RejectedDate);
                ch.Property(x => x.RejectedById);
                ch.Property(x => x.IsCancelled).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.Property(x => x.CancelledDate);
                ch.Property(x => x.CancelledBy);
                ch.Property(x => x.CancelledState).HasMaxLength(100);
                ch.Property(x => x.ArchivedDate);
                ch.HasMany(x => x.Approvals).WithRequired(x => x.Application).HasForeignKey(x => x.ApplicationNr);
                ch.Property(x => x.WaitingForAdditionalInformationDate).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.Property(x => x.HideFromManualListsUntilDate).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.Property(x => x.CanSkipAdditionalQuestions).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.HasMany(x => x.Events).WithRequired(x => x.Application).HasForeignKey(x => x.ApplicationNr);
                ch.HasMany(x => x.Documents).WithRequired(x => x.CreditApplication).HasForeignKey(x => x.ApplicationNr);
                ch.HasMany(x => x.PauseItems).WithRequired(x => x.CreditApplication).HasForeignKey(x => x.ApplicationNr);
                ch.HasMany(x => x.ListMemberships).WithRequired(x => x.CreditApplication).HasForeignKey(x => x.ApplicationNr);
                ch.HasMany(x => x.ListMembershipOperations).WithRequired(x => x.CreditApplication).HasForeignKey(x => x.ApplicationNr);
                ch.HasMany(x => x.CustomerListMemberships).WithRequired(x => x.CreditApplication).HasForeignKey(x => x.ApplicationNr);
                ch.HasMany(x => x.CustomerListMembershipOperations).WithRequired(x => x.CreditApplication).HasForeignKey(x => x.ApplicationNr);
                ch.HasMany(x => x.ComplexApplicationListItems).WithRequired(x => x.Application).HasForeignKey(x => x.ApplicationNr);
            });

            var cs = modelBuilder.Entity<CreditApplicationSearchTerm>();
            ConfigureInfrastructureFields(cs);
            cs.HasRequired(x => x.CreditApplication).WithMany(x => x.SearchTerms).HasForeignKey(x => x.ApplicationNr);
            cs.Property(x => x.ApplicationNr).HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new[]
                        {
                            new IndexAttribute("CreditApplicationSearchTeBrmCoveringIndex", 3)
                        }));
            cs.HasKey(x => x.Id);
            cs.Property(x => x.Name).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new[]
                        {
                            new IndexAttribute("CreditApplicationSearchTermNameIndex", 1),
                            new IndexAttribute("CreditApplicationSearchTermCoveringIndex", 1)
                        }));
            cs.Property(x => x.Value).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new[]
                        {
                            new IndexAttribute("CreditApplicationSearchTermValueIndex", 1),
                            new IndexAttribute("CreditApplicationSearchTermCoveringIndex", 2)
                        }));

            var ci = modelBuilder.Entity<CreditApplicationItem>();
            ConfigureInfrastructureFields(ci);
            ci.HasRequired(x => x.CreditApplication).WithMany(x => x.Items).HasForeignKey(x => x.ApplicationNr);
            ci.HasKey(x => x.Id);
            ci.Property(x => x.AddedInStepName);
            ci.Property(x => x.GroupName).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new[]
                        {
                            new IndexAttribute("CreditApplicationItemNamesIndex", 1),
                            new IndexAttribute("CreditApplicationItemGroupNameIndex", 1),
                        }));
            ci.Property(x => x.Name).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new[]
                        {
                            new IndexAttribute("CreditApplicationItemNamesIndex", 2),
                            new IndexAttribute("CreditApplicationItemNameIndex", 1),
                        }));
            ci.Property(x => x.Value).IsRequired();

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
                e.HasKey(x => x.Id);
                ConfigureInfrastructureFields(e);
                e.Property(x => x.DecisionDate).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                e.Property(x => x.DecisionById).IsRequired();
                e.Property(x => x.WasAutomated).IsRequired();
                e.Property(x => x.AcceptedDecisionModel).IsRequired();
            });

            Cfg<RejectedCreditDecision>(modelBuilder, e =>
            {
                e.HasKey(x => x.Id);
                ConfigureInfrastructureFields(e);
                e.Property(x => x.DecisionDate).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                e.Property(x => x.DecisionById).IsRequired();
                e.Property(x => x.WasAutomated).IsRequired();
                e.Property(x => x.RejectedDecisionModel).IsRequired();
            });

            Cfg<CreditDecision>(modelBuilder, e =>
            {
                e.Property(x => x.DecisionType).HasMaxLength(20);
                e.HasMany(x => x.PauseItems).WithRequired(x => x.Decision).HasForeignKey(x => x.CreditDecisionId);
                e.HasMany(x => x.SearchTerms).WithRequired(x => x.Decision).HasForeignKey(x => x.CreditDecisionId);
                e.HasMany(x => x.DecisionItems).WithRequired(x => x.Decision).HasForeignKey(x => x.CreditDecisionId);
            });

            Cfg<CreditDecisionPauseItem>(modelBuilder, e =>
            {
                e.HasKey(x => x.Id);
                ConfigureInfrastructureFields(e);
                e.Property(x => x.CustomerId).IsRequired();
                e.Property(x => x.PausedUntilDate).IsRequired();
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
                e.Property(x => x.PausedUntilDate).IsRequired();
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
                fc.HasOptional(x => x.ReplacesFraudControl).WithMany(x => x.ReplacedByFraudControls).HasForeignKey(x => x.ReplacesFraudControl_Id);
                fc.Property(x => x.IsCurrentData).IsRequired();
            });

            Cfg<FraudControlItem>(modelBuilder, item =>
            {
                item.HasKey(x => x.Id);
                ConfigureInfrastructureFields(item);
                item.HasOptional(x => x.FraudControl).WithMany(x => x.FraudControlItems).HasForeignKey(x => x.FraudControl_Id);
            });

            Cfg<CreditApplicationComment>(modelBuilder, e =>
            {
                e.HasKey(x => x.Id);
                ConfigureInfrastructureFields(e);
                e.HasRequired(x => x.CreditApplication).WithMany(x => x.Comments).HasForeignKey(x => x.ApplicationNr);
                e.Property(x => x.CommentById).IsRequired();
                e.Property(x => x.CommentDate).IsRequired();
                e.Property(x => x.CommentText);
                e.Property(x => x.Attachment);
                e.Property(x => x.EventType).HasMaxLength(100);
            });

            Cfg<CreditApplicationOneTimeToken>(modelBuilder, e =>
            {
                e.HasKey(x => x.Token);
                e.Property(x => x.Token).HasMaxLength(60);
                e.HasRequired(x => x.CreditApplication).WithMany(x => x.OneTimeTokens).HasForeignKey(x => x.ApplicationNr);
                ConfigureInfrastructureFields(e);
                e.Property(x => x.TokenType).IsRequired().HasMaxLength(100);
                e.Property(x => x.TokenExtraData);
                e.Property(x => x.ValidUntilDate);
                e.Property(x => x.RemovedDate);
                e.Property(x => x.RemovedBy);
                e.Property(x => x.CreationDate).IsRequired();
            });

            Cfg<SystemItem>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.Id);
                e.Property(x => x.Key).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                e.Property(x => x.Value);
            });

            Cfg<HandlerLimitLevel>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.HandlerUserId);
                e.Property(x => x.HandlerUserId).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
                e.Property(x => x.IsOverrideAllowed).IsRequired();
                e.Property(x => x.LimitLevel).IsRequired();
            });

            Cfg<ApplicationReportCasheRow>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.ApplicationNr);
                e.Property(x => x.ApplicationDate);
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
                e.HasMany(x => x.Items).WithRequired(x => x.CreditApprovalBatch).HasForeignKey(x => x.CreditApprovalBatchHeaderId);
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
                e.HasMany(x => x.Overrides).WithRequired(x => x.BatchItem).HasForeignKey(x => x.CreditApprovalBatchItemId);
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
                e.Property(x => x.ApplicationNr).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new[]
                        {
                            new IndexAttribute("CreditApplicationChangeLogItemApplicationNrsIndex", 1),
                        }));
                e.Property(x => x.Name).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new[]
                        {
                            new IndexAttribute("CreditApplicationChangeLogItemNamesIndex", 2),
                        }));
                e.Property(x => x.GroupName).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new[]
                        {
                            new IndexAttribute("CreditApplicationChangeLogItemGroupNamesIndex", 3),
                        }));
                e.Property(x => x.OldValue).IsRequired().HasMaxLength(100);
                e.Property(x => x.TransactionType).IsRequired().HasMaxLength(100);
            });

            Cfg<CustomerCheckpoint>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.Id);
                e.Property(x => x.CustomerId).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                e.Property(x => x.IsCheckpointActive).IsRequired();
                e.Property(x => x.IsReasonTextEncrypted).IsRequired();
                e.Property(x => x.ReasonText).HasMaxLength(2000);
                e.Property(x => x.StateBy).IsRequired();
                e.Property(x => x.StateDate).IsRequired();
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
                e.Property(x => x.TermName).IsRequired().HasMaxLength(128).HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new[]
                        {
                            new IndexAttribute("CreditDecisionSearchTermSearchIdx1", 1),
                        })); ;
                e.Property(x => x.TermValue).IsRequired().HasMaxLength(128).HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new[]
                        {
                            new IndexAttribute("CreditDecisionSearchTermSearchIdx1", 2),
                        }));
            });

            Cfg<MortgageLoanCreditApplicationHeaderExtension>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.ApplicationNr);
                ch.Property(x => x.CustomerOfferStatus).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.Property(x => x.AdditionalQuestionsStatus).HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.Property(x => x.DocumentCheckStatus).HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.Property(x => x.InitialCreditCheckStatus).HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.Property(x => x.FinalCreditCheckStatus).HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.Property(x => x.DirectDebitCheckStatus).HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
            });

            Cfg<CreditApplicationEvent>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.EventType).IsRequired().HasMaxLength(100);
                ch.Property(x => x.EventDate).IsRequired();
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.HasMany(x => x.CreatedExtensions).WithRequired(x => x.CreatedByBusinessEvent).HasForeignKey(x => x.CreatedByBusinessEventId);
                ch.HasMany(x => x.CreatedCreditApplicationListOperations).WithOptional(x => x.ByEvent).HasForeignKey(x => x.CreditApplicationEventId);
                ch.HasMany(x => x.CreatedCreditApplicationCustomerListOperations).WithOptional(x => x.ByEvent).HasForeignKey(x => x.CreditApplicationEventId);
                ch.HasMany(x => x.CreatedCreditApplicationChangeLogItems).WithOptional(x => x.EditEvent).HasForeignKey(x => x.EditEventId);

                ch.HasMany(x => x.CreatedComplexApplicationListItems).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByEventId).WillCascadeOnDelete(false);
                ch.HasMany(x => x.ChangedComplexApplicationListItems).WithRequired(x => x.LatestChangeEvent).HasForeignKey(x => x.LatestChangeEventId).WillCascadeOnDelete(false);
                ch.HasMany(x => x.CreatedHComplexApplicationListItems).WithOptional(x => x.ChangeEvent).HasForeignKey(x => x.ChangeEventId).WillCascadeOnDelete(false);
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
                ch.Property(x => x.VerifiedDate);
                ch.Property(x => x.VerifiedByUserId);
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
                ch.Property(x => x.CipherText).IsRequired();
                ch.Property(x => x.ProtocolVersionName).IsRequired().HasMaxLength(100);
                ch.Property(x => x.DeleteAfterDate).IsRequired();
                ch.Property(x => x.AddedDate).IsRequired();
            });

            Cfg<AffiliateReportingEvent>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.Id);
                ch.Property(x => x.CreationDate).IsRequired();
                ch.Property(x => x.WaitUntilDate).IsRequired();
                ch.Property(x => x.DeleteAfterDate).IsRequired();
                ch.Property(x => x.EventData).IsRequired();
                ch.Property(x => x.EventType).IsRequired().HasMaxLength(128);
                ch.Property(x => x.ApplicationNr).IsRequired().HasMaxLength(128);
                ch.Property(x => x.ProviderName).IsRequired().HasMaxLength(128);
                ch.Property(x => x.ProcessedStatus).HasMaxLength(128).IsRequired();
            });

            Cfg<AffiliateReportingLogItem>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.Id);
                ch.Property(x => x.ProviderName).IsRequired().HasMaxLength(128);
                ch.Property(x => x.LogDate).IsRequired();
                ch.Property(x => x.DeleteAfterDate).IsRequired();
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
                e.Property(x => x.CreationDate).IsRequired();
                e.Property(x => x.CommentText).IsRequired();
                e.Property(x => x.UnsignedDocumentArchiveKey);
                e.Property(x => x.IsRemoved);
                e.Property(x => x.RemovedDate);
                e.Property(x => x.IsHandled);
                e.Property(x => x.HandledDate);
                e.Property(x => x.SignedDocumentArchiveKey);
                e.Property(x => x.SignedDate);
                e.Property(x => x.HandleByUser);
            });

            Cfg<WorkListHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);

                ch.HasKey(x => x.Id);
                ch.Property(x => x.ListType).IsRequired().HasMaxLength(128);
                ch.Property(x => x.CreationDate).IsRequired();
                ch.Property(x => x.CreatedByUserId).IsRequired();
                ch.Property(x => x.ClosedByUserId);
                ch.Property(x => x.ClosedDate);
                ch.Property(x => x.CustomData);
                ch.Property(x => x.IsUnderConstruction).IsRequired();
                ch.HasMany(x => x.Items).WithRequired(x => x.WorkList).HasForeignKey(x => x.WorkListHeaderId);
                ch.HasMany(x => x.FilterItems).WithRequired(x => x.WorkList).HasForeignKey(x => x.WorkListHeaderId);
            });

            Cfg<WorkListItem>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);

                ch.HasKey(x => new { x.WorkListHeaderId, x.ItemId });
                ch.Property(x => x.ItemId).IsRequired().HasMaxLength(128);
                ch.Property(x => x.WorkListHeaderId).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("OrderUIdx") { Order = 1, IsUnique = true }));
                ch.Property(x => x.OrderNr).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("OrderUIdx") { Order = 2, IsUnique = true }));
                ch.Property(x => x.TakenByUserId);
                ch.Property(x => x.TakenDate);
                ch.Property(x => x.CompletedDate);
                ch.HasMany(x => x.Properties).WithRequired(x => x.Item).HasForeignKey(x => new { x.WorkListHeaderId, x.ItemId });
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
            });

            Cfg<WorkListFilterItem>(modelBuilder, ch =>
            {
                ch.HasKey(x => new { x.WorkListHeaderId, x.Name });
                ch.Property(x => x.WorkListHeaderId).IsRequired();
                ch.Property(x => x.Name).IsRequired().HasMaxLength(128);
                ch.Property(x => x.Value).IsRequired().HasMaxLength(128);
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
                ch.Property(x => x.Name).IsRequired().HasMaxLength(256);
                ch.Property(x => x.CreatedDate).IsRequired();
                ch.Property(x => x.CreatedByUserId).IsRequired();
                ch.Property(x => x.IsActive).IsRequired();
                ch.HasMany(x => x.CampaignCodes).WithRequired(x => x.Campaign).HasForeignKey(x => x.CampaignId);
            });

            Cfg<CampaignCode>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.Id);
                ch.Property(x => x.Code).IsRequired().HasMaxLength(256);
                ch.Property(x => x.StartDate).HasColumnType("date");
                ch.Property(x => x.EndDate).HasColumnType("date");
                ch.Property(x => x.CreatedDate).IsRequired();
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
            });
        }

        private static void Cfg<T>(DbModelBuilder mb, Action<EntityTypeConfiguration<T>> a) where T : class
        {
            a(mb.Entity<T>());
        }

        public static T RunWithExclusiveLock<T>(string lockName, Func<T> ifLockAquired, Func<T> ifAlreadyLocked, TimeSpan? waitForLock = null) =>
            NTechPerServiceExclusiveLock.RunWithExclusiveLock(lockName, ifLockAquired, ifAlreadyLocked, waitForLock: waitForLock);

        public static void InitDatabase()
        {
            System.Data.Entity.Database.SetInitializer(new System.Data.Entity.MigrateDatabaseToLatestVersion<PreCreditContext, Migrations.Configuration>());
            using (var context = new PreCreditContext())
            {
                context.Database.Initialize(false);
            }

            using (var context = new PreCreditContext())
            {
                context.CreditApplicationKeySequences.Any();
            }
        }

        public void RequireAmbientTransaction()
        {
            if (this.Database.CurrentTransaction == null)
            {
                throw new Exception("This methods writes directly to the database so it needs bo done in an ambient transaction.");
            }
        }

        public TReturn WithAmbientTransaction<TReturn>(Func<TReturn> f)
        {
            var tr = this.Database.BeginTransaction();
            try
            {
                var returnValue = f();
                tr.Commit();
                return returnValue;
            }
            catch
            {
                tr.Rollback();
                throw;
            }
        }

        public virtual int ExecuteDatabaseSqlCommand(string sql, params object[] parameters)
        {
            return Database.ExecuteSqlCommand(sql, parameters);
        }

        public static TReturn WithContext<TReturn>(Func<PreCreditContext, TReturn> f)
        {
            using (var context = new PreCreditContext())
            {
                return f(context);
            }
        }

        public override int SaveChanges()
        {
            try
            {
                return base.SaveChanges();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                if (ex.EntityValidationErrors != null && ex.EntityValidationErrors.Any())
                {
                    //By default these set the text to the utterly useless:
                    //Validation failed for one or more entities. See 'EntityValidationErrors' property for more details.
                    //Unroll it to actually show what's up
                    //Max 20 items to prevent potentially huge errors (number is very arbitrary)
                    var errors = ex.EntityValidationErrors.SelectMany(y => y.ValidationErrors.Select(z => $"{z.PropertyName}: {z.ErrorMessage}")).Distinct().Take(20).ToList();
                    if (errors.Count == 0)
                        throw;
                    throw new Exception($"Validation failed for one or more entities: {string.Join(", ", errors)}", ex);
                }
                else
                    throw;
            }
        }
    }

    public interface IPreCreditContext : INTechDbContext, IDisposable
    {
        DbSet<FraudControlProperty> FraudControlProperties { get; set; }
        DbSet<CreditApplicationKeySequence> CreditApplicationKeySequences { get; set; }
        DbSet<CreditApplicationHeader> CreditApplicationHeaders { get; set; }
        DbSet<CreditApplicationSearchTerm> CreditApplicationSearchTerms { get; set; }
        DbSet<CreditApplicationItem> CreditApplicationItems { get; set; }
        DbSet<CreditApplicationChangeLogItem> CreditApplicationChangeLogItems { get; set; }
        DbSet<CreditApplicationComment> CreditApplicationComments { get; set; }
        DbSet<EncryptedValue> EncryptedValues { get; set; }
        DbSet<CreditDecision> CreditDecisions { get; set; }
        DbSet<FraudControl> FraudControls { get; set; }
        DbSet<FraudControlItem> FraudControlItems { get; set; }
        DbSet<CreditApplicationOneTimeToken> CreditApplicationOneTimeTokens { get; set; }
        DbSet<SystemItem> SystemItems { get; set; }
        DbSet<HandlerLimitLevel> HandlerLimitLevels { get; set; }
        DbSet<CreditApprovalBatchHeader> CreditApprovalBatchHeaders { get; set; }
        DbSet<CreditApprovalBatchItem> CreditApprovalBatchItems { get; set; }
        DbSet<CreditApprovalBatchItemOverride> CreditApprovalBatchItemOverrides { get; set; }
        DbSet<CustomerCheckpoint> CustomerCheckpoints { get; set; }
        DbSet<CreditApplicationCancellation> CreditApplicationCancellations { get; set; }
        DbSet<CreditDecisionPauseItem> CreditDecisionPauseItems { get; set; }
        DbSet<CreditDecisionSearchTerm> CreditDecisionSearchTerms { get; set; }
        DbSet<MortgageLoanCreditApplicationHeaderExtension> MortgageLoanCreditApplicationHeaderExtensions { get; set; }
        DbSet<CreditApplicationEvent> CreditApplicationEvents { get; set; }
        DbSet<CreditApplicationDocumentHeader> CreditApplicationDocumentHeaders { get; set; }
        DbSet<KeyValueItem> KeyValueItems { get; set; }
        DbSet<TemporaryExternallyEncryptedItem> TemporaryExternallyEncryptedItems { get; set; }
        DbSet<CreditApplicationPauseItem> CreditApplicationPauseItems { get; set; }
        DbSet<AffiliateReportingEvent> AffiliateReportingEvents { get; set; }
        DbSet<AffiliateReportingLogItem> AffiliateReportingLogItems { get; set; }
        DbSet<CreditApplicationListMember> CreditApplicationListMembers { get; set; }
        DbSet<CreditApplicationListOperation> CreditApplicationListOperations { get; set; }
        DbSet<CreditApplicationCustomerListMember> CreditApplicationCustomerListMembers { get; set; }
        DbSet<CreditApplicationCustomerListOperation> CreditApplicationCustomerListOperations { get; set; }
        DbSet<CreditDecisionItem> CreditDecisionItems { get; set; }
        DbSet<ComplexApplicationListItem> ComplexApplicationListItems { get; set; }
        DbSet<HComplexApplicationListItem> HComplexApplicationListItems { get; set; }
        DbSet<ApplicationReportCasheRow> ApplicationReportCasheRows { get; set; }
        DbSet<ManualSignature> ManualSignatures { get; set; }
        DbSet<WorkListHeader> WorkListHeaders { get; set; }
        DbSet<WorkListItem> WorkListItems { get; set; }
        DbSet<WorkListItemProperty> WorkListItemProperties { get; set; }
        DbSet<WorkListFilterItem> WorkListFilterItems { get; set; }
        DbSet<AbTestingExperiment> AbTestingExperiments { get; set; }
        DbSet<Campaign> Campaigns { get; set; }
        DbSet<CampaignCode> CampaignCodes { get; set; }

        int SaveChanges();
        int ExecuteDatabaseSqlCommand(string sql, params object[] parameters);
    }
}