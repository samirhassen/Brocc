namespace MortgageLoanApplicationDualAuditApproveAgreementComponentNs {
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
            return 'mortgageLoanApplicationDualAuditApproveAgreement'
        }

        onChanges() {
            this.m = null

            if (!this.initialData || !this.initialData.applicationInfo) {
                return
            }

            let ai = this.initialData.applicationInfo

            let setup = (lockedAgreement: NTechPreCreditApi.LockedAgreementModel) => {
                MortgageLoanDualCustomerRoleHelperNs.getApplicationCustomerRolesByCustomerId(ai.ApplicationNr, this.apiClient).then(x => {
                    let stepCode = this.getStepCode()
                    if (!(stepCode === StepCode.Audit || stepCode === StepCode.Approve)) {
                        return
                    }

                    let isAudit = stepCode === StepCode.Audit
                    if (wf.areAllStepBeforeThisAccepted(ai)) {
                        let m: Model = {
                            customers: [],
                            isApproveAllowed: false,
                            isCancelAllowed: false,
                            lockedAgreement: lockedAgreement,
                            isApproveStep: stepCode === StepCode.Approve,
                            showWaitingForApproval: false,
                            isApproved: false
                        }
                        for (let customerId of x.customerIds) {
                            let agreementUrl: string
                            if (ai.HasLockedAgreement) {
                                let archiveKey = lockedAgreement && lockedAgreement.UnsignedAgreementArchiveKeyByCustomerId ? lockedAgreement.UnsignedAgreementArchiveKeyByCustomerId[customerId] : null
                                agreementUrl = this.apiClient.getArchiveDocumentUrl(archiveKey)
                            } else {
                                agreementUrl = this.getLocalModuleUrl('api/MortgageLoan/Create-Dual-Agreement-Pdf',
                                    [['ApplicationNr', ai.ApplicationNr],
                                    ['CustomerId', customerId.toString()],
                                    ['DisableTemplateCache', this.initialData.isTest ? 'True' : null]])
                            }
                            m.customers.push({
                                customerId: customerId,
                                firstName: x.firstNameAndBirthDateByCustomerId[customerId]['firstName'],
                                birthDate: x.firstNameAndBirthDateByCustomerId[customerId]['birthDate'],
                                roleNames: x.rolesByCustomerId[customerId],
                                agreementUrl: agreementUrl
                            })
                        }
                        m.isCancelAllowed = ai.IsActive && ai.HasLockedAgreement && wf.areAllStepsAfterInitial(ai) && wf.isStatusAccepted(ai)
                        if (isAudit) {
                            m.isApproveAllowed = ai.IsActive && !ai.HasLockedAgreement && wf.isStatusInitial(ai)
                        } else {
                            let isApproveAllowedBeforeUserCheck = ai.IsActive
                                && ai.HasLockedAgreement
                                && wf.isStatusInitial(ai)

                            m.isApproveAllowed = isApproveAllowedBeforeUserCheck && lockedAgreement.LockedByUserId !== this.initialData.currentUserId
                            m.showWaitingForApproval = isApproveAllowedBeforeUserCheck && !m.isApproveAllowed
                            m.isApproved = wf.isStatusAccepted(ai)

                            if (isApproveAllowedBeforeUserCheck && this.initialData.isTest) {
                                let tf = this.initialData.testFunctions
                                tf.addFunctionCall(tf.generateUniqueScopeName(), 'Force approve', () => {
                                    this.approveInternal(true)
                                })
                            }
                        }
                        this.m = m
                    } else {
                        //Show nothing
                    }
                })
            }

            let wf = this.initialData.workflowModel

            if (ai.HasLockedAgreement) {
                this.apiClient.getLockedAgreement(ai.ApplicationNr).then(x => setup(x.LockedAgreement))
            } else {
                setup(null)
            }
        }

        private approveInternal(requestOverrideDuality: boolean, evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            let s = this.getStepCode()

            let applicationNr = this.initialData.applicationInfo.ApplicationNr
            if (s === StepCode.Audit) {
                this.apiClient.auditAndCreateMortgageLoanLockedAgreement(applicationNr).then(x => {
                    this.signalReloadRequired()
                })
            } else if (s === StepCode.Approve) {
                this.apiClient.approveLockedAgreement(applicationNr, requestOverrideDuality).then(x => {
                    if (x.WasApproved) {
                        this.apiClient.setMortgageApplicationWorkflowStatus(applicationNr, this.initialData.workflowModel.currentStep.Name, 'Accepted', 'Agreement approved').then(y => {
                            this.signalReloadRequired()
                        })
                    } else {
                        toastr.warning('Approve failed')
                    }
                })
            }
        }

        approve(evt?: Event) {
            this.approveInternal(false, evt)
        }

        cancel(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }

            let ai = this.initialData.applicationInfo
            let step = this.initialData.workflowModel

            let s = this.getStepCode()
            if (s === StepCode.Audit) {
                this.apiClient.removeLockedAgreement(ai.ApplicationNr).then(x => {
                    this.apiClient.setMortgageApplicationWorkflowStatus(ai.ApplicationNr, step.stepName, 'Initial', 'Audit agreement cancelled').then(() => {
                        this.signalReloadRequired()
                    })
                })
            } else if (s === StepCode.Approve) {
                this.apiClient.setMortgageApplicationWorkflowStatus(ai.ApplicationNr, step.stepName, 'Initial', 'Approve agreement cancelled').then(() => {
                    this.signalReloadRequired()
                })
            }
        }

        getUserDisplayNameByUserId(userId: number) {
            if (!userId) {
                return null
            }

            if (this.initialData) {
                let d = this.initialData.userDisplayNameByUserId[userId]
                if (d) {
                    return d
                }
            }

            return `User ${userId}`
        }

        private getStepCode(): StepCode {
            if (!this.initialData || !this.initialData.workflowModel) {
                return StepCode.Unknown
            }
            let c = this.initialData.workflowModel.getCustomStepData<CustomStepDataModel>()
            if (c && c.IsApproveAgreement == "yes") {
                return StepCode.Approve
            }
            if (c && c.IsAuditAgreement) {
                return StepCode.Audit
            }
            return StepCode.Unknown
        }
    }

    export enum StepCode {
        Unknown = "Unknown",
        Audit = "Audit",
        Approve = "Approve"
    }

    export interface CustomStepDataModel {
        IsApproveAgreement: string
        IsAuditAgreement: string
    }

    export class Model {
        isApproveStep: boolean
        isApproveAllowed: boolean
        isCancelAllowed: boolean
        isApproved: boolean
        showWaitingForApproval: boolean
        lockedAgreement: NTechPreCreditApi.LockedAgreementModel
        customers: CustomerModel[]
    }

    export class CustomerModel {
        customerId: number
        firstName: string
        birthDate: string
        roleNames: string[]
        agreementUrl: string
    }

    export class MortgageLoanApplicationDualInitialCreditCheckComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationRawController;
            this.template = `<div class="container frame" ng-if="$ctrl.m">

<div class="row pb-3" ng-if="$ctrl.m.isCancelAllowed">
    <div class="text-right">
        <button class="n-main-btn n-white-btn" ng-click="$ctrl.cancel($event)">
            Cancel
        </button>
    </div>
</div>

<div class="row pb-3" ng-if="$ctrl.m.lockedAgreement">
    <div class="col-xs-6">
        <div class="form-horizontal">
            <div class="form-group">
                <label class="col-xs-6 control-label">Audited by</label>
                <div class="col-xs-6 form-control-static">{{$ctrl.getUserDisplayNameByUserId($ctrl.m.lockedAgreement.LockedByUserId)}}</div>
            </div>
            <div class="form-group">
                <label class="col-xs-6 control-label">Audited date</label>
                <div class="col-xs-6 form-control-static">{{$ctrl.m.lockedAgreement.LockedDate | date:'short'}}</div>
            </div>
        </div>
    </div>
    <div class="col-xs-6" ng-if="$ctrl.m.isApproveStep">
        <div class="form-horizontal">
            <div class="form-group">
                <label class="col-xs-6 control-label">Approved by</label>
                <div class="col-xs-6 form-control-static"><span ng-if="$ctrl.m.isApproved">{{$ctrl.getUserDisplayNameByUserId($ctrl.m.lockedAgreement.ApprovedByUserId)}}</span></div>
            </div>
            <div class="form-group">
                <label class="col-xs-6 control-label">Approved date</label>
                <div class="col-xs-6 form-control-static"><span ng-if="$ctrl.m.isApproved">{{$ctrl.m.lockedAgreement.ApprovedDate | date:'short'}}</span></div>
            </div>
        </div>
    </div>
</div>

<div class="row" ng-if="$ctrl.m.customers">
    <table class="table col-sm-12">
        <tbody>
            <tr ng-repeat="c in $ctrl.m.customers">
                <td class="col-xs-9">{{c.firstName}}, {{c.birthDate}} (<span ng-repeat="r in c.roleNames" class="comma">{{r}}</span>)</td>
                <td class="col-xs-3 text-right">
                    <a ng-if="c.agreementUrl" ng-href="{{c.agreementUrl}}" target="_blank" class="n-direct-btn n-purple-btn">File <span class="glyphicon glyphicon-save"></span></a>
                    <span ng-if="!c.agreementUrl">Missing</span>
                </td>
            </tr>
        </tbody>
    </table>
</div>

<div class="row" ng-if="$ctrl.m.isApproveAllowed">
    <div class="text-center pt-3">
        <button class="n-main-btn n-green-btn" ng-click="$ctrl.approve($event)">
            Approve
        </button>
    </div>
</div>

<div class="row" ng-if="$ctrl.m.showWaitingForApproval">
    <div class="text-center pt-3">
        <span style="font-weight:bold;font-style:italic">Waiting for approval</span>
    </div>
</div>

</div>`
        }
    }
}

angular.module('ntech.components').component('mortgageLoanApplicationDualAuditApproveAgreement', new MortgageLoanApplicationDualAuditApproveAgreementComponentNs.MortgageLoanApplicationDualInitialCreditCheckComponent())