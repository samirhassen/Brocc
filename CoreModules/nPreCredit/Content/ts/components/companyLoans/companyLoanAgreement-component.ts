namespace CompanyLoanAgreementComponentNs {
    
    export class CompanyLoanAgreementController extends NTechComponents.NTechComponentControllerBase {
        initialData: CompanyLoanApplicationComponentNs.StepInitialData;
        m: Model;
        fileUploadHelper: NtechAngularFileUpload.FileUploadHelper

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService', '$scope']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService,
            private $scope: ng.IScope) {
            super(ntechComponentService, $http, $q);

        }

        componentName(): string {
            return 'companyLoanAgreement'
        }
        
        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }
            this.referesh()
        }

        cancel(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            let ai = this.initialData.applicationInfo

            this.initialData.companyLoanApiClient.removeSignedAgreement(ai.ApplicationNr).then(x => {
                this.signalReloadRequired()
            })
        }

        isAttachAllowed() {
            if (!this.m) {
                return false
            }
            //Since everyone signs the same copy we can use attach as long as there are any pending signatures.
            //This will then replace all previous signatures also.
            return NTechLinq.any(this.m.session.Static.Signers, s => this.isResendAllowed(s))
        }

        isResendAllowed(signer: NTechCompanyLoanPreCreditApi.AgreementSignatureSessionSignerModel) {
            return this.m && this.m.isEditAllowed === true && this.m.haveAllSigned() === false && !this.m.getSignedDate(signer)
        }

        attachDocument(event?: Event) {
            if (event) {
                event.preventDefault()
            }

            let input = document.createElement('input');
            input.type = 'file';
            let form = document.createElement('form');
            form.appendChild(input);
            let ul = new NtechAngularFileUpload.FileUploadHelper(input,
                form,
                this.$scope, this.$q);
            ul.addFileAttachedListener(filesNames => {
                if (filesNames.length > 1) {
                    toastr.warning('More than one file attached')
                } else if (filesNames.length == 1) {
                    ul.loadSingleAttachedFileAsDataUrl().then((result : NtechAngularFileUpload.FileUploadHelperResult) => {
                        this.initialData.companyLoanApiClient.attachSignedAgreement(this.initialData.applicationInfo.ApplicationNr, result.dataUrl, result.filename).then(x => {
                            this.signalReloadRequired()
                        })
                    })
                }
            });
            ul.showFilePicker()
        }

        private referesh(resendFor?: NTechCompanyLoanPreCreditApi.AgreementSignatureSessionSignerModel) {
            let ai = this.initialData.applicationInfo
            let step = this.initialData.step

            if (step.areAllStepBeforeThisAccepted(ai)) {
                let isEditAllowed = ai.IsActive && !ai.IsPartiallyApproved && !ai.IsFinalDecisionMade && step.isStatusInitial(ai)
                let options: NTechCompanyLoanPreCreditApi.GetOrCreateAgreementSignatureSessionOptions = resendFor ? {
                    RefreshSignatureSessionIfNeeded: true,
                    ResendLinkOnExistingCustomerIds: [resendFor.CustomerId]
                } : {}
                let isCancelAllowed = step.isStatusAccepted(ai) && step.areAllStepsAfterInitial(ai) && ai.IsActive
                this.initialData.companyLoanApiClient.getOrCreateAgreementSignatureSession(ai.ApplicationNr, options).then((x : NTechCompanyLoanPreCreditApi.GetOrCreateAgreementSignatureSessionResponse) => {
                    this.m = new Model(isEditAllowed, x.Session, false, isCancelAllowed)
                })
            } else {
                this.m = new Model(false, null, true, false)
            }
        }

        resend(s: NTechCompanyLoanPreCreditApi.AgreementSignatureSessionSignerModel, evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.referesh(s)
        }
    }

    export class CompanyLoanAgreementComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanAgreementController;
            this.templateUrl = 'company-loan-agreement.html';
        }
    }
    
    export class Model {
        constructor(public isEditAllowed: boolean,
            public session: NTechCompanyLoanPreCreditApi.AgreementSignatureSessionModel,
            public isWaitingForPreviousStep: boolean,
            public isCancelAllowed: boolean)
        {
            
        }

        getSentButNotSignedDate(s: NTechCompanyLoanPreCreditApi.AgreementSignatureSessionSignerModel) {
            if (this.getSignedDate(s)) {
                return null
            }
            if (!this.session) {
                return null
            }
            let d = this.session.State.LatestSentDateByCustomerId
            if (!d) {
                return null
            }
            return d[s.CustomerId]
        }

        getSignedDate(s: NTechCompanyLoanPreCreditApi.AgreementSignatureSessionSignerModel) {
            if (!this.session) {
                return null
            }
            let d = this.session.State.SignedDateByCustomerId
            if (!d) {
                return null
            }
            return d[s.CustomerId]
        }

        haveAllSigned() {
            if (!this.session) {
                return null
            }
            let d = this.session.State.SignedDateByCustomerId
            if (!d) {
                return false
            }
            for (let s of this.session.Static.Signers) {
                if (!d[s.CustomerId]) {
                    return false
                }
            }
            return true
        }
    }
}

angular.module('ntech.components').component('companyLoanAgreement', new CompanyLoanAgreementComponentNs.CompanyLoanAgreementComponent())