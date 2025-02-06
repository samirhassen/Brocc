import { Component, Input, SimpleChanges } from '@angular/core';
import { MlSeAmortizationBasisComponentInitialData, MlSeAmortizationBasisModel } from 'projects/ntech-components/src/public-api';
import { DateOnly, dateOnlyToIsoDate } from 'src/app/common.types';
import { LoanAmortizationPlan, LoanAmortizationPlanItem } from '../../../services/mypages-api.service';

const PageSize = 5;

@Component({
    selector: 'ml-amortization-plan',
    templateUrl: './ml-amortization-plan.component.html',
    styleUrls: ['./ml-amortization-plan.component.scss'],
})
export class MlAmortizationPlanComponent {
    constructor() {}

    @Input()
    public initialData: MlAmortizationPlanInitialData;

    public m: Model;

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let m: Model = {
            futureTransactions: [],
            annuityAmount: this.initialData.amortizationPlan.AnnuityAmount,
            visibleCount: null,
            amortizationBasisSeData:  {
                highlightCreditNr: this.initialData.highlightCreditNr,
                basis: this.initialData.amortizationBasisSeData
            }
        };
        for (let t of this.initialData.amortizationPlan.AmortizationPlanItems.filter((x) => x.IsFutureItem)) {
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

    toDate(d: DateOnly) {
        return dateOnlyToIsoDate(d);
    }
}

class Model {
    futureTransactions: LocalLoanAmortizationPlanItem[];
    visibleCount: number;
    annuityAmount: number;
    amortizationBasisSeData: MlSeAmortizationBasisComponentInitialData;
}

interface LocalLoanAmortizationPlanItem extends LoanAmortizationPlanItem {
    isExpanded: boolean;
    capitalAndInterestTransaction: number;
}

export interface MlAmortizationPlanInitialData {
    highlightCreditNr: string
    amortizationPlan: LoanAmortizationPlan;
    amortizationBasisSeData: MlSeAmortizationBasisModel;
}
