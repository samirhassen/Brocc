namespace CompanyLoanFraudCheckComponentNs {
    
    export class CompanyLoanFraudCheckController extends NTechComponents.NTechComponentControllerBase {
        initialData: CompanyLoanApplicationComponentNs.StepInitialData;
        m: Model;

        backUrl() {
            if (this.initialData) {
                return this.initialData.backUrl
            } else {
                return null;
            }                        
        }       

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService', '$translate']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService,
            private $translate: any ) {
            super(ntechComponentService, $http, $q);

        }
        
        componentName(): string {
            return 'companyLoanFraudCheck'
        }

        isEditAllowed() {
            if (!this.initialData || !this.initialData.applicationInfo) {
                return false
            }
            let ai = this.initialData.applicationInfo
            return ai.IsActive && !ai.IsPartiallyApproved && !ai.IsFinalDecisionMade && this.initialData.step.areAllStepBeforeThisAccepted(ai)
        }

        isApproveAccountNrAllowed() {
            if (!this.initialData || !this.initialData.applicationInfo) {
                return false
            }
            let ai = this.initialData.applicationInfo
            return this.m
                && !this.m.isMissingValidBankAccountNr
                && this.initialData.step.isStatusInitial(ai)
                && this.initialData.applicationInfo.ListNames.indexOf('CompanyLoanBankAccountNrCheck_Accepted') < 0
        }

        isAccountNrApproved() {
            if (!this.initialData || !this.initialData.applicationInfo) {
                return false
            }
            let ai = this.initialData.applicationInfo
            return ai.ListNames.indexOf('CompanyLoanBankAccountNrCheck_Accepted') >= 0            
        }

        approveBankAccountNr(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            let ai = this.initialData.applicationInfo
            if (!ai) {
                return
            }            
            this.apiClient.switchApplicationListStatus(ai.ApplicationNr, 'CompanyLoanBankAccountNrCheck', 'Accepted', 'Company bank account verified').then(() => {
                this.approveFraudCheckIfAllStepsAccepted()
            })
        }

        private approveFraudCheckIfAllStepsAccepted() {
            let applicationNr = this.initialData.applicationInfo.ApplicationNr
            const requiredListNames = ['CompanyLoanBankAccountNrCheck_Accepted']
            this.apiClient.fetchApplicationInfo(applicationNr).then(x => {
                let isOk = true
                for (let n of requiredListNames) {
                    if (x.ListNames.indexOf(n) < 0) {
                        isOk = false
                    }
                }
                if (isOk) {                    
                    this.initialData.companyLoanApiClient.setApplicationWorkflowStatus(applicationNr, this.initialData.step.stepName, 'Accepted', 'Fraud check accepted', this.initialData.step.stepName + 'Accepted', 'AcceptFraudCheck').then(() => {
                        this.signalReloadRequired()
                    })
                } else {
                    this.signalReloadRequired()
                }
            })
        }

        getBankGiroUrl() {
            if (this.m && !this.m.isMissingValidBankAccountNr && this.m.bankAccountNrType === 'BankGiroSe') {
                return `https://www.bankgirot.se/sok-bankgironummer/?bgnr=${this.m.bankAccountNrUsable}&orgnr=${this.m.companyOrgnr}#bgsearchform`
            } else {
                return 'https://www.bankgirot.se/sok-bankgironummer/#bgsearchform'
            }
        }

        getBankAccountEditUrl() {
            if (!this.initialData) {
                return null
            }
            let ai = this.initialData.applicationInfo
            let isEditAllowed = ai.IsActive && this.initialData.step.isStatusInitial(ai)
            return `/Ui/CompanyLoan/Application/EditItem?applicationNr=${ai.ApplicationNr}&dataSourceName=BankAccountTypeAndNr&itemName=BankAccountTypeAndNr&ro=${isEditAllowed ? 'False' : 'True'}&backUrl=${this.initialData.urlToHere}`
        }
        
        onChanges() {
            this.m = null

            if (!this.initialData || !this.initialData.applicationInfo) {
                return
            }

            if (!this.initialData.step.areAllStepBeforeThisAccepted(this.initialData.applicationInfo)) {
                this.m = {
                    isWaitingForPreviousSteps: true,
                    bankAccountNrReadable: null,
                    bankAccountNrType: null,
                    bankAccountNrTypeReadable: null,
                    bankAccountNrUsable: null,
                    isMissingValidBankAccountNr: null,
                    companyOrgnr: null
                }
                return
            }
           
            this.apiClient.fetchCreditApplicationItemSimple(
                this.initialData.applicationInfo.ApplicationNr,
                ['application.bankAccountNr', 'application.bankAccountNrType', 'application.companyOrgnr'], 'missing').then(x => {

                let bankAccountNr = x['application.bankAccountNr']
                let bankAccountNrType = x['application.bankAccountNrType']
                let companyOrgnr = x['application.companyOrgnr']                    

                if (bankAccountNr === 'missing' || bankAccountNrType === 'missing') {
                    this.m = {
                        bankAccountNrType: null,
                        bankAccountNrTypeReadable: null,
                        bankAccountNrReadable: null,
                        bankAccountNrUsable: null,
                        isMissingValidBankAccountNr: true,
                        companyOrgnr: null,
                        isWaitingForPreviousSteps: false
                    }
                } else {
                    this.apiClient.validateBankAccountNr(bankAccountNr, bankAccountNrType).then(y => {
                        if (y.isValid) {
                            let readable = y.validAccount.displayNr
                            if (y.validAccount.bankAccountNrType === 'BankAccountSe') {
                                readable = `Clearing: ${y.validAccount.clearingNr} Account: ${y.validAccount.accountNr} Bank: ${y.validAccount.bankName}`
                            } else if (y.validAccount.bankAccountNrType === 'IBANFi') {
                                readable = `Nr: ${y.validAccount.displayNr} Bic: ${y.validAccount.bic} Bank: ${y.validAccount.bankName}`
                            }
                            this.m = {
                                bankAccountNrType: y.validAccount.bankAccountNrType,
                                bankAccountNrTypeReadable: this.$translate.instant('enum.bankAccountNumberTypeCode.' + y.validAccount.bankAccountNrType),
                                bankAccountNrReadable: '(' + readable + ')',
                                bankAccountNrUsable: y.validAccount.normalizedNr,
                                isMissingValidBankAccountNr: false,
                                companyOrgnr: companyOrgnr,
                                isWaitingForPreviousSteps: false
                            }
                        } else {
                            this.m = {
                                bankAccountNrType: null,
                                bankAccountNrTypeReadable: this.$translate.instant('enum.bankAccountNumberTypeCode.' + bankAccountNrType),
                                bankAccountNrUsable: bankAccountNr,
                                bankAccountNrReadable: '(invalid!)',
                                isMissingValidBankAccountNr: true,
                                companyOrgnr: null,
                                isWaitingForPreviousSteps: false
                            }
                        }
                    })
                }
            })
        }
    }

    export class CompanyLoanFraudCheckComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanFraudCheckController;
            this.templateUrl = 'company-loan-fraud-check.html';
        }
    }
    
    export class Model {
        bankAccountNrTypeReadable: string
        bankAccountNrType: string
        bankAccountNrUsable: string
        bankAccountNrReadable: string
        isMissingValidBankAccountNr: boolean
        companyOrgnr: string
        isWaitingForPreviousSteps: boolean
    }
}

angular.module('ntech.components').component('companyLoanFraudCheck', new CompanyLoanFraudCheckComponentNs.CompanyLoanFraudCheckComponent())