import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, ValidatorFn, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { BehaviorSubject } from 'rxjs';
import { TestFunctionsModel } from 'src/app/common-components/test-functions-popup/test-functions-popup.component';
import { ConfigService } from 'src/app/common-services/config.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import {
    BasicRepaymentTimePaymentPlanRequest,
    NTechPaymentPlanService,
    PaymentPlan,
    RepaymentTimePaymentPlanRequest,
} from 'src/app/common-services/ntech-paymentplan.service';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { NTechMath } from 'src/app/common-services/ntech.math';
import { Dictionary, StringDictionary } from 'src/app/common.types';
import { CreditDecisionEditorTabsComponentInitialData } from 'src/app/shared-application-components/components/credit-decision-editor-tabs/credit-decision-editor-tabs.component';
import { CreditDecisionRejectionEditorComponentInitialData } from 'src/app/shared-application-components/components/credit-decision-rejection-editor/credit-decision-rejection-editor.component';
import { StandardCreditApplicationModel } from '../../services/standard-credit-application-model';
import {
    CreditRecommendationModel,
    UnsecuredLoanApplicationApiService,
} from '../../services/unsecured-loan-application-api.service';
import { PaymentOrderService } from 'src/app/common-services/payment-order-service';

@Component({
    selector: 'credit-check-decision-editor',
    templateUrl: './credit-check-decision-editor.component.html',
    styles: [],
})
export class CreditCheckDecisionEditorComponent implements OnInit {
    constructor(
        private fb: UntypedFormBuilder,
        private apiService: UnsecuredLoanApplicationApiService,
        private router: Router,
        private validationService: NTechValidationService,
        private toastr: ToastrService,
        private paymentPlanService: NTechPaymentPlanService,
        private config: ConfigService,
        private paymentOrderService: PaymentOrderService
    ) {}

    ngOnInit(): void {}

    @Input()
    public initialData: CreditCheckDecisionEditorInitialData;

    public m: Model;

    async ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (this.initialData == null) {
            return;
        }

        await this.init(this.initialData.application, this.initialData.recommendationTemporaryStorageKey);
    }

    private async init(application: StandardCreditApplicationModel, recommendationTemporaryStorageKey: string) {
        let f = (x: string, y: string, z: ValidatorFn[], requiresFeature?: string) =>
            !requiresFeature || this.config.isFeatureEnabled(requiresFeature) ? new OfferFieldViewModel(x, y, z) : null;
        let filterOutNulls = (x: OfferFieldViewModel[]) => x.filter((x) => x !== null);

        let vs = this.validationService;

        let isEditing = this.initialData.sharedIsEditing || new BehaviorSubject<boolean>(false);
        let initialMode = 'offer';
        let isRejectTabActive = new BehaviorSubject<boolean>(initialMode !== 'offer');
        let isCalculating = new BehaviorSubject<boolean>(false);

        let customCosts = await this.paymentOrderService.getCustomCosts();
        let model: Model = {
            rejectionEditorInitialData: {
                isActive: isRejectTabActive,
                onRejected: (x) => {
                    this.apiService
                        .setCurrentCreditDecision({
                            ApplicationNr: this.m.application.applicationNr,
                            WasAutomated: false,
                            SupressUserNotification: false,
                            Rejection: {
                                RejectionReasons: x.RejectionReasons,
                                OtherText: x.OtherText,
                            },
                            Offer: null,
                            RecommendationTemporaryStorageKey: this.m.recommendationTemporaryStorageKey,
                        })
                        .then((x) => {
                            this.router.navigate([
                                '/unsecured-loan-application/application/',
                                this.m.application.applicationNr,
                            ]);
                        });
                },
                isEditing: isEditing,
                recommendation: this.initialData.recommendation,
            },
            application: application,
            recommendationTemporaryStorageKey: recommendationTemporaryStorageKey,
            offerForm: null,
            offerFields: {
                column1: filterOutNulls([
                    f(
                        'Initial fee withheld',
                        'initialFeeWithheldAmount',
                        [vs.getPositiveIntegerValidator()],
                        'ntech.feature.standard.withheldInitialFee'
                    ),
                    f('Customer payment', 'paidToCustomerAmount', [
                        vs.getPositiveIntegerValidator(),
                        Validators.required,
                    ])
                ]),
                column2: filterOutNulls([
                    f(
                        'Initial fee capitalized',
                        'initialFeeCapitalizedAmount',
                        [vs.getPositiveIntegerValidator()],
                        'ntech.feature.standard.capitalizedInitialFee'
                    ),
                    f('Notification fee', 'notificationFee', [vs.getPositiveIntegerValidator(), Validators.required]),
                    f('Margin interest rate', 'marginInterestRate', [
                        vs.getPositiveDecimalValidator(),
                        Validators.required,
                    ]),
                ]),
                firstNotificationCosts: customCosts.map(customCost => new FirstNotificationCostOfferFieldViewModel(customCost.text, `customCost_${customCost.code}`, 
                    [vs.getPositiveIntegerValidator(), Validators.required], customCost.code)),
                loansToSettleAmount: this.initialData.loansToSettleAmount,
                referenceInterestRatePercent: this.initialData.application.getCurrentReferenceInterestRatePercent(),
            },
            isEditing: isEditing,
            tabsInitialData: {
                isCalculating: isCalculating,
                isRejectTabActive: isRejectTabActive,
                acceptTitle: 'Offer',
            },
        };
        
        let applicationRepaymentTime = vs.parseRepaymentTimeWithPeriodMarker(this.initialData.application
            .getComplexApplicationList('Application', true).getRow(1, true).getUniqueItem('requestedRepaymentTime'), true);
            
        let offerControls: Dictionary<any> = {
            'requestedRepaymentTimeInPeriod': ['', [vs.getPositiveIntegerValidator(), Validators.required]],
            'requestedRepaymentTimePeriod': [applicationRepaymentTime?.isDays === true ? 'd' : 'm', [Validators.required]],
        };
        for (let offerField of model.offerFields.column1.concat(model.offerFields.column2).concat(model.offerFields.firstNotificationCosts)) {
            offerControls[offerField.formControlName] = ['', offerField.validators];
        }
        model.offerForm = new FormsHelper(this.fb.group(offerControls));
        model.offerForm.form.valueChanges.subscribe((_) => {
            this.m.offerFields.computedPaymentPlan = this.computePaymentPlan();
            this.m.offerFields.calculatedLoanAmount = this.computeLoanAmount();
            this.m.offerFields.calculatedTotalInterestRatePercent = this.computeTotalInterestRate();
            this.checkHandlerLimit();
        });

        this.registerTestFunctions(this.initialData.testFunctions);

        this.m = model;

        this.m.offerFields.calculatedLoanAmount = this.computeLoanAmount();
        this.m.offerFields.calculatedTotalInterestRatePercent = this.computeTotalInterestRate();
    }

    accept(evt?: Event) {
        evt?.preventDefault();

        let paymentPlan = this.computePaymentPlan();

        let f = this.m.offerForm;
        let requestedRepaymentTimeInPeriod = this.validationService.parseIntegerOrNull(f.getValue('requestedRepaymentTimeInPeriod'));
        let isRequestedRepaymentTimeInPeriodDays = f.getValue('requestedRepaymentTimePeriod') === 'd';        
        if(isRequestedRepaymentTimeInPeriodDays) {
            if(requestedRepaymentTimeInPeriod < 10) {
                this.toastr.warning('Requested repayment time in days must be >= 10');
                return;
            }
            if(requestedRepaymentTimeInPeriod > 30) {
                this.toastr.warning('Requested repayment time in days must be <= 30');
                return;
            }
        }

        let firstNotificationCosts : { Code: string, Value: number }[] = [];
        for(let cost of this.m.offerFields.firstNotificationCosts) {
            let value = this.validationService.parseIntegerOrNull(f.getValue(cost.formControlName), true);
            if(value > 0) {
                firstNotificationCosts.push({ Code: cost.costCode, Value: value });
            }
        }       

        this.apiService
            .setCurrentCreditDecision({
                ApplicationNr: this.m.application.applicationNr,
                WasAutomated: false,
                SupressUserNotification: false,
                Rejection: null,
                Offer: {
                    SinglePaymentLoanRepaymentTimeInDays: isRequestedRepaymentTimeInPeriodDays ? requestedRepaymentTimeInPeriod : null,
                    AnnuityAmount: isRequestedRepaymentTimeInPeriodDays ? null : paymentPlan.MonthlyCostExcludingFeesAmount,
                    PaidToCustomerAmount: paymentPlan.PaidToCustomerAmount,
                    NotificationFeeAmount: paymentPlan.MonthlyFeeAmount,
                    SettleOtherLoansAmount: paymentPlan.LoansToSettleAmount,
                    InitialFeeCapitalizedAmount: paymentPlan.InitialFeeCapitalizedAmount,
                    InitialFeeDrawnFromLoanAmount: paymentPlan.InitialFeeWithheldAmount,
                    ReferenceInterestRatePercent: paymentPlan.ReferenceInterestRatePercent,
                    NominalInterestRatePercent: paymentPlan.MarginInterestRatePercent,
                    RepaymentTimeInMonths: isRequestedRepaymentTimeInPeriodDays ? null : requestedRepaymentTimeInPeriod,
                    FirstNotificationCosts: firstNotificationCosts,
                },
                RecommendationTemporaryStorageKey: this.m.recommendationTemporaryStorageKey,
            })
            .then(
                (onSuccess) => {
                    this.router.navigate([
                        '/unsecured-loan-application/application/',
                        this.m.application.applicationNr,
                    ]);
                },
                (onError) => {
                    this.toastr.error(`Could not accept offer: ${onError.statusText}`);
                }
            );
    }

    private computeLoanAmount() {
        let sum = 0;
        sum += this.m.offerFields.loansToSettleAmount;

        for (let name of ['initialFeeWithheldAmount', 'paidToCustomerAmount']) {
            let value = this.validationService.parseIntegerOrNull(this.m.offerForm.getValue(name));
            if (value !== null) {
                sum += value;
            }
        }

        return sum > 0 ? sum : null;
    }

    private getReferenceInterestRate() {
        return this.m.application.getCurrentReferenceInterestRatePercent();
    }

    private getMarginInterestRate() {
        return this.validationService.parsePositiveDecimalOrNull(this.m.offerForm.getValue('marginInterestRate'));
    }

    private computeTotalInterestRate() {
        let marginInterestRate = this.getMarginInterestRate();
        if (marginInterestRate == null) {
            return null;
        }
        return this.getReferenceInterestRate() + marginInterestRate;
    }

    private computePaymentPlan() {
        let f = this.m?.offerForm;
        if (!f || f.invalid()) {
            return null;
        }
        let v = this.validationService;

        let initialFeeOnFirstNotificationAmount = NTechMath.sum(this.m.offerFields.firstNotificationCosts, x => v.parseIntegerOrNull(f.getValue(x.formControlName)) ?? 0);

        let requestedRepaymentTimeInPeriod = v.parseIntegerOrNull(f.getValue('requestedRepaymentTimeInPeriod'));
        let isRequestedRepaymentTimeInPeriodDays = f.getValue('requestedRepaymentTimePeriod') === 'd';
        let requestBasic: BasicRepaymentTimePaymentPlanRequest = {
            loansToSettleAmount: this.m.offerFields.loansToSettleAmount,
            paidToCustomerAmount: v.parseIntegerOrNull(f.getValue('paidToCustomerAmount'), true),
            marginInterestRatePercent: this.getMarginInterestRate(),
            referenceInterestRatePercent: this.getReferenceInterestRate(),
            initialFeeWithheldAmount: v.parseIntegerOrNull(f.getValue('initialFeeWithheldAmount'), true),
            initialFeeCapitalizedAmount: v.parseIntegerOrNull(f.getValue('initialFeeCapitalizedAmount'), true),
            notificationFee: v.parseInteger(f.getValue('notificationFee')),
            initialFeeOnFirstNotificationAmount: initialFeeOnFirstNotificationAmount
        };
        if(isRequestedRepaymentTimeInPeriodDays) {
            return this.paymentPlanService.caclculatePlanWithSinglePaymentWithRepaymentTimeInDays(requestBasic, requestedRepaymentTimeInPeriod);
        } else {
            let request: RepaymentTimePaymentPlanRequest = {
                ...requestBasic,
                repaymentTimeInMonths: requestedRepaymentTimeInPeriod
            };

            return this.paymentPlanService.calculatePlanWithAnnuitiesFromRepaymentTime(request);
        }
    }

    public isSinglePaymentWithRepaymentTimeInDaysPaymentPlan() {
        if(!this.m?.offerFields?.computedPaymentPlan) {
            return false;
        }
        return this.m.offerFields.computedPaymentPlan.Payments.length === 1 && this.m.offerFields.computedPaymentPlan.Payments[0].nonStandardPaymentDays;
    }

    private checkHandlerLimit() {
        let form = this.m?.offerForm;
        if (form && !form.invalid()) {
            this.apiService.handlerLimitCheckAmount(this.m.offerFields.calculatedLoanAmount, true).then((x) => {
                this.m.handlerLimit = {
                    isOverHandlerLimit: x.isOverHandlerLimit,
                    isAllowedToOverrideHandlerLimit: x.isAllowedToOverrideHandlerLimit,
                };
            });
        }
    }

    overrideHandlerLimit() {
        let oldValue = this.m.handlerLimit.overrideHandlerLimit;
        this.m.handlerLimit.overrideHandlerLimit = !oldValue;
    }

    private registerTestFunctions(tf: TestFunctionsModel) {
        tf.addFunctionCall('Randomize offer', () => {
            let r = NTechMath.getRandomIntInclusive;
            this.m.tabsInitialData.isRejectTabActive.next(false);

            let loanAmountMode = r(1, 3); //1: only paid to customer, 2: only settle, 3: both

            //TODO: Respect handler limit when that is implemented
            //TODO: Multiplier for non SEK amounts
            let f = this.m.offerForm;
            let formEdit: StringDictionary = {
                paidToCustomerAmount: loanAmountMode === 1 || loanAmountMode === 3 ? r(10000, 100000).toString() : '0',
                requestedRepaymentTimeInPeriod: (6 * r(2, 20)).toString(),
                requestedRepaymentTimePeriod: 'm',
                notificationFee: r(1, 3) === 1 ? '0' : (5 * r(4, 20)).toString(),
                marginInterestRate: r(6, 25).toString(),
            };

            if (r(1, 3) !== 1) {
                //30% no initial fee
                let addCapitalized = () => (formEdit['initialFeeCapitalizedAmount'] = r(100, 1000).toString());
                let addWithheld = () => (formEdit['initialFeeWithheldAmount'] = r(100, 1000).toString());
                let isCapitalizedEnabled = f.hasFormControl(null, 'initialFeeCapitalizedAmount');
                let isWithheldEnabled = f.hasFormControl(null, 'initialFeeWithheldAmount');

                if (isCapitalizedEnabled && isWithheldEnabled && r(1, 2) == 1) {
                    addCapitalized(); // coin flip if both are enabled
                } else if (isCapitalizedEnabled) {
                    addCapitalized();
                } else if (isWithheldEnabled) {
                    addWithheld();
                }
            }

            f.form.patchValue(formEdit);
        });
        return tf;
    }
}

export class CreditCheckDecisionEditorInitialData {
    application: StandardCreditApplicationModel;
    sharedIsEditing: BehaviorSubject<boolean>;
    testFunctions: TestFunctionsModel;
    loansToSettleAmount: number;
    recommendationTemporaryStorageKey: string;
    recommendation: CreditRecommendationModel;
}

class Model {
    application: StandardCreditApplicationModel;
    recommendationTemporaryStorageKey: string;
    offerForm: FormsHelper;
    rejectionEditorInitialData: CreditDecisionRejectionEditorComponentInitialData;
    offerFields: {
        column1: OfferFieldViewModel[];
        column2: OfferFieldViewModel[];
        firstNotificationCosts: FirstNotificationCostOfferFieldViewModel[];
        computedPaymentPlan?: PaymentPlan;
        loansToSettleAmount: number;
        calculatedLoanAmount?: number;
        calculatedTotalInterestRatePercent?: number;
        referenceInterestRatePercent: number;
    };
    isEditing: BehaviorSubject<boolean>;
    handlerLimit?: {
        isOverHandlerLimit: boolean;
        isAllowedToOverrideHandlerLimit: boolean;
        overrideHandlerLimit?: boolean;
    };
    tabsInitialData: CreditDecisionEditorTabsComponentInitialData;
}

class OfferFieldViewModel {
    constructor(public labelText: string, public formControlName: string, public validators: ValidatorFn[]) {}
}

class FirstNotificationCostOfferFieldViewModel extends OfferFieldViewModel {
    constructor(public labelText: string, public formControlName: string, public validators: ValidatorFn[], public costCode: string) {
        super(labelText, formControlName, validators)
    }
}
