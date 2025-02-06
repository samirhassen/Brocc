namespace MortgageLoanApplicationDynamicComponentNs {
    export class MortgageLoanApplicationDynamicController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData;
        m: Model;
        f: FullScreenModel

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);
            this.ntechComponentService.subscribeToReloadRequired(() => {
                this.reload()
            })
        }

        componentName(): string {
            return 'mortgageLoanApplicationDynamic'
        }

        cancelApplication(evt: Event) {
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.cancelApplication(this.m.applicationInfo.ApplicationNr).then(() => {
                this.signalReloadRequired();
            })
        }

        reactivateApplication(evt: Event) {
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.reactivateCancelledApplication(this.m.applicationInfo.ApplicationNr).then(() => {
                this.signalReloadRequired();
            })
        }

        isCancelApplicationAllowed(): boolean {
            if (!this.m || !this.m.applicationInfo) {
                return false;
            }
            let ai = this.m.applicationInfo;
            return ai.IsActive === true && ai.IsFinalDecisionMade === false && ai.IsWaitingForAdditionalInformation === false
        }

        isReactivateApplicationAllowed(): boolean {
            if (!this.m || !this.m.applicationInfo) {
                return false;
            }
            let ai = this.m.applicationInfo;
            return ai.IsActive === false && ai.IsCancelled === true && ai.IsWaitingForAdditionalInformation === false
        }

        onBack(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            NavigationTargetHelper.handleBack(
                NavigationTargetHelper.createCodeTarget(this.initialData.backTarget || NavigationTargetHelper.NavigationTargetCode.MortgageLoanSearch),
                this.apiClient,
                this.$q,
                { applicationNr: this.initialData.applicationNr }
            )
        }

        onChanges() {
            this.reload()
        }

        reload() {
            this.m = null
            this.f = null

            if (!this.initialData) {
                return
            }

            this.apiClient.fetchApplicationInfo(this.initialData.applicationNr).then(x => {
                let commentsInitialData: ApplicationCommentsComponentNs.InitialData = {
                    applicationInfo: x,
                    hideAdditionalInfoToggle: true,
                    reloadPageOnWaitingForAdditionalInformation: false
                }

                if (this.initialData.workflowModel.WorkflowVersion.toString() != x.WorkflowVersion) {
                    this.f = {
                        fullScreenModeName: 'invalidWorkflowVersion',
                        invalidWorkflowVersionData: {
                            applicationVersion: x.WorkflowVersion ? x.WorkflowVersion : 'Unknown',
                            serverVersion: this.initialData.workflowModel.WorkflowVersion.toString()
                        },
                    }
                    return
                }

                let createCustomerInfoInitialData = applicantNr => {
                    let d: ApplicationCustomerInfoComponentNs.InitialData = {
                        applicationNr: x.ApplicationNr,
                        applicantNr: applicantNr,
                        customerIdCompoundItemName: null,
                        backTarget: this.initialData.navigationTargetCodeToHere
                    }
                    return d
                }

                let createStepInitialData = s => {
                    let d = WorkflowHelper.createInitialData<StepLocalInitialData, StepInitialData>(initialData, {
                        applicationInfo: x,
                        workflowModel: new WorkflowHelper.WorkflowStepModel(this.initialData.workflowModel, s)
                    })

                    return d
                }

                let assignedHandlersInitialData: ApplicationAssignedHandlersComponentNs.InitialData = {
                    applicationNr: this.initialData.applicationNr,
                    hostData: this.initialData
                }

                this.m = new Model(x,
                    commentsInitialData,
                    createCustomerInfoInitialData(1),
                    x.NrOfApplicants > 1 ? createCustomerInfoInitialData(2) : null,
                    assignedHandlersInitialData,
                    this.initialData.workflowModel,
                    createStepInitialData)
            })
        }
    }

    export class MortgageLoanApplicationDynamicComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationDynamicController;
            this.templateUrl = 'mortgage-loan-application-dynamic.html';
        }
    }

    export interface LocalInitialData {
        applicationNr: string
        workflowModel: WorkflowHelper.WorkflowServerModel
    }

    export interface StepLocalInitialData {
        applicationInfo: NTechPreCreditApi.ApplicationInfoModel
        workflowModel: WorkflowHelper.WorkflowStepModel
    }

    export interface StepInitialData extends ComponentHostNs.ComponentHostInitialData, StepLocalInitialData {
    }

    export interface InitialData extends LocalInitialData, ComponentHostNs.ComponentHostInitialData {
    }

    export class Model {
        public statusBlocksInitialData: NTechPreCreditApi.IStringDictionary<ApplicationStatusBlockComponentNs.InitialData>
        public stepsInitialData: NTechPreCreditApi.IStringDictionary<StepInitialData>

        constructor(public applicationInfo: NTechPreCreditApi.ApplicationInfoModel,
            public commentsInitialData: ApplicationCommentsComponentNs.InitialData,
            public applicationCustomerInfo1InitialData: ApplicationCustomerInfoComponentNs.InitialData,
            public applicationCustomerInfo2InitialData: ApplicationCustomerInfoComponentNs.InitialData,
            public assignedHandlersInitialData: ApplicationAssignedHandlersComponentNs.InitialData,
            workflowModel: WorkflowHelper.WorkflowServerModel,
            createStepInitialData: ((stepName: string) => StepInitialData)) {
            let i = applicationInfo
            this.statusBlocksInitialData = {}
            this.stepsInitialData = {}
            let hasExpandedStep = false
            for (let s of workflowModel.Steps) {
                let newStep = {
                    title: s.DisplayName,
                    isActive: i.IsActive,
                    status: WorkflowHelper.isStepAccepted(s.Name, i) ? 'Accepted' : (WorkflowHelper.isStepRejected(s.Name, i) ? 'Rejected' : 'Initial'),
                    isInitiallyExpanded: false
                }
                if (!hasExpandedStep && newStep.status === 'Initial' && i.IsActive) {
                    newStep.isInitiallyExpanded = true
                    hasExpandedStep = true
                }
                this.statusBlocksInitialData[s.Name] = newStep
                this.stepsInitialData[s.Name] = createStepInitialData(s.Name)
            }
        }
    }

    export class FullScreenModel {
        fullScreenModeName: string
        invalidWorkflowVersionData: { serverVersion: string, applicationVersion: string }
    }
}

angular.module('ntech.components').component('mortgageLoanApplicationDynamic', new MortgageLoanApplicationDynamicComponentNs.MortgageLoanApplicationDynamicComponent())