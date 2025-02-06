class ChangeExternalAccountManagementCtrl {
    static $inject = ['$scope']
    constructor(
        private $scope: ng.IScope
    ) {
        window.scope = this;
        this.pendingChanges = initialData.pendingChanges
        this.backUrl = initialData.backUrl
    }

    pendingChanges: Array<ChangeExternalAccountManagementNs.IPendingChange>
    isLoading: boolean;
    backUrl: string;
}
var app = angular.module('app', ['ntech.forms'])
app.controller('changeExternalAccountManagementCtrl', ChangeExternalAccountManagementCtrl)

module ChangeExternalAccountManagementNs {
    export interface IPendingChange {
        SavingsAccountNr : string,
        InitiatedByUserDisplayName : string,
        InitiatedTransactionDate: Date,
        WasInitiatedByCurrentUser
    }
}