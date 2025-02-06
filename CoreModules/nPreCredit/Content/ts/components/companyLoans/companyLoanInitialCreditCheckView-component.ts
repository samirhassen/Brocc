namespace CompanyLoanInitialCreditCheckViewComponentNs {

    export class CompanyLoanInitialCreditCheckViewController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model


        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        onBack = (evt) => {
            if (evt) {
                evt.preventDefault()
            }
            let target = this.initialData.backTarget 
                ? NavigationTargetHelper.createCodeTarget(this.initialData.backTarget)
                : NavigationTargetHelper.createCrossModule('CompanyLoanApplication', { applicationNr: initialData.applicationNr })            
            
            NavigationTargetHelper.handleBack(target, this.apiClient, this.$q, { applicationNr: initialData.applicationNr })
        }

        componentName(): string {
            return 'companyLoanInitialCreditCheckView'
        }

        headerClassFromStatus(isAccepted: boolean) {
            return { 'text-success': isAccepted, 'text-danger': !isAccepted }
        }

        iconClassFromStatus(isAccepted: boolean) {
            return { 'glyphicon-ok': isAccepted, 'glyphicon-remove': !isAccepted, 'glyphicon': true, 'text-success': isAccepted, 'text-danger': !isAccepted }
        }

        getDisplayRejectionReason(reasonName: string) {
            let n: string = null
            let mapping = this.m.recommendationInitialData.rejectionReasonToDisplayNameMapping            
            if (mapping) {
                n = mapping[reasonName]
            }
            return n ? n : reasonName
        }

        init(applicationNr: string, result: NTechCompanyLoanPreCreditApi.FetchCurrentCreditDecisionResult) {
            this.m = {
                applicationNr: applicationNr,
                recommendationInitialData: {
                    applicationNr: applicationNr,
                    recommendation: result.Decision.Recommendation,
                    apiClient: this.initialData.apiClient,
                    companyLoanApiClient: this.initialData.companyLoanApiClient,
                    creditUrlPattern: this.initialData.creditUrlPattern,
                    isTest: this.initialData.isTest,
                    rejectionReasonToDisplayNameMapping: this.initialData.rejectionReasonToDisplayNameMapping,
                    rejectionRuleToReasonNameMapping: this.initialData.rejectionRuleToReasonNameMapping,
                    isEditAllowed: false,
                    navigationTargetToHere: NTechNavigationTarget.createCrossModuleNavigationTargetCode("CompanyLoanViewCreditCheckDetails", { applicationNr: applicationNr })
                },
                d: result.Decision
            }
        }
        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            this.initialData.companyLoanApiClient.fetchCurrentCreditDecision(this.initialData.applicationNr).then(x => {
                this.init(this.initialData.applicationNr, x)
            })
        }
    }

    export class CompanyLoanInitialCreditCheckViewComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanInitialCreditCheckViewController;
            this.templateUrl = 'company-loan-initial-credit-check-view.html';
        }
    }

    export interface LocalInitialData {
        applicationNr: string
        rejectionReasonToDisplayNameMapping: NTechPreCreditApi.IStringDictionary<string>
        rejectionRuleToReasonNameMapping: NTechPreCreditApi.IStringDictionary<string>
        creditUrlPattern: string
    }
  
    export interface InitialData extends ComponentHostNs.ComponentHostInitialData, LocalInitialData {

    }

    export class Model {
        applicationNr: string
        d: NTechCompanyLoanPreCreditApi.FetchCurrentCreditDecisionResultDecision
        recommendationInitialData: CompanyLoanInitialCreditCheckRecommendationComponentNs.InitialData
    }
}

angular.module('ntech.components').component('companyLoanInitialCreditCheckView', new CompanyLoanInitialCreditCheckViewComponentNs.CompanyLoanInitialCreditCheckViewComponent())