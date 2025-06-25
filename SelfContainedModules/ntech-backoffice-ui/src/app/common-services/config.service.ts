import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { StringDictionary } from '../common.types';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import * as moment from 'moment';
import { LoginInitResult, LoginState, parseLoginState } from '../login/login-manager';
import { ServiceRegistry } from './service-registry';
import { LoaderInterceptorSkipHeader } from './loader.interceptor';

@Injectable({
    providedIn: 'root',
})
export class ConfigService {
    private serverData?: {
        queryStringParameters: StringDictionary;
        configResult: ConfigResult;
    } = undefined;

    private loginResult: LoginInitResult = null;
    private serviceRegistry?: ServiceRegistry = null;

    constructor(private httpClient: HttpClient) {}

    initialize(loginResult: LoginInitResult, withLoadedConfig?: (c: ConfigService) => void): Promise<Object> {
        let queryStringParameters = loginResult.queryStringParameters;
        this.loginResult = loginResult;
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

    getServiceRegistry(): ServiceRegistry {
        return this.withServerData((x) => {
            if (!this.serviceRegistry) {
                this.serviceRegistry = new ServiceRegistry(x.configResult?.ServiceRegistry);
            }
            return this.serviceRegistry;
        });
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

    isAuthenticated() {
        return this.loginResult && this.loginResult.user;
    }

    config() {
        return this.withServerData((x) => x.configResult);
    }

    baseCountry(): string {
        return this.withServerData((x) => x.configResult?.Client?.BaseCountry);
    }

    userLanguage(): string {
        let actualUserLanguage = navigator?.language;
        if(actualUserLanguage && ['fi', 'sv', 'en'].includes(actualUserLanguage)) {
            return actualUserLanguage;
        } else {
            return this.clientLanguage();
        }
    }

    clientLanguage(): string {
        let country = this.baseCountry();
        if(country === 'FI') {
            return 'fi';
        } else if(country === 'SE') {
            return 'sv';
        } else {
            return 'en';
        }
    }

    hasAccessTokenExpired() {
        return (
            !this.loginResult.accessTokenExpirationEpoch ||
            this.loginResult.accessTokenExpirationEpoch < moment().valueOf()
        );
    }

    accessTokenSecondsRemaining() {
        if (!this.loginResult?.accessTokenExpirationEpoch) {
            return 0;
        }
        return Math.round((moment().valueOf() - this.loginResult.accessTokenExpirationEpoch) / 1000);
    }

    accessToken() {
        return this.loginResult?.user?.access_token;
    }

    userDisplayName() {
        let profile = this.loginResult?.user?.profile;
        return profile ? profile['ntech.username'] : '';
    }

    getLoginState(): LoginState {
        return parseLoginState(this.loginResult?.user?.state);
    }

    getUserPermissions(): { productNames: []; groupNames: []; roleNames: [] } {
        let result: { productNames: []; groupNames: []; roleNames: [] } = {
            productNames: [],
            groupNames: [],
            roleNames: [],
        };
        let profile = this.loginResult?.user?.profile;
        if (!profile) {
            return result;
        }

        /*
        The library we use will set profile[...] to a string when there is only one value and an array of strings when there are multiple
        causing consumers of getUserPermissions to break for things like users with only one group. This fixes that
        */
        let arrayIfy = (x: any) => {
            if(!x) {
                return x;
            }
            if(Array.isArray(x)) {
                return x;
            } else {
                return [x];
            }
        }

        result.roleNames = arrayIfy(profile['ntech.role']) || [];
        result.productNames = arrayIfy(profile['ntech.product']) || [];
        result.groupNames = arrayIfy(profile['ntech.group']) || [];
        return result;
    }

    getCurrentUserId(): number | null {
        let profile = this.loginResult?.user?.profile;
        if (!profile) {
            return null;
        }
        let userid = profile['ntech.userid'];
        if (!userid) {
            return null;
        }
        return parseInt(userid);
    }

    getClient(): {
        ClientName: string;
        BaseCountry: string;
        BaseCurrency: string;
    } | null {
        return this.withServerData((x) => x?.configResult?.Client);
    }

    isFeatureEnabled(featureName: string) {
        return this.withServerData((x) => x.configResult?.ActiveFeatures?.indexOf(featureName?.toLowerCase()) >= 0);
    }

    isAnyFeatureEnabled(featureNames: string[]) {
        return this.withServerData((x) =>
            x.configResult?.ActiveFeatures?.some((af) => featureNames.some((fn) => fn === af))
        );
    }

    areAllFeaturesEnabled(featureNames: string[]) {
        return this.withServerData((x) =>
            featureNames.every((featureName) => x.configResult.ActiveFeatures.some((fn) => fn === featureName))
        );
    }

    hasAnyUserGroup(groupNames: string[]) {
        return this.getUserPermissions().groupNames?.some((x) => groupNames.some((y) => x === y));
    }

    getCurrentDateAndTime(): moment.Moment {
        return this.withServerData((x) => moment(x.configResult.CurrentDateAndTime));
    }

    hasEmailProvider() {
        return this.withServerData((x) => x.configResult.HasEmailProvider);
    }

    getSettingValue(settingName: string): string {
        return this.withServerData((x) => x.configResult.Settings[settingName]);
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
        return this.postWithAccessToken('/api/embedded-backoffice/fetch-config', {}, { supressAccessTokenCheck: true });
    }

    public postWithAccessToken<TResponse>(
        url: string,
        request: any,
        options?: { supressAccessTokenCheck?: boolean; skipLoadingIndicator?: boolean; forceCamelCase?: boolean }
    ): Observable<TResponse> {
        let supressAccessTokenCheck = options && options.supressAccessTokenCheck;

        if (!supressAccessTokenCheck) {
            if (this.hasAccessTokenExpired()) {
                this.loginResult?.forceReLogin(this.getLoginState()); //Will redirect
                return new Observable<TResponse>(); //Never resolve since we just want the next piece to wait for the redirect to happen
            }
        }
        var rawHeaders: { [name: string]: string } = {
            'Content-Type': 'application/json',
            Authorization: 'Bearer ' + this.loginResult?.user?.access_token,
        };
        if (options?.forceCamelCase) {
            rawHeaders['X-NTech-Force-CamelCase'] = '1';
        }
        var reqHeader = new HttpHeaders(rawHeaders);
        if (options?.skipLoadingIndicator === true) {
            reqHeader = reqHeader.append(LoaderInterceptorSkipHeader, '1'); //NOTE: Not intented to actually be sent to the server. See interceptor for why this is here.
        }
        return this.httpClient.post<TResponse>(url, request, { headers: reqHeader });
    }

    public getWithAccessToken<TResponse>(
        url: string,
        options?: { supressAccessTokenCheck?: boolean; skipLoadingIndicator?: boolean }
    ): Observable<TResponse> {
        let supressAccessTokenCheck = options && options.supressAccessTokenCheck;

        if (!supressAccessTokenCheck) {
            if (this.hasAccessTokenExpired()) {
                this.loginResult?.forceReLogin(this.getLoginState()); //Will redirect
                return new Observable<TResponse>(); //Never resolve since we just want the next piece to wait for the redirect to happen
            }
        }

        var reqHeader = new HttpHeaders({
            'Content-Type': 'application/json',
            Authorization: 'Bearer ' + this.loginResult?.user?.access_token,
        });
        if (options?.skipLoadingIndicator === true) {
            reqHeader = reqHeader.append(LoaderInterceptorSkipHeader, '1'); //NOTE: Not intented to actually be sent to the server. See interceptor for why this is here.
        }
        return this.httpClient.get<TResponse>(url, { headers: reqHeader });
    }

    public getBinaryFileWithAccessToken(
        url: string,
        options?: { supressAccessTokenCheck?: boolean; skipLoadingIndicator?: boolean }
    ): Observable<Blob> {
        let supressAccessTokenCheck = options && options.supressAccessTokenCheck;

        if (!supressAccessTokenCheck) {
            if (this.hasAccessTokenExpired()) {
                this.loginResult?.forceReLogin(this.getLoginState()); //Will redirect
                return new Observable<Blob>(); //Never resolve since we just want the next piece to wait for the redirect to happen
            }
        }

        var reqHeader = new HttpHeaders({
            Authorization: 'Bearer ' + this.loginResult?.user?.access_token,
        });
        if (options?.skipLoadingIndicator === true) {
            reqHeader = reqHeader.append(LoaderInterceptorSkipHeader, '1'); //NOTE: Not intented to actually be sent to the server. See interceptor for why this is here.
        }
        return this.httpClient.get(url, { headers: reqHeader, responseType: 'blob' });
    }

    public downloadWithAccessToken(
        url: string,
        request: any,
        options?: { supressAccessTokenCheck?: boolean }
    ): Observable<any> {
        let supressAccessTokenCheck = options && options.supressAccessTokenCheck;

        if (!supressAccessTokenCheck) {
            if (this.hasAccessTokenExpired()) {
                this.loginResult?.forceReLogin(this.getLoginState()); //Will redirect
                return new Observable<any>(); //Never resolve since we just want the next piece to wait for the redirect to happen
            }
        }

        var reqHeader = new HttpHeaders({
            'Content-Type': 'application/json',
            Authorization: 'Bearer ' + this.loginResult?.user?.access_token,
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
    ReleaseNumber: string;
    LogoutUrl: string;
    BackOfficeUrl: string;
    Skinning?: {
        LogoUrl: string;
    };
    Client: {
        ClientName: string;
        BaseCountry: string;
        BaseCurrency: string;
    };
    ActiveServiceNames: string[];
    ActiveFeatures: string[];
    Settings: StringDictionary;
    ServiceRegistry: StringDictionary;
    CurrentDateAndTime: string;
    HasEmailProvider: boolean;
}
