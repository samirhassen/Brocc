var DocumentCheckStatusController = /** @class */ (function () {
    function DocumentCheckStatusController($http, $q) {
        this.$http = $http;
        this.$q = $q;
    }
    DocumentCheckStatusController.prototype.$onInit = function () {
        var _this = this;
        var client = new NTechPreCreditApi.ApiClient(function (errorMessage) {
            toastr.error(errorMessage);
        }, this.$http, this.$q);
        client.fetchDocumentCheckStatus(this.applicationnr).then(function (data) {
            _this.status = data;
        });
    };
    DocumentCheckStatusController.prototype.getStatusText = function () {
        if (!this.status) {
            return 'Loading...'; //This should never be visible to the user
        }
        else if (this.status.isRejected) {
            return 'Rejected'; //This should never be visible to the user
        }
        else if (this.status.isAccepted) {
            return 'Documents ok';
        }
        else if (this.status.allApplicantsHaveSignedAgreement && !this.status.allApplicantsHaveAttachedDocuments) {
            return 'Waiting for documents';
        }
        else if (this.status.allApplicantsHaveAttachedDocuments && this.status.allApplicantsHaveSignedAgreement) {
            return 'Pending document check';
        }
        else {
            return 'Waiting for signed agreements';
        }
    };
    DocumentCheckStatusController.prototype.getViewUrl = function () {
        if (this.backtarget) {
            return "/DocumentCheck/View?applicationNr=".concat(this.applicationnr, "&backTarget=").concat(this.backtarget);
        }
        else {
            return "/DocumentCheck/View?applicationNr=".concat(this.applicationnr);
        }
    };
    DocumentCheckStatusController.prototype.getNewUrl = function () {
        if (this.backtarget) {
            return "/DocumentCheck/New?applicationNr=".concat(this.applicationnr, "&backTarget=").concat(this.backtarget);
        }
        else {
            return "/DocumentCheck/New?applicationNr=".concat(this.applicationnr);
        }
    };
    DocumentCheckStatusController.$inject = ['$http', '$q'];
    return DocumentCheckStatusController;
}());
var DocumentCheckStatusComponent = /** @class */ (function () {
    function DocumentCheckStatusComponent() {
        //Bind to the controller
        this.bindings = {
            applicationnr: '<',
            backtarget: '<'
        };
        this.controller = DocumentCheckStatusController;
        this.templateUrl = 'documentcheckstatus.html';
    }
    return DocumentCheckStatusComponent;
}());
angular.module('ntech.components').component('documentcheckstatus', new DocumentCheckStatusComponent());
