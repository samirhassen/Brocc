namespace ListAndBuyCreditReportsForCustomerComponentNs {
    export class ListAndBuyCreditReportsForCustomerController extends NTechComponents.NTechComponentControllerBase {
        initialData: IInitialData
        m: Model
        creditReportDialogId: string

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);
            this.creditReportDialogId = modalDialogService.generateDialogId()
        }

        componentName(): string {
            return 'listAndBuyCreditReportsForCustomer'
        }

        onChanges() {
            this.reload()
        }

        private getCustomerId(inputCustomerId): ng.IPromise<number> {
            let customerIdDeferred = this.$q.defer<number>()
            if (!inputCustomerId) {
                let amdl = this.initialData.applicationNrAndApplicantNr;
                this.apiClient.fetchCustomerComponentInitialData(amdl.applicationNr, amdl.applicantNr, null).then(result => {
                    customerIdDeferred.resolve(result.customerId)
                })
            } else {
                customerIdDeferred.resolve(this.initialData.customerId)
            }

            return customerIdDeferred.promise
        }

        private reload() {
            this.m = null
            if (!this.initialData) {
                return
            }

            let customerIdPromise = this.getCustomerId(this.initialData.customerId)
            let providers = this.initialData.listProviders
            customerIdPromise.then(customerId => {
                this.apiClient.postUsingApiGateway<{}, CreditReport[]>("nCreditReport", "CreditReport/FindForProviders", { providers: providers, customerId: customerId }).then(result => {
                    this.m = (({
                        applicantNr: this.initialData.applicationNrAndApplicantNr.applicantNr,
                        customerId: customerId,
                        creditReports: result,
                        popupTabledValues: null
                    }) as Model)
                })
            })
        }
        
        buyNewSatReport(customerId) {
            this.apiClient.buyCreditReportForCustomerId(customerId, this.initialData.creditReportProviderName).then(() => {
                this.reload();
            });
        }

        showCreditReport(creditReportId) {
            this.modalDialogService.openDialog(this.creditReportDialogId, () => {
                this.apiClient.postUsingApiGateway<{}, TabledValue[]>("nCreditReport", "CreditReport/FetchTabledValues",
                    { creditReportId: creditReportId }).then(result => {
                        this.m.popupTabledValues = result
                })
            });
        }
    }

    class CreditReport {
        date: string;
    }

    class TabledValue {
        title: string;
        value: string;
    }

    export class Model {
        applicantNr: number;
        customerId: number;
        creditReports: CreditReport[];
        popupTabledValues: TabledValue[]
    }

    export interface IInitialData {
        customerId: number
        creditReportProviderName: string
        listProviders: string[]
        applicationNrAndApplicantNr: {
            applicationNr: string
            applicantNr: number
        } 
    }

    export class ListAndBuyCreditReportsForCustomerComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;
        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ListAndBuyCreditReportsForCustomerController;
            this.template = `
<div>
    <h3 class="text-center">Applicant {{$ctrl.m.applicantNr}}</h3>
    <div style="position: relative; height: 1em;">
        <div style="position: absolute; top: 0px; right: 0px; text-align: right;">
            <button class="n-direct-btn n-green-btn" ng-click="$ctrl.buyNewSatReport($ctrl.m.customerId)">Buy new <span class="glyphicon glyphicon-shopping-cart"></span></button>
        </div>
    </div>
    <table class="table">
        <thead>
            <tr>
                <th>Date</th>
                <th></th>
            </tr>
        </thead>
        <tbody>
            <tr ng-if="$ctrl.m.creditReports.length === 0">
                <td>-</td>
                <td></td>
            </tr>
            <tr ng-repeat="cr in $ctrl.m.creditReports">
                <td>{{cr.RequestDate | date:'dd.MM.yyyy hh:mm'}}</td>
                <td style="text-align: right;">
                    <button ng-disabled="!cr.CanFetchTabledValues" ng-click="$ctrl.showCreditReport(cr.CreditReportId)" class="n-direct-btn n-turquoise-btn">
                        <span ng-show="cr.CanFetchTabledValues">Show <span class="glyphicon glyphicon-resize-full"></span></span>
                        <span ng-show="!cr.CanFetchTabledValues">No preview</span>
                    </button>
                </td>
            </tr>
        </tbody>
    </table>
    <modal-dialog dialog-id="$ctrl.creditReportDialogId" dialog-title="'Credit Report'">
    <div ng-if="$ctrl.m.popupTabledValues">
        <table class="table">
            <thead>
                <tr>
                    <th>Key</th>
                    <th>Value</th>
                </tr>
            </thead>
            <tbody>
                <tr ng-repeat="row in $ctrl.m.popupTabledValues">
                    <td>{{row.title}}</td>
                    <td>{{row.value}}</td>
                </tr>
            </tbody>
        </table>
    </div>
    </modal-dialog>
</div>`

        }
    }
}

angular.module('ntech.components').component('listAndBuyCreditReportsForCustomer', new ListAndBuyCreditReportsForCustomerComponentNs.ListAndBuyCreditReportsForCustomerComponent())