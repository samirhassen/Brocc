namespace CompanyLoanAuditAgreementComponentNs {
    export class CompanyLoanAuditAgreementController extends NTechComponents.NTechComponentControllerBase {
        initialData: CompanyLoanApplicationComponentNs.StepInitialData;
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'companyLoanAuditAgreement'
        }

        getUserDisplayNameByUserId(userId: number) {
            if (this.initialData) {
                let d = this.initialData.userDisplayNameByUserId[userId]
                if (d) {
                    return d
                }
            }

            return `User ${userId}`
        }

        cancel(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            let ai = this.initialData.applicationInfo
            let step = this.initialData.step

            this.initialData.apiClient.removeLockedAgreement(ai.ApplicationNr).then(x => {
                this.initialData.companyLoanApiClient.setApplicationWorkflowStatus(ai.ApplicationNr, step.stepName, 'Initial', 'Agreement unlocked').then(() => {
                    this.signalReloadRequired()
                })
            })
        }

        approve(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            let ai = this.initialData.applicationInfo
            let step = this.initialData.step

            this.initialData.companyLoanApiClient.createLockedAgreement(ai.ApplicationNr).then(x => {
                this.initialData.companyLoanApiClient.setApplicationWorkflowStatus(ai.ApplicationNr, step.stepName, 'Accepted', 'Agreement audited and locked').then(() => {
                    this.signalReloadRequired()
                })
            })
        }

        initLocked(a: NTechPreCreditApi.GetLockedAgreementResponse) {
            let ai = this.initialData.applicationInfo
            let s = this.initialData.step
            this.m = {
                preview: null,
                lockedAgreement: a && a.LockedAgreement ? {
                    agreementUrl: `/CreditManagement/ArchiveDocument?key=${a.LockedAgreement.UnsignedAgreementArchiveKey}`,
                    auditedByUserId: a.LockedAgreement.LockedByUserId,
                    auditedDate: a.LockedAgreement.LockedDate,
                    loanAmount: a.LockedAgreement.LoanAmount,
                    isCancelAllowed: s.isStatusAccepted(ai) && s.areAllStepsAfterInitial(ai) && ai.IsActive
                } : null
            }
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            let s = this.initialData.step
            let ai = this.initialData.applicationInfo

            if (!s.areAllStepBeforeThisAccepted(ai)) {
                this.m = {
                    lockedAgreement: null,
                    preview: null
                }
            } else if (s.isStatusAccepted(ai)) {
                this.apiClient.getLockedAgreement(ai.ApplicationNr).then(x => {
                    this.initLocked(x)
                })
            } else {
                this.initialData.companyLoanApiClient.fetchCurrentCreditDecision(ai.ApplicationNr).then(decision => {
                    this.initialData.companyLoanApiClient.checkHandlerLimits(this.initialData.currentUserId, decision.Decision.CompanyLoanOffer.LoanAmount).then(approvedHandlerLimits => {
                        this.m = {
                            preview: {
                                loanAmount: decision && decision.Decision && decision.Decision.CompanyLoanOffer ? decision.Decision.CompanyLoanOffer.LoanAmount : null,
                                agreementUrl: `/api/CompanyLoan/Create-Agreement-Pdf?ApplicationNr=${this.initialData.applicationInfo.ApplicationNr}`,
                                isApprovedAllowd: ai.IsActive && s.areAllStepBeforeThisAccepted(ai) && approvedHandlerLimits.Approved
                            },
                            lockedAgreement: null
                        }
                    })
                })
            }
        }
    }

    export class Model {
        preview: PreviewModel
        lockedAgreement: LockedAgreementModel
    }

    export class PreviewModel {
        loanAmount: number
        agreementUrl: string
        isApprovedAllowd: boolean
    }

    export class LockedAgreementModel {
        loanAmount: number
        agreementUrl: string
        auditedByUserId: number
        auditedDate: Date
        isCancelAllowed: boolean
    }

    export interface StepCustomDataModel {
        IsApproveAgreement: string
        IsAuditAgreement: string
    }

    export class CompanyLoanAuditAgreementComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanAuditAgreementController;
            this.templateUrl = 'company-loan-audit-agreement.html'
        }
    }
}

angular.module('ntech.components').component('companyLoanAuditAgreement', new CompanyLoanAuditAgreementComponentNs.CompanyLoanAuditAgreementComponent())