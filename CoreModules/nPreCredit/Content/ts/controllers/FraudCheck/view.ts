var app = angular.module('app', ['ntech.forms', 'ntech.components']);

class ViewFraudCheckCtr {
    static $inject = ['$scope', '$http', '$q', '$timeout']
    constructor(
        $scope: any, //ng.IScope
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $timeout: ng.ITimeoutService
    ) {
        window.scope = $scope; //for console debugging

        let apiClient = new NTechPreCreditApi.ApiClient(err => {
            toastr.error(err)
        }, $http, $q);

        $scope.onBack = (evt?: Event) => {
            if (evt) {
                evt.preventDefault()
            }
            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, apiClient, this.$q, { applicationNr: initialData.applicationNr }, NavigationTargetHelper.NavigationTargetCode.UnsecuredLoanApplication)
        }

        $scope.headerClassFromStatus = function (status) {
            var isAccepted = status === 'Approved'
            var isRejected = status === 'Rejected'
            var isOther = !isAccepted && !isRejected
            return { 'text-success': isAccepted, 'text-danger': isRejected }
        }

        $scope.iconClassFromStatus = function (status) {
            var isAccepted = status === 'Approved'
            var isRejected = status === 'Rejected'
            var isOther = !isAccepted && !isRejected
            return { 'glyphicon-ok': isAccepted, 'glyphicon-remove': isRejected, 'glyphicon-minus': isOther, 'glyphicon': true, 'text-success': isAccepted, 'text-danger': isRejected }
        }

        $scope.app = initialData;
        $scope.currentLanguage = function () {
            return "sv";
        }

        fraudCheckSharedData.translateApp($scope.app)

        $scope.pickItemByKey = function (list, keyName, keyValue) {
            var item = null
            angular.forEach(list, function (v) {
                if (v[keyName] === keyValue) {
                    item = v
                }
            })
            return item
        }

        $scope.unlock = function (evt, sensitiveItem) {
            if (evt) { evt.preventDefault() }
            $http({
                method: 'POST',
                url: $scope.app.unlockSensitiveItemUrl,
                data: { item: sensitiveItem }
            }).then(function successCallback(response) {
                sensitiveItem.Locked = false;
                sensitiveItem.Value = response.data;
            }, function errorCallback(response) {
                location.reload()
            })
        }
    }
}

app.controller('ctr', ViewFraudCheckCtr);

module ViewFraudCheckNs {
}