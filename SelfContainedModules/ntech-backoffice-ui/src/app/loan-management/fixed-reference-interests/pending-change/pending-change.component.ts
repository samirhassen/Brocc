import { Component, Input, SimpleChanges } from '@angular/core';
import { getNumberDictionaryKeys } from 'src/app/common.types';
import { ActivePendingChange, FixedRateService, RateServerModel } from '../../services/fixed-rate-service';

@Component({
    selector: 'pending-change',
    templateUrl: './pending-change.component.html',
    styleUrls: ['./pending-change.component.scss'],
})
export class PendingChangeComponent {
    constructor(private fixedRateService: FixedRateService) {}

    @Input()
    public initialData: PendingChangeComponentInitialData;

    public m: Model;

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let activeChange = this.initialData.activeChange;
        let m: Model = {
            isCommitAllowed: activeChange.IsCommitAllowed,
            initiatedByUserName: activeChange.InitiatedByUserDisplayName,
            initiatedDate: activeChange.InitiatedDate,
            rates: [],
            removedRatesText: null,
        };

        let monthCounts = getNumberDictionaryKeys(activeChange.NewRateByMonthCount);
        monthCounts.sort((x, y) => x - y);
        for (let monthCount of monthCounts) {
            let previousRate = this.initialData.currentRates.find((x) => x.MonthCount === monthCount);
            let rate = activeChange.NewRateByMonthCount[monthCount];
            let diff = previousRate ? rate - previousRate.RatePercent : null;
            m.rates.push({
                monthCount: monthCount,
                rate: rate,
                isNew: !previousRate,
                isUnchanged: previousRate && Math.abs(diff) < 0.000001,
                diff: diff,
            });
        }

        let removedMonthCounts: number[] = [];
        for (let currentRate of this.initialData.currentRates) {
            if (!m.rates.find((x) => x.monthCount === currentRate.MonthCount)) {
                removedMonthCounts.push(currentRate.MonthCount);
            }
        }
        if (removedMonthCounts.length > 0) {
            m.removedRatesText = removedMonthCounts
                .map((x) => {
                    let p = this.parseMonthCount(x);
                    return `${p.nrOfMonthsOrYears} ${p.localizedUnitName}`;
                })
                .join(', ');
        }

        this.m = m;
    }

    public parseMonthCount(nrOfMonths: number) {
        return this.fixedRateService.parseMonthCountShared(nrOfMonths);
    }

    commitChange(evt?: Event) {
        evt?.preventDefault();
        this.initialData.onCommit();
    }

    cancelChange(evt?: Event) {
        evt?.preventDefault();
        this.initialData.onCancel();
    }
}

interface Model {
    isCommitAllowed: boolean;
    initiatedByUserName: string;
    initiatedDate: string;
    rates: {
        monthCount: number;
        rate: number;
        isNew: boolean;
        isUnchanged: boolean;
        diff: number;
    }[];
    removedRatesText: string;
}

export interface PendingChangeComponentInitialData {
    currentRates: RateServerModel[];
    activeChange: ActivePendingChange;
    onCommit: () => void;
    onCancel: () => void;
}
