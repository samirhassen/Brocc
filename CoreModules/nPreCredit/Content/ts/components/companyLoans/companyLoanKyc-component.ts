namespace CompanyLoanKycComponentNs {
    const nonWorkflowStepNames: string[] = [
        'CompanyLoanLexPressScreen',
        'CompanyLoanPepSanctionScreen',
    ]
    export class CompanyLoanKycController extends NTechComponents.NTechComponentControllerBase {
        initialData: CompanyLoanApplicationComponentNs.StepInitialData;
        m: CompanyLoanKycComponentNs.Model;

        backUrl() {
            if (this.initialData) {
                return this.initialData.backUrl
            } else {
                return null;
            }
        }

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);
        }

        isEditAllowed() {
            if (!this.initialData || !this.initialData.applicationInfo) {
                return false
            }
            let ai = this.initialData.applicationInfo

            return ai.IsActive && !ai.IsPartiallyApproved && !ai.IsFinalDecisionMade
                && this.initialData.step.areAllStepBeforeThisAccepted(ai)
        }

        isStepComplete(stepName: string) {
            if (!this.initialData || !this.initialData.applicationInfo) {
                return false
            }
            let ai = this.initialData.applicationInfo
            return WorkflowHelper.isStepAccepted(stepName, ai)
        }

        onStatusChanged(evt?: Event, forceDoneStepName?: string) {
            if (evt) {
                evt.preventDefault()
            }

            let i = this.initialData
            let ai = i.applicationInfo

            let arePreviousStepsOk = i.step.areAllStepBeforeThisAccepted(ai)            
            let areLocalStepsOk = WorkflowHelper.areAllStepsAccepted(nonWorkflowStepNames, ai, forceDoneStepName)
            let areAllPepSanctionDone = NTechLinq.all(this.m.Customers, x => x.IsPepSanctionDone)

            if (arePreviousStepsOk && areLocalStepsOk && areAllPepSanctionDone) {
                i.companyLoanApiClient.setApplicationWorkflowStatus(ai.ApplicationNr,
                    i.step.stepName, 'Accepted', 'KYC approved', i.step.stepName + 'Accepted', 'AcceptCustomerCheck').then(() => {
                        this.signalReloadRequired()
                    })
            } else {
                this.signalReloadRequired()
            }
        }

        approveLexPress(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.switchApplicationListStatus(this.initialData.applicationInfo.ApplicationNr, 'CompanyLoanLexPressScreen', 'Accepted', 'Lex Press screening done').then(() => {
                this.onStatusChanged(null, 'CompanyLoanLexPressScreen')
            })
        }

        approvePepSanctionScreen(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.switchApplicationListStatus(this.initialData.applicationInfo.ApplicationNr, 'CompanyLoanPepSanctionScreen', 'Accepted', 'Pep/Sanction screening done').then(() => {
                this.onStatusChanged(null, 'CompanyLoanPepSanctionScreen')
            })
        }

        screenNow(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            this.apiClient.kycScreenBatchByApplicationNr(this.initialData.applicationInfo.ApplicationNr, moment(this.initialData.today).toDate()).then(success => {
                if (!success) {
                    toastr.error('Could not screen customers.');
                } else {
                    this.apiClient.switchApplicationListStatus(this.initialData.applicationInfo.ApplicationNr,
                        'CompanyLoanPepSanctionScreen', 'Accepted', 'Pep/Sanction screening done').then(() => {
                            this.onStatusChanged(null, 'CompanyLoanPepSanctionScreen')
                        })
                }
            })
        }

        isToggleCompanyLoanCollateralCheckStatusAllowed() {
            if (!this.initialData) {
                return false
            }
            let ai = this.initialData.applicationInfo

            return ai.IsActive && this.initialData.step.areAllStepBeforeThisAccepted(ai) && this.m && this.m.IsCompanyCustomerOk
        }

        OverrideKYC(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            let i = this.initialData
            let ai = i.applicationInfo

            let arePreviousStepsOk = i.step.areAllStepBeforeThisAccepted(ai)

            if (arePreviousStepsOk) {
                i.companyLoanApiClient.setApplicationWorkflowStatus(ai.ApplicationNr,
                    i.step.stepName, 'Accepted', 'KYC approved', i.step.stepName + 'Accepted', 'AcceptCustomerCheck').then(() => {
                        this.signalReloadRequired()
                    })
            }
        }

        getGlyphIconClass(stepName: string) {
            if (this.isStepComplete(stepName)) {
                return 'glyphicon-ok'
            } else {
                return 'glyphicon-minus'
            }
        }

        glyphIconClassFromBoolean(isAccepted: boolean) {
            return isAccepted ? 'glyphicon-ok' : 'glyphicon-minus';
        }

        getCustomerKycManagementUrl(customerId: number) {
            return this.getUiGatewayUrl('nCustomer', 'Ui/KycManagement/Manage', [
                ['customerId', customerId.toString()],
                ['backTarget', this.initialData.navigationTargetCodeToHere]])
        }

        componentName(): string {
            return 'companyLoanKyc'
        }

        CheckScreenDisable() {
            if (this.m == null)
                return true
            if (this.m.Customers == null)
                return true
            if (this.m.Customers.length === 0)
                return true
            return false
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            this.apiClient.fetchListCustomersWithKycStatusMethod(this.initialData.applicationInfo.ApplicationNr, ['companyLoanBeneficialOwner', 'companyLoanAuthorizedSignatory']).then(x => {
                let m: Model = {
                    Customers: [],
                    IsCompanyCustomerOk: false
                }

                for (let customerRaw of x.Customers) {
                    let customer = customerRaw as CustomerModel
                    customer.IsPepSanctionDone = this.isStepComplete('CompanyLoanPepSanctionScreen') 
                        && NTechBooleans.isExactlyTrueOrFalse(customer.IsPep) 
                        && NTechBooleans.isExactlyTrueOrFalse(customer.IsSanction)
                    m.Customers.push(customer)
                }

                if (this.isStepComplete('CompanyLoanKycCheck')) {
                    m.IsCompanyCustomerOk = true
                    this.m = m
                } else {
                    this.apiClient.fetchCreditApplicationItemSimple(this.initialData.applicationInfo.ApplicationNr, ['application.companyCustomerId'], '-').then(x => {
                        let companyCustomerId = parseInt(x['application.companyCustomerId'])
                        CompanyLoanCollateralCheckComponentNs.checkForRequiredCustomerProperties([companyCustomerId], true, this.apiClient).then(x => {
                            m.IsCompanyCustomerOk = x.isOk
                            this.m = m
                        })
                    })
                }
            })
        }

        getListDisplayName(listName: string) {
            if (!listName) {
                return ''
            }
            return listName.replace('companyLoan', '')
        }
    }

    export class CompanyLoanKycComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanKycController;
            this.templateUrl = 'company-loan-kyc.html';
        }
    }

    export class Model {
        Customers: CustomerModel[]
        IsCompanyCustomerOk: boolean
    }
    
    export interface CustomerModel extends NTechPreCreditApi.ListCustomersWithKycStatusModel {
        IsPepSanctionDone: boolean
    }
}

angular.module('ntech.components').component('companyLoanKyc', new CompanyLoanKycComponentNs.CompanyLoanKycComponent())