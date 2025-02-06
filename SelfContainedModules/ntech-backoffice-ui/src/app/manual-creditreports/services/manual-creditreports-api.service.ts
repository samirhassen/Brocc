import { Injectable } from '@angular/core';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { Dictionary, StringDictionary } from 'src/app/common.types';

@Injectable({
    providedIn: 'root',
})
export class ManualCreditReportsApiService {
    constructor(private apiService: NtechApiService) {}

    public getProviders(): Promise<CreditReportProviderModel[]> {
        return this.fetchAllCreditReportProviderNames().then((x) => {
            return this.fetchProviderMetadataBulk(x.AllProviderNames).then((metaData) => {
                let result: CreditReportProviderModel[] = [];
                for (let provideName of x.AllProviderNames) {
                    let providerData = metaData.ProvidersByName[provideName];
                    if (providerData) {
                        result.push({
                            name: providerData.ProviderName,
                            displayName: providerData.ProviderName,
                            isCompanyProvider: providerData.IsCompanyProvider,
                            isActive: providerData.IsActive,
                        });
                    }
                }
                return result;
            });
        });
    }

    public getCreditReports(
        customerId: number,
        isCompany: boolean,
        skipCount?: number,
        batchSize?: number
    ): Promise<{
        CreditReportsBatch: CreditReportModel[];
        RemainingReportsCount: number;
    }> {
        return this.apiService.post('nCreditReport', 'CreditReport/FindForCustomer', {
            customerId,
            isCompany,
            skipCount,
            batchSize,
        });
    }

    public fetchTabledValues(creditReportId: number): Promise<{ title: string; value: string }[]> {
        return this.apiService.post('nCreditReport', 'CreditReport/FetchTabledValues', { creditReportId });
    }

    public fetchReason(creditReportId: number): Promise<{ ReasonType: string; ReasonData: string }> {
        return this.apiService.post('nCreditReport', 'CreditReport/FetchReason', { creditReportId });
    }

    public buyNewPersonReport(
        civicRegNr: string,
        request: CreditReportRequest
    ): Promise<{ Success: boolean; CreditReportId: number; Items?: { Name: string; Value: string }[] }> {
        return this.apiService.post('nCreditReport', 'CreditReport/BuyNew', { ...request, civicRegNr });
    }

    public buyNewCompanyReport(
        orgnr: string,
        request: CreditReportRequest
    ): Promise<{ Success: boolean; CreditReportId: number; Items?: { Name: string; Value: string }[] }> {
        return this.apiService.post('nCreditReport', 'CompanyCreditReport/BuyNew', { ...request, orgnr });
    }

    private fetchAllCreditReportProviderNames(): Promise<{ CurrentProviderName: string; AllProviderNames: string[] }> {
        return this.apiService.post('nPreCredit', 'api/CreditReportProviders/FetchAll', {});
    }

    private fetchProviderMetadataBulk(providerNames: string[]): Promise<{
        NonExistingProviderNames: string[];
        ProvidersByName: Dictionary<{ ProviderName: string; IsCompanyProvider: boolean; IsActive: boolean }>;
    }> {
        return this.apiService.post('nCreditReport', 'CreditReport/FetchProviderMetadataBulk', { providerNames });
    }
}

export interface CreditReportRequest {
    providerName: string;
    customerId: number;
    returningItemNames: string[];
    additionalParameters?: StringDictionary;
    reasonType: string;
    reasonData: string;
}

export interface CreditReportProviderModel {
    name: string;
    displayName: string;
    isCompanyProvider: boolean;
    isActive: boolean;
}

export interface CreditReportModel {
    Id: number;
    RequestDate: string; //DatetimeOffset
    CreditReportProviderName: string;
    CustomerId: number;
    HasReason: boolean;
    HtmlPreviewArchiveKey: string;
    PdfPreviewArchiveKey: string;
    HasTableValuesPreview: boolean;
    RawXmlArchiveKey: string;
}
