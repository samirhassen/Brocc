namespace CompanyLoanAuthorizedSignatoryCheckComponentNs {
    export class CompanyLoanAuthorizedSignatoryCheckController extends NTechComponents.NTechComponentControllerBase {
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
            return 'companyLoanAuthorizedSignatoryCheck'
        }

        toggleCompanyLoanListBasedStatus() {
            if (!this.initialData) {
                return
            }
            let ai = this.initialData.applicationInfo
            let step = this.initialData.step

            //TODO: This is not a great way of doing this
            this.initialData.companyLoanApiClient.setApplicationWorkflowStatus(ai.ApplicationNr, step.stepName, step.isStatusAccepted(ai) ? 'Initial' : 'Accepted').then(() => {
                this.signalReloadRequired()
            })
        }

        private checkForRequiredCustomerProperties(requiredCustomerProperties: string[], customerIds: number[], messageIfNotOk: string, doOnOk: () => void) {
            this.apiClient.fetchCustomerItemsBulk(customerIds, requiredCustomerProperties).then(customersDataResult => {
                let areAllOk = true
                for (let customerId of customerIds) {
                    for (let propertyName of requiredCustomerProperties) {
                        if (!this.containsCustomerProperty(customersDataResult, customerId, propertyName)) {
                            areAllOk = false
                        }
                    }
                }
                if (!areAllOk) {
                    toastr.warning(messageIfNotOk)
                    return
                }
                doOnOk()
            })
        }

        isToggleCompanyLoanAuthorizedSignatoryCheckStatusAllowed() {
            if (!this.initialData) {
                return false
            }

            let ai = this.initialData.applicationInfo

            return ai.IsActive && this.initialData.step.areAllStepBeforeThisAccepted(ai) && !ai.IsPartiallyApproved && !ai.HasLockedAgreement
        }

        getCompanyLoanAuthorizedSignatoryCheckStatus() {
            if (!this.initialData) {
                return null
            }

            return this.initialData.step.getStepStatus(this.initialData.applicationInfo)
        }

        toggleCompanyLoanAuthorizedSignatoryCheckStatus(evt: Event) {
            if (evt) {
                evt.stopPropagation()
                evt.preventDefault()
            }
            this.apiClient.fetchCustomerApplicationListMembers(this.initialData.applicationInfo.ApplicationNr, 'companyLoanAuthorizedSignatory').then(listMembersResult => {
                let customerIds = listMembersResult.CustomerIds
                if (customerIds.length === 0) {
                    toastr.warning('At least one authorized signatory is required')
                    return
                }
                this.checkForRequiredCustomerProperties(['firstName', 'lastName', 'email', 'phone'], customerIds, 'Authorized signatories are missing name, email or phone', () => {
                    this.toggleCompanyLoanListBasedStatus()
                })
            })
        }

        private containsCustomerProperty(result: NTechPreCreditApi.INumberDictionary<NTechPreCreditApi.IStringDictionary<string>>, customerId: number, propertyName: string) {
            if (!result) {
                return false
            }
            let d = result[customerId]
            if (!d) {
                return false
            }
            return !!d[propertyName]
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            let ai = this.initialData.applicationInfo

            this.m = {
                authorizedSignatoryCheckInitialData: {
                    applicationInfo: ai,
                    header: "Authorized Signatories", 
                    backUrl: this.initialData.urlToHereFromOtherModule,
                    backTarget: this.initialData.navigationTargetCodeToHere,
                    editorService: new ApplicationCustomerListComponentNs.CustomerApplicationListEditorService(ai.ApplicationNr, 'companyLoanAuthorizedSignatory', this.apiClient),
                    multipleEditorService: {
                        editorService: new ApplicationCustomerListComponentNs.CustomerApplicationListEditorService(ai.ApplicationNr, 'companyLoanBeneficialOwner', this.apiClient),
                        header: "Beneficial Owners", 
                        includeCompanyRoles: true
                    },
                    isEditable: ai.IsActive && !ai.IsPartiallyApproved && this.initialData.step.isStatusInitial(ai)
                }
            }
        }
    }

    export class Model {
        authorizedSignatoryCheckInitialData: ApplicationCustomerListComponentNs.InitialData
    }

    export class CompanyLoanAuthorizedSignatoryCheckComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanAuthorizedSignatoryCheckController;
            this.template = `<div> 
                    <application-customer-list initial-data="$ctrl.m.authorizedSignatoryCheckInitialData"></application-customer-list>
                    
                    <div ng-show="$ctrl.isToggleCompanyLoanAuthorizedSignatoryCheckStatusAllowed()">
                        <label class="pr-2">Company connection control {{$ctrl.getCompanyLoanAuthorizedSignatoryCheckStatus() === 'Accepted' ? 'done' : 'not done'}}</label>

                        <label class="n-toggle">
                            <input type="checkbox" ng-checked="$ctrl.getCompanyLoanAuthorizedSignatoryCheckStatus() === 'Accepted'" ng-click="$ctrl.toggleCompanyLoanAuthorizedSignatoryCheckStatus($event)" />
                            <span class="n-slider"></span>
                        </label>
                    </div>

                    </div>`
        }
    }
}

angular.module('ntech.components').component('companyLoanAuthorizedSignatoryCheck', new CompanyLoanAuthorizedSignatoryCheckComponentNs.CompanyLoanAuthorizedSignatoryCheckComponent())