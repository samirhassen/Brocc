import { WorkflowStepHelper } from 'src/app/shared-application-components/services/workflow-helper';
import { StandardMortgageLoanApplicationModel } from '../../services/mortgage-loan-application-model';

const WaitingForAdditionalStepName = 'WaitingForAdditionalInfo';
const InitialCreditCheckStepName = 'InitialCreditCheck';
const CollateralStepName = 'Collateral';

export const MortageLoanApplicationDocumentType = 'MortgageLoanDocument';

export function getPossibleDocumentsForApplication(application: StandardMortgageLoanApplicationModel) {
    let getStep = (name: string) => {
        let step = application.workflow.Model.Steps.find((x) => x.Name === name);
        return new WorkflowStepHelper(application.workflow.Model, step, application.applicationInfo);
    };

    let applicationRow = application.getComplexApplicationList('Application', true).getRow(1, true);

    let isBrf = applicationRow.getUniqueItem('objectTypeCode') === 'seBrf';
    let isPurchase = applicationRow.getUniqueItemBoolean('isPurchase');

    let includedDocuments: MortgageApplicationDocumentType[] = [];
    for (let document of PossibleDocuments) {
        let include = true;

        if (document.showsUpAtWorkflowStep) {
            let step = getStep(document.showsUpAtWorkflowStep);
            if (!step.areAllStepBeforeThisAccepted()) {
                include = false;
            }
        }

        if ((document.onlyWhenIsBrf === true || document.onlyWhenIsBrf === false) && document.onlyWhenIsBrf !== isBrf) {
            include = false;
        }

        if (
            (document.onlyWhenIsPurchase === true || document.onlyWhenIsPurchase === false) &&
            document.onlyWhenIsPurchase !== isPurchase
        ) {
            include = false;
        }

        if (include) {
            includedDocuments.push(document);
        }
    }
    return includedDocuments;
}

export interface MortgageApplicationDocumentType {
    displayName: string;
    showsUpAtWorkflowStep?: string;
    onlyWhenIsBrf?: boolean;
    onlyWhenIsPurchase?: boolean;
    //The document must at least be attached to be allowed to pass this step
    requireAttachedWorkflowStep?: string;
    requireVerifiedWorkflowStep?: string;
}

const PossibleDocuments: MortgageApplicationDocumentType[] = [
    {
        displayName: 'Lägenhetsutdrag',
        showsUpAtWorkflowStep: WaitingForAdditionalStepName,
        onlyWhenIsBrf: true,
        onlyWhenIsPurchase: false,
        requireAttachedWorkflowStep: WaitingForAdditionalStepName,
        requireVerifiedWorkflowStep: CollateralStepName,
    },
    {
        displayName: 'Brf årsredovisning',
        showsUpAtWorkflowStep: WaitingForAdditionalStepName,
        onlyWhenIsBrf: true,
        requireAttachedWorkflowStep: WaitingForAdditionalStepName,
        requireVerifiedWorkflowStep: CollateralStepName,
    },
    {
        displayName: 'Fastighetsutdrag',
        showsUpAtWorkflowStep: InitialCreditCheckStepName,
        onlyWhenIsBrf: false,
        requireVerifiedWorkflowStep: CollateralStepName,
    },
    {
        displayName: 'Köpekontrakt',
        showsUpAtWorkflowStep: WaitingForAdditionalStepName,
        onlyWhenIsPurchase: true,
        onlyWhenIsBrf: false,
        requireAttachedWorkflowStep: WaitingForAdditionalStepName,
        requireVerifiedWorkflowStep: CollateralStepName,
    },
    {
        displayName: 'Överlåtelseavtal',
        showsUpAtWorkflowStep: WaitingForAdditionalStepName,
        onlyWhenIsBrf: true,
        onlyWhenIsPurchase: true,
        requireAttachedWorkflowStep: WaitingForAdditionalStepName,
        requireVerifiedWorkflowStep: CollateralStepName,
    },
    {
        displayName: 'Objektbeskrivning',
        showsUpAtWorkflowStep: WaitingForAdditionalStepName,
        onlyWhenIsPurchase: true,
        requireAttachedWorkflowStep: WaitingForAdditionalStepName,
        requireVerifiedWorkflowStep: CollateralStepName,
    },
    {
        displayName: 'Lönespecifikation',
        showsUpAtWorkflowStep: WaitingForAdditionalStepName,
    },
    {
        displayName: 'Arbetsgivarintyg',
        showsUpAtWorkflowStep: WaitingForAdditionalStepName,
    },
    {
        displayName: 'Amorteringsunderlag',
        showsUpAtWorkflowStep: WaitingForAdditionalStepName,
        onlyWhenIsPurchase: false,
        requireAttachedWorkflowStep: WaitingForAdditionalStepName,
    },
    {
        displayName: 'Likvidavräkning',
        showsUpAtWorkflowStep: WaitingForAdditionalStepName,
        onlyWhenIsPurchase: true,
    },
    {
        displayName: 'Köpebrev',
        showsUpAtWorkflowStep: WaitingForAdditionalStepName,
        onlyWhenIsPurchase: true,
    },
];
