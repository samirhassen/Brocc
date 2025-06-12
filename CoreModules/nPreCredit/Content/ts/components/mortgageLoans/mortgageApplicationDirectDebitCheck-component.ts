namespace MortgageApplicationDirectDebitCheckComponentNs {
    
    export class MortgageApplicationDirectDebitCheckController extends NTechComponents.NTechComponentControllerBase {
        initialData: MortgageLoanApplicationDynamicComponentNs.StepInitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', '$timeout']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private $timeout: ng.ITimeoutService) {
            super(ntechComponentService, $http, $q);
        }
        
        componentName(): string {
            return 'mortgageApplicationDirectDebitCheck'
        }
        
        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            this.apiClient.fetchMortgageLoanDirectDebitCheckStatus(this.initialData.applicationInfo.ApplicationNr).then(result => {
                this.m = this.createLocalData(result, this.initialData.applicationInfo);
            })
        }
        
        getApplicant = (result: NTechPreCreditApi.MortgageLoanApplicationDirectDebitStatusModel, applicantNr: number) => {
            if (applicantNr == 1) {
                return result.Applicant1
            } else if (applicantNr == 2) {
                return result.Applicant2
            } else {
                return null
            }
        }

        getPersonModel(result: NTechPreCreditApi.MortgageLoanApplicationDirectDebitStatusModel, applicantNr: number): PersonNameAndDateModel {
            let a = this.getApplicant(result, applicantNr)
            return {
                ApplicantNr: applicantNr,
                BirthDate: a.BirthDate,
                FirstName: a.FirstName
            }
        }

        createLocalData(result: NTechPreCreditApi.MortgageLoanApplicationDirectDebitStatusModel, applicationInfo: NTechPreCreditApi.ApplicationInfoModel): Model {
            var m = new Model();
            m.latestResult = result
            
            if (!this.initialData.workflowModel.areAllStepBeforeThisAccepted(this.initialData.applicationInfo)) {
                return m                
            }

            m.ro = new ReadonlyDataModel()
            if (result.AdditionalQuestionAccountOwnerApplicantNr) {
                let aqa = this.getApplicant(result, result.AdditionalQuestionAccountOwnerApplicantNr)
                m.ro.AdditionalQuestionAccountOwner = this.getPersonModel(result, result.AdditionalQuestionAccountOwnerApplicantNr)
                m.ro.AdditionalQuestionPaymentNumber = aqa.StandardPaymentNumber
            }

            if (result.SignedDirectDebitConsentDocumentDownloadUrl) {
                m.ro.SignedDirectDebitConsentDocumentDownloadUrl = result.SignedDirectDebitConsentDocumentDownloadUrl
            }
            
            if (result.AdditionalQuestionsBankAccountNr) {
                m.ro.AdditionalQuestionsBankAccountNr = result.AdditionalQuestionsBankAccountNr
                m.ro.AdditionalQuestionsBankName = result.AdditionalQuestionsBankName
            }

            m.dd = new DirectDebitSavedStateModel()
            if (result.DirectDebitCheckStatus) {
                m.dd.DirectDebitCheckStatus = result.DirectDebitCheckStatus
                m.dd.DirectDebitCheckStatusDate = result.DirectDebitCheckStatusDate
            }

            if (result.DirectDebitCheckAccountOwnerApplicantNr) {
                let dqa = this.getApplicant(result, result.DirectDebitCheckAccountOwnerApplicantNr)
                m.dd.AccountOwner = this.getPersonModel(result, result.DirectDebitCheckAccountOwnerApplicantNr)
                m.dd.PaymentNumber = dqa.StandardPaymentNumber                
            }
            if (result.DirectDebitCheckBankAccountNr) {
                m.dd.BankAccountNr = result.DirectDebitCheckBankAccountNr
                m.dd.BankName = result.DirectDebitCheckBankName
            }

            m.dd.IsEditAllowed = result.IsEditAllowed

            m.allOwners = []
            if (result.Applicant1) {
                m.allOwners.push(this.getPersonModel(result, 1))
            }
            if (result.Applicant2) {
                m.allOwners.push(this.getPersonModel(result, 2))
            }

            return m
        }

        beginEdit(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            this.m.edit = {
                AccountOwnerApplicantNr: this.m.dd.AccountOwner ?
                    this.m.dd.AccountOwner.ApplicantNr.toString() :
                    (this.m.ro.AdditionalQuestionAccountOwner ? this.m.ro.AdditionalQuestionAccountOwner.ApplicantNr.toString() : null),
                Status: this.m.dd.DirectDebitCheckStatus ? this.m.dd.DirectDebitCheckStatus : 'Initial',
                BankAccountNr: this.m.dd.BankAccountNr ? this.m.dd.BankAccountNr : this.m.ro.AdditionalQuestionsBankAccountNr,
                BankAccountValidationResult: null,
                WasAccountOwnerApplicantNrRecentlyChanged: false
            }
        }

        cancelEdit(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.m.edit = null
        }

        commitEdit(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            this.apiClient.updateMortgageLoanDirectDebitCheckStatus(this.initialData.applicationInfo.ApplicationNr, this.m.edit.Status, this.m.edit.BankAccountNr, parseInt(this.m.edit.AccountOwnerApplicantNr)).then(() => {
                this.signalReloadRequired()
            })
        }

        onBankAccountEdited() {
            if (!this.m.edit) {
                return
            }
            this.m.edit.BankAccountValidationResult = null
            if (!this.m.edit.BankAccountNr) {
                return
            }
            this.apiClient.validateBankAccountNr(this.m.edit.BankAccountNr).then(result => {
                this.m.edit.BankAccountValidationResult = result
            })
        }

        onAccountOwnerApplicantNrEdited() {
            if (this.m.edit) {
                this.m.edit.WasAccountOwnerApplicantNrRecentlyChanged = true
                this.$timeout(() => {
                    if (this.m.edit) {
                        this.m.edit.WasAccountOwnerApplicantNrRecentlyChanged = false
                    }                    
                }, 400)
            }
        }
    }

    export class MortgageApplicationDirectDebitCheckComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationDirectDebitCheckController;
            this.templateUrl = 'mortgage-application-direct-debit-check.html';
        }
    }

    export class Model {
        ro: ReadonlyDataModel
        dd: DirectDebitSavedStateModel
        allOwners: PersonNameAndDateModel[]
        latestResult: NTechPreCreditApi.MortgageLoanApplicationDirectDebitStatusModel
        edit?: EditModel
    }

    export class ReadonlyDataModel {
        AdditionalQuestionAccountOwner: PersonNameAndDateModel
        AdditionalQuestionsBankAccountNr: string
        AdditionalQuestionsBankName: string
        AdditionalQuestionPaymentNumber: string
        SignedDirectDebitConsentDocumentDownloadUrl: string
    }

    export class DirectDebitSavedStateModel {
        DirectDebitCheckStatus: string
        DirectDebitCheckStatusDate: Date
        IsEditAllowed: boolean
        AccountOwner: PersonNameAndDateModel
        PaymentNumber: string
        BankAccountNr: string
        BankName: string
    }

    export class PersonNameAndDateModel {
        ApplicantNr: number
        FirstName: string
        BirthDate: Date
    }

    export class EditModel {
        AccountOwnerApplicantNr: string
        BankAccountNr: string
        Status: string
        BankAccountValidationResult: NTechPreCreditApi.ValidateBankAccountNrResult
        WasAccountOwnerApplicantNrRecentlyChanged : boolean
    }
}

angular.module('ntech.components').component('mortgageApplicationDirectDebitCheck', new MortgageApplicationDirectDebitCheckComponentNs.MortgageApplicationDirectDebitCheckComponent())