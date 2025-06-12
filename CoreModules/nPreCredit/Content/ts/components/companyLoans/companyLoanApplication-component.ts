    namespace CompanyLoanApplicationComponentNs {
    export class CompanyLoanApplicationController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model
        f: FullScreenModel

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
            ntechComponentService.subscribeToNTechEvents(x => {
                if (x.eventName == 'companyLoanInitialCreditCheckBack' && x.eventData == this.initialData.applicationNr) {
                    this.f = null
                } else if (x.eventName == 'companyLoanInitialCreditCheckCompleted' && x.eventData == this.initialData.applicationNr) {
                    this.f = null
                    this.reload()
                }
            })
            ntechComponentService.subscribeToReloadRequired(x => {
                this.reload()
            })
        }

        componentName(): string {
            return 'companyLoanApplication'
        }

        onChanges() {
            this.reload()
        }

        isCancelApplicationAllowed(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.m) {
                return false
            }
            let ai = this.m.applicationInfo
            return ai.IsActive && !ai.IsCancelled && !ai.IsFinalDecisionMade
        }

        isApproveApplicationAllowed(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.m) {
                return false
            }
            let ai = this.m.applicationInfo

            let lastStepName = this.initialData.workflowModel.Steps[this.initialData.workflowModel.Steps.length - 1].Name

            let isAtLastStepOrPartiallyApproved = WorkflowHelper.isStepInitial(lastStepName, ai) || (ai.IsPartiallyApproved && WorkflowHelper.isStepAccepted(lastStepName, ai))

            return ai.IsActive
                && !ai.IsFinalDecisionMade
                && WorkflowHelper.areAllStepBeforeThisAccepted(lastStepName, this.m.workflowStepOrder, ai)
                && isAtLastStepOrPartiallyApproved
        }

        cancelApplication(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.isCancelApplicationAllowed()) {
                return
            }
            this.apiClient.cancelApplication(this.initialData.applicationNr).then(() => {
                this.reload()
            })
        }

        approveApplication(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.isApproveApplicationAllowed()) {
                return
            }
            this.initialData.companyLoanApiClient.approveApplication(this.initialData.applicationNr).then(() => {
                this.reload()
            })
        }

        isReactivateApplicationAllowed(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.m) {
                return false
            }

            let ai = this.m.applicationInfo
            return !ai.IsActive && ai.IsCancelled && !ai.IsFinalDecisionMade
        }

        reactivateApplication(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.isReactivateApplicationAllowed()) {
                return
            }
            this.apiClient.reactivateCancelledApplication(this.initialData.applicationNr).then(() => {
                this.reload()
            })
        }

        onBack = (evt) => {
            if (evt) {
                evt.preventDefault()
            }
            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, this.apiClient, this.$q, { applicationNr: initialData.applicationNr }, NavigationTargetHelper.NavigationTargetCode.CompanyLoanSearch)
        }

        private reload() {
            this.m = null
            this.f = null

            if (!this.initialData) {
                return
            }

            this.apiClient.fetchApplicationInfoWithCustom(this.initialData.applicationNr, false, true).then(x => {
                this.init(x.Info)
            })
        }

        private init(ai: NTechPreCreditApi.ApplicationInfoModel) {
            if (this.initialData.workflowModel.WorkflowVersion.toString() != ai.WorkflowVersion) {
                this.f = {
                    fullScreenModeName: 'invalidWorkflowVersion',
                    invalidWorkflowVersionData: {
                        applicationVersion: ai.WorkflowVersion ? ai.WorkflowVersion : 'Unknown',
                        serverVersion: this.initialData.workflowModel.WorkflowVersion.toString()
                    },
                    initialCreditCheckViewInitialData: null
                }
                return
            }

            let createStepInitialData = s => {
                let d: StepInitialData = <any>{ ... this.initialData }
                d.rejectionReasonToDisplayNameMapping = this.initialData.rejectionReasonToDisplayNameMapping
                d.applicationInfo = ai
                d.step = new WorkflowHelper.WorkflowStepModel(this.initialData.workflowModel, s)

                return d
            }

            this.m = new Model(ai,
                this.initialData.workflowModel,
                createStepInitialData)

            this.m.commentsInitialData = {
                applicationInfo: ai,
                hideAdditionalInfoToggle: true
            }

            this.m.companyCustomerInitialData = {
                applicationNr: ai.ApplicationNr,
                customerIdCompoundItemName: 'application.companyCustomerId',
                applicantNr: null,
                showKycBlock: false,
                onkycscreendone: null,
                backTarget: this.initialData.navigationTargetCodeToHere
            }

            this.m.applicantCustomerInitialData = {
                applicationNr: ai.ApplicationNr,
                customerIdCompoundItemName: 'application.applicantCustomerId',
                applicantNr: null,
                showKycBlock: false,
                onkycscreendone: null,
                backTarget: this.initialData.navigationTargetCodeToHere
            }

            this.m.checkpointsInitialData = {
                applicationNr: ai.ApplicationNr,
                applicationType: 'companyLoan'
            }

            if (this.initialData.isTest) {
                let tf = this.initialData.testFunctions
                let testScope = this.initialData.testFunctions.generateUniqueScopeName()
                tf.addLink(testScope, 'Show testemails', '/TestLatestEmails/List')
            }
        }
    }

    export class CompanyLoanApplicationComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanApplicationController;
            this.templateUrl = 'company-loan-application.html';
        }
    }

    export interface InitialData extends ComponentHostNs.ComponentHostInitialData {
        applicationNr: string
        workflowModel: WorkflowHelper.WorkflowServerModel
        rejectionReasonToDisplayNameMapping: NTechPreCreditApi.IStringDictionary<string>
        rejectionRuleToReasonNameMapping: NTechPreCreditApi.IStringDictionary<string>
        creditUrlPattern: string
        backTarget: string
    }

    export interface StepInitialData extends ComponentHostNs.ComponentHostInitialData {
        applicationInfo: NTechPreCreditApi.ApplicationInfoModel
        step: WorkflowHelper.WorkflowStepModel
        rejectionReasonToDisplayNameMapping: NTechPreCreditApi.IStringDictionary<string>
    }

    export class FullScreenModel {
        fullScreenModeName: string
        initialCreditCheckViewInitialData: CompanyLoanInitialCreditCheckViewComponentNs.InitialData
        invalidWorkflowVersionData: { serverVersion: string, applicationVersion: string }
    }

    export class Model {
        constructor(public applicationInfo: NTechPreCreditApi.ApplicationInfoModel,
            workflowModel: WorkflowHelper.WorkflowServerModel,
            createStepInitialData: ((stepName: string) => StepInitialData)) {
            this.workflowStepOrder = _.map(workflowModel.Steps, x => x.Name)

            let i = applicationInfo
            this.statusBlocksInitialData = {}
            this.stepsInitialData = {}
            let hasExpandedStep = false
            for (let s of workflowModel.Steps) {
                if (s.ComponentName) {
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

        public commentsInitialData: ApplicationCommentsComponentNs.InitialData
        public statusBlocksInitialData: NTechPreCreditApi.IStringDictionary<ApplicationStatusBlockComponentNs.InitialData>
        public stepsInitialData: NTechPreCreditApi.IStringDictionary<StepInitialData>
        public workflowStepOrder: string[]
        public companyCustomerInitialData: ApplicationCustomerInfoComponentNs.InitialData
        public applicantCustomerInitialData: ApplicationCustomerInfoComponentNs.InitialData
        public checkpointsInitialData: ApplicationCheckpointsComponentNs.InitialData
    }
}

angular.module('ntech.components').component('companyLoanApplication', new CompanyLoanApplicationComponentNs.CompanyLoanApplicationComponent())