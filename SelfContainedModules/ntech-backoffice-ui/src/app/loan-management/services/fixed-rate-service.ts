import { Injectable } from '@angular/core';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { NumberDictionary } from 'src/app/common.types';

@Injectable({
    providedIn: 'root',
})
export class FixedRateService {
    public constructor(private apiService: NtechApiService) {}

    public parseMonthCountShared(nrOfMonths: number): {
        isMonths: boolean;
        nrOfMonthsOrYears: number;
        localizedUnitName: string;
        nrOfMonths: number;
    } {
        let isMonths = nrOfMonths % 12 !== 0;
        return {
            nrOfMonths: nrOfMonths,
            isMonths: isMonths,
            localizedUnitName: isMonths ? 'månader' : 'år',
            nrOfMonthsOrYears: isMonths ? nrOfMonths : nrOfMonths / 12,
        };
    }

    public async getCurrentRates(): Promise<CurrentRatesResult> {
        return this.apiService.post('nCredit', 'api/MortgageLoans/FixedInterest/Fetch-All-Current', {});
    }

    public async initiateChange(newRateByMonthCount: NumberDictionary<number>): Promise<void> {
        return this.apiService.post('nCredit', 'api/MortgageLoans/FixedInterest/Handle-Change', {
            NewRateByMonthCount: newRateByMonthCount,
        });
    }

    public async commitCurrentChange(overrideDualityCommitRequirement: boolean) {
        return this.apiService.post('nCredit', 'api/MortgageLoans/FixedInterest/Handle-Change', {
            IsCommit: true,
            //NOTE: The override only works in test
            OverrideDualityCommitRequirement: overrideDualityCommitRequirement,
        });
    }

    public async cancelCurrentChange() {
        return this.apiService.post('nCredit', 'api/MortgageLoans/FixedInterest/Handle-Change', {
            IsCancel: true,
        });
    }

    public getFallbackMonthCount() {
        return 3;
    }
}

export interface RateServerModel {
    MonthCount: number;
    RatePercent: number;
}

export interface CurrentRatesResult {
    CurrentRates: RateServerModel[];
    PendingChange?: ActivePendingChange;
}

export interface ActivePendingChange {
    IsCommitAllowed: boolean;
    NewRateByMonthCount: NumberDictionary<number>;
    InitiatedByUserId: number;
    InitiatedByUserDisplayName: string;
    InitiatedDate: string;
}
