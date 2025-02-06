namespace WorkflowHelper {
    export const InitialName: string = 'Initial'
    export const AcceptedName: string = 'Accepted'
    export const RejectedName: string = 'Rejected'

    export function isStepAccepted(stepName: string, ai: NTechPreCreditApi.ApplicationInfoModel) {
        if (!ai) {
            return false
        }
        return ai.ListNames.indexOf(stepName + `_${AcceptedName}`) >= 0
    }

    export function isStepRejected(stepName: string, ai: NTechPreCreditApi.ApplicationInfoModel) {
        if (!ai) {
            return false
        }
        return ai.ListNames.indexOf(stepName + `_${RejectedName}`) >= 0
    }

    export function isStepInitial(stepName: string, ai: NTechPreCreditApi.ApplicationInfoModel) {
        if (!ai) {
            return false
        }
        return !isStepAccepted(stepName, ai) && !isStepRejected(stepName, ai)
    }

    export function areAllStepBeforeThisAccepted(stepName: string, allStepNames: string[], ai: NTechPreCreditApi.ApplicationInfoModel) {
        for (let s of allStepNames) {
            if (s === stepName) {
                return true
            } else if (!isStepAccepted(s, ai)) {
                return false
            }
        }
        throw new Error('The step does not exist in the workflow: ' + stepName)
    }

    export function getStepStatus(stepName: string, ai: NTechPreCreditApi.ApplicationInfoModel) {
        return isStepAccepted(stepName, ai) ? AcceptedName : (isStepRejected(stepName, ai) ? RejectedName : InitialName)
    }

    export function areAllStepsAccepted(allStepNames: string[], ai: NTechPreCreditApi.ApplicationInfoModel, forceDoneStepName?: string) {
        for (let sn of allStepNames) {
            if (!isStepAccepted(sn, ai) && (!forceDoneStepName || sn !== forceDoneStepName)) {
                return false
            }
        }
        return true
    }

    export function createInitialData<T, U extends ComponentHostNs.ComponentHostInitialData & T>(baseData: ComponentHostNs.ComponentHostInitialData, extData: T): U {
        let b = { ...baseData } as U
        //{ ...baseData, ...extData } should work but doesnt yet in this version of typescript hence the below hack
        for (let p of Object.keys((<any>extData))) { //The any cast here is just to please the compiler
            b[p] = extData[p]
        }

        return b
    }

    export function getStepModelByCustomData(serverModel: WorkflowServerModel, predicate: (x: any) => boolean): WorkflowStepModel | null {
        for (let step of serverModel.Steps) {
            if (step && step.CustomData && predicate(step.CustomData)) {
                return new WorkflowHelper.WorkflowStepModel(serverModel, step.Name)
            }
        }
        return null
    }

    export class WorkflowStepModel {
        public allStepNames: string[]
        public currentStep: WorkflowServerStepModel

        constructor(public serverModel: WorkflowServerModel, public stepName: string) {
            this.allStepNames = []
            for (let s of serverModel.Steps) {
                this.allStepNames.push(s.Name)
                if (s.Name == stepName) {
                    this.currentStep = s
                }
            }
        }

        areAllStepBeforeThisAccepted(ai: NTechPreCreditApi.ApplicationInfoModel) {
            return WorkflowHelper.areAllStepBeforeThisAccepted(this.stepName, this.allStepNames, ai)
        }

        getStepStatus(ai: NTechPreCreditApi.ApplicationInfoModel) {
            return WorkflowHelper.getStepStatus(this.stepName, ai)
        }

        isStatusAccepted(ai: NTechPreCreditApi.ApplicationInfoModel) {
            return WorkflowHelper.isStepAccepted(this.stepName, ai)
        }

        areAllStepsAfterInitial(ai: NTechPreCreditApi.ApplicationInfoModel) {
            let passed = false
            for (let stepName of this.allStepNames) {
                if (this.currentStep.Name == stepName) {
                    passed = true
                } else if (passed && !WorkflowHelper.isStepInitial(stepName, ai)) {
                    return false
                }
            }
            return true
        }

        isStatusRejected(ai: NTechPreCreditApi.ApplicationInfoModel) {
            return WorkflowHelper.isStepRejected(this.stepName, ai)
        }

        getAcceptedListName() {
            return `${this.stepName}_${AcceptedName}`
        }

        getRejectedListName() {
            return `${this.stepName}_${RejectedName}`
        }

        getInitialListName() {
            return `${this.stepName}_${InitialName}`
        }

        isStatusInitial(ai: NTechPreCreditApi.ApplicationInfoModel) {
            return WorkflowHelper.isStepInitial(this.stepName, ai)
        }

        getCustomStepData<T>(): T {
            return this.currentStep.CustomData as T
        }
    }

    export interface WorkflowServerModel {
        WorkflowVersion: number
        Steps: WorkflowServerStepModel[]
        SeparatedWorkLists: WorkflowSeparatedWorkListModel[]
    }

    export interface WorkflowServerStepModel {
        Name: string, ComponentName: string, DisplayName: string, CustomData: any
    }

    export interface WorkflowSeparatedWorkListModel {
        ListName: string, ListDisplayName: string
    }
}