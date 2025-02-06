import { Component, Input, SimpleChanges } from '@angular/core';
import { ConfigService } from 'src/app/common-services/config.service';
import { DateOnly, dateOnlyToIsoDate } from 'src/app/common.types';
import { CreditService, IAmortizationPlan, IAmortizationPlanItem } from '../../credit.service';

@Component({
    selector: 'mortgage-amortization-plan',
    templateUrl: './mortgage-amortization-plan.component.html',
    styles: [],
})
export class MortgageAmortizationPlanComponent {
    constructor(private creditService: CreditService, private config: ConfigService) {}

    @Input()
    public creditNr: string;

    public m: Model;

    async ngOnChanges(changes: SimpleChanges): Promise<void> {
        this.reload(this.creditNr);
    }

    private async reload(creditNr: string) {
        this.m = null;

        if (!this.creditNr) {
            return;
        }

        let creditDetails = (await this.creditService.getCreditDetails(creditNr))?.details;
        let m: Model = {
            isSweden: this.config.baseCountry() === 'SE',
            creditNr: creditNr,
            currentLoanAmount: creditDetails.capitalDebtAmount,
            currentFixedMonthlyCapitalPayment: null,
            annuityAmount: null,
        };

        //Since 0 is falsy but also a valid monthly payment for mortgage loans
        let isNumberPresent = (x: number) => x !== undefined && x !== null;

        if (isNumberPresent(creditDetails.currentFixedMonthlyCapitalPayment)) {
            m.currentFixedMonthlyCapitalPayment = creditDetails.currentFixedMonthlyCapitalPayment;
        } else if (isNumberPresent(creditDetails.annuityAmount)) {
            m.annuityAmount = creditDetails.annuityAmount;
        }

        this.m = m;
    }

    asDate(d: DateOnly) {
        return dateOnlyToIsoDate(d);
    }

    async toggleAmortizationPlan(evt?: Event) {
        evt?.preventDefault();

        if (this.m.amortPlan) {
            this.m.amortPlan = null;
            return;
        }

        let amortizationPlan = await this.creditService.fetchAmortizationPlan(this.creditNr);
        let extendedPlan: IExtendedAmortizationPlan = {
            ...amortizationPlan,
            totalInterestAmount: 0,
            totalCapitalAmount: 0,
        };
        for (let item of amortizationPlan.items) {
            extendedPlan.totalCapitalAmount += item.capitalTransaction > 0 ? item.capitalTransaction : 0;
            extendedPlan.totalInterestAmount += item.interestTransaction;
        }

        this.m.amortPlan = extendedPlan;
    }

    getAmortItems(isFutureItem: boolean) {
        return this.m.amortPlan ? this.m.amortPlan.items.filter((x) => x.isFutureItem === isFutureItem) : null;
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
            } else {
                return i.eventTypeCode;
            }
        }
    }
}

interface Model {
    isSweden: boolean;
    creditNr: string;
    amortPlan?: IExtendedAmortizationPlan;
    currentLoanAmount: number;
    annuityAmount: number;
    currentFixedMonthlyCapitalPayment: number;
}

export interface IExtendedAmortizationPlan extends IAmortizationPlan {
    totalInterestAmount: number;
    totalCapitalAmount: number;
}
