import { Injectable } from '@angular/core';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';

@Injectable({
    providedIn: 'root',
})
export class LoanDefaultManagementApiService {
    constructor(private apiService: NtechApiService) {}

    postponeTerminationLetters(creditNr: string): Promise<any> {
        return this.apiService.post('NTechHost', 'Api/Credit/TerminationLetters/Postpone', {
            creditNr,
            useDefaultDate: true
        });
    }

    resumeTerminationLetters(creditNr: string): Promise<any> {
        return this.apiService.post('NTechHost', 'Api/Credit/TerminationLetters/Resume', {
            creditNr
        });
    }

    postponeOrResumeDebtCollections(creditNr: string, postponeUntilDate?: string): Promise<any> {
        return this.apiService.post('nCredit', 'Api/Credit/DebtCollectionCandidates/PostponeOrResume', {
            creditNr,
            postponeUntilDate,
        });
    }

    getPageTerminationLetters(
        pageSize: number,
        pageNr: number,
        omniSearch?: string
    ): Promise<TerminationLetterPageResult> {
        return this.apiService.post('nCredit', 'Api/Credit/TerminationLetterCandidates/GetPage', {
            pageSize,
            pageNr,
            omniSearch,
        });
    }

    getPageDebtCollection(pageSize: number, pageNr: number, omniSearch?: string): Promise<DebtCollectionPageResult> {
        return this.apiService.post('nCredit', 'Api/Credit/DebtCollectionCandidates/GetPage', {
            pageSize,
            pageNr,
            omniSearch,
        });
    }
}

export class TerminationLetterPageResult {
    public CurrentPageNr: number;
    public TotalNrOfPages: number;
    public Page: TerminationLetter[];
}

export class TerminationLetter {
    ActivePostponedUntilDate?: string; // date?
    ActiveTerminationLetterDueDate?: string; // date?
    AttentionHasRecentOverdueTerminationLetter?: string; // date?
    AttentionNotificationLowBalanceAmount?: number;
    AttentionPromisedToPayDateRecentOrFuture?: string; // date?
    AttentionSettlementOfferDate?: string; // date?
    AttentionTotalLowBalanceAmount?: number;
    AttentionWasPostponedUntilDate?: string; // date?
    BalanceUnpaidOverdueNotifications: number;
    CreditNr: string;
    CreditUrl: string;
    FractionBalanceUnpaidOverdueNotifications: number;
    HasAttention: boolean;
    InitialUnpaidOverdueNotifications: number;
    IsEligableForTerminationLetter: boolean;
    IsEligableForTerminationLetterExpectDate: boolean;
    NrOfDaysOverdue: number;
    NrUnpaidOverdueNotifications: number;
    TerminationCandidateDate: string;
    TerminationPreviewDate: string;
}

export class DebtCollectionPageResult {
    public CurrentPageNr: number;
    public TotalNrOfPages: number;
    public Page: DebtCollectionModel[];
}

export interface DebtCollectionModel {
    ActivePostponedUntilDate?: string; // date?
    ActiveTerminationLetterDueDate?: string; // date?
    AttentionHasRecentOverdueTerminationLetter?: string; // date?
    AttentionNotificationLowBalanceAmount?: number;
    AttentionPromisedToPayDateRecentOrFuture?: string; // date?
    AttentionSettlementOfferDate?: string; // date?
    AttentionTotalLowBalanceAmount?: number;
    AttentionWasPostponedUntilDate?: string; // date?
    BalanceUnpaidOverdueNotifications: number;
    CreditNr: string;
    CreditUrl: string;
    FractionBalanceUnpaidOverdueNotifications: number;
    HasAttention: boolean;
    InitialUnpaidOverdueNotifications: number;
    IsEligableForTerminationLetter: boolean;
    NrOfDaysOverdue: number;
    NrUnpaidOverdueNotifications: number;
    IsEligableForDebtCollectionExport: boolean;
    IsEligableForDebtCollectionExportExceptDate: boolean;
    AttentionLatestPaymentDateAfterTerminationLetterDueDate: string; //date?
    WithGraceLatestEligableTerminationLetterDueDate: string
}
