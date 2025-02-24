var ChangeExternalAccountManagementCtrl = /** @class */ (function () {
    function ChangeExternalAccountManagementCtrl($scope) {
        this.$scope = $scope;
        window.scope = this;
        this.pendingChanges = initialData.pendingChanges;
        this.backUrl = initialData.backUrl;
    }
    ChangeExternalAccountManagementCtrl.$inject = ['$scope'];
    return ChangeExternalAccountManagementCtrl;
}());
var app = angular.module('app', ['ntech.forms']);
app.controller('changeExternalAccountManagementCtrl', ChangeExternalAccountManagementCtrl);
