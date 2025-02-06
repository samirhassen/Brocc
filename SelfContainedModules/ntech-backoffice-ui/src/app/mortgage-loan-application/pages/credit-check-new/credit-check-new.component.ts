import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { BehaviorSubject } from 'rxjs';
import { TestFunctionsModel } from 'src/app/common-components/test-functions-popup/test-functions-popup.component';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechEventService, ReloadApplicationEventName } from 'src/app/common-services/ntech-event.service';
import { ApplicantDataEditorInitialData } from 'src/app/shared-application-components/components/applicant-data-editor/applicant-data-editor.component';
import { ApplicationCreditReportsInitialData } from 'src/app/shared-application-components/components/application-creditreports/application-creditreports.component';
import { HouseholdEconomyDataEditorInitialData } from 'src/app/shared-application-components/components/household-economy-data-editor/household-economy-data-editor.component';
import { ApplicationValuationsComponentInitialData } from '../../components/application-valuations/application-valuations.component';
import { MlApplicationPolicyInfoInitialData } from '../../components/ml-application-policy-info/ml-application-policy-info.component';
import { MlCreditCheckDecisionEditorComponentInitialData } from '../../components/ml-credit-check-decision-editor/ml-credit-check-decision-editor.component';
import {
    MlCreditRecommendationModel,
    MortgageLoanApplicationApiService,
} from '../../services/mortgage-loan-application-api.service';
import { StandardMortgageLoanApplicationModel } from '../../services/mortgage-loan-application-model';
import { MlAppGeneralDataEditorInitialData } from './ml-application-general-data-editor/ml-application-general-data-editor.component';
import { MlPropertyDataEditorInitialData } from './ml-property-editor/ml-property-editor.component';
import { PropertyLoansInitialData } from './property-loans/property-loans.component';

@Component({
    selector: 'app-credit-check-new',
    templateUrl: './credit-check-new.component.html',
    styles: [],
})
export class CreditCheckNewComponent implements OnInit {
    constructor(
        private apiService: MortgageLoanApplicationApiService,
        private route: ActivatedRoute,
        private eventService: NtechEventService,
        private toastr: ToastrService,
        private configService: ConfigService
    ) {}

    public m: Model;

    ngOnInit(): void {
        this.eventService.applicationEvents.subscribe((x) => {
            if (x.eventCode === ReloadApplicationEventName && this.m?.application?.applicationNr === x.customData) {
                this.reload(this.m.application.applicationNr, this.m.isNewCreditCheck, this.m.isFinal);
            }
        });

        this.reload(
            this.route.snapshot.params['applicationNr'],
            this.route.snapshot.data['isNewCreditCheck'] === true,
            this.route.snapshot.data['isFinal'] === true
        );
    }

    private reload(applicationNr: string, isNewCreditCheck: boolean, isFinal: boolean) {
        this.apiService.fetchApplicationInitialData(applicationNr).then((applicationResult) => {
            if (applicationResult == 'noSuchApplicationExists') {
                this.toastr.warning('No such application exists');
            } else if (isNewCreditCheck) {
                this.apiService.newCreditCheck(applicationNr).then((creditCheckResult) => {
                    this.init(
                        applicationResult,
                        true,
                        creditCheckResult?.Recommendation,
                        creditCheckResult?.RecommendationTemporaryStorageKey,
                        isFinal
                    );
                });
            } else {
                this.init(
                    applicationResult,
                    false,
                    applicationResult.getCurrentCreditDecisionRecommendation(isFinal),
                    null,
                    isFinal
                );
            }
        });
    }

    private init(
        application: StandardMortgageLoanApplicationModel,
        isNewCreditCheck: boolean,
        recommendation: MlCreditRecommendationModel,
        recommendationTemporaryStorageKey: string,
        isFinal: boolean
    ) {
        let testFunctions = new TestFunctionsModel();
        let isEditing = new BehaviorSubject<boolean>(false);

        let model: Model = {
            isNewCreditCheck: isNewCreditCheck,
            isFinal: isFinal,
            application: application,
            testFunctions: testFunctions,
            sharedIsEditing: isEditing,
            mainApplicantInitialData: {
                application: application,
                applicantNr: 1,
                applicantInfo: application.getApplicantInfo(1),
                forceReadonly: !isNewCreditCheck,
                sharedIsEditing: isEditing,
                apiService: this.apiService,
            },
            coApplicantInitialData:
                application.nrOfApplicants > 1
                    ? {
                          application: application,
                          applicantNr: 2,
                          applicantInfo: application.getApplicantInfo(2),
                          forceReadonly: !isNewCreditCheck,
                          sharedIsEditing: isEditing,
                          apiService: this.apiService,
                      }
                    : null,
            householdEconomyInitialData: {
                application: application,
                forceReadonly: !isNewCreditCheck,
                sharedIsEditing: isEditing,
                apiService: this.apiService,
            },
            propertyInitialData: {
                application: application,
                forceReadonly: !isNewCreditCheck,
                sharedIsEditing: isEditing,
            },
            policyInfoInitialData: {
                recommendation: recommendation,
            },
            applicationGeneralDataInitialData: {
                application: application,
                forceReadonly: !isNewCreditCheck,
                sharedIsEditing: isEditing,
            },
            creditReportInitialData: {
                customerIds: application.customerIdByApplicantNr,
                applicationNr: application.applicationNr,
                isActiveApplication: application.applicationInfo.IsActive,
                applicationApiService: this.apiService,
            },
            decisionEditorInitialData: {
                application: application,
                testFunctions: testFunctions,
                recommendationTemporaryStorageKey: recommendationTemporaryStorageKey,
                isFinalCreditCheck: isFinal,
                recommendation: recommendation,
            },
            valuationsInitialData: application.isPropertyValuationActive()
                ? {
                      application: application,
                  }
                : null,
            propertyLoansInitialData: {
                application: application,
                isReadOnly: !isNewCreditCheck,
            },
        };

        if (this.configService.isNTechTest()) {
            this.setupTestFunctions(model.testFunctions);
        }

        this.m = model;
    }

    getApplicantHeader = (applicantNr: number) => {
        const isMainApplicant = applicantNr === 1;
        const firstName = isMainApplicant
            ? this.m.mainApplicantInitialData.applicantInfo.FirstName
            : this.m.coApplicantInitialData.applicantInfo.FirstName;

        const birthDate = isMainApplicant
            ? this.m.mainApplicantInitialData.applicantInfo.BirthDate
            : this.m.coApplicantInitialData.applicantInfo.BirthDate;

        if (firstName !== null && firstName !== undefined && birthDate !== null && birthDate !== undefined)
            return `${firstName}, ${birthDate}`;

        if (firstName !== null && firstName !== undefined) return firstName;

        return isMainApplicant ? 'Main applicant' : 'Co applicant';
    };

    private setupTestFunctions(tf: TestFunctionsModel) {
        tf.addFunctionCall('Add policyfilter', () => {
            this.apiService.addTestPolicyFilterRuleSet(true).then((x) => {
                if (x.WasCreated) {
                    this.toastr.info('New ruleset added');
                } else {
                    this.toastr.warning('A ruleset was already present');
                }
            });
        });
    }
}

class Model {
    isNewCreditCheck: boolean;
    isFinal: boolean;
    application: StandardMortgageLoanApplicationModel;
    testFunctions: TestFunctionsModel;
    sharedIsEditing: BehaviorSubject<boolean>;
    mainApplicantInitialData: ApplicantDataEditorInitialData;
    coApplicantInitialData?: ApplicantDataEditorInitialData;
    householdEconomyInitialData: HouseholdEconomyDataEditorInitialData;
    propertyInitialData: MlPropertyDataEditorInitialData;
    applicationGeneralDataInitialData: MlAppGeneralDataEditorInitialData;
    creditReportInitialData: ApplicationCreditReportsInitialData;
    policyInfoInitialData: MlApplicationPolicyInfoInitialData;
    decisionEditorInitialData: MlCreditCheckDecisionEditorComponentInitialData;
    valuationsInitialData?: ApplicationValuationsComponentInitialData;
    propertyLoansInitialData: PropertyLoansInitialData;
}
