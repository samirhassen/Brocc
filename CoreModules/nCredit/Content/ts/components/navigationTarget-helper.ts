namespace NavigationTargetHelper {
    export function createUrlTarget(url: string): CodeOrUrl {
        return { targetUrl: url }
    }

    export function createTargetFromComponentHostToHere(i: ComponentHostNs.ComponentHostServerInitialData) {
        if (i.navigationTargetCodeToHere) {
            return createCodeTarget(i.navigationTargetCodeToHere)
        } else {
            return null
        }
    }

    export function createCodeTarget(targetCode: string | NavigationTargetCode, context?: NavigationContext): CodeOrUrl {
        if (!context) {
            return { targetCode: targetCode }
        } else {
            return { targetCode: targetCode }
        }
    }

    export function create(backUrl: string, targetCode: string | NavigationTargetCode, context: NavigationContext) {
        if (targetCode) {
            return createCodeTarget(targetCode, context)
        } else if (backUrl) {
            return createUrlTarget(backUrl)
        } else {
            return null
        }
    }

    export function AppendBackNavigationToUrl(url: string, target: CodeOrUrl): string {
        if (!target) {
            return url
        }
        if (!target.targetCode && !target.targetUrl) {
            return url
        }

        if (url.indexOf('backUrl') > 0) {
            return url
        }
        if (url.indexOf('backTarget') > 0) {
            return url
        }

        let newUrl = url + (url.indexOf('?') < 0 ? '?' : '&')
        if (target.targetCode) {
            newUrl += `backTarget=${decodeURIComponent(encodeURIComponent(target.targetCode))}`
        } else {
            newUrl += `backUrl=${decodeURIComponent(encodeURIComponent(target.targetUrl))}`
        }
        return newUrl
    }

    export class CodeOrUrl {
        targetCode?: string | NavigationTargetCode
        targetUrl?: string
    }

    export enum NavigationTargetCode {
        Credit = "Credit",
        UnplacedPayments = "UnplacedPayments"
    }

    export interface NavigationContext {
        creditNr?: string
    }

    export function resolveNavigationUrl(codeOrUrl: CodeOrUrl, context: NavigationContext): string | null {
        if (!codeOrUrl) {
            return null
        }

        if (codeOrUrl.targetCode) {
            let c = codeOrUrl.targetCode

            if (c.length > 2 && c.substr(0, 2) === 't-') {
                    return getLocalModuleUrl('/Ui/Gateway/nBackOffice/Ui/CrossModuleNavigate', [['targetCode', codeOrUrl.targetCode]])
            }

            if (c == NavigationTargetCode.Credit && context && context.creditNr) {
                return getLocalModuleUrl('/Ui/Credit', [['creditNr', context.creditNr]])
            } else if (c == NavigationTargetCode.UnplacedPayments) {
                return getLocalModuleUrl('/Ui/UnplacedPayments/List')
            } else {
                throw new Error("Invalid navigation target")
            }
        }

        if (initialData && initialData.disableBackUrlSupport) {
            return null
        } else {
            return codeOrUrl.targetUrl
        }
    }

    export function createCodeOrUrlFromInitialData(initialData: any, context?: NavigationContext): CodeOrUrl {
        let backUrl = initialData ? initialData.backUrl : null
        let backTarget = initialData ? initialData.backTarget : null
        return create(backUrl, backTarget, context)
    }

    export function handleBackWithInitialDataDefaults(initialData: any, apiClient: NTechCreditApi.ApiClient, $q: ng.IQService, context?: NavigationContext): ng.IPromise<void> {
        let t = createCodeOrUrlFromInitialData(initialData, context)
        return handleBack(t, apiClient, $q, context)
    }

    export function handleBack(codeOrUrl: CodeOrUrl, apiClient: NTechCreditApi.ApiClient, $q: ng.IQService, context: NavigationContext): ng.IPromise<void> {
        let getUrl: ng.IPromise<string>
        let url = resolveNavigationUrl(codeOrUrl, context)
        if (!url) {
            getUrl = apiClient.getUserModuleUrl('nBackOffice', '/').then(x => x.Url)
        } else {
            let deferred = $q.defer<string>()
            deferred.resolve(url)
            getUrl = deferred.promise
        }
        return getUrl.then(x => {
            document.location.href = x
        })
    }

    //TODO: Share with controllerbase
    function startsWith(s: string, prefix: string): boolean {
        if (!s) {
            return false
        }
        return s.substring(0, prefix.length) === prefix
    }

    function isNullOrWhitespace(input: any) {
        if (typeof input === 'undefined' || input == null) return true;

        if ($.type(input) === 'string') {
            return $.trim(input).length < 1;
        } else {
            return false
        }
    }

    function getLocalModuleUrl(moduleLocalPath: string, queryStringParameters?: [string, string][]) {
        if (moduleLocalPath[0] === '/') {
            moduleLocalPath = moduleLocalPath.substring(1)
        }
        let p = `/${moduleLocalPath}`
        if (queryStringParameters) {
            let s = moduleLocalPath.indexOf('?') >= 0 ? '&' : '?'
            for (let q of queryStringParameters) {
                if (!isNullOrWhitespace(q[1])) {
                    p += `${s}${q[0]}=${encodeURIComponent(decodeURIComponent(q[1]))}`
                    s = '&'
                }
            }
        }
        return p
    }
}