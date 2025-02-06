import { Component, Input, SimpleChanges } from '@angular/core';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { getNumberDictionaryValues } from 'src/app/common.types';
import {
    getApplicantsKycStatusModel,
    KycStepApplicantStatusesComponentInitialData,
} from 'src/app/shared-application-components/components/kyc-step-applicant-statuses/kyc-step-applicant-statuses.component';
import { createRandomizedKycQuestionAnswersForApplicants } from 'src/app/shared-application-components/services/kyc-question-test-generator';
import { StandardCreditApplicationModel } from '../../services/standard-credit-application-model';
import {
    UnsecuredLoanApplicationApiService,
    UnsecuredLoanApplicationKycQuestionSourceType,
} from '../../services/unsecured-loan-application-api.service';
import { WorkflowStepComponent, WorkflowStepInitialData } from '../workflow-step';

@Component({
    selector: 'kyc-standard',
    templateUrl: './kyc-standard.component.html',
    styles: [],
})
export class KycStandardComponent implements WorkflowStepComponent {
    constructor(
        private config: ConfigService,
        private apiService: UnsecuredLoanApplicationApiService,
        private eventService: NtechEventService
    ) {}

    public m: Model;

    @Input()
    public initialData: WorkflowStepInitialData;

    ngOnChanges(changes: SimpleChanges): void {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let applicaton = this.initialData.application;
        let ai = applicaton.applicationInfo;

        let applicantNrs = this.getApplicantNrs(applicaton);
        this.apiService
            .fetchKycCustomerOnboardingStatuses(
                getNumberDictionaryValues(applicaton.customerIdByApplicantNr),
                UnsecuredLoanApplicationKycQuestionSourceType,
                applicaton.applicationNr
            )
            .then((x) => {
                let applicantStatuses = applicantNrs.map((applicantNr) => {
                    let customerId = applicaton.customerIdByApplicantNr[applicantNr];
                    return x[customerId];
                });

                let kycStatus = getApplicantsKycStatusModel(applicantStatuses);

                let isStepActiveAndCurrent =
                    ai.IsActive &&
                    this.initialData.workflow.step.areAllStepBeforeThisAccepted() &&
                    this.initialData.workflow.step.isStatusInitial();

                this.m = {
                    application: this.initialData.application,
                    hasAnsweredQuestions: kycStatus.hasAnsweredQuestions,
                    isPossibleToApprove: isStepActiveAndCurrent && kycStatus.hasStatusThatAllowsApprove,
                    isPossibleToRevert: this.initialData.workflow.step.isRevertable(),
                    kycStatusesInitialData: {
                        applicationNr: this.initialData.application.applicationNr,
                        customerStatuses: applicantStatuses,
                        applicationNavigationTarget: this.initialData.applicationNavigationTarget,
                        apiService: this.apiService,
                        isStepActiveAndCurrent: isStepActiveAndCurrent,
                        allConnectedCustomerIdsWithRoles: null,
                    },
                };

                this.addTestFunctions();
            });
    }

    getApplicantNrs(application: StandardCreditApplicationModel) {
        let applicantNrs: number[] = [];
        for (var applicantNr = 1; applicantNr <= application.nrOfApplicants; applicantNr++) {
            applicantNrs.push(applicantNr);
        }
        return applicantNrs;
    }

    addTestFunctions() {
        let workflowStep = this.initialData.workflow.step;
        let ai = this.initialData.application.applicationInfo;
        if (!workflowStep.areAllStepBeforeThisAccepted() || !workflowStep.isStatusInitial() || !ai.IsActive) {
            return;
        }
        let tf = this.initialData.testFunctions;
        tf.addFunctionCall('Generate kyc question answers', () => {
            let applicaton = this.initialData.application;
            let now = this.config.getCurrentDateAndTime();
            let questionSets = createRandomizedKycQuestionAnswersForApplicants(
                applicaton.customerIdByApplicantNr,
                now,
                this.config.getClient().BaseCountry
            );
            this.apiService
                .addKycCustomerQuestionsSets(
                    questionSets,
                    UnsecuredLoanApplicationKycQuestionSourceType,
                    applicaton.applicationNr
                )
                .then((x) => {
                    this.eventService.signalReloadApplication(applicaton.applicationNr);
                });
        });
        tf.addFunctionCall('Set local pep and sanction to no for all applicants', () => {
            let applicaton = this.initialData.application;
            let items = [];
            for (let applicantNr of this.getApplicantNrs(applicaton)) {
                let customerId = applicaton.customerIdByApplicantNr[applicantNr];
                items.push({
                    customerId,
                    name: 'localIsPep',
                    group: 'pepKyc',
                    value: 'false',
                    isSensitive: true,
                });
                items.push({
                    customerId,
                    name: 'localIsSanction',
                    group: 'sanction',
                    value: 'false',
                    isSensitive: true,
                });
            }
            this.apiService.updateCustomerProperties(items, true).then((x) => {
                this.eventService.signalReloadApplication(applicaton.applicationNr);
            });
        });
    }

    approve(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.initialData.application.applicationNr;
        this.apiService.setIsApprovedKycStep(applicationNr, true).then((x) => {
            this.eventService.signalReloadApplication(applicationNr);
        });
    }

    revert(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.initialData.application.applicationNr;
        this.apiService.setIsApprovedKycStep(applicationNr, false).then((x) => {
            this.eventService.signalReloadApplication(applicationNr);
        });
    }
}

class Model {
    application: StandardCreditApplicationModel;
    hasAnsweredQuestions: boolean;
    isPossibleToApprove: boolean;
    isPossibleToRevert: boolean;
    kycStatusesInitialData: KycStepApplicantStatusesComponentInitialData;
}
