var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components'])

ntech.angular.setupTranslation(app);

class ComponentHostCtr {
    static $inject = ['$scope', '$http', '$q', '$timeout', '$translate', 'ntechComponentService', 'trafficCop', 'ntechLog']
    constructor(
        $scope: ComponentHostNs.IScope,
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $timeout: ng.ITimeoutService,
        private $translate: any,
        private ntechComponentService: NTechComponents.NTechComponentService,
        trafficCop: NTechComponents.NTechHttpTrafficCopService,
        ntechLog: NTechComponents.NTechLoggingService
    ) {
        let onError = errMsg => {
            toastr.error(errMsg);
        }

        let apiClient = new NTechPreCreditApi.ApiClient(onError, $http, $q)
        let companyLoanApiClient = new NTechCompanyLoanPreCreditApi.ApiClient(onError, $http, $q)

        $scope.isLoading = trafficCop.pending.all > 0;
        trafficCop.addStateChangeListener(() => {
            $scope.isLoading = trafficCop.pending.all > 0;
        });

        let d: ComponentHostNs.ComponentHostInitialData = initialData
        d.apiClient = apiClient
        d.companyLoanApiClient = companyLoanApiClient
        d.isLoading = () => $scope.isLoading
        d.setIsLoading = x => $scope.isLoading = x
        d.testFunctions = new ComponentHostNs.TestFunctionsModel()
        if (d.isTest) {
            $scope.testFunctions = d.testFunctions
        }

        $scope.componentInitialData = d

        window.scope = $scope; //for console debugging
    }

    componentInitialData: ComponentHostNs.ComponentHostInitialData
}

app.controller('ctr', ComponentHostCtr)

module ComponentHostNs {
    export interface ComponentHostServerInitialData {
        isTest: true,
        backTarget: string
        backUrl: string
        urlToHere: string
        navigationTargetCodeToHere?: string
        urlToHereFromOtherModule: string
        translation: any
        backofficeMenuUrl: string
        currentUserId: number
        customerCardUrlPattern: string
        today: string
        userDisplayNameByUserId: NTechPreCreditApi.IStringDictionary<string>
    }

    export interface ComponentHostInitialData extends ComponentHostServerInitialData {
        apiClient: NTechPreCreditApi.ApiClient
        companyLoanApiClient: NTechCompanyLoanPreCreditApi.ApiClient
        isLoading: () => boolean
        setIsLoading: (v: boolean) => void
        testFunctions: TestFunctionsModel
    }

    export interface IScope extends ng.IScope {
        isLoading: boolean
        componentInitialData: ComponentHostInitialData
        testFunctions: TestFunctionsModel
    }

    export class TestFunctionsModel {
        constructor() {
            this.items = []
        }
        items: TestFunctionItem[]

        clearItems(exceptForScopeName?: string) {
            let i2: TestFunctionItem[] = []

            for (let i of this.items) {
                if (!!exceptForScopeName && i.scopeName === exceptForScopeName) {
                    i2.push(i)
                }
            }

            this.items = i2
        }

        generateUniqueScopeName() {
            return NTechComponents.generateUniqueId(6)
        }

        addLink(scopeName: string, text: string, linkUrl: string) {
            this.clearItems(scopeName)
            this.items.push({ scopeName: scopeName, text: text, isLink: true, linkUrl: linkUrl })
        }

        addFunctionCall(scopeName: string, text: string, functionCall?: () => void) {
            this.clearItems(scopeName)
            this.items.push({
                scopeName: scopeName, text: text, isFunctionCall: true, functionCall: (evt: Event) => {
                    if (evt) {
                        evt.preventDefault()
                    }
                    functionCall()
                }
            })
        }

        generateTestPdfDataUrl(text: string): string {
            //Small pdf from https://stackoverflow.com/questions/17279712/what-is-the-smallest-possible-valid-pdf
            let pdfData = `%PDF-1.2
9 0 obj
<<
>>
stream
BT/ 9 Tf(${text})' ET
endstream
endobj
4 0 obj
<<
/Type /Page
/Parent 5 0 R
/Contents 9 0 R
>>
endobj
5 0 obj
<<
/Kids [4 0 R ]
/Count 1
/Type /Pages
/MediaBox [ 0 0 300 50 ]
>>
endobj
3 0 obj
<<
/Pages 5 0 R
/Type /Catalog
>>
endobj
trailer
<<
/Root 3 0 R
>>
%%EOF`

            return 'data:application/pdf;base64,' + btoa(unescape(encodeURIComponent(pdfData)))
        }
    }

    export class TestFunctionItem {
        scopeName: string
        text: string
        isLink?: boolean
        linkUrl?: string
        isFunctionCall?: boolean
        functionCall?: (evt: Event) => void
    }
}