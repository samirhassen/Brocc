import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { CreditService, IAmortizationPlan, IAmortizationPlanItem } from '../../credit.service';
import { getAlternatePaymentPlanState } from '../../notifications-page/alt-paymentplan/alt-paymentplan.component';
import { ConfigService } from 'src/app/common-services/config.service';
import { NTechMath } from 'src/app/common-services/ntech.math';

@Component({
    selector: 'credit-amortization-plan',
    templateUrl: './credit-amortization-plan.component.html',
    styles: [],
})
export class CreditAmortizationPlanComponent implements OnInit {
    constructor(
        private creditService: CreditService,
        private apiService: NtechApiService,
        private eventService: NtechEventService,
        private configService: ConfigService
    ) {}

    @Input()
    public creditNr: string;

    public m: Model;

    ngOnInit(): void {}

    async ngOnChanges(changes: SimpleChanges): Promise<void> {
        this.reload();
    }

    private async reload() {
        this.m = null;

        if (!this.creditNr) {
            return;
        }
        
        await this.reloadFromPlan(await this.creditService.fetchAmortizationPlan(this.creditNr));
    }

    private async reloadFromPlan(plan: IAmortizationPlan) {
        let m: Model = plan as Model;
        m.totalInterestAmount = 0;
        m.totalCapitalAmount = 0;
        for (let item of m.items) {
            m.totalInterestAmount += item.interestTransaction;
            if (item.capitalTransaction > 0) {
                m.totalCapitalAmount += item.capitalTransaction;
            }
        }

        if(this.configService.isFeatureEnabled('ntech.feature.paymentplan')) {
            let alternatePaymentPlanState =  await getAlternatePaymentPlanState(this.apiService, this.creditNr);
            if(alternatePaymentPlanState.paymentPlanState && alternatePaymentPlanState.paymentPlanState.alternatePaymentPlanMonths.length > 0) {
                let months = alternatePaymentPlanState.paymentPlanState.alternatePaymentPlanMonths;
                m.activeAlternatePaymentPlan = {
                    untilDate: months[months.length - 1].dueDate,
                    expectedAmount: NTechMath.sum(months, x => x.monthAmount),
                    paidAmount: NTechMath.sum(alternatePaymentPlanState.paymentPlanState.paymentPlanPaidAmountsResult, x => x.paidAmount ?? 0)
                }
            } else {
                m.activeAlternatePaymentPlan = null;
            }
        } else {
            m.activeAlternatePaymentPlan = null;
        }

        this.m = m;
    }

    getItems(isFuture: boolean) {
        return this.m.items.filter((x) => x.isFutureItem === isFuture);
    }

    getAmortizationPlanPdfUrl(creditNr: string) {
        return this.apiService.getUiGatewayUrl('nCredit', 'Api/Credit/AmortizationPlanPdf', [['creditNr', creditNr]]);
    }

    getAmortizationItemDesc(i: IAmortizationPlanItem) {
        if (i.isWriteOff) {
            return 'Adjustment';
        } else {
            if (i.eventTypeCode == 'PlacedUnplacedIncomingPayment') {
                return 'Extra amortization';
            } else if (i.eventTypeCode == 'NewMortgageLoan') {
                return 'New Loan';
            } else if (i.eventTypeCode == 'CapitalizedInitialFee') {
                return 'Capitalized Initial Fee';
            } else if (i.eventTypeCode == 'NewNotification') {
                return 'Notification';
            } else if (i.eventTypeCode == 'NewAdditionalLoan') {
                return 'New Additional Loan';
            } else if(i.eventTypeCode == 'TerminationLetterProcessSuspension') {
                return 'Termination letter sent';
            } else if(i.eventTypeCode == 'TerminationLetterProcessReactivation') {
                return 'Termination letter lifted';
            } else {
                return i.eventTypeCode;
            }
        }
    }

    isAmountLessEvent(i: IAmortizationPlanItem) {
        return i.isTerminationLetterProcessSuspension === true || i.isTerminationLetterProcessReActivation === true;
    }

    async addFuturePaymentFreeMonth(item: IAmortizationPlanItem, evt?: Event) {
        evt?.preventDefault();
        let newPlan = await this.creditService.addFuturePaymentFreeMonth(this.creditNr, item);
        await this.reloadFromPlan(newPlan);
        this.eventService.signalReloadCreditComments(this.creditNr);
    }

    async cancelFuturePaymentFreeMonth(item: IAmortizationPlanItem, evt?: Event) {
        evt?.preventDefault();
        let newPlan = await this.creditService.cancelFuturePaymentFreeMonth(this.creditNr, item);
        await this.reloadFromPlan(newPlan);
        this.eventService.signalReloadCreditComments(this.creditNr);
    }
}

interface Model extends IAmortizationPlan {
    totalInterestAmount: number;
    totalCapitalAmount: number;
    activeAlternatePaymentPlan ?: {
        untilDate: Date
        expectedAmount: number
        paidAmount: number
    }
}
