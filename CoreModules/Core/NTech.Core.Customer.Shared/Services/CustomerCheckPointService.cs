using Dapper;
using nCustomer.DbModel;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace NTech.Core.Customer.Shared.Services
{
    public class CustomerCheckPointService
    {
        private readonly CustomerContextFactory customerContextFactory;
        private readonly INTechServiceRegistry serviceRegistry;

        public const string FeatureName = "ntech.feature.customercheckpoints";

        public CustomerCheckPointService(CustomerContextFactory customerContextFactory, INTechServiceRegistry serviceRegistry)
        {
            this.customerContextFactory = customerContextFactory;
            this.serviceRegistry = serviceRegistry;
        }

        public FetchStateAndHistoryForCustomerResult FetchStateAndHistoryForCustomer(FetchStateAndHistoryForCustomerRequest request)
        {
            if (request.CustomerId <= 0)
                throw new NTechCoreWebserviceException("Invalid customerId") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            using (var context = customerContextFactory.CreateContext())
            {
                var result = GetCheckPointStateAndHistory(request.CustomerId, context);

                FetchStateAndHistoryForCustomerResult.Item ToView(CustomerCheckPointViewModel x) => x == null ? null : new FetchStateAndHistoryForCustomerResult.Item
                {
                    Id = x.Id,
                    Codes = x.Codes ?? new List<string>(),
                    StateBy = x.StateBy,
                    StateDate = x.StateDate
                };

                var outResult = new FetchStateAndHistoryForCustomerResult
                {
                    CustomerId = request.CustomerId,
                    CurrentState = ToView(result.Item1),
                    HistoryStates = (result.Item1 != null ? new[] { result.Item1 }.Concat(result.Item2) : result.Item2)
                        .Select(ToView)
                        .ToList()
                };

                return outResult;
            }
        }

        public FetchReasonTextResult FetchReasonText(FetchReasonTextRequest request)
        {
            using (var context = customerContextFactory.CreateContext())
            {
                return new FetchReasonTextResult
                {
                    CheckpointId = request.CheckpointId,
                    ReasonText = FetchReasonText(request.CheckpointId, context)
                };
            }
        }

        public SetCheckpointStateResult SetCheckpointState(SetCheckpointStateRequest request)
        {
            using (var context = customerContextFactory.CreateContext())
            {
                var c = SetCheckpointState(request.CustomerId, request.ReasonText, request.Codes, context);

                context.SaveChanges();

                return new SetCheckpointStateResult { Id = c.Id };
            }
        }

        public GetActiveCheckPointIdsOnCustomerIdsResult GetActiveCheckPointIdsOnCustomerIds(GetActiveCheckPointIdsOnCustomerIdsRequest request)
        {
            using (var context = customerContextFactory.CreateContext())
            {
                var customerWithActiveCheckpoints = GetActiveCheckPointIdsOnCustomerIdsInternal(request.CustomerIds?.ToHashSetShared(), request.OnlyAmongTheseCodes, context);

                return new GetActiveCheckPointIdsOnCustomerIdsResult
                {
                    CheckPointByCustomerId = customerWithActiveCheckpoints.ToDictionary(x => x.Key, x => new GetActiveCheckPointIdsOnCustomerIdsResult.CheckPoint
                    {
                        CustomerId = x.Key,
                        CheckPointId = x.Value,
                        CheckpointUrl = serviceRegistry.InternalServiceUrl("nBackOffice", $"s/customer-checkpoints/for-customer/{x.Key}").ToString()
                    })
                };
            }
        }

        public void BulkInsertCheckpoints(BulkInsertCheckpointsRequest request)
        {
            if (request?.Checkpoints == null || request.Checkpoints.Count == 0)
                return;

            using (var context = customerContextFactory.CreateContext())
            {
                context.BeginTransaction();
                try
                {
                    var currentCustomerIds = request.Checkpoints.Where(x => x.IsCurrentState).Select(x => x.CustomerId).Distinct().ToList();
                    if (currentCustomerIds.Any())
                    {
                        //Unset current on any where a current is being migrated in
                        context.GetConnection().Execute("update CustomerCheckpoint set IsCurrentState = 0 where CustomerId in @currentCustomerIds",
                            param: new { currentCustomerIds },
                            transaction: context.CurrentTransaction);
                    }

                    var newCheckPoints = request.Checkpoints.Select(x => context.FillInfrastructureFields(new CustomerCheckpoint
                    {
                        Codes = (x.Codes ?? new List<string>()).Select(code => new CustomerCheckpointCode { Code = code }).ToList(),
                        CustomerId = x.CustomerId,
                        IsCurrentState = x.IsCurrentState,
                        ReasonText = x.ReasonText,
                        StateBy = x.StateBy,
                        StateDate = x.StateDate
                    })).ToArray();

                    foreach (var checkPoint in newCheckPoints)
                    {
                        foreach (var code in checkPoint.Codes)
                        {
                            code.Checkpoint = checkPoint;
                        }
                    }

                    context.AddCustomerCheckpoints(newCheckPoints);

                    context.SaveChanges();

                    context.CommitTransaction();
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }
            }
        }

        private class CustomerCheckPointViewModel
        {
            public int Id { get; set; }
            public List<string> Codes { get; set; }
            public DateTime StateDate { get; set; }
            public int StateBy { get; set; }
        }

        private IDictionary<int, int> GetActiveCheckPointIdsOnCustomerIdsInternal(HashSet<int> customerIds, List<string> onlyAmongTheseCodes, ICustomerContextExtended context)
        {
            var q = context
                .CustomerCheckpointsQueryable;

            if (onlyAmongTheseCodes != null && onlyAmongTheseCodes.Count > 0)
            {
                q = q.Where(x => customerIds.Contains(x.CustomerId) && x.IsCurrentState
                    && x.Codes.Any(y => onlyAmongTheseCodes.Contains(y.Code)));
            }
            else
            {
                q = q.Where(x => customerIds.Contains(x.CustomerId) && x.IsCurrentState
                    && x.Codes.Any());
            }

            return q
                .Select(x => new
                {
                    x.CustomerId,
                    x.Id
                })
                .ToDictionary(x => x.CustomerId, x => x.Id);
        }

        private Tuple<CustomerCheckPointViewModel, List<CustomerCheckPointViewModel>> GetCheckPointStateAndHistory(int customerId, ICustomerContextExtended context)
        {
            var checkpoints = context
                .CustomerCheckpointsQueryable
                .Where(x => x.CustomerId == customerId)
                .Select(x => new
                {
                    x.Id,
                    Codes = x.Codes.Select(y => y.Code),
                    x.IsCurrentState,
                    x.StateDate,
                    x.StateBy
                })
                .OrderByDescending(x => x.Id)
                .ToList()
                .Select(x => new
                {
                    x.IsCurrentState,
                    Model = new CustomerCheckPointViewModel
                    {
                        Id = x.Id,
                        Codes = x.Codes.ToList(),
                        StateDate = x.StateDate,
                        StateBy = x.StateBy
                    }
                });

            var current = checkpoints.SingleOrDefault(x => x.IsCurrentState)?.Model;
            var history = checkpoints.Where(x => !x.IsCurrentState).Select(x => x.Model).ToList();
            return Tuple.Create(current, history);
        }

        private string FetchReasonText(int checkpointId, ICustomerContextExtended context)
        {
            return context
                .CustomerCheckpointsQueryable
                .Where(x => x.Id == checkpointId).Select(x => x.ReasonText).Single();
        }

        private CustomerCheckpoint SetCheckpointState(int customerId, string reasonText, List<string> codes, ICustomerContextExtended context)
        {
            context.GetConnection().Execute("update CustomerCheckpoint set IsCurrentState = 0 where CustomerId = @customerId", new { customerId });

            var c = context.FillInfrastructureFields(new CustomerCheckpoint
            {
                CustomerId = customerId,
                IsCurrentState = true,
                Codes = (codes ?? new List<string>()).Select(code => new CustomerCheckpointCode { Code = code }).ToList(),
                ReasonText = reasonText,
                StateBy = context.CurrentUser.UserId,
                StateDate = context.CoreClock.Now.DateTime
            });

            foreach (var code in c.Codes)
            {
                code.Checkpoint = c;
            }

            context.AddCustomerCheckpoints(c);

            context.SaveChanges();

            return c;
        }
    }

    public class FetchStateAndHistoryForCustomerRequest
    {
        [Required]
        public int CustomerId { get; set; }
    }

    public class FetchReasonTextRequest
    {
        [Required]
        public int CheckpointId { get; set; }
    }

    public class SetCheckpointStateRequest
    {
        [Required]
        public int CustomerId { get; set; }
        [Required]
        public List<string> Codes { get; set; }
        public string ReasonText { get; set; }
    }

    public class GetActiveCheckPointIdsOnCustomerIdsResult
    {
        public Dictionary<int, CheckPoint> CheckPointByCustomerId { get; set; }
        public class CheckPoint
        {
            public int CustomerId { get; set; }
            public int CheckPointId { get; set; }
            public string CheckpointUrl { get; set; }
            public List<string> Codes { get; set; }
        }
    }

    public class FetchStateAndHistoryForCustomerResult
    {
        public int CustomerId { get; set; }
        public Item CurrentState { get; set; }
        public List<Item> HistoryStates { get; set; }

        public class Item
        {
            public int Id { get; set; }
            public List<string> Codes { get; set; }
            public int StateBy { get; set; }
            public DateTime StateDate { get; set; }
        }
    }

    public class FetchReasonTextResult
    {
        public int CheckpointId { get; set; }
        public string ReasonText { get; set; }
    }

    public class SetCheckpointStateResult
    {
        public int Id { get; set; }
    }

    public class BulkInsertCheckpointsRequest
    {
        public class HistoricalCheckpoint
        {
            public int CustomerId { get; set; }
            public bool IsCurrentState { get; set; }
            public string ReasonText { get; set; }
            public List<string> Codes { get; set; }
            public DateTime StateDate { get; set; }
            public int StateBy { get; set; }
        }
        public List<HistoricalCheckpoint> Checkpoints { get; set; }
    }

    public class GetActiveCheckPointIdsOnCustomerIdsRequest
    {
        [Required]
        public List<int> CustomerIds { get; set; }

        /// <summary>
        /// If this is not present or empty then any code will do.
        /// </summary>
        public List<string> OnlyAmongTheseCodes { get; set; }
    }
}