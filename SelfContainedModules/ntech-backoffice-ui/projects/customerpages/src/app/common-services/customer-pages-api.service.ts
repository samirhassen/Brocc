import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { IsoCountry } from 'projects/ntech-components/src/public-api';
import { normalizeStringSlashes } from 'src/app/common.types';
import { CustomerPagesConfigService } from './customer-pages-config.service';

@Injectable({
    providedIn: 'root',
})
export class CustomerPagesApiService {
    constructor(private config: CustomerPagesConfigService) {}

    /** Post thru the nCustomerPages proxy towards another module.  */
    public post<TResponse>(
        moduleName: string,
        relativeUrl: string,
        request: any,
        options?: {
            handleNTechError?: (error: NTechApiErrorResponse) => TResponse;
            skipLoadingIndicator?: boolean;
            isAnonymous?: boolean;
        }
    ): Promise<TResponse> {
        let normalizedRelativeUrl = normalizeStringSlashes(relativeUrl, false, false);
        let url = `/api/embedded-customerpages/${
            options?.isAnonymous ? 'anonymous-' : ''
        }proxy/${moduleName}/${normalizedRelativeUrl}`;
        let result = this.config
            .post<TResponse>(url, request, { skipLoadingIndicator: options?.skipLoadingIndicator })
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

    public download(moduleName: string, relativeUrl: string, request: any): Promise<Blob> {
        let normalizedRelativeUrl = normalizeStringSlashes(relativeUrl, false, false);
        let url = `/api/embedded-customerpages/proxy/${moduleName}/${normalizedRelativeUrl}`;

        return this.config.download(url, request).toPromise();
    }

    public getArchiveDocumentUrl(archiveKey: string, skipFilename?: boolean) {
        let url = `/api/embedded-customerpages/download-document?archiveKey=${encodeURIComponent(archiveKey)}`;
        if (skipFilename) {
            url += '&skipFilename=True';
        }
        return url;
    }

    public fetchLoggedInUserDetails(): Promise<{ name: string; civicRegNr: string }> {
        return this.config
            .post<{ name: string; civicRegNr: string }>('/api/embedded-customerpages/fetch-loggedin-user-details', {})
            .toPromise();
    }

    private isoCountries: IsoCountry[] = null;
    public async getIsoCountries(): Promise<IsoCountry[]> {
        if (this.isoCountries) {
            return new Promise<IsoCountry[]>((resolve) => resolve(this.isoCountries));
        } else {
            this.isoCountries = await this.config
                .post<IsoCountry[]>('/api/embedded-customerpages/iso-countries', {})
                .toPromise();
            return this.isoCountries;
        }
    }

    public parseCivicRegNr(civicRegNr: string) {
        return this.config.post<{
            isValid: boolean
            normalizedValue: string
            ageInYears: number
            isMale: boolean
        }>('/api/embedded-customerpages/parse-civicregnr', { civicRegNr }).toPromise();
    }

    /** HttpPost towards nCustomerPages specifically.  */
    public postLocal<TResponse>(
        localUrl: string,
        request: any,
        options?: {
            handleNTechError?: (error: NTechApiErrorResponse) => TResponse;
            skipLoadingIndicator?: boolean;
        }
    ): Promise<TResponse> {
        let url = `/${normalizeStringSlashes(localUrl, false, false)}`;
        let result = this.config
            .post<TResponse>(url, request, { skipLoadingIndicator: options?.skipLoadingIndicator })
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
}

export interface NTechApiErrorResponse {
    errorCode: string;
    errorMessage: string;
}
