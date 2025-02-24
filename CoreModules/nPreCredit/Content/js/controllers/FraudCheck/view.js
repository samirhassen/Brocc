var app = angular.module('app', ['ntech.forms', 'ntech.components']);
var ViewFraudCheckCtr = /** @class */ (function () {
    function ViewFraudCheckCtr($scope, //ng.IScope
    $http, $q, $timeout) {
        var _this = this;
        this.$http = $http;
        this.$q = $q;
        this.$timeout = $timeout;
        window.scope = $scope; //for console debugging
        var apiClient = new NTechPreCreditApi.ApiClient(function (err) {
            toastr.error(err);
        }, $http, $q);
        $scope.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, apiClient, _this.$q, { applicationNr: initialData.applicationNr }, NavigationTargetHelper.NavigationTargetCode.UnsecuredLoanApplication);
        };
        $scope.headerClassFromStatus = function (status) {
            var isAccepted = status === 'Approved';
            var isRejected = status === 'Rejected';
            var isOther = !isAccepted && !isRejected;
            return { 'text-success': isAccepted, 'text-danger': isRejected };
        };
        $scope.iconClassFromStatus = function (status) {
            var isAccepted = status === 'Approved';
            var isRejected = status === 'Rejected';
            var isOther = !isAccepted && !isRejected;
            return { 'glyphicon-ok': isAccepted, 'glyphicon-remove': isRejected, 'glyphicon-minus': isOther, 'glyphicon': true, 'text-success': isAccepted, 'text-danger': isRejected };
        };
        $scope.app = initialData;
        $scope.currentLanguage = function () {
            return "sv";
        };
        fraudCheckSharedData.translateApp($scope.app);
        $scope.pickItemByKey = function (list, keyName, keyValue) {
            var item = null;
            angular.forEach(list, function (v) {
                if (v[keyName] === keyValue) {
                    item = v;
                }
            });
            return item;
        };
        $scope.unlock = function (evt, sensitiveItem) {
            if (evt) {
                evt.preventDefault();
            }
            $http({
                method: 'POST',
                url: $scope.app.unlockSensitiveItemUrl,
                data: { item: sensitiveItem }
            }).then(function successCallback(response) {
                sensitiveItem.Locked = false;
                sensitiveItem.Value = response.data;
            }, function errorCallback(response) {
                location.reload();
            });
        };
    }
    ViewFraudCheckCtr.$inject = ['$scope', '$http', '$q', '$timeout'];
    return ViewFraudCheckCtr;
}());
app.controller('ctr', ViewFraudCheckCtr);
