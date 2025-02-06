import { ApplicationInfoModel, WorkflowModel, WorkflowStepModel } from './shared-loan-application-api.service';

export class WorkflowHelper {
    public static InitialName: string = 'Initial';
    public static AcceptedName: string = 'Accepted';
    public static RejectedName: string = 'Rejected';

    static isStepAccepted(stepName: string, ai: ApplicationInfoModel) {
        if (!ai) {
            return false;
        }
        return ai.ListNames.indexOf(stepName + `_${WorkflowHelper.AcceptedName}`) >= 0;
    }

    static isStepRejected(stepName: string, ai: ApplicationInfoModel) {
        if (!ai) {
            return false;
        }
        return ai.ListNames.indexOf(stepName + `_${WorkflowHelper.RejectedName}`) >= 0;
    }

    static isStepInitial(stepName: string, ai: ApplicationInfoModel) {
        if (!ai) {
            return false;
        }
        return !WorkflowHelper.isStepAccepted(stepName, ai) && !WorkflowHelper.isStepRejected(stepName, ai);
    }

    static getStepStatus(stepName: string, ai: ApplicationInfoModel) {
        return WorkflowHelper.isStepAccepted(stepName, ai)
            ? WorkflowHelper.AcceptedName
            : WorkflowHelper.isStepRejected(stepName, ai)
            ? WorkflowHelper.RejectedName
            : WorkflowHelper.InitialName;
    }

    static areAllStepBeforeThisAccepted(stepName: string, workflow: WorkflowModel, ai: ApplicationInfoModel) {
        for (let s of workflow.Steps) {
            if (s.Name === stepName) {
                return true;
            } else if (!WorkflowHelper.isStepAccepted(s.Name, ai)) {
                return false;
            }
        }
        throw new Error('The step does not exist in the workflow: ' + stepName);
    }

    static getInitialListName(stepName: string) {
        return `${stepName}_${this.InitialName}`;
    }
}

export class WorkflowStepHelper {
    constructor(
        public workflow: WorkflowModel,
        public currentStep: WorkflowStepModel,
        private ai: ApplicationInfoModel
    ) {}

    areAllStepBeforeThisAccepted() {
        return WorkflowHelper.areAllStepBeforeThisAccepted(this.currentStep.Name, this.workflow, this.ai);
    }

    getStepStatus() {
        return WorkflowHelper.getStepStatus(this.currentStep.Name, this.ai);
    }

    isStatusAccepted() {
        return WorkflowHelper.isStepAccepted(this.currentStep.Name, this.ai);
    }

    areAllStepsAfterInitial() {
        let passed = false;
        for (let step of this.workflow.Steps) {
            if (this.currentStep.Name == step.Name) {
                passed = true;
            } else if (passed && !WorkflowHelper.isStepInitial(step.Name, this.ai)) {
                return false;
            }
        }
        return true;
    }

    isStatusRejected() {
        return WorkflowHelper.isStepRejected(this.currentStep.Name, this.ai);
    }

    isStatusInitial() {
        return WorkflowHelper.isStepInitial(this.currentStep.Name, this.ai);
    }

    /*
     * If this step is accepted and the next step is initial on an active application then this step is revertable from a workflow perspective.
     * Note that it may still not be actually revertable for other reasons specific to that individual step
     */
    isRevertable() {
        if (!this.ai.IsActive || this.ai.IsFinalDecisionMade) {
            return false;
        }
        if (!this.isStatusAccepted()) {
            return false;
        }
        return this.areAllStepsAfterInitial();
    }

    getAcceptedListName() {
        return `${this.currentStep.Name}_${WorkflowHelper.AcceptedName}`;
    }

    getRejectedListName() {
        return `${this.currentStep.Name}_${WorkflowHelper.RejectedName}`;
    }

    getInitialListName() {
        return `${this.currentStep.Name}_${WorkflowHelper.InitialName}`;
    }

    getCustomStepData<T>(): T {
        return this.currentStep.CustomData as T;
    }
}
