import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { CapitalTransactionHistory } from '../../../services/mypages-api.service';

@Component({
    selector: 'capital-transaction-history',
    templateUrl: './capital-transaction-history.component.html',
    styles: ['.transaction-date { display: block; font-size: small; padding-top: 5px; }'],
})
export class CapitalTransactionHistoryComponent implements OnInit {
    constructor() {}

    @Input()
    public capitalTransactionHistory: CapitalTransactionHistory;

    public m: Model;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.capitalTransactionHistory) {
            return;
        }

        this.m = {
            items: (this.capitalTransactionHistory.Transactions ?? []).map((x) => {
                return {
                    date: x.TransactionDate,
                    amount: x.Amount,
                    description: this.getDescription(x.BusinessEventType, x.BusinessEventRoleCode, x.SubAccountCode),
                };
            }),
        };
        this.showMoreItems(null);
    }

    private getDescription(businessEventType: string, businessEventRoleCode: string, subAccountCode: string) {
        switch (businessEventType) {
            case 'NewIncomingPaymentFile':
                return 'Amortering';
            case 'PlacedUnplacedIncomingPayment':
                return 'Amortering';
            case 'NewCredit':
                switch (subAccountCode) {
                    case 'initialFeeWithheld':
                        return 'Uppläggningsavgift';
                    case 'settledLoan':
                        return 'Löst lån';
                    case 'paidToCustomer':
                        return 'Utbetalning';
                    default:
                        return 'Lån';
                }
            case 'NewMortgageLoan':
                return 'Lån';
            default:
                return businessEventType;
        }
    }

    hasMoreItems() {
        return this.m.items.length > 0 && this.m.items.findIndex((x) => !x.isVisible) >= 0;
    }

    showMoreItems(evt?: Event) {
        evt?.preventDefault();

        let countShown = 0;
        for (let i of this.m.items) {
            if (!i.isVisible) {
                i.isVisible = true;
                countShown++;
            }
            if (countShown >= 5) {
                return;
            }
        }
    }
}

class Model {
    items: {
        date: string;
        amount: number;
        description: string;
        isVisible?: boolean;
    }[];
}
