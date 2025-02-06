namespace MortgageLoanApplicationKycComponentNs {
    export class MortgageApplicationRawController extends NTechComponents.NTechComponentControllerBase {
        initialData: MortgageLoanApplicationDynamicComponentNs.StepInitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'mortgageLoanApplicationKyc'
        }

        onChanges() {
            this.m = null
            if (!this.initialData || !this.initialData.applicationInfo) {
                return
            }

            let ai = this.initialData.applicationInfo
            let wf = this.initialData.workflowModel
            let isWaitingForPreviousSteps = !wf.areAllStepBeforeThisAccepted(ai)
            let isReadOnly = isWaitingForPreviousSteps || !ai.IsActive || ai.IsFinalDecisionMade || ai.HasLockedAgreement
            let isReadOnlyScreening = !ai.IsActive || ai.IsFinalDecisionMade || ai.HasLockedAgreement

            if (ai.IsCancelled) {
                this.m = {
                    isReadOnly: isReadOnly,
                    isWaitingForPreviousSteps: isWaitingForPreviousSteps,
                    isStepAccepted: wf.isStatusAccepted(ai),
                    status: null
                }
            } else {
                MortgageLoanDualCustomerRoleHelperNs.getApplicationCustomerRolesByCustomerId(ai.ApplicationNr, this.apiClient).then(x => {
                    let customerIds = x.customerIds
                    let listCustomers = x.rolesByCustomerId

                    this.apiClient.fetchCustomerOnboardingStatuses(customerIds).then(kycStatus => {
                        let m: Model = {
                            isWaitingForPreviousSteps: isWaitingForPreviousSteps,
                            isReadOnly: isReadOnly,
                            isStepAccepted: wf.isStatusAccepted(ai),
                            status: {
                                pepSanctionsCustomers: [],
                                isListScreeningDone: true,
                                isListScreeningPossible: false,
                                isToggleAcceptedAllowed: false
                            }
                        }

                        for (let customerId of customerIds) {
                            let customerStatus = kycStatus[customerId]
                            if (!customerStatus.LatestScreeningDate) {
                                m.status.isListScreeningDone = false
                            }
                            let isPepSanctionDone = NTechBooleans.isExactlyTrueOrFalse(customerStatus.IsPep) && NTechBooleans.isExactlyTrueOrFalse(customerStatus.IsSanction)
                            m.status.pepSanctionsCustomers.push({
                                birthDate: x.firstNameAndBirthDateByCustomerId[customerId]['birthDate'],
                                firstName: x.firstNameAndBirthDateByCustomerId[customerId]['firstName'],
                                customerId: customerId,
                                isAccepted: isPepSanctionDone && customerStatus.IsSanction === false,
                                isRejected: isPepSanctionDone && customerStatus.IsSanction === true,
                                roles: listCustomers[customerId],
                                wasScreened: !!customerStatus.LatestScreeningDate
                            })
                        }

                        m.status.isListScreeningPossible = !m.status.isListScreeningDone && !isReadOnlyScreening
                        m.status.isToggleAcceptedAllowed = !isReadOnly && !isWaitingForPreviousSteps && wf.areAllStepsAfterInitial(ai)
                            && m.status.isListScreeningDone
                            && NTechLinq.all(m.status.pepSanctionsCustomers, x => x.isAccepted)
                        this.m = m
                    })
                })
            }
        }

        glyphIconClassFromBoolean(isAccepted: boolean, isRejected: boolean) {
            return ApplicationStatusBlockComponentNs.getIconClass(isAccepted, isRejected)
        }

        getCustomerKycManagementUrl(customerId: number) {
            if (!customerId || !this.initialData) {
                return null
            }
            return this.getUiGatewayUrl('nCustomer', 'Ui/KycManagement/Manage', [
                ['customerId', customerId.toString()],
                ['backTarget', this.initialData.navigationTargetCodeToHere]])
        }

        toggleStepAccepted(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            let i = this.initialData
            let ai = i.applicationInfo
            let wf = this.initialData.workflowModel
            this.initialData.apiClient.setMortgageApplicationWorkflowStatus(ai.ApplicationNr, wf.stepName, wf.isStatusAccepted(ai) ? 'Initial' : 'Accepted').then(() => {
                this.signalReloadRequired()
            })
        }

        screenNow(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            let customerIds = NTechLinq.select(
                NTechLinq.where(this.m.status.pepSanctionsCustomers, x => !x.wasScreened),
                x => x.customerId)
            if (customerIds.length == 0) {
                return
            }
            this.apiClient.kycScreenBatch(customerIds, moment(this.initialData.today).toDate()).then(_ => {
                this.signalReloadRequired()
            })
        }
    }

    export class Model {
        isWaitingForPreviousSteps: boolean
        isReadOnly: boolean
        isStepAccepted: boolean
        status: {
            isListScreeningDone: boolean
            isListScreeningPossible: boolean
            pepSanctionsCustomers: {
                customerId: number
                isAccepted: boolean
                isRejected: boolean
                wasScreened: boolean
                firstName: string
                birthDate: string
                roles: string[]
            }[],
            isToggleAcceptedAllowed: boolean
        }
    }

    export class MortgageLoanApplicationKycComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationRawController;
            this.template = `<div ng-if="$ctrl.m">

<div ng-if="$ctrl.m.status">
    <table class="table">
        <thead>
            <tr>
                <th class="col-xs-2">Control</th>
                <th class="col-xs-1">Status</th>
                <th class="col-xs-6"></th>
                <th class="col-xs-3 text-right">Action</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td>List screening</td>
                <td><span class="glyphicon" ng-class="{{$ctrl.glyphIconClassFromBoolean($ctrl.m.status.isListScreeningDone, false)}}"></span></td>
                <td></td>
                <td class="text-right">
                    <button ng-if="$ctrl.m.status.isListScreeningPossible" ng-click="$ctrl.screenNow($event)" class="n-direct-btn n-green-btn">Screen now</button>
                </td>
            </tr>
            <tr ng-repeat="c in $ctrl.m.status.pepSanctionsCustomers">
                <td>PEP &amp; Sanction</td>
                <td><span class="glyphicon" ng-class="{{$ctrl.glyphIconClassFromBoolean(c.isAccepted, c.isRejected)}}"></span></td>
                <td>{{c.firstName}}, {{c.birthDate}} (<span ng-repeat="r in c.roles" class="comma">{{r}}</span>)</td>
                <td class="text-right">
                    <a class="n-anchor" ng-href="{{$ctrl.getCustomerKycManagementUrl(c.customerId)}}">View details</a>
                </td>
            </tr>
        </tbody>
    </table>
</div>

<div class="pt-3" ng-show="$ctrl.m.status.isToggleAcceptedAllowed">
    <label class="pr-2">Kyc {{$ctrl.m.isStepAccepted ? 'done' : 'not done'}}</label>
    <label class="n-toggle">
        <input type="checkbox" ng-checked="$ctrl.m.isStepAccepted" ng-click="$ctrl.toggleStepAccepted($event)" />
        <span class="n-slider"></span>
    </label>
</div>
</div>`
        }
    }
}

angular.module('ntech.components').component('mortgageLoanApplicationKyc', new MortgageLoanApplicationKycComponentNs.MortgageLoanApplicationKycComponent())