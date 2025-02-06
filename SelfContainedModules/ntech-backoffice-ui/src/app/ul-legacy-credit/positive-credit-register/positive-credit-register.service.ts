import { Injectable } from '@angular/core';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';

export const CreditApplicationCheckpointCode = 'CreditApplicationCheckpoint';
export const SavingsAccountCreationRemarkCode = 'SavingsAccountCreationRemark';
export const SavingsAccountBlockTransactionsCode = 'SavingsAccountBlockTransactions';

@Injectable({
    providedIn: 'root',
})
export class PositiveCreditRegisterService {
    constructor(private apiService: NtechApiService) {}

    fetchGetLoan(creditNr: string) {
        return this.apiService.post<FetchGetLoanResponse>(
            'NTechHost',
            'Api/Credit/PositiveCreditRegisterExport/GetLoan',
            { creditNr }
        );
    }

    fetchLogs(numberOfLogs: number) {
        return this.apiService.post<PcrLogsResponse>('NTechHost', 'Api/Credit/PositiveCreditRegisterExport/GetLogs', {
            numberOfLogs,
        });
    }

    fetchSingleBatchLogs(logCorrelationId: string) {
        return this.apiService.post<PcrSingleBatchLogsResponse>('NTechHost', 'Api/Credit/PositiveCreditRegisterExport/GetBatchLogs', {
            logCorrelationId,
        });
    }

    fetchSingleBatchLogFileContent(logCorrelationId: string, filename: string) {
        return this.apiService.download('NTechHost', 'Api/Credit/PositiveCreditRegisterExport/BatchLogFileContent', { logCorrelationId, filename });
    }

    exportBatch(fromDate: any, toDate: any, isFirstTimeExport: boolean) {
        return this.apiService.post<ExportResponse>(
            'NTechHost',
            'Api/Credit/PositiveCreditRegisterExport/ManualExport',
            { fromDate, toDate, isFirstTimeExport }
        );
    }
}

export interface FetchGetLoanResponse {
    isSuccess: boolean;
    rawResponse: string;
}

export interface PcrLogsResponse {
    statusLogs: string[];
    exportLogs: string[];
}

export interface PcrSingleBatchLogsResponse {
    logFiles: {
        isRequestLog: boolean
        isResponseLog: boolean
        logDate: string
        logFileName: string
    }[]
}

export interface ExportResponse {
    successCount: number;
    rawResponse: string;
}
