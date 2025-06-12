namespace MortgageApplicationCustomerCheckComponentNs {

    export class MortgageApplicationCustomerCheckController extends NTechComponents.NTechComponentControllerBase {
        initialData: MortgageLoanApplicationDynamicComponentNs.StepInitialData;
        m: Model;

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'mortgageApplicationCustomerCheck'
        }

        toModel(dm: NTechPreCreditApi.MortgageLoanApplicationCustomerCheckStatusModel, applicationInfo: NTechPreCreditApi.ApplicationInfoModel): Model {
            let d = dm.Status
            dm.CustomerLocalDecisionUrlPattern
            return {
                isApproveAllowed: d.IsApproveAllowed,
                isKycListScreenAllowed: applicationInfo.IsActive && !applicationInfo.IsFinalDecisionMade && !applicationInfo.IsPartiallyApproved,
                proofOfIdentityIssues: _.filter(d.Issues, x => x.Code === 'ProofOfIdentityMissing'),
                pepQuestionNotAnsweredIssues: _.filter(d.Issues, x => x.Code === 'PepQuestionNotAnswered'),
                kycListScreenNotDoneIssues: _.filter(d.Issues, x => x.Code === 'KycListScreenNotDone'),
                kycCustomerDecisionMissingIssues: _.filter(d.Issues, x => x.Code === 'KycCustomerDecisionMissing'),
                customerLocalDecisionUrlPattern: dm.CustomerLocalDecisionUrlPattern
            }
        }

        isApproved() {
            return this.initialData && this.initialData.workflowModel.isStatusAccepted(this.initialData.applicationInfo)
        }

        onChanges() {
            this.m = null;

            if (!this.initialData) {
                return
            }

            if (!this.isApproved()) {
                this.apiClient.fetchMortageLoanApplicationCustomerCheckStatus(this.initialData.applicationInfo.ApplicationNr, this.initialData.urlToHereFromOtherModule, this.initialData.applicationInfo.IsActive).then(result => {
                    this.m = this.toModel(result, this.initialData.applicationInfo);
                })
            }
        }

        tryKycScreen(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            if (!this.m || this.m.kycListScreenNotDoneIssues.length == 0) {
                return;
            }

            var applicantNrs = _.map(this.m.kycListScreenNotDoneIssues, x => x.ApplicantNr)

            this.apiClient.doCustomerCheckKycScreen(this.initialData.applicationInfo.ApplicationNr, applicantNrs).then(result => {
                var anySuccess = _.any(result, x => x.IsSuccess)
                var anyFailure = _.any(result, x => !x.IsSuccess)
                if (anyFailure) {
                    let warningMessage = 'All applicants could not be screened: '
                    for (let f of _.filter(result, x => !x.IsSuccess)) {                        
                        warningMessage = warningMessage + ` Applicant ${f.ApplicantNr} - ${f.FailureCode}.`;
                    }
                    toastr.warning(warningMessage)
                } else {
                    toastr.info('Ok')
                }
                if (anySuccess) {
                    this.signalReloadRequired();
                }
            });
        }

        approve(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.m || !this.m.isApproveAllowed) {
                return;
            }
            this.apiClient.approveMortageLoanApplicationCustomerCheck(this.initialData.applicationInfo.ApplicationNr).then(() => {
                this.signalReloadRequired()
            })
        }

        getCustomerLocalDecisionUrl(i: NTechPreCreditApi.MortgageLoanApplicationCustomerCheckStatusModelIssue, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.m || !i.CustomerId) {
                return null
            }
            return this.m.customerLocalDecisionUrlPattern.replace('{{customerId}}', i.CustomerId.toString())
        }
    }

    export class ApplicationCustomerCheckComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {            
            this.bindings = {
                initialData: '<',
            };
            this.controller = MortgageApplicationCustomerCheckController;
            this.templateUrl = 'mortgage-application-customer-check.html';
        }
    }

    export class Model {
        isApproveAllowed: boolean
        isKycListScreenAllowed: boolean
        proofOfIdentityIssues: NTechPreCreditApi.MortgageLoanApplicationCustomerCheckStatusModelIssue[]
        pepQuestionNotAnsweredIssues: NTechPreCreditApi.MortgageLoanApplicationCustomerCheckStatusModelIssue[]
        kycListScreenNotDoneIssues: NTechPreCreditApi.MortgageLoanApplicationCustomerCheckStatusModelIssue[]
        kycCustomerDecisionMissingIssues: NTechPreCreditApi.MortgageLoanApplicationCustomerCheckStatusModelIssue[]
        customerLocalDecisionUrlPattern: string
    }
}

angular.module('ntech.components').component('mortgageApplicationCustomerCheck', new MortgageApplicationCustomerCheckComponentNs.ApplicationCustomerCheckComponent())