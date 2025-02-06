using Moq;
using NTech.Core.Customer.Database;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Customer.Shared.Services;
using NTech.Core.Module;

namespace NTech.Core.Host.IntegrationTests.UlLegacy.Checkpoints
{
    public class CustomerCheckpointTests
    {
        [Test]
        public void CheckpointHappyFlow()
        {
            UlLegacyTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var contextFactory = new CustomerContextFactory(() => new CustomerContextExtended(support.CurrentUser, support.Clock));
                var serviceRegistry = new Mock<INTechServiceRegistry>();
                serviceRegistry
                    .Setup(x => x.InternalServiceUrl(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Tuple<string, string>[]>()))
                    .Returns(new Uri("https://test.example.org"));
                var service = new CustomerCheckPointService(contextFactory, serviceRegistry.Object);
                var customerId = 42;
                var stateRequest = new FetchStateAndHistoryForCustomerRequest { CustomerId = customerId };
                var activeRequest = new GetActiveCheckPointIdsOnCustomerIdsRequest { CustomerIds = new List<int> { customerId } };

                //Get checkpoint status when none exist
                {
                    var stateAndHistory = service.FetchStateAndHistoryForCustomer(stateRequest);
                    var active = service.GetActiveCheckPointIdsOnCustomerIds(activeRequest);

                    Assert.That(stateAndHistory.HistoryStates.Count, Is.EqualTo(0));
                    Assert.That(stateAndHistory.CurrentState, Is.Null);
                    Assert.That(active.CheckPointByCustomerId.Count, Is.EqualTo(0));
                }

                //Set status
                {
                    service.SetCheckpointState(new SetCheckpointStateRequest
                    {
                        CustomerId = customerId,
                        Codes = new List<string> { "Something" },
                        ReasonText = "test"
                    });
                    var stateAndHistory = service.FetchStateAndHistoryForCustomer(stateRequest);
                    var active = service.GetActiveCheckPointIdsOnCustomerIds(activeRequest);
                    var activeFiltered = service.GetActiveCheckPointIdsOnCustomerIds(new GetActiveCheckPointIdsOnCustomerIdsRequest
                    {
                        OnlyAmongTheseCodes = new List<string> { "SomethingElse" },
                        CustomerIds = new List<int> { customerId }
                    });

                    Assert.That(stateAndHistory.HistoryStates.Count, Is.EqualTo(1));
                    Assert.That(stateAndHistory.CurrentState.Codes, Is.SubsetOf(new List<string> { "Something" }));
                    Assert.That(active.CheckPointByCustomerId.Count, Is.EqualTo(1));
                    Assert.That(activeFiltered.CheckPointByCustomerId.Count, Is.EqualTo(0));

                    var text = service.FetchReasonText(new FetchReasonTextRequest { CheckpointId = stateAndHistory.CurrentState.Id })?.ReasonText;
                    Assert.That(text, Is.EqualTo("test"));
                }

                //Unset status
                {
                    service.SetCheckpointState(new SetCheckpointStateRequest
                    {
                        CustomerId = customerId,
                        Codes = null
                    });
                    var stateAndHistory = service.FetchStateAndHistoryForCustomer(stateRequest);
                    var active = service.GetActiveCheckPointIdsOnCustomerIds(activeRequest);

                    Assert.That(stateAndHistory.CurrentState.Codes.Count, Is.EqualTo(0));
                    Assert.That(stateAndHistory.HistoryStates.Count, Is.EqualTo(2));
                }

                //Set again and then migrate in something else
                {
                    service.SetCheckpointState(new SetCheckpointStateRequest
                    {
                        CustomerId = customerId,
                        Codes = new List<string> { "Something" },
                        ReasonText = "value1"
                    });
                    service.BulkInsertCheckpoints(new BulkInsertCheckpointsRequest
                    {
                        Checkpoints = new List<BulkInsertCheckpointsRequest.HistoricalCheckpoint>
                        {
                            new BulkInsertCheckpointsRequest.HistoricalCheckpoint
                            {
                                Codes = new List<string> { "Something" },
                                CustomerId = customerId,
                                IsCurrentState = false,
                                ReasonText = "value2",
                                StateBy = 1,
                                StateDate = support.Clock.Today
                            },
                            new BulkInsertCheckpointsRequest.HistoricalCheckpoint
                            {
                                Codes = new List<string> { "Something" },
                                CustomerId = customerId,
                                IsCurrentState = true,
                                ReasonText = "value3",
                                StateBy = 1,
                                StateDate = support.Clock.Today
                            }
                        }
                    });

                    var stateAndHistory = service.FetchStateAndHistoryForCustomer(stateRequest);
                    var text = service.FetchReasonText(new FetchReasonTextRequest { CheckpointId = stateAndHistory.CurrentState.Id })?.ReasonText;
                    Assert.That(text, Is.EqualTo("value3"));
                }
            });
        }
    }
}
