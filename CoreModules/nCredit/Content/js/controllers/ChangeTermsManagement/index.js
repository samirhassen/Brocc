var ChangeTermsManagementCtrl = /** @class */ (function () {
    function ChangeTermsManagementCtrl($scope, $http, $q, $timeout) {
        this.$http = $http;
        this.$q = $q;
        this.$timeout = $timeout;
        window.scope = this;
        this.today = initialData.today;
        this.creditsWithPendingTermChanges = initialData.creditsWithPendingTermChanges;
    }
    ChangeTermsManagementCtrl.prototype.onBack = function (evt) {
        if (evt) {
            evt.preventDefault();
        }
        NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, new NTechCreditApi.ApiClient(toastr.error, this.$http, this.$q), this.$q);
    };
    ChangeTermsManagementCtrl.$inject = ['$scope', '$http', '$q'];
    return ChangeTermsManagementCtrl;
}());
var app = angular.module('app', ['ntech.forms', 'ntech.components']);
app.controller('changeTermsManagementCtrl', ChangeTermsManagementCtrl);
