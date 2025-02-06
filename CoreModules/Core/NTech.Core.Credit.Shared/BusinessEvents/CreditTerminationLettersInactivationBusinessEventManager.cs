using nCredit.Code.Services;
using NTech;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class CreditTerminationLettersInactivationBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        private readonly CreditContextFactory creditContextFactory;
        private readonly TerminationLetterCandidateService terminationLetterCandidateService;

        public const int DefaultPostponeDaysCount = 20;

        public CreditTerminationLettersInactivationBusinessEventManager(INTechCurrentUserMetadata currentUser, ICoreClock clock, IClientConfigurationCore clientConfiguration,
            CreditContextFactory creditContextFactory, TerminationLetterCandidateService terminationLetterCandidateService)
            : base(currentUser, clock, clientConfiguration)
        {
            this.creditContextFactory = creditContextFactory;
            this.terminationLetterCandidateService = terminationLetterCandidateService;
        }

        public void InactivateTerminationLettersWhereNotificationsPaid(ICreditContextExtended context, HashSet<string> creditNrsToCheck, BusinessEvent businessEvent = null)
        {
            var lettersToInactivate = context
                .CreditTerminationLetterHeadersQueryable
                .Where(terminationLetter =>
                    terminationLetter.SuspendsCreditProcess == true
                    && creditNrsToCheck.Contains(terminationLetter.CreditNr)
                    && terminationLetter.InactivatedByBusinessEvent == null
                    //Notifications that were overdue when letter was sent must all be fully paid
                    && !terminationLetter.Credit.Notifications.Any(notification => notification.ClosedTransactionDate == null && notification.DueDate <= terminationLetter.TransactionDate))
                .ToList();

            if (lettersToInactivate.Count == 0)
                return;

            var evt = CreateAndAddInactivationEvent(context, businessEvent: businessEvent);

            foreach (var letter in lettersToInactivate)
            {
                if (!TryInactivateTerminationLetter(letter, context, out var failedMessage, businessEvent: evt))
                    throw new NTechCoreWebserviceException($"Failed to inactivate termination letter on {letter.CreditNr}: {failedMessage}");
            }
        }

        public bool TryInactivateTerminationLetter(int terminationLetterId, ICreditContextExtended context, out string failureMessage, BusinessEvent businessEvent = null)
        {
            var letter = context.CreditTerminationLetterHeadersQueryable.SingleOrDefault(x => x.Id == terminationLetterId);
            if (letter == null)
            {
                failureMessage = "No such termination letter exists";
                return false;
            }
            return TryInactivateTerminationLetter(letter, context, out failureMessage, businessEvent: businessEvent);
        }

        public bool TryInactivateTerminationLetter(CreditTerminationLetterHeader letter, ICreditContextExtended context, out string failureMessage, BusinessEvent businessEvent = null)
        {
            if (letter.InactivatedByBusinessEventId.HasValue)
            {
                failureMessage = "Termination letter already inactivated";
                return false;
            }

            var evt = CreateAndAddInactivationEvent(context, businessEvent: businessEvent);

            letter.InactivatedByBusinessEvent = evt;

            AddComment($"Termination letter inactivated",
                BusinessEventType.InactivateTerminationLetter,
                context,
                creditNr: letter.CreditNr);

            PostponeOrResumeTerminationLetters(letter.CreditNr, context, new Lazy<BusinessEvent>(() => evt), Clock.Today.AddDays(DefaultPostponeDaysCount));

            failureMessage = null;
            return true;
        }

        public Dictionary<string, HashSet<int>> InactivateAllTerminationLetters(ICreditContextExtended context, HashSet<string> creditNrs, BusinessEvent businessEvent = null)
        {
            var result = new Dictionary<string, HashSet<int>>();

            var lettersToInactivate = context
                .CreditTerminationLetterHeadersQueryable
                .Where(x => creditNrs.Contains(x.CreditNr) && x.InactivatedByBusinessEventId == null && x.Credit.Status == CreditStatus.Normal.ToString())
                .Select(x => new { x.Id, x.CreditNr })
                .ToList();

            if (lettersToInactivate.Count == 0)
            {
                return result;
            }

            var evt = CreateAndAddInactivationEvent(context, businessEvent: businessEvent);

            foreach (var letter in lettersToInactivate)
            {
                if (!TryInactivateTerminationLetter(letter.Id, context, out var failedMessage, businessEvent: evt))
                {
                    throw new NTechCoreWebserviceException($"Inactivate failed unexpectedly: {failedMessage}");
                }

                if (!result.ContainsKey(letter.CreditNr))
                {
                    result[letter.CreditNr] = new HashSet<int>();
                }

                result[letter.CreditNr].Add(letter.Id);
            }

            return result;
        }

        public PostponeTerminationLettersResponse PostponeTerminationLetters(PostponeTerminationLettersRequest request)
        {
            if (request.UseDefaultDate.HasValue == request.PostponeUntilDate.HasValue)
                throw new NTechCoreWebserviceException("Exactly one of UseDefaultDate and PostponeUntilDate is required") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            using (var context = creditContextFactory.CreateContext())
            {
                if (!context.CreditHeadersQueryable.Any(x => x.CreditNr == request.CreditNr))
                    throw new NTechCoreWebserviceException("Credit does not exist") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                DateTime postponeUntilDate;
                if (request.PostponeUntilDate.HasValue)
                    postponeUntilDate = request.PostponeUntilDate.Value;
                else if (request.UseDefaultDate != true)
                    throw new NTechCoreWebserviceException("Exactly one of UseDefaultDate and PostponeUntilDate is required") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
                else
                    postponeUntilDate = GetDefaultPostponeUntilDate(request.CreditNr, context);

                PostponeOrResumeTerminationLetters(request.CreditNr, context, postponeUntilDate);

                context.SaveChanges();
            }

            return new PostponeTerminationLettersResponse();
        }

        private DateTime GetDefaultPostponeUntilDate(string creditNr, ICreditContextExtended context)
        {
            var candidateDate = terminationLetterCandidateService.GetTerminationCandidateDate(creditNr) ?? context.CoreClock.Today;
            return Dates.Max(candidateDate, context.CoreClock.Today).AddDays(DefaultPostponeDaysCount);
        }

        public ResumeTerminationLettersResponse ResumeTerminationLetters(ResumeTerminationLettersRequest request)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                if (!context.CreditHeadersQueryable.Any(x => x.CreditNr == request.CreditNr))
                    throw new NTechCoreWebserviceException("Credit does not exist") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                PostponeOrResumeTerminationLetters(request.CreditNr, context, null);

                context.SaveChanges();
            }
            return new ResumeTerminationLettersResponse();
        }

        private void PostponeOrResumeTerminationLetters(string creditNr, ICreditContextExtended context, Lazy<BusinessEvent> evt, DateTime? postponeUntilDate)
        {
            var date = postponeUntilDate.HasValue ? postponeUntilDate.Value : Clock.Today;
            var isPostpone = postponeUntilDate.HasValue;

            context.AddDatedCreditDate(new DatedCreditDate
            {
                CreditNr = creditNr,
                BusinessEvent = evt.Value,
                TransactionDate = Now.ToLocalTime().Date,
                Name = DatedCreditDateCode.TerminationLettersPausedUntilDate.ToString(),
                Value = date,
                ChangedById = UserId,
                ChangedDate = Now,
                InformationMetaData = InformationMetadata
            });

            AddComment(
                isPostpone ? $"Termination letters paused until {date:yyyy-MM-dd}" : "Termination letters manually resumed",
                isPostpone ? BusinessEventType.PostponeTerminationLetters : BusinessEventType.ResumeTerminationLetters,
                context,
                creditNr: creditNr);
        }

        private void PostponeOrResumeTerminationLetters(string creditNr, ICreditContextExtended context, DateTime? postponeUntilDate)
        {
            var newEvent = new Lazy<BusinessEvent>(() =>
            {
                var evt = new BusinessEvent
                {
                    EventDate = Now,
                    EventType = postponeUntilDate.HasValue ? BusinessEventType.PostponeTerminationLetters.ToString() : BusinessEventType.ResumeTerminationLetters.ToString(),
                    BookKeepingDate = Now.ToLocalTime().Date,
                    TransactionDate = Now.ToLocalTime().Date,
                    ChangedById = UserId,
                    ChangedDate = Now,
                    InformationMetaData = InformationMetadata
                };
                context.AddBusinessEvent(evt);
                return evt;
            });

            PostponeOrResumeTerminationLetters(creditNr, context, newEvent, postponeUntilDate);
        }

        private BusinessEvent CreateAndAddInactivationEvent(ICreditContextExtended context, BusinessEvent businessEvent = null)
        {
            var evt = businessEvent ?? new BusinessEvent
            {
                EventDate = Now,
                EventType = BusinessEventType.InactivateTerminationLetter.ToString(),
                BookKeepingDate = Now.ToLocalTime().Date,
                TransactionDate = Now.ToLocalTime().Date,
                ChangedById = UserId,
                ChangedDate = Now,
                InformationMetaData = InformationMetadata
            };
            if (businessEvent == null)
            {
                context.AddBusinessEvent(evt);
            }
            return evt;
        }
    }

    public class PostponeTerminationLettersRequest
    {
        [Required]
        public string CreditNr { get; set; }
        public DateTime? PostponeUntilDate { get; set; }
        public bool? UseDefaultDate { get; set; }
    }

    public class PostponeTerminationLettersResponse
    {

    }

    public class ResumeTerminationLettersRequest
    {
        [Required]
        public string CreditNr { get; set; }
    }

    public class ResumeTerminationLettersResponse
    {

    }
}