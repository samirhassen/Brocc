import { Component, Input, SimpleChanges } from '@angular/core';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { getDictionaryValues, getNumberDictionaryKeys } from 'src/app/common.types';
import {
    getApplicantsKycStatusModel,
    KycStepApplicantStatusesComponentInitialData,
} from 'src/app/shared-application-components/components/kyc-step-applicant-statuses/kyc-step-applicant-statuses.component';
import {
    MortgageLoanApplicationApiService,
    MortgageLoanApplicationKycQuestionSourceType,
} from '../../services/mortgage-loan-application-api.service';
import { StandardMortgageLoanApplicationModel } from '../../services/mortgage-loan-application-model';
import { WorkflowStepInitialData } from '../workflow-step';

@Component({
    selector: 'kyc-standard',
    templateUrl: './kyc-standard.component.html',
    styles: [],
})
export class KycStandardMLComponent {
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

        let application = this.initialData.application;

        let ai = application.applicationInfo;

        let screenedRoles = ['Applicant', 'mortgageLoanPropertyOwner', 'mortgageLoanConsentingParty'];
        let isScreenedRole = (role: string) => screenedRoles.includes(role);
        let allConnectedCustomerIdsWithRoles = application.getAllConnectedCustomerIdsWithRoles();
        let screenedCustomerIds = getNumberDictionaryKeys(allConnectedCustomerIdsWithRoles).filter((x) =>
            allConnectedCustomerIdsWithRoles[x].find(isScreenedRole)
        );

        let result = await this.apiService.fetchKycCustomerOnboardingStatuses(
            screenedCustomerIds,
            MortgageLoanApplicationKycQuestionSourceType,
            application.applicationNr
        );

        let customerStatuses = getDictionaryValues(result);
        let kycStatus = getApplicantsKycStatusModel(customerStatuses);

        let isStepActiveAndCurrent =
            ai.IsActive &&
            this.initialData.workflow.step.areAllStepBeforeThisAccepted() &&
            this.initialData.workflow.step.isStatusInitial();

        let applicationRow = this.initialData.application
            .getComplexApplicationList('Application', true)
            .getRow(1, true);
        let hasAnsweredQuestions = applicationRow.getUniqueItemBoolean('hasAnsweredKycQuestions');
        this.m = {
            application: this.initialData.application,
            isPossibleToApprove: isStepActiveAndCurrent && kycStatus.hasStatusThatAllowsApprove && hasAnsweredQuestions,
            isPossibleToRevert: this.initialData.workflow.step.isRevertable(),
            kycStatusesInitialData: {
                applicationNr: this.initialData.application.applicationNr,
                customerStatuses: customerStatuses,
                applicationNavigationTarget: this.initialData.applicationNavigationTarget,
                apiService: this.apiService,
                isStepActiveAndCurrent: isStepActiveAndCurrent,
                allConnectedCustomerIdsWithRoles: allConnectedCustomerIdsWithRoles,
            },
        };

        if (this.config.isNTechTest()) {
            this.addTestFunctions();
        }
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

    addTestFunctions() {
        let workflowStep = this.initialData.workflow.step;
        let ai = this.initialData.application.applicationInfo;
        if (!workflowStep.areAllStepBeforeThisAccepted() || !workflowStep.isStatusInitial() || !ai.IsActive) {
            return;
        }
        let tf = this.initialData.testFunctions;

        tf.addFunctionCall('Set local pep and sanction to no for all applicants', async () => {
            let applicaton = this.initialData.application;
            let isDefined = (x: boolean) => x === true || x === false;
            let customerIdsMissingAnswers = this.m.kycStatusesInitialData.customerStatuses
                .filter((x) => !isDefined(x.IsPep) || !isDefined(x.IsSanction))
                .map((x) => x.CustomerId);
            let items = [];
            for (let customerId of customerIdsMissingAnswers) {
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
            await this.apiService.updateCustomerProperties(items, true);
            await this.eventService.signalReloadApplication(applicaton.applicationNr);
        });
    }
}

class Model {
    application: StandardMortgageLoanApplicationModel;
    isPossibleToApprove: boolean;
    isPossibleToRevert: boolean;
    kycStatusesInitialData: KycStepApplicantStatusesComponentInitialData;
}
