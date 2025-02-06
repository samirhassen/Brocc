import { Injectable } from '@angular/core';
import { of } from 'rxjs';
import { Dictionary, StringDictionary } from '../common.types';
import { ConfigService } from './config.service';
import { NtechApiService } from './ntech-api.service';
import { SizeAndTimeLimitedCache } from './size-and-time-limited-cache';

@Injectable({
    providedIn: 'root',
})
export class BankAccountNrValidationService {
    constructor(private apiService: NtechApiService, private config: ConfigService) {}

    private getValidateBankAccountNrCacheKey(request: BankAccountNrValidationRequest): string {
        return `${request.bankAccountNr}#${request.bankAccountNrType}`;
    }

    private validateBankAccountNrsCacheWithoutExternals: SizeAndTimeLimitedCache = new SizeAndTimeLimitedCache(200, 30);
    private validateBankAccountNrsCacheWithExternals: SizeAndTimeLimitedCache = new SizeAndTimeLimitedCache(200, 30);

    private getCache(allowExternals: boolean) {
        return allowExternals
            ? this.validateBankAccountNrsCacheWithExternals
            : this.validateBankAccountNrsCacheWithoutExternals;
    }
    private validateBankAccountNrsBatchRaw(
        request: Dictionary<BankAccountNrValidationRequest>,
        options?: { allowExternalSources?: boolean; skipLoadingIndicator?: boolean }
    ): Promise<{
        ValidatedAccountsByKey: Dictionary<BankAccountNrValidationResult>;
    }> {
        let accounts = Object.keys(request).map((x) => {
            return {
                requestKey: x,
                bankAccountNr: request[x].bankAccountNr,
                bankAccountNrType: request[x].bankAccountNrType,
            };
        });
        return this.apiService.post(
            'nCustomer',
            'api/bankaccount/validate-nr-batch',
            { accounts, allowExternalSources: options?.allowExternalSources },
            { skipLoadingIndicator: options?.skipLoadingIndicator }
        );
    }

    async validateBankAccountNr(
        request: BankAccountNrValidationRequest,
        options?: { allowExternalSources?: boolean; skipLoadingIndicator?: boolean }
    ) {
        if(!request.bankAccountNr || request.bankAccountNr.length < 2) {
            //swedish plusgiro can be 2 so that is the lowest. skip server roundtrip for other lengths
            return {
                RawNr: request.bankAccountNr,
                IsValid: false
            }
        }
        let bulkRequest: Dictionary<BankAccountNrValidationRequest> = {
            '1': request,
        };
        let result = await this.validateBankAccountNrsBatch(bulkRequest, options);
        return result.ValidatedAccountsByKey['1'];
    }

    validateBankAccountNrsBatch(
        request: Dictionary<BankAccountNrValidationRequest>,
        options?: { allowExternalSources?: boolean; skipLoadingIndicator?: boolean }
    ): Promise<{
        ValidatedAccountsByKey: Dictionary<BankAccountNrValidationResult>;
    }> {
        let fromCacheResult: {
            ValidatedAccountsByKey: Dictionary<BankAccountNrValidationResult>;
        } = { ValidatedAccountsByKey: {} };

        if (!request || Object.keys(request).length === 0) {
            return of({ ValidatedAccountsByKey: {} }).toPromise();
        }

        let anyNotInCache = false;

        for (let key of Object.keys(request)) {
            let account = request[key];
            let cacheHit = this.getCache(options?.allowExternalSources).get<BankAccountNrValidationResult>(
                this.getValidateBankAccountNrCacheKey(account)
            );
            if (!cacheHit) {
                anyNotInCache = true;
                break;
            }
            fromCacheResult.ValidatedAccountsByKey[key] = cacheHit;
        }

        if (anyNotInCache) {
            return this.validateBankAccountNrsBatchRaw(request, options).then((x) => {
                for (let key of Object.keys(x.ValidatedAccountsByKey)) {
                    this.getCache(options?.allowExternalSources).set(
                        this.getValidateBankAccountNrCacheKey(request[key]),
                        x.ValidatedAccountsByKey[key]
                    );
                }
                return x;
            });
        } else {
            return of(fromCacheResult).toPromise();
        }
    }

    getAllBankAccountNrTypeCodes() {
        let country = this.config.baseCountry();
        if (country === 'SE') {
            return ['BankAccountSe', 'BankGiroSe', 'PlusGiroSe'];
        } else if (country === 'FI') {
            return ['IBANFi'];
        } else {
            return [];
        }
    }
}

export interface BankAccountNrValidationRequest {
    bankAccountNr: string;
    bankAccountNrType: string;
}

export interface BankAccountNrValidationResult {
    RawNr: string;
    IsValid: boolean;
    ValidAccount?: BankAccountNrValidationValidAccountResult;
}

export interface BankAccountNrValidationValidAccountResult {
    BankName: string;
    ClearingNr: string;
    AccountNr: string;
    NormalizedNr: string;
    Bic: string;
    DisplayNr: string;
    BankAccountNrType: string;
    ExternalData?: StringDictionary;
}