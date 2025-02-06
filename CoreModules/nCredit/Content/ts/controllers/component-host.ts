var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components'])

if (ntech.angular) {
    ntech.angular.setupTranslation(app);
}

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

        let apiClient = new NTechCreditApi.ApiClient(onError, $http, $q)

        $scope.isLoading = trafficCop.pending.all > 0;
        trafficCop.addStateChangeListener(() => {
            $scope.isLoading = trafficCop.pending.all > 0;
        });

        let d: ComponentHostNs.ComponentHostInitialData = initialData
        d.apiClient = apiClient
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
        backTarget?: string
        navigationTargetCodeToHere?: string
        backofficeMenuUrl: string
        crossModuleNavigateUrlPattern ?: string
        currentUserId: number
        customerCardUrlPattern: string
        today: string
        userDisplayNameByUserId: NTechCreditApi.IStringDictionary<string>
        providers: { ProviderName: string, DisplayToEnduserName: string, IsSelf: boolean }[]
    }

    export interface ComponentHostInitialData extends ComponentHostServerInitialData {
        apiClient: NTechCreditApi.ApiClient
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