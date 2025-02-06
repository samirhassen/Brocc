import { Component, Input, SimpleChanges } from '@angular/core';
import { LoanAmortizationPlan, LoanAmortizationPlanItem } from '../../../services/mypages-api.service';

const PageSize = 5;

@Component({
    selector: 'ul-amortization-plan',
    templateUrl: './ul-amortization-plan.component.html',
    styleUrls: ['./ul-amortization-plan.component.scss'],
})
export class UlAmortizationPlanComponent {
    constructor() {}

    @Input()
    public amortizationPlan: LoanAmortizationPlan;

    public m: Model;

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.amortizationPlan) {
            return;
        }

        let m: Model = {
            singlePaymentLoanRepaymentDays: this.amortizationPlan.SinglePaymentLoanRepaymentDays,
            futureTransactions: [],
            annuityAmount: this.amortizationPlan.AnnuityAmount,
            visibleCount: null,
        };
        for (let t of this.amortizationPlan.AmortizationPlanItems.filter((x) => x.IsFutureItem)) {
            m.futureTransactions.push({
                ...t,
                isExpanded: false,
                capitalAndInterestTransaction: t.CapitalTransaction + t.InterestTransaction,
            });
        }
        m.visibleCount = Math.min(m.futureTransactions.length, PageSize);
        this.m = m;
    }

    showMoreTransactions(m: Model, evt?: Event) {
        evt?.preventDefault();
        m.visibleCount = Math.min(m.futureTransactions.length, m.visibleCount + PageSize);
    }

    getVisibleTransactions() {
        return this.m.futureTransactions.filter((_, i) => i < this.m.visibleCount);
    }

    hasMoreTransactions() {
        return this.m.visibleCount < this.m.futureTransactions.length;
    }

    toggleTransactionDetails(i: LocalLoanAmortizationPlanItem, evt?: Event) {
        evt?.preventDefault();
        i.isExpanded = !i.isExpanded;
    }

    public getAmortizationItemDesc(i: LoanAmortizationPlanItem) {
        if (i.IsWriteOff) {
            return 'Justering';
        } else {
            if (i.EventTypeCode == 'PlacedUnplacedIncomingPayment') {
                return 'Extra amortering';
            } else if (i.EventTypeCode == 'NewCredit') {
                return 'Nytt lån';
            } else if (i.EventTypeCode == 'CapitalizedInitialFee') {
                return 'Kapitaliserad uppläggningsavgift';
            } else if (i.EventTypeCode == 'NewNotification') {
                return 'Avi';
            } else if (i.EventTypeCode == 'NewAdditionalLoan') {
                return 'Nytt tilläggslån';
            } else {
                return i.EventTypeCode;
            }
        }
    }
}

class Model {
    singlePaymentLoanRepaymentDays: number;
    futureTransactions: LocalLoanAmortizationPlanItem[];
    visibleCount: number;
    annuityAmount: number;
}

interface LocalLoanAmortizationPlanItem extends LoanAmortizationPlanItem {
    isExpanded: boolean;
    capitalAndInterestTransaction: number;
}
