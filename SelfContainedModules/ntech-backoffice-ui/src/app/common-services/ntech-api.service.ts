import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { normalizeStringSlashes } from '../common.types';
import { ConfigService } from './config.service';
import { NTechValidationService } from './ntech-validation.service';
import { SharedApisService } from './shared-api.service';

@Injectable({
    providedIn: 'root',
})
export class NtechApiService {
    constructor(private config: ConfigService, private validation: NTechValidationService) {
        this.shared = new SharedApisService(this);
    }

    public shared: SharedApisService;

    public post<TResponse>(
        moduleName: string,
        relativeUrl: string,
        request: any,
        options?: {
            handleNTechError?: (error: NTechApiErrorResponse) => TResponse;
            skipLoadingIndicator?: boolean;
            forceCamelCase?: boolean;
        }
    ): Promise<TResponse> {
        let normalizedRelativeUrl = normalizeStringSlashes(relativeUrl, false, false);
        let rootUrl = normalizeStringSlashes(
            this.config.getServiceRegistry().getServiceRootUrl(moduleName),
            null,
            false
        );
        let url = `${rootUrl}/${normalizedRelativeUrl}`;
        let result = this.config
            .postWithAccessToken<TResponse>(url, request, {
                skipLoadingIndicator: options?.skipLoadingIndicator,
                forceCamelCase: options?.forceCamelCase,
            })
            .toPromise();
        if (options?.handleNTechError) {
            return result.catch((x: HttpErrorResponse) => {
                let error: NTechApiErrorResponse = x?.error;
                if (error?.errorCode && error?.errorMessage) {
                    return options.handleNTechError(error);
                } else {
                    throw x;
                }
            });
        } else {
            return result;
        }
    }

    public get<TResponse>(
        moduleName: string,
        relativeUrl: string,
        options?: {
            handleNTechError?: (error: NTechApiErrorResponse) => TResponse;
            skipLoadingIndicator?: boolean;
        }
    ): Promise<TResponse> {
        let normalizedRelativeUrl = normalizeStringSlashes(relativeUrl, false, false);
        let rootUrl = normalizeStringSlashes(
            this.config.getServiceRegistry().getServiceRootUrl(moduleName),
            null,
            false
        );
        let url = `${rootUrl}/${normalizedRelativeUrl}`;
        let result = this.config
            .getWithAccessToken<TResponse>(url, { skipLoadingIndicator: options?.skipLoadingIndicator })
            .toPromise();
        if (options?.handleNTechError) {
            return result.catch((x: HttpErrorResponse) => {
                let error: NTechApiErrorResponse = x?.error;
                if (error?.errorCode && error?.errorMessage) {
                    return options.handleNTechError(error);
                } else {
                    throw x;
                }
            });
        } else {
            return result;
        }
    }  

    public download(moduleName: string, relativeUrl: string, request: any, isGet: boolean = false): Promise<Blob> {
        let normalizedRelativeUrl = normalizeStringSlashes(relativeUrl, false, false);
        let rootUrl = normalizeStringSlashes(
            this.config.getServiceRegistry().getServiceRootUrl(moduleName),
            null,
            false
        );
        let url = `${rootUrl}/${normalizedRelativeUrl}`;

        if(!isGet) {
            return this.config.downloadWithAccessToken(url, request).toPromise();
        } else {
            return this.config.getBinaryFileWithAccessToken(url).toPromise();
        }        
    }

    public getUiGatewayUrl(moduleName: string, moduleLocalPath: string, queryStringParameters?: [string, string][]) {
        moduleLocalPath = normalizeStringSlashes(moduleLocalPath, false, false);
        let p = `/Ui/Gateway/${moduleName}/${moduleLocalPath}`;
        if (queryStringParameters) {
            let s = moduleLocalPath.indexOf('?') >= 0 ? '&' : '?';
            for (let q of queryStringParameters) {
                if (!this.validation.isNullOrWhitespace(q[1])) {
                    p += `${s}${q[0]}=${encodeURIComponent(decodeURIComponent(q[1]))}`;
                    s = '&';
                }
            }
        }
        return p;
    }

    public getArchiveDocumentUrl(archiveKey: string, skipFilename: boolean = false) {
        let params: [string, string][] = [['key', archiveKey]];
        if (skipFilename) {
            params.push(['skipFilename', 'True']);
        }
        return this.config.getServiceRegistry().createUrl('nDocument', 'Archive/Fetch', params);
    }
}

export interface NTechApiErrorResponse {
    errorCode: string;
    errorMessage: string;
}
