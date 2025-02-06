namespace UnsecuredApplicationCreditCheckStatusComponentNs {

    export class UnsecuredApplicationCreditCheckStatusController extends NTechComponents.NTechComponentControllerBase {
        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q); 
        }

        initialData: InitialData
        m: Model

        componentName(): string {
            return 'unsecuredApplicationCreditCheckStatus'
        }

        onChanges() {
            this.m = null;
            if (!this.initialData) {
                return
            }
            this.apiClient.fetchUnsecuredLoanCreditCheckStatus(this.initialData.applicationInfo.ApplicationNr, null, null, false, true).then(x => {
                let r: { [index: string]: string } = {}
                for (let y of x.RejectionReasonDisplayNames) {
                    r[y.Name] = y.Value
                }
                this.m = {
                    acceptedCreditDecision: x.CurrentCreditDecision && x.CurrentCreditDecision.AcceptedDecisionModel ? JSON.parse(x.CurrentCreditDecision.AcceptedDecisionModel) : null,
                    rejectedCreditDecision: x.CurrentCreditDecision && x.CurrentCreditDecision.RejectedDecisionModel ? JSON.parse(x.CurrentCreditDecision.RejectedDecisionModel) : null,
                    NewCreditCheckUrl: x.NewCreditCheckUrl,
                    ViewCreditDecisionUrl: x.ViewCreditDecisionUrl,
                    RejectionReasonToDisplayNameMapping: r
                }
            })
        }

        headerClassFromStatus(status: string) {
            var isAccepted = status === 'Accepted'
            var isRejected = status === 'Rejected'

            return { 'text-success': isAccepted, 'text-danger': isRejected }
        }

        iconClassFromStatus(status: string) {
            var isAccepted = status === 'Accepted'
            var isRejected = status === 'Rejected'
            var isOther = !isAccepted && !isRejected
            return { 'glyphicon-ok': isAccepted, 'glyphicon-remove': isRejected, 'glyphicon-minus': isOther, 'glyphicon': true, 'text-success': isAccepted, 'text-danger': isRejected }
        }

        getRejectionReasonDisplayName(reason) {
            if (this.m && this.m.RejectionReasonToDisplayNameMapping[reason]) {
                return this.m.RejectionReasonToDisplayNameMapping[reason]
            } else {
                return reason
            }
        }
    }

    export class UnsecuredApplicationCreditCheckStatusComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = UnsecuredApplicationCreditCheckStatusController;
            this.templateUrl = 'unsecured-application-credit-check-status.html';
        }
    }

    export class InitialData {
        applicationInfo: NTechPreCreditApi.ApplicationInfoModel
    }

    export class Model {
        acceptedCreditDecision: any
        rejectedCreditDecision: any
        ViewCreditDecisionUrl: string
        NewCreditCheckUrl: string
        RejectionReasonToDisplayNameMapping: {[index: string] : string}
    }
}

angular.module('ntech.components').component('unsecuredApplicationCreditCheckStatus', new UnsecuredApplicationCreditCheckStatusComponentNs.UnsecuredApplicationCreditCheckStatusComponent())