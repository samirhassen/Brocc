class DocumentCheckStatusController  {
    public applicationnr: string
    public backtarget: string
    public status: NTechPreCreditApi.DocumentCheckStatusResult
    

    static $inject = ['$http', '$q']
    constructor(private $http: ng.IHttpService, private $q: ng.IQService) {

    }

    $onInit() {
        let client = new NTechPreCreditApi.ApiClient(errorMessage => {
            toastr.error(errorMessage)
        }, this.$http, this.$q)

        client.fetchDocumentCheckStatus(this.applicationnr).then(data => {
            this.status = data
        })
    }

    public getStatusText() {
        if (!this.status) {
            return 'Loading...' //This should never be visible to the user
        } else if (this.status.isRejected) {
            return 'Rejected' //This should never be visible to the user
        } else if (this.status.isAccepted) {
            return 'Documents ok'
        } else if (this.status.allApplicantsHaveSignedAgreement && !this.status.allApplicantsHaveAttachedDocuments) {
            return 'Waiting for documents'
        } else if (this.status.allApplicantsHaveAttachedDocuments && this.status.allApplicantsHaveSignedAgreement) {
            return 'Pending document check'
        } else {
            return 'Waiting for signed agreements'
        }
    }

    public getViewUrl() {
        if (this.backtarget) {
            return `/DocumentCheck/View?applicationNr=${this.applicationnr}&backTarget=${this.backtarget}`;
        } else {
            return `/DocumentCheck/View?applicationNr=${this.applicationnr}`;
        }
    }

    public getNewUrl() {
        if (this.backtarget) {
            return `/DocumentCheck/New?applicationNr=${this.applicationnr}&backTarget=${this.backtarget}`;
        } else {
            return `/DocumentCheck/New?applicationNr=${this.applicationnr}`;
        }
    }
}

class DocumentCheckStatusComponent implements ng.IComponentOptions {
    public bindings: any;
    public controller: any;
    public templateUrl: string;

    constructor() {
        //Bind to the controller
        this.bindings = {
            applicationnr: '<',
            backtarget: '<'
        };
        this.controller = DocumentCheckStatusController;
        this.templateUrl = 'documentcheckstatus.html';
    }
}

angular.module('ntech.components').component('documentcheckstatus', new DocumentCheckStatusComponent())