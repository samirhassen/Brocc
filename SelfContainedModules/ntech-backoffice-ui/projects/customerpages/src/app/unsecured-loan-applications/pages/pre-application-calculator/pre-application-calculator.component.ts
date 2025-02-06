import { Component, OnInit } from '@angular/core';
import { UlStandardPreApplicationService, UlStandardWebApplicationEnabledSettings } from '../../services/pre-application.service';
import { Router } from '@angular/router';
import { CalculatorSliderInitialData } from '../../../shared-components/calculator-slider/calculator-slider.component';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { UntypedFormBuilder } from '@angular/forms';
import { CustomerpagesShellInitialData } from '../../../shared-components/customerpages-shell/customerpages-shell.component';
import { CustomerPagesValidationService } from '../../../common-services/customer-pages-validation.service';
import { BasicRepaymentTimePaymentPlanRequest, NTechPaymentPlanService, PaymentPlan } from 'src/app/common-services/ntech-paymentplan.service';
import { RepaymentTimeWithPeriodMarker } from 'src/app/common-services/ntech-validation-shared-base.service';

@Component({
    selector: 'np-pre-application-calculator',
    templateUrl: './pre-application-calculator.component.html',
    styleUrls: ['./pre-application-calculator.component.scss']
})
export class PreApplicationCalculatorComponent implements OnInit {
    constructor(private storageService: UlStandardPreApplicationService, private router: Router,
        private formBuilder: UntypedFormBuilder, private validationService: CustomerPagesValidationService, private paymentPlanService: NTechPaymentPlanService) { }

    public shellData: CustomerpagesShellInitialData = {
        logoRouterLink: null,
        skipBodyLayout: false,
        wideNavigation: false
    };

    public hasNoActiveProducts: boolean;
    public m: Model;

    async ngOnInit() {
        this.m = null;

        let {isEnabled, settings} = await this.storageService.getUlStandardWebApplicationSettings();
        if(!isEnabled) {
            this.hasNoActiveProducts = true;
            return;
        }


        if((settings.loanAmounts ?? []).length === 0 || (settings.repaymentTimes ?? []).length === 0) {
            this.hasNoActiveProducts = true;
            return;
        } else {
            this.hasNoActiveProducts = false;
        }

        let form: FormsHelper = new FormsHelper(this.formBuilder.group({
            'loanAmountIndex': [0, []],
            'repaymentTimeIndex': [0, []]
        }));

        let m: Model = {
            settings: settings,
            loanAmountSliderData: {
                minValue: 0,
                maxValue: settings.loanAmounts.length - 1,
                stepSize: 1,
                tickButtonStepSize: null,
                formControlName: 'loanAmountIndex',
                form: form
            },
            repaymentTimeSliderData: {
                minValue: 0,
                maxValue: settings.repaymentTimes.length - 1,
                stepSize: 1,
                tickButtonStepSize: null,
                formControlName: 'repaymentTimeIndex',
                form: form
            },
            form: form
        }

        this.recalculatePaymentPlan(m);

        m.form.form.valueChanges.subscribe(_ => {
            this.recalculatePaymentPlan(m);
        });

        this.m = m;
    }

    private recalculatePaymentPlan(m: Model) {
        let repaymentTime = this.getRepaymentTime(m);
        let loanAmount = this.getLoanAmount(m);
        let basicRequest : BasicRepaymentTimePaymentPlanRequest = {
            loansToSettleAmount: 0,
            paidToCustomerAmount: loanAmount,
            marginInterestRatePercent: m.settings.exampleMarginInterestRatePercent,
            referenceInterestRatePercent: 0,
            initialFeeWithheldAmount: m.settings.exampleInitialFeeWithheldAmount,
            initialFeeCapitalizedAmount: m.settings.exampleInitialFeeCapitalizedAmount,
            initialFeeOnFirstNotificationAmount: m.settings.exampleInitialFeeOnFirstNotificationAmount,
            notificationFee: m.settings.exampleNotificationFee
        }

        if(repaymentTime.isDays) {
            m.calculatedPaymentPlan = this.paymentPlanService.caclculatePlanWithSinglePaymentWithRepaymentTimeInDays(basicRequest, repaymentTime.repaymentTime);
        } else {
            m.calculatedPaymentPlan = this.paymentPlanService.calculatePlanWithAnnuitiesFromRepaymentTime({
                ...basicRequest,
                repaymentTimeInMonths: repaymentTime.repaymentTime
            })
        }
    }

    private getItemByIndex<T>(items: T[], index: number) {
        if(index < 0) {
            return items[0];
        } else if(index >= items.length) {
            return items[items.length - 1];
        } else {
            return items[index];
        }
    }

    formatRepaymentTimeForDisplay(value: RepaymentTimeWithPeriodMarker) {
        return `${value.repaymentTime} ${value.isDays ? ' dagar' : ' månader'}`;
    }

    getRepaymentTime(m: Model) {
        let index: number = m.form.getValue('repaymentTimeIndex');
        let rawValue = this.getItemByIndex(m.settings.repaymentTimes, index);
        return this.validationService.parseRepaymentTimeWithPeriodMarker(rawValue, false);
    }

    getLoanAmount(m: Model) {
        let index: number = m.form.getValue('loanAmountIndex');
        return this.getItemByIndex(m.settings.loanAmounts, index);
    }

    getTermsBoxPaymentPlanInitialFee() {
        let p = this.m.calculatedPaymentPlan;
        if(!p) {
            return 0;
        }
        let amount = p.InitialFeeCapitalizedAmount ?? 0 + p.InitialFeeWithheldAmount ?? 0;
        if(p.Payments.length > 0) {
            amount +=  + p.Payments[0].initialFee ?? 0;
        }
        return amount;
    }

    getTermsBoxNotificationFee() {
        let p = this.m.calculatedPaymentPlan;
        if(!p) {
            return 0;
        }
        if(p.Payments.length > 0) {
            return p.Payments[0].monthlyFee;
        } else {
            return 0;
        }
    }

    public async apply(evt ?: Event) {
        evt?.preventDefault();
        let repaymentTime = this.getRepaymentTime(this.m);
        let preApplicationId = await this.storageService.beginPreApplication({
            requestedLoanAmount: this.getLoanAmount(this.m),
            requestedRepaymentTime: this.validationService.formatRepaymentTimeWithPeriodMarkerForStorage(repaymentTime.repaymentTime, repaymentTime.isDays)
        });
        this.router.navigate(['ul-webapplications/application-applicants/' + preApplicationId]);
    }
}

interface Model {
    settings: UlStandardWebApplicationEnabledSettings
    form: FormsHelper
    loanAmountSliderData: CalculatorSliderInitialData
    repaymentTimeSliderData: CalculatorSliderInitialData
    calculatedPaymentPlan ?: PaymentPlan
}
