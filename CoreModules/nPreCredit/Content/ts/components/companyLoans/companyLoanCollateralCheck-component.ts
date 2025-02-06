namespace CompanyLoanCollateralCheckComponentNs {
    export class CompanyLoanCollateralCheckController extends NTechComponents.NTechComponentControllerBase {
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
            return 'companyLoanCollateralCheck'
        }

        isToggleCompanyLoanCollateralCheckStatusAllowed() {
            if (!this.initialData) {
                return false
            }
            let ai = this.initialData.applicationInfo

            return ai.IsActive && this.initialData.step.areAllStepBeforeThisAccepted(ai) && !ai.IsPartiallyApproved && !ai.HasLockedAgreement
        }

        getCompanyLoanCollateralCheckStatus() {
            if (!this.initialData) {
                return null
            }
            return this.initialData.step.getStepStatus(this.initialData.applicationInfo)
        }

        toggleCompanyLoanCollateralCheckStatus(evt: Event) {
            if (evt) {
                evt.stopPropagation()
                evt.preventDefault()
            }
            this.apiClient.fetchCustomerApplicationListMembers(this.initialData.applicationInfo.ApplicationNr, 'companyLoanCollateral').then(listMembersResult => {
                let customerIds = listMembersResult.CustomerIds
                let onOk = () => {
                    this.toggleCompanyLoanListBasedStatus()
                }
                if (customerIds.length === 0) {
                    onOk()
                } else {
                    checkForRequiredCustomerProperties(customerIds, false, this.apiClient).then(x => {
                        if (x.isOk) {
                            onOk()
                        } else {
                            toastr.warning('Collaterals are missing name, email, phone or adress')
                        }
                    })
                }
            })
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

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            let ai = this.initialData.applicationInfo

            this.m = {
                collateralInitialData: {
                    applicationInfo: ai,
                    backUrl: this.initialData.urlToHereFromOtherModule,
                    backTarget: this.initialData.navigationTargetCodeToHere,
                    editorService: new ApplicationCustomerListComponentNs.CustomerApplicationListEditorService(ai.ApplicationNr, 'companyLoanCollateral', this.apiClient),
                    isEditable: ai.IsActive && !ai.IsPartiallyApproved && this.initialData.step.isStatusInitial(ai)
                }
            }
        }
    }

    export class Model {
        collateralInitialData: ApplicationCustomerListComponentNs.InitialData
    }

    export function checkForRequiredCustomerProperties(customerIds: number[], isCompany: boolean, apiClient: NTechPreCreditApi.ApiClient): ng.IPromise<{ isOk: boolean }> {
        let requiredCustomerProperties = ['email', 'addressZipcode']
        if (isCompany) {
            requiredCustomerProperties.unshift('companyName')
        } else {
            requiredCustomerProperties.unshift('phone')
            requiredCustomerProperties.unshift('lastName')
            requiredCustomerProperties.unshift('firstName')
        }

        let containsCustomerProperty = (result: NTechPreCreditApi.INumberDictionary<NTechPreCreditApi.IStringDictionary<string>>, customerId: number, propertyName: string) => {
            if (!result) {
                return false
            }
            let d = result[customerId]
            if (!d) {
                return false
            }
            return !!d[propertyName]
        }
        return apiClient.fetchCustomerItemsBulk(customerIds, requiredCustomerProperties).then(customersDataResult => {
            let areAllOk = true
            for (let customerId of customerIds) {
                for (let propertyName of requiredCustomerProperties) {
                    if (!containsCustomerProperty(customersDataResult, customerId, propertyName)) {
                        areAllOk = false
                    }
                }
            }
            return { isOk: areAllOk }
        })
    }

    export class CompanyLoanCollateralCheckComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanCollateralCheckController;
            this.template = `<div>
                    <application-customer-list initial-data="$ctrl.m.collateralInitialData"></application-customer-list>

                    <div ng-show="$ctrl.isToggleCompanyLoanCollateralCheckStatusAllowed()">
                        <label class="pr-2">Collateral control {{$ctrl.getCompanyLoanCollateralCheckStatus() === 'Accepted' ? 'done' : 'not done'}}</label>

                        <label class="n-toggle">
                            <input type="checkbox" ng-checked="$ctrl.getCompanyLoanCollateralCheckStatus() === 'Accepted'" ng-click="$ctrl.toggleCompanyLoanCollateralCheckStatus($event)" />
                            <span class="n-slider"></span>
                        </label>
                    </div>

                    </div>`
        }
    }
}

angular.module('ntech.components').component('companyLoanCollateralCheck', new CompanyLoanCollateralCheckComponentNs.CompanyLoanCollateralCheckComponent())