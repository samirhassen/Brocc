var app = angular.module('app', ['ntech.forms', 'ntech.components']);

class Loan {
    public amount: number
}

class CreditApplicationsToApproveCtr {
    static $inject = ['$scope', '$http', '$q', '$timeout']
    constructor(
        $scope: any, //ng.IScope
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $timeout: ng.ITimeoutService
    ) {
        let client = new NTechPreCreditApi.ApiClient(errorMessage => {
            toastr.error(errorMessage);
            $scope.isLoading = false;
        }, $http, $q);

        var apps = angular.copy(initialData.applications)
        angular.forEach(apps, function (a) {
            a.isApproved = a.overrides.length === 0
        })

        $scope.onBack = (evt) => {
            if (evt) {
                evt.preventDefault()
            }
            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, client, $q, {})
        }

        $scope.approvedApps = function () { return _.where(apps, { isApproved: true }) }

        $scope.totalCountToApprove = function () {
            return _.reduce($scope.approvedApps(), function (memo, x) { return memo + 1; }, 0)
        }

        $scope.totalAmountToApprove = function () {
            return _.reduce($scope.approvedApps(), function (memo, x: Loan) { return memo + x.amount; }, 0)
        }

        $scope.newLoanCountToApprove = function () {
            return _.chain($scope.approvedApps()).where({ typeName: 'NewLoan' }).reduce(function (memo, x) { return memo + 1; }, 0).value()
        }

        $scope.newLoanAmountToApprove = function () {
            return _.chain($scope.approvedApps()).where({ typeName: 'NewLoan' }).reduce(function (memo, x) { return memo + x.amount; }, 0).value()
        }

        $scope.additionalLoanCountToApprove = function () {
            return _.chain($scope.approvedApps()).where({ typeName: 'AdditionalLoan' }).reduce(function (memo, x) { return memo + 1; }, 0).value()
        }

        $scope.additionalLoanAmountToApprove = function () {
            return _.chain($scope.approvedApps()).where({ typeName: 'AdditionalLoan' }).reduce(function (memo, x) { return memo + x.amount; }, 0).value()
        }

        $scope.applications = apps
        $scope.isLoading = false

        $scope.createCredits = function (evt) {
            if (evt) {
                evt.preventDefault()
            }
            $scope.isLoading = true
            var applicationNrsToApprove = _.pluck($scope.approvedApps(), 'applicationNr')
            $http({
                method: 'POST',
                url: initialData.approveUrl,
                data: {
                    applicationNrs: applicationNrsToApprove
                }
            }).then(function successCallback(response) {
                $scope.isLoading = false
                location.reload()
            }, function errorCallback(response) {
                $scope.isLoading = false
                toastr.error(response.data.errorMessage, 'Error')
            })
        }

        var historyLoadedOnce = false

        $scope.$watch('showHistoryBlock', function () {
            if ($scope.showHistoryBlock && !historyLoadedOnce) {
                historyLoadedOnce = true
                $scope.filterHistory()
            }
        })

        function isNullOrWhitespace(input) {
            if (typeof input === 'undefined' || input == null) return true;

            if ($.type(input) === 'string') {
                return $.trim(input).length < 1;
            } else {
                return false
            }
        }

        $scope.isValidDate = function (value) {
            if (isNullOrWhitespace(value))
                return true
            return moment(value, 'YYYY-MM-DD', true).isValid()
        }

        $scope.historyFromDate = moment().add(-10, 'days').format('YYYY-MM-DD')
        $scope.historyToDate = moment().format('YYYY-MM-DD')

        $scope.filterHistory = function (evt) {
            if (evt) {
                evt.preventDefault()
            }
            if ($scope.filterform.$invalid) {
                return
            }
            $scope.isLoading = true

            client.findHistoricalDecisions($scope.historyFromDate, $scope.historyToDate).then(result => {
                $scope.isLoading = false
                $scope.historyBatches = result.batches
            })
        }

        $scope.loadBatchDetails = function (b, evt) {
            if (evt) {
                evt.preventDefault()
            }
            if (b.details) {
                b.details = null
                return
            }
            $scope.isLoading = true

            client.getBatchDetails(b.Id).then(result => {
                $scope.isLoading = false
                b.details = { batchItems: result.batchItems }
            })
        }
        window.scope = $scope
    }
}

app.controller('ctr', CreditApplicationsToApproveCtr);

module CreditApplicationsToApproveNs {
}