namespace CompanyLoanApproveAgreementComponentNs {
    
    export class CompanyLoanApproveAgreementController extends NTechComponents.NTechComponentControllerBase {
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
            return 'companyLoanApproveAgreement'
        }

        approve(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.approveInternal(false)
        }

        cancel(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.initialData.companyLoanApiClient.cancelAgreementSignatureSession(this.initialData.applicationInfo.ApplicationNr).then(x => {
                let msg = 'Agreement approval reversed'
                if (x.WasCancelled) {
                    msg += ' and pending signature session cancelled'
                }
                this.initialData.companyLoanApiClient.setApplicationWorkflowStatus(this.initialData.applicationInfo.ApplicationNr, this.initialData.step.stepName, 'Initial', msg).then(() => {
                    this.signalReloadRequired()
                })
            })
        }

        private approveInternal(requestOverrideDuality: boolean) {
            this.apiClient.approveLockedAgreement(this.initialData.applicationInfo.ApplicationNr, requestOverrideDuality).then(x => {
                if (x.WasApproved) {
                    this.initialData.companyLoanApiClient.setApplicationWorkflowStatus(this.initialData.applicationInfo.ApplicationNr, this.initialData.step.stepName, 'Accepted', 'Agreement approved').then(() => {
                        this.initialData.companyLoanApiClient.getOrCreateAgreementSignatureSession(this.initialData.applicationInfo.ApplicationNr).then(x => {
                            this.signalReloadRequired()
                        })
                    })
                } else {
                    toastr.warning('Agreement could not be approved')
                }
            })
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

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            let s = this.initialData.step
            let ai = this.initialData.applicationInfo

            if (!s.areAllStepBeforeThisAccepted(ai)) {
                this.m = {
                    approved: null,
                    pendingApproval: null
                }
            } else if (s.isStatusAccepted(ai)) {
                this.apiClient.getLockedAgreement(ai.ApplicationNr).then(x => {
                    let ai = this.initialData.applicationInfo
                    let s = this.initialData.step
                    this.m = {
                        approved: x && x.LockedAgreement ? {
                            agreementUrl: `/CreditManagement/ArchiveDocument?key=${x.LockedAgreement.UnsignedAgreementArchiveKey}`,
                            auditedByUserId: x.LockedAgreement.LockedByUserId,
                            auditedDate: x.LockedAgreement.LockedDate,
                            loanAmount: x.LockedAgreement.LoanAmount,
                            isCancelAllowed: ai.IsActive && s.isStatusAccepted(ai) && s.areAllStepsAfterInitial(ai),
                            approvedByUserId: x.LockedAgreement.ApprovedByUserId,
                            approvedDate: x.LockedAgreement.ApprovedDate
                        } : null,
                        pendingApproval: null
                    }

                    if (this.initialData.isTest) {
                        let tf = this.initialData.testFunctions
                        tf.addFunctionCall(tf.generateUniqueScopeName(), 'Force approve', () => {
                            this.approveInternal(true)
                        })
                    }
                })
            } else {
                this.apiClient.getLockedAgreement(ai.ApplicationNr).then(x => {
                    this.initialData.companyLoanApiClient.checkHandlerLimits(this.initialData.currentUserId, x.LockedAgreement.LoanAmount).then(approvedHandlerLimits => {
                    let ai = this.initialData.applicationInfo
                    let s = this.initialData.step
                    let isApprovePossible = ai.IsActive && s.areAllStepBeforeThisAccepted(ai)
                    this.m = {
                        pendingApproval: x && x.LockedAgreement ? {
                            agreementUrl: `/CreditManagement/ArchiveDocument?key=${x.LockedAgreement.UnsignedAgreementArchiveKey}`,
                            auditedByUserId: x.LockedAgreement.LockedByUserId,
                            auditedDate: x.LockedAgreement.LockedDate,
                            loanAmount: x.LockedAgreement.LoanAmount,
                            isApprovePossible: isApprovePossible,
                            isApproveAllowed: isApprovePossible && x.LockedAgreement.LockedByUserId !== this.initialData.currentUserId,
                            isLoanAmountToLimitApproved: isApprovePossible && x.LockedAgreement.LockedByUserId !== this.initialData.currentUserId && approvedHandlerLimits.Approved,
                            showWaitingForApproval: isApprovePossible && x.LockedAgreement.LockedByUserId == this.initialData.currentUserId, 
                        } : null,
                        approved: null
                    }

                    if (this.initialData.isTest) {
                        let tf = this.initialData.testFunctions
                        tf.addFunctionCall(tf.generateUniqueScopeName(), 'Force approve', () => {
                            this.approveInternal(true)
                        })
                        }
                    })
                })
            }
        }
    }

    export class Model {
        pendingApproval: PendingApprovalModel
        approved: ApprovedModel
    }

    export class PendingApprovalModel {
        loanAmount: number
        agreementUrl: string
        auditedByUserId: number
        auditedDate: Date
        isApprovePossible: boolean //It can be approved
        isApproveAllowed: boolean //But maybe not by the current user
        isLoanAmountToLimitApproved: boolean
        showWaitingForApproval: boolean
    }

    export class ApprovedModel {
        loanAmount: number
        agreementUrl: string
        auditedByUserId: number
        auditedDate: Date
        approvedByUserId: number
        approvedDate: Date
        isCancelAllowed: boolean
    }

    export class CompanyLoanApproveAgreementComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanApproveAgreementController;
            this.templateUrl = 'company-loan-approve-agreement.html'
        }
    }
}

angular.module('ntech.components').component('companyLoanApproveAgreement', new CompanyLoanApproveAgreementComponentNs.CompanyLoanApproveAgreementComponent())