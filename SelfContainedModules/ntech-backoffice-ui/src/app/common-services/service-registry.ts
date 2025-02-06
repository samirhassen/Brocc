import { normalizeStringSlashes, StringDictionary } from '../common.types';

export class ServiceRegistry {
    private serviceRootCache: StringDictionary = null;

    //@ts-ignore TODO remove unused locals
    constructor(private serviceRootUrlByServiceName: StringDictionary) {
        this.serviceRootCache = {};
        for (let serviceName of Object.keys(serviceRootUrlByServiceName)) {
            this.serviceRootCache[serviceName.toLowerCase()] = serviceRootUrlByServiceName[serviceName];
        }
    }

    getServiceRootUrl(serviceName: string, allowMissing = false): string {
        let rootUrl = this.serviceRootCache[serviceName?.toLowerCase()];
        if (!rootUrl && !allowMissing) {
            throw new Error('Service registry is missing service ' + serviceName);
        }
        return rootUrl;
    }

    containsService(serviceName: string): boolean {
        return !!this.getServiceRootUrl(serviceName, true);
    }

    createUrl(serviceName: string, relativeUrl: string, queryParams?: [string, string][]) {
        let serviceRootUrl = this.getServiceRootUrl(serviceName);
        let url = `${normalizeStringSlashes(serviceRootUrl, null, false)}/${normalizeStringSlashes(
            relativeUrl,
            false,
            false
        )}`;
        if (queryParams) {
            let separator = '?';
            for (let queryParam of queryParams.filter((x) => x[1])) {
                url += `${separator}${encodeURIComponent(queryParam[0])}=${encodeURIComponent(queryParam[1])}`;
                separator = '&';
            }
        }
        return url;
    }
}
