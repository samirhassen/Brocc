import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { BehaviorSubject, Subscription } from 'rxjs';
import { TestFunctionsModel } from 'src/app/common-components/test-functions-popup/test-functions-popup.component';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechEventService, ReloadApplicationEventName } from 'src/app/common-services/ntech-event.service';
import { ApplicationCreditReportsInitialData } from '../../../shared-application-components/components/application-creditreports/application-creditreports.component';
import { ApplicationBasisComponentInitialData } from '../../components/application-basis/application-basis.component';
import { ApplicationPolicyInfoInitialData } from '../../components/application-policy-info/application-policy-info.component';
import { CreditCheckDecisionEditorInitialData } from '../../components/credit-check-decision-editor/credit-check-decision-editor.component';
import { StandardCreditApplicationModel } from '../../services/standard-credit-application-model';
import {
    CreditRecommendationModel,
    UnsecuredLoanApplicationApiService,
} from '../../services/unsecured-loan-application-api.service';
import { ApplicationSharedBankdataComponentInitialData } from 'src/app/shared-application-components/components/application-shared-bankdata/application-shared-bankdata.component';

@Component({
    selector: 'app-credit-check-new',
    templateUrl: './credit-check-new.component.html',
    styles: [],
})
export class CreditCheckNewComponent implements OnInit {
    constructor(
        private apiService: UnsecuredLoanApplicationApiService,
        private route: ActivatedRoute,
        private toastr: ToastrService,
        private eventService: NtechEventService,
        private configService: ConfigService
    ) {}

    public m: Model;

    private initSubs: Subscription[];

    ngOnInit(): void {
        this.initSubs = [];
        this.initSubs.push(
            this.eventService.applicationEvents.subscribe((x) => {
                if (x.eventCode === ReloadApplicationEventName && this.m?.application?.applicationNr === x.customData) {
                    this.reload(this.m.application.applicationNr, this.m.isNewCreditCheck);
                }
            })
        );

        this.reload(this.route.snapshot.params['applicationNr'], this.route.snapshot.data['isNewCreditCheck'] === true);
    }

    ngOnDestroy(): void {
        if (this.initSubs) {
            for (let sub of this.initSubs) {
                sub.unsubscribe();
            }
            this.initSubs = [];
        }
    }

    private reload(applicationNr: string, isNewCreditCheck: boolean) {
        this.apiService.fetchApplicationInitialData(applicationNr).then((applicationResult) => {
            if (applicationResult == 'noSuchApplicationExists') {
                this.toastr.warning('No such application exists');
            } else if (isNewCreditCheck) {
                this.apiService.newCreditCheck(applicationNr).then((creditCheckResult) => {
                    this.init(
                        applicationResult,
                        true,
                        creditCheckResult?.Recommendation,
                        creditCheckResult?.RecommendationTemporaryStorageKey
                    );
                });
            } else {
                this.init(applicationResult, false, applicationResult.getCurrentCreditDecisionRecommendation(), null);
            }
        });
    }

    private init(
        application: StandardCreditApplicationModel,
        isNewCreditCheck: boolean,
        recommendation: CreditRecommendationModel,
        recommendationTemporaryStorageKey: string
    ) {
        let testFunctions = new TestFunctionsModel();
        let isEditing = new BehaviorSubject<boolean>(false);

        let model: Model = {
            isNewCreditCheck: isNewCreditCheck,
            application: application,
            testFunctions: testFunctions,
            sharedIsEditing: isEditing,
            decisionEditorInitialData: isNewCreditCheck
                ? {
                      application: application,
                      sharedIsEditing: isEditing,
                      testFunctions: testFunctions,
                      loansToSettleAmount: application.getLoansToSettleAmount(),
                      recommendationTemporaryStorageKey: recommendationTemporaryStorageKey,
                      recommendation: recommendation,
                  }
                : null,
            applicationBasisInitialData: {
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
            policyInfoInitialData: {
                recommendation: recommendation,
                preScoreRecommendation: application.preScoreRecommendation
            },
        };

        if (this.configService.isNTechTest()) {
            this.setupTestFunctions(model.testFunctions);
        }

        if(this.configService.isFeatureEnabled('ntech.feature.unsecuredloans.datasharing')) {
            model.sharedBankDataInitialData = {
                application: application
            }
        }

        this.m = model;
    }

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
    application: StandardCreditApplicationModel;
    testFunctions: TestFunctionsModel;
    applicationBasisInitialData: ApplicationBasisComponentInitialData;
    sharedIsEditing: BehaviorSubject<boolean>;
    decisionEditorInitialData?: CreditCheckDecisionEditorInitialData;
    creditReportInitialData: ApplicationCreditReportsInitialData;
    policyInfoInitialData: ApplicationPolicyInfoInitialData;
    sharedBankDataInitialData ?: ApplicationSharedBankdataComponentInitialData
}
