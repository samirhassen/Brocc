import { Injectable } from '@angular/core';
import * as moment from 'moment';
import { ConfigService } from '../common-services/config.service';
import { NtechApiService } from '../common-services/ntech-api.service';

@Injectable({
    providedIn: 'root',
})
export class ApiKeyService {
    constructor(private apiService: NtechApiService, private configService: ConfigService) {}

    async getInitialData(): Promise<ApiKeysInitialData> {
        let availableProviders: ApiKeyProviderModel[] = [];
        if (this.configService.getServiceRegistry().containsService('nPreCredit')) {
            //TODO: Teach NTechWebserviceMethod to respect the case preservation header
            let { Affiliates } = await this.apiService.post<AffiliatesResult>(
                'nPreCredit',
                'Api/Affiliates/FetchAll',
                {},
                { forceCamelCase: false }
            );
            for (let affiliate of Affiliates) {
                availableProviders.push({
                    providerName: affiliate.ProviderName,
                    providerDisplayName: affiliate.DisplayToEnduserName,
                });
            }
        }
        return new Promise<ApiKeysInitialData>((resolve) => {
            let result : ApiKeysInitialData = {
                availableScopes: [
                    {
                        name: 'ExternalCustomerPagesApi',
                        displayName: 'External customerpages api',
                        isConnectedToProvider: false,
                    }
                ],
                availableProviders: availableProviders,
            };
            if(this.configService.isFeatureEnabled('ntech.feature.precredit')) {
                result.availableScopes.push({
                    name: 'ExternalCreditApplicationApi',
                    displayName: 'External credit application api',
                    isConnectedToProvider: true,
                });
            }
            resolve(result);
        });
    }

    private apiHost() {
        return 'nUser';
    }

    private post<TResponse>(relativeUrl: string, request: any): Promise<TResponse> {
        return this.apiService.post(this.apiHost(), relativeUrl, request, { forceCamelCase: true });
    }

    getApiKey(keyId: string): Promise<ApiKeyModel> {
        return this.post('Api/User/ApiKeys/GetSingle', { apiKeyId: keyId });
    }

    getApiKeys(): Promise<ApiKeyModel[]> {
        return this.post('Api/User/ApiKeys/GetAll', {});
    }

    createApiKey(request: {
        scopeName: string;
        description: string;
        providerName: string;
        expiresAfterDays: number;
        ipAddressFilter: string;
    }) {
        return this.post<{ rawApiKey: string; storedModel: ApiKeyModel }>('Api/User/ApiKeys/Create', request);
    }

    revoke(apiKeyId: string) {
        return this.post<{ wasRevoked: boolean }>('Api/User/ApiKeys/Revoke', { apiKeyId: apiKeyId });
    }

    authenticate(request: { rawApiKey: string; authenticationScope: string; callerIpAddress: string }) {
        return this.post<{
            isAuthenticated: boolean;
            failedAuthenticationReason: string;
            authenticatedKeyModel: ApiKeyModel;
        }>('Api/User/ApiKeys/Authenticate', request);
    }

    getExpirationInfo(key: ApiKeyModel): ApiKeyExpirationInfo {
        let now = () => this.configService.getCurrentDateAndTime();
        let ageInDays = now().diff(moment(key.creationDate), 'days');
        let expiredForDays = key.expirationDate ? now().diff(moment(key.expirationDate), 'days') : null;
        if (key.revokedDate || expiredForDays > 0) {
            if (key.revokedDate) {
                return {
                    ageInDays: ageInDays,
                    revokedForDays: now().diff(moment(key.revokedDate), 'days'),
                    expiredForDays: null,
                    expiresInDays: null,
                    code: 'revoked',
                };
            } else {
                return {
                    ageInDays: ageInDays,
                    revokedForDays: null,
                    expiredForDays: expiredForDays,
                    expiresInDays: null,
                    code: 'expired',
                };
            }
        } else {
            return {
                ageInDays: ageInDays,
                expiredForDays: null,
                revokedForDays: null,
                expiresInDays: expiredForDays !== null ? -expiredForDays : null,
                code: 'active',
            };
        }
    }

    getExpirationText(expirationInfo: ApiKeyExpirationInfo) {
        let ageText: string = '';
        if (expirationInfo.code === 'revoked') {
            ageText =
                expirationInfo.revokedForDays > 0
                    ? `Revoked ${expirationInfo.revokedForDays} days ago`
                    : `Revoked today`;
        } else if (expirationInfo.code === 'expired') {
            ageText = `Expired ${expirationInfo.expiredForDays} days ago`;
        } else if (expirationInfo.code === 'active') {
            ageText = `${expirationInfo.ageInDays} days`;
            if (expirationInfo.expiresInDays !== null) {
                ageText += ` (expires in ${expirationInfo.expiresInDays} days)`;
            } else {
                ageText += ' (never expires)';
            }
        }
        return ageText;
    }
}

export interface ApiKeyModel {
    version: number;
    id: string;
    description: string;
    scopeName: string;
    creationDate: Date;
    expirationDate: Date;
    revokedDate: Date;
    ipAddressFilter: string;
    providerName: string;
}

export interface ApiKeysInitialData {
    availableScopes: ApiKeyScopeModel[];
    availableProviders: ApiKeyProviderModel[];
}

export interface ApiKeyScopeModel {
    name: string;
    displayName: string;
    isConnectedToProvider: boolean;
}

export interface ApiKeyProviderModel {
    providerName: string;
    providerDisplayName: string;
}

export interface ApiKeyExpirationInfo {
    ageInDays: number;
    expiredForDays: number | null;
    revokedForDays: number | null;
    expiresInDays: number | null;
    code: 'active' | 'expired' | 'revoked';
}

interface AffiliatesResult {
    Affiliates: {
        ProviderName: string;
        DisplayToEnduserName: string;
    }[];
}
