import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import * as moment from 'moment';
import { Dictionary, parseQueryStringParameters, StringDictionary } from 'src/app/common.types';
import { LoaderInterceptorSkipHeader } from 'src/app/common-services/loader.interceptor';

@Injectable({
    providedIn: 'root',
})
export class CustomerPagesConfigService {
    private serverData?: {
        queryStringParameters: StringDictionary;
        configResult: ConfigResult;
    } = undefined;

    constructor(private httpClient: HttpClient) {}

    initialize(withLoadedConfig?: (c: CustomerPagesConfigService) => void): Promise<Object> {
        let queryStringParameters = parseQueryStringParameters();
        return this.fetchConfigFromServer()
            .pipe(
                tap((config) => {
                    this.serverData = {
                        queryStringParameters: queryStringParameters,
                        configResult: config,
                    };
                    if (withLoadedConfig) {
                        withLoadedConfig(this);
                    }
                })
            )
            .toPromise();
    }

    hasAuthenticatedRole(roleName: string) {
        return this.withServerData((x) => x.configResult.AuthenticatedRoles.indexOf(roleName) >= 0);
    }

    isAuthenticated() {
        return this.withServerData((x) => x.configResult.IsAuthenticated);
    }

    getLoginUrl(targetName: string, targetCustomData?: string) {
        if (!targetName) {
            throw 'targetName required';
        }
        let pattern = this.withServerData((x) =>
            targetCustomData ? x.configResult.LoginUrlWithCustomDataPattern : x.configResult.LoginUrlPattern
        );
        pattern = pattern.replace('___targetname___', encodeURIComponent(targetName));
        if (targetCustomData) {
            pattern = pattern.replace('___targetcustomdata___', encodeURIComponent(targetCustomData));
        }
        return pattern;
    }

    getQueryStringParameters() {
        return this.withServerData((x) => x.queryStringParameters);
    }

    isNTechTest() {
        return this.withServerData((x) => x.configResult?.IsTest);
    }

    isConfigLoaded() {
        return !!this.serverData;
    }

    config() {
        return this.withServerData((x) => x.configResult);
    }

    baseCountry(): string {
        return this.withServerData((x) => x.configResult?.Client?.BaseCountry);
    }

    uiLanguage(): string {
        let country = this.baseCountry();
        if (country === 'SE') {
            return 'sv';
        } else {
            return 'sv';
        }
    }

    userDisplayName() {
        return this.withServerData((x) => x.configResult?.UserDisplayName);
    }

    getClient(): {
        ClientName: string;
        BaseCountry: string;
        BaseCurrency: string;
    } | null {
        return this.withServerData((x) => x?.configResult?.Client);
    }

    isFeatureEnabled(featureName: string) {
        //ActiveFeatures is always all lowercase from the serverside
        return this.withServerData((x) => x.configResult?.ActiveFeatures?.indexOf(featureName?.toLowerCase()) > 0);
    }

    isAnyFeatureEnabled(featureNames: string[]) {
        return this.withServerData((x) =>
            x.configResult?.ActiveFeatures?.some((af) => featureNames.some((fn) => fn === af))
        );
    }

    isMortgageLoansStandardEnabled() {
        return this.isFeatureEnabled('ntech.feature.mortgageloans.standard');
    }

    isUnsecuredLoansStandardEnabled() {
        return this.isFeatureEnabled('ntech.feature.unsecuredloans.standard');
    }

    isLoansStandardEnabled() {
        return this.isMortgageLoansStandardEnabled() || this.isUnsecuredLoansStandardEnabled();
    }

    getCurrentDateAndTime(): moment.Moment {
        return this.withServerData((x) => moment(x.configResult.CurrentDateAndTime));
    }

    getEnums() {
        return this.withServerData((x) => x.configResult.Enums);
    }

    private withServerData<T>(
        f: (cfg: { queryStringParameters?: StringDictionary; configResult?: ConfigResult }) => T
    ): T {
        if (!this.serverData) {
            throw new Error(
                'Config has not been loaded from the server yet. Call initialize and wait for it to complete first!'
            );
        }
        return f(this.serverData);
    }

    private fetchConfigFromServer(): Observable<ConfigResult> {
        return this.post('/api/embedded-customerpages/fetch-config', {});
    }

    public fetchMlWebappSettings(): Observable<{ MortgageLoanExternalApplicationSettings: Dictionary<string> }> {
        return this.post('/api/embedded-customerpages/fetch-ml-webapp-settings', {});
    }

    public post<TResponse>(
        url: string,
        request: any,
        options?: { skipLoadingIndicator?: boolean }
    ): Observable<TResponse> {
        var reqHeader = new HttpHeaders({
            'Content-Type': 'application/json',
        });
        if (options?.skipLoadingIndicator === true) {
            reqHeader = reqHeader.append(LoaderInterceptorSkipHeader, '1'); //NOTE: Not intented to actually be sent to the server. See interceptor for why this is here.
        }
        return this.httpClient.post<TResponse>(url, request, { headers: reqHeader });
    }

    public download(url: string, request: any): Observable<any> {
        var reqHeader = new HttpHeaders({
            'Content-Type': 'application/json',
        });
        const requestOptions: any = {
            //Because the typescript compiler is stupid
            headers: reqHeader,
            responseType: 'blob',
        };
        return this.httpClient.post<any>(url, request, requestOptions);
    }
}

interface ConfigResult {
    IsTest: boolean;
    LogoutUrl: string;
    LoginUrlPattern: string;
    LoginUrlWithCustomDataPattern: string;
    Client: {
        ClientName: string;
        BaseCountry: string;
        BaseCurrency: string;
    };
    Skinning?: {
        LogoUrl: string;
    };
    ActiveServiceNames: string[];
    ActiveFeatures: string[];
    CurrentDateAndTime: string;
    UserDisplayName: string;
    AuthenticatedRoles: string[];
    IsAuthenticated: boolean;
    Enums: {
        CivilStatuses: NTechEnum[];
        EmploymentStatuses: NTechEnum[];
        HousingTypes: NTechEnum[];
        OtherLoanTypes: NTechEnum[];
    };
}

export interface NTechEnum {
    Code: string;
    DisplayName: string;
}
