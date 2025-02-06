using nPreCredit.Code.Clients;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class ApplicationAssignedHandlerService : IApplicationAssignedHandlerService
    {
        private readonly PreCreditContextFactoryService contextFactoryService;
        private readonly IUserClient userClient;

        public const string ListName = "Handlers";
        public const int ListRowNr = 1;
        public const string AssignedHandlersItemName = "AssignedHandlerIds";

        public ApplicationAssignedHandlerService(PreCreditContextFactoryService contextFactoryService, IUserClient userClient)
        {
            this.contextFactoryService = contextFactoryService;
            this.userClient = userClient;
        }

        public List<int> ChangeAssignedHandlerStateForUsers(string applicationNr, List<(int UserId, bool IsAssigned)> handlersWithIsAssigned)
        {
            handlersWithIsAssigned = handlersWithIsAssigned ?? new List<(int userId, bool isAssigned)>();

            if (handlersWithIsAssigned.GroupBy(x => x.UserId).Any(x => x.Count() > 1))
                throw new NTechWebserviceMethodException("The same userId occurs more than once") { ErrorCode = "userIdOccursMoreThanOnce", ErrorHttpStatusCode = 400, IsUserFacing = true };

            using (var context = contextFactoryService.CreateExtendedConcrete())
            {
                var currentHandlerIds = AssignedHandlerUserIds(applicationNr, context);

                var newHandlerIds = new List<int>();

                //NOTE: All the gymnastics below is to ensure we preserve the order of users already in the list so the ui doesnt "jump around" when you add and remove users. New users are added at the end.

                //Keep the the ones who have not been removed
                var existingUserIdsToRemove = handlersWithIsAssigned.Where(x => !x.IsAssigned).Select(x => x.UserId).ToHashSet();
                foreach (var userId in currentHandlerIds)
                {
                    if (!existingUserIdsToRemove.Contains(userId))
                        newHandlerIds.Add(userId);
                }

                //Add new ones at the end in the order of the input, not reordered in the hashset
                var newUserIdsToAdd = handlersWithIsAssigned.Where(x => x.IsAssigned).Select(x => x.UserId).ToHashSet();
                newUserIdsToAdd.ExceptWith(currentHandlerIds);
                foreach (var handler in handlersWithIsAssigned)
                {
                    if (newUserIdsToAdd.Contains(handler.UserId))
                        newHandlerIds.Add(handler.UserId);
                }

                ComplexApplicationListService.ChangeListComposable(new List<ComplexApplicationListOperation>
                {
                    new ComplexApplicationListOperation
                    {
                        ApplicationNr = applicationNr,
                        ListName = ListName,
                        Nr = ListRowNr,
                        ItemName = AssignedHandlersItemName,
                        RepeatedValue = newHandlerIds.Select(x => x.ToString()).ToList()
                    }
                }, context);

                context.SaveChanges();

                return newHandlerIds;
            }
        }

        public List<int> GetAssignedHandlerUserIds(string applicationNr)
        {
            using (var context = contextFactoryService.CreateExtendedConcrete())
            {
                return AssignedHandlerUserIds(applicationNr, context);
            }
        }

        public List<int> GetPossibleHandlerUserIds(string applicationNr = null)
        {
            //NOTE: This will likely need to be changed in the future to allow users to actively pick which users can be handlers.
            //Application nr here to allow both the use case of a dropdown for filtering applications with "all" handlers
            //and a more narrow list on a specific application of which to add that respects say credit limits
            return this.userClient.GetUsersIdsInMiddle();
        }

        private static List<int> AssignedHandlerUserIds(string applicationNr, IPreCreditContext context)
        {
            return context
                .ComplexApplicationListItems.Where(x =>
                    x.ApplicationNr == applicationNr && x.ListName == ListName && x.Nr == ListRowNr &&
                    x.ItemName == AssignedHandlersItemName && x.IsRepeatable)
                .OrderBy(x => x.Id)
                .Select(x => x.ItemValue)
                .ToList()
                .Select(int.Parse)
                .ToList();
        }
    }

    public interface IApplicationAssignedHandlerService
    {
        /// <summary>
        /// Changes the assigned/not assigned state for the given users. Does not change the state for users not included in the dictionary.
        /// </summary>
        List<int> ChangeAssignedHandlerStateForUsers(string applicationNr, List<(int UserId, bool IsAssigned)> handlersWithIsAssigned);
        List<int> GetAssignedHandlerUserIds(string applicationNr);
        List<int> GetPossibleHandlerUserIds(string applicationNr = null);
    }
}