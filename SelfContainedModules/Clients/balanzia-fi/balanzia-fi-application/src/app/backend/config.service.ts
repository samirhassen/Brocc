import { Injectable } from '@angular/core';
import { StringDictionary } from './common.types';
import { of, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

@Injectable({
    providedIn: 'root'
})
export class ConfigService {
    private queryStringParameters: StringDictionary = null
    private isTestRaw: boolean = null
    private isLegalInterestCeilingEnabledRaw: boolean = null

    constructor(private httpClient: HttpClient) { }

    initialize(withLoadedConfig?: (c: ConfigService) => void): Promise<Object> {
        let queryStringParameters = this.parseQueryStringParameters()
        return this.fetchConfigFromServer().pipe(
            tap(config => {
                this.queryStringParameters = queryStringParameters,
                this.isTestRaw = config.IsTest
                this.isLegalInterestCeilingEnabledRaw = this.hasValue(config.IsLegalInterestCeilingEnabled) ? config.IsLegalInterestCeilingEnabled : false
                if(withLoadedConfig) {
                    withLoadedConfig(this)
                }
            })
        ).toPromise()
    }

    private hasValue(b: boolean) {
        return b === true || b === false
    }

    private parseQueryStringParameters(): StringDictionary {
        let d: StringDictionary = {}
        let query = window.location.search.substring(1);
        let vars = query.split('&');
        if(vars && vars.length > 0 && vars[0].trim().length > 0) {
            for (var i = 0; i < vars.length; i++) {
                var pair = vars[i].toString().toLowerCase().split('=')
                d[decodeURIComponent(pair[0])] = decodeURIComponent(pair[1])            
            }
        }
        return d
    }

    getQueryStringParameters() {
        return this.queryStringParameters
    }

    isNTechTest(): boolean {
        if(!this.hasValue(this.isTestRaw)) {
            throw new Error('Config broken, missing isTest')
        }
        return this.isTestRaw
    }

    isLegalInterestCeilingEnabled() {        
        if(!this.hasValue(this.isLegalInterestCeilingEnabledRaw)) {
            throw new Error('Config broken, missing isLegalInterestCeilingEnabled')
        }        
        return this.isLegalInterestCeilingEnabledRaw
    }

    private fetchConfigFromServer(): Observable<ConfigResult> {
        if(environment.useMockApi) {
            return of({
                IsTest: true,
                IsLegalInterestCeilingEnabled: true
            })
        } else {
            return this.httpClient.post<ConfigResult>('/api/application/fetch-config', {})
        }
    }
}

class ConfigResult {
    IsTest: boolean
    IsLegalInterestCeilingEnabled: boolean
}
