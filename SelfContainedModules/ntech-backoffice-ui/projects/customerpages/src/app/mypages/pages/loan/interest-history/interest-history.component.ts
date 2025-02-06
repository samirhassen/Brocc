import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { InterestHistory } from '../../../services/mypages-api.service';

@Component({
    selector: 'interest-history',
    templateUrl: './interest-history.component.html',
    styles: [],
})
export class InterestHistoryComponent implements OnInit {
    constructor() {}

    @Input()
    public interestHistory: InterestHistory;

    public m: Model;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.interestHistory) {
            return;
        }

        this.m = {
            items: (this.interestHistory.InterestChanges ?? []).map((x) => {
                return { date: x.TransactionDate, percent: x.InterestRatePercent };
            }),
        };
        this.showMoreInterestHistoryItems(null);
    }

    hasMoreInterestHistoryItems() {
        return this.m.items.length > 0 && this.m.items.findIndex((x) => !x.isVisible) >= 0;
    }

    showMoreInterestHistoryItems(evt?: Event) {
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
    items: { date: string; percent: number; isVisible?: boolean }[];
}
