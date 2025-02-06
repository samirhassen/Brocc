namespace NavigationTargetHelper {
    export function createUrlTarget(url: string): CodeOrUrl {
        return { targetUrl: url }
    }

    export function createTargetFromComponentHostToHere(i: ComponentHostNs.ComponentHostServerInitialData) {
        if (i.navigationTargetCodeToHere) {
            return createCodeTarget(i.navigationTargetCodeToHere)
        } else if (i.urlToHere) {
            return createUrlTarget(i.urlToHere)
        } else {
            return null
        }
    }

    export function createCodeTarget(targetCode: string | NavigationTargetCode, context?: NavigationContext): CodeOrUrl {
        if (!context) {
            return { targetCode: targetCode }
        } else if (targetCode === NavigationTargetCode.MortgageLoanEditCollateral && context.listNr) {
            return { targetCode: targetCode + '_' + context.listNr }
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

    export function createCodeOrUrlFromInitialData(initialData: any, context?: NavigationContext, defaultTarget?: NavigationTargetCode): CodeOrUrl {
        let backUrl = initialData ? initialData.backUrl : null
        let backTarget = initialData ? initialData.backTarget : null
        if (!backTarget && !backUrl) {
            backTarget = defaultTarget
        }
        return create(backUrl, backTarget, context)
    }

    export function createCrossModule(targetName: string, targetContext: { [key: string]: string}): CodeOrUrl {
        let code = NTechNavigationTarget.createCrossModuleNavigationTargetCode(targetName, targetContext)
        return create(null, code, null)
    }

    export function handleBackWithInitialDataDefaults(initialData: any, apiClient: NTechPreCreditApi.ApiClient, $q: ng.IQService, context?: NavigationContext, defaultTarget?: NavigationTargetCode): ng.IPromise<void> {
        let t = createCodeOrUrlFromInitialData(initialData, context, defaultTarget)
        return handleBack(t, apiClient, $q, context)
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
        MortgageLoanCreditCheckNewInitial = 'MortgageLoanCreditCheckNewInitial',
        MortgageLoanCreditCheckNewFinal = 'MortgageLoanCreditCheckNewFinal',
        MortgageLoanCreditCheckViewInitial = 'MortgageLoanCreditCheckViewInitial',
        MortgageLoanCreditCheckViewFinal = 'MortgageLoanCreditCheckViewFinal',
        MortgageLoanApplication = 'MortgageLoanApplication',
        MortgageLoanSearch = 'MortgageLoanSearch',
        MortgageLoanCreateLeadWorkList = 'MortgageLoanCreateLeadWorkList',
        MortgageLoanEditCollateral = 'MortgageLoanEditCollateral',
        MortgageLoanHandleSettlement = 'MortgageLoanHandleSettlement',
        UnsecuredLoanCreditCheckNewInitial = 'UnsecuredLoanCreditCheckNewInitial',
        UnsecuredLoanCreditCheckViewInitial = 'UnsecuredLoanCreditCheckViewInitial',
        UnsecuredLoanApplication = 'UnsecuredLoanApplication',
        UnsecuredLoanApplications = 'UnsecuredLoanApplications',
        MortgageLoanLead = 'MortgageLoanLead',
        MortgageLoanApplications = 'MortgageLoanApplications',
        CompanyLoanSearch = 'CompanyLoanSearch',
        UllApplicationBasis = 'UllApplicationBasis'
    }

    export interface NavigationContext {
        applicationNr?: string
        listNr?: string
        creditDecisionId?: string
        workListId?: string
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

            if (c === NavigationTargetCode.MortgageLoanCreditCheckNewInitial || c === NavigationTargetCode.MortgageLoanCreditCheckNewFinal) {
                if (!context || !context.applicationNr) {
                    throw new Error("Missing applicationNr")
                }
                return getLocalModuleUrl('/Ui/MortgageLoan/NewCreditCheck', [['applicationNr', context.applicationNr], ['scoringWorkflowStepName', c === NavigationTargetCode.MortgageLoanCreditCheckNewFinal ? 'FinalCreditCheck' : 'InitialCreditCheck']])
            } if (c === NavigationTargetCode.MortgageLoanCreditCheckViewInitial || c === NavigationTargetCode.MortgageLoanCreditCheckViewFinal) {
                if (!context || !context.applicationNr) {
                    throw new Error("Missing applicationNr")
                }
                return getLocalModuleUrl('/Ui/MortgageLoan/ViewCreditCheckDetails', [['applicationNr', context.applicationNr], ['scoringWorkflowStepName', c === NavigationTargetCode.MortgageLoanCreditCheckViewFinal ? 'FinalCreditCheck' : 'InitialCreditCheck']])
            } else if (c === NavigationTargetCode.MortgageLoanApplication) {
                if (!context || !context.applicationNr) {
                    throw new Error("Missing applicationNr")
                }
                return getLocalModuleUrl('/Ui/MortgageLoan/Application', [['applicationNr', context.applicationNr]])
            } else if (c === NavigationTargetCode.MortgageLoanLead) {
                if (!context || !context.applicationNr) {
                    throw new Error("Missing applicationNr")
                }
                return getLocalModuleUrl('/Ui/MortgageLoan/Lead', [['applicationNr', context.applicationNr], ['workListId', context.workListId]])
            } else if (c === NavigationTargetCode.MortgageLoanSearch) {
                return getLocalModuleUrl('/Ui/MortgageLoan/Search', [['tabName', 'search']])
            } else if (c == NavigationTargetCode.MortgageLoanCreateLeadWorkList) {
                return getLocalModuleUrl('/Ui/MortgageLoan/Search', [['tabName', 'createWorkList']])
            } else if (c == NavigationTargetCode.MortgageLoanApplications) {
                return getLocalModuleUrl('/Ui/MortgageLoan/Search', [['tabName', 'workList']])
            } else if (c === NavigationTargetCode.MortgageLoanEditCollateral || startsWith(c, NavigationTargetCode.MortgageLoanEditCollateral + '_')) {
                if (!context || !context.applicationNr) {
                    throw new Error("Missing applicationNr")
                }
                let listNr: string = context.listNr
                if (!listNr && c.length > NavigationTargetCode.MortgageLoanEditCollateral.length) {
                    listNr = c.substring(NavigationTargetCode.MortgageLoanEditCollateral.length + 1)
                } else {
                    throw new Error("Missing listNr")
                }
                return getLocalModuleUrl('/Ui/MortgageLoan/Edit-Collateral', [['applicationNr', context.applicationNr], ['listNr', listNr]])
            } else if (c === NavigationTargetCode.MortgageLoanHandleSettlement) {
                return getLocalModuleUrl('/Ui/MortgageLoan/Handle-Settlement', [['applicationNr', context.applicationNr]])
            } else if (c === NavigationTargetCode.UnsecuredLoanCreditCheckNewInitial) {
                if (!context || !context.applicationNr) {
                    throw new Error("Missing applicationNr")
                }
                return getLocalModuleUrl('/CreditCheck/New', [['applicationNr', context.applicationNr]])
            } else if (c === NavigationTargetCode.UnsecuredLoanCreditCheckViewInitial) {
                if (!context || !context.creditDecisionId) {
                    throw new Error("Missing creditDecisionId")
                }
                return getLocalModuleUrl('/CreditCheck/View', [['id', context.creditDecisionId]])
            } else if (c === NavigationTargetCode.UnsecuredLoanApplication) {
                if (!context || !context.applicationNr) {
                    throw new Error("Missing applicationNr")
                }
                return getLocalModuleUrl('/CreditManagement/CreditApplication', [['applicationNr', context.applicationNr]])
            } else if (c == NavigationTargetCode.UnsecuredLoanApplications) {
                return getLocalModuleUrl('/CreditManagement/CreditApplications')
            } else if (c == NavigationTargetCode.CompanyLoanSearch) {
                return getLocalModuleUrl('/Ui/CompanyLoan/Search')
            } else if (c === NavigationTargetCode.UllApplicationBasis) {
                if (!context || !context.applicationNr) {
                    throw new Error("Missing applicationNr")
                }
                return getLocalModuleUrl(`/Ui/Gateway/nBackOffice/s/loan-application/application-basis/${context.applicationNr}`)
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

    export function handleBack(codeOrUrl: CodeOrUrl, apiClient: NTechPreCreditApi.ApiClient, $q: ng.IQService, context: NavigationContext): ng.IPromise<void> {
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

    export function tryNavigateTo(code: NavigationTargetCode, context: NavigationContext): boolean {        
        let url = resolveNavigationUrl(createCodeTarget(code, context), context)
        if (!url) {
            return false
        } else {
            document.location.href = url
            return true
        }
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