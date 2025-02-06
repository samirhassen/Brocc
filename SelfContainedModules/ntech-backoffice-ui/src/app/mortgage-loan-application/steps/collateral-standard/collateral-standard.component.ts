import { Component, Input, SimpleChanges } from '@angular/core';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import {
    getPossibleDocumentsForApplication,
    MortageLoanApplicationDocumentType,
} from '../../pages/application-documents/application-documents-model';
import { CollateralService } from '../../services/collateral-service';
import { MortgageLoanApplicationApiService } from '../../services/mortgage-loan-application-api.service';
import { StandardMortgageLoanApplicationModel } from '../../services/mortgage-loan-application-model';
import { WorkflowStepInitialData } from '../workflow-step';

@Component({
    selector: 'collateral-standard',
    templateUrl: './collateral-standard.component.html',
    styles: [],
})
export class CollateralStandardMLComponent {
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
            (x) => x.requireVerifiedWorkflowStep === this.initialData.workflow.step.currentStep.Name
        );

        let applicationDocuments = await this.apiService.fetchApplicationDocuments(application.applicationNr, [
            MortageLoanApplicationDocumentType,
        ]);

        let isOwnershipCheckApproved = CollateralService.isOwnershipCheckApproved(application);
        let m: Model = {
            application: application,
            documentControls: [],
            isPossibleToRevert: this.initialData.workflow.step.isRevertable(),
            isPossibleToApprove: false,
            isOwnershipCheckApproved: isOwnershipCheckApproved,
        };
        let areAllDocumentsVerified = true;
        for (let documentType of documentTypesRequired) {
            let applicationDocument = applicationDocuments.find((x) => x.DocumentSubType === documentType.displayName);
            let isAttached = !!applicationDocument;
            let isVerified = isAttached && !!applicationDocument.VerifiedDate;

            if (!isVerified) {
                areAllDocumentsVerified = false;
            }
            m.documentControls.push({
                displayName: documentType.displayName,
                documentId: applicationDocument?.DocumentId,
                isVerified: isVerified,
                isAttached: isAttached,
            });
        }

        m.isPossibleToApprove = isActiveAndCurrent && areAllDocumentsVerified && isOwnershipCheckApproved;

        if (this.config.isNTechTest()) {
            this.addTestFunctions();
        }
        this.m = m;
    }

    approve(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.m.application.applicationNr;
        this.apiService.setIsApprovedCollateralStep(applicationNr, true).then((x) => {
            this.eventService.signalReloadApplication(applicationNr);
        });
    }

    revert(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.m.application.applicationNr;
        this.apiService.setIsApprovedCollateralStep(applicationNr, false).then((x) => {
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

        tf.addFunctionCall('Attach and verify all documents', () => {
            let applicationNr = this.m.application.applicationNr;
            let promises: Promise<any>[] = [];
            this.m.documentControls.forEach((x) => {
                if (!x.isAttached) {
                    let dataUrl = tf.generateTestPdfDataUrl(x.displayName);
                    promises.push(
                        this.apiService
                            .addApplicationDocument(
                                applicationNr,
                                MortageLoanApplicationDocumentType,
                                x.displayName,
                                dataUrl,
                                `${x.displayName}-${applicationNr}.pdf`
                            )
                            .then((y) => {
                                return this.apiService.setApplicationDocumentVerified(
                                    applicationNr,
                                    y.DocumentId,
                                    true
                                );
                            })
                    );
                } else if (x.isAttached && !x.isVerified) {
                    promises.push(this.apiService.setApplicationDocumentVerified(applicationNr, x.documentId, true));
                }
            });
            Promise.all(promises).then(() => {
                this.eventService.signalReloadApplication(applicationNr);
            });
        });
    }
}

interface Model {
    application: StandardMortgageLoanApplicationModel;
    documentControls: DocumentControlModel[];
    isPossibleToApprove: boolean;
    isPossibleToRevert: boolean;
    isOwnershipCheckApproved: boolean;
}

interface DocumentControlModel {
    displayName: string;
    documentId: number;
    isVerified: boolean;
    isAttached: boolean;
}
