import { Component, Input, SimpleChanges } from '@angular/core';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { getDictionaryValues } from 'src/app/common.types';
import { createRandomizedKycQuestionAnswersForApplicants } from 'src/app/shared-application-components/services/kyc-question-test-generator';
import {
    getPossibleDocumentsForApplication,
    MortageLoanApplicationDocumentType,
} from '../../pages/application-documents/application-documents-model';
import {
    MortgageLoanApplicationApiService,
    ServerDocumentModel,
} from '../../services/mortgage-loan-application-api.service';
import { StandardMortgageLoanApplicationModel } from '../../services/mortgage-loan-application-model';
import { WorkflowStepInitialData } from '../workflow-step';

@Component({
    selector: 'waiting-for-info-standard',
    templateUrl: './waiting-for-info-standard.component.html',
    styles: [],
})
export class WaitingForInfoStandardComponent {
    constructor(
        private apiService: MortgageLoanApplicationApiService,
        private eventService: NtechEventService,
        private config: ConfigService
    ) {}

    @Input()
    public initialData: WorkflowStepInitialData;

    public m: Model;

    async ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let wf = this.initialData.workflow;

        if (!wf.step.areAllStepBeforeThisAccepted()) {
            return;
        }

        let application = this.initialData.application;

        let isActiveAndCurrent =
            application.applicationInfo.IsActive && wf.step.areAllStepBeforeThisAccepted() && wf.step.isStatusInitial();

        let documentTypesRequired = getPossibleDocumentsForApplication(application).filter(
            (x) => x.requireAttachedWorkflowStep === this.initialData.workflow.step.currentStep.Name
        );

        let applicationDocuments = await this.apiService.fetchApplicationDocuments(application.applicationNr, [
            MortageLoanApplicationDocumentType,
        ]);
        let applicationRow = this.initialData.application
            .getComplexApplicationList('Application', true)
            .getRow(1, true);

        let hasAnsweredKycQuestions = applicationRow.getUniqueItemBoolean('hasAnsweredKycQuestions');

        let m: Model = {
            application: application,
            documentControls: [],
            isPossibleToRevert: this.initialData.workflow.step.isRevertable(),
            isPossibleToApprove: false,
            hasAnsweredKycQuestions: hasAnsweredKycQuestions,
        };
        let areAllDocumentsAttached = true;
        for (let documentType of documentTypesRequired) {
            let applicationDocument = applicationDocuments.find((x) => x.DocumentSubType === documentType.displayName);
            let isAttached = !!applicationDocument;
            if (!isAttached) {
                areAllDocumentsAttached = false;
            }
            m.documentControls.push({
                displayName: documentType.displayName,
                isAttached: isAttached,
            });
        }

        m.isPossibleToApprove = isActiveAndCurrent && areAllDocumentsAttached && hasAnsweredKycQuestions;

        if (this.config.isNTechTest()) {
            this.addTestFunctions();
        }
        this.m = m;
    }

    approve(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.m.application.applicationNr;
        this.apiService.setIsApprovedWaitingForAdditionalInfoStep(applicationNr, true).then((x) => {
            this.eventService.signalReloadApplication(applicationNr);
        });
    }

    revert(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.m.application.applicationNr;
        this.apiService.setIsApprovedWaitingForAdditionalInfoStep(applicationNr, false).then((x) => {
            this.eventService.signalReloadApplication(applicationNr);
        });
    }

    addTestFunctions() {
        let workflowStep = this.initialData.workflow.step;
        let ai = this.initialData.application.applicationInfo;
        if (!workflowStep.areAllStepBeforeThisAccepted() || !workflowStep.isStatusInitial() || !ai.IsActive) {
            return;
        }
        let tf = this.initialData.testFunctions;

        tf.addFunctionCall('Attach all documents', () => {
            let applicationNr = this.m.application.applicationNr;
            let promises: Promise<ServerDocumentModel>[] = [];
            this.m.documentControls.forEach((x) => {
                if (!x.isAttached) {
                    let dataUrl = tf.generateTestPdfDataUrl(x.displayName);
                    promises.push(
                        this.apiService.addApplicationDocument(
                            applicationNr,
                            MortageLoanApplicationDocumentType,
                            x.displayName,
                            dataUrl,
                            `${x.displayName}-${applicationNr}.pdf`
                        )
                    );
                }
            });
            Promise.all(promises).then(() => {
                this.eventService.signalReloadApplication(applicationNr);
            });
        });

        tf.addFunctionCall('Generate kyc question answers', async () => {
            let applicaton = this.initialData.application;
            let questionSets = createRandomizedKycQuestionAnswersForApplicants(
                applicaton.customerIdByApplicantNr,
                this.config.getCurrentDateAndTime(),
                this.config.getClient().BaseCountry
            );
            await this.apiService
                .api()
                .post('nPreCredit', 'api/MortgageLoanStandard/CustomerPages/Answer-Kyc-Questions', {
                    CustomerId: applicaton.customerIdByApplicantNr[1],
                    ApplicationNr: applicaton.applicationNr,
                    Customers: getDictionaryValues(applicaton.customerIdByApplicantNr).map((customerId) => ({
                        CustomerId: customerId,
                        Answers: questionSets.find((x) => x.CustomerId === customerId).Items,
                    })),
                });
            await this.eventService.signalReloadApplication(applicaton.applicationNr);
        });
    }
}

interface Model {
    application: StandardMortgageLoanApplicationModel;
    documentControls: DocumentControlModel[];
    isPossibleToApprove: boolean;
    isPossibleToRevert: boolean;
    hasAnsweredKycQuestions: boolean;
}

interface DocumentControlModel {
    displayName: string;
    isAttached: boolean;
}
