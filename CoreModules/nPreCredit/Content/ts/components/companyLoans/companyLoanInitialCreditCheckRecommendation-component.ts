namespace CompanyLoanInitialCreditCheckRecommendationComponentNs {

    export class CompanyLoanInitialCreditCheckRecommendationController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model
        decisionDetailsDialogId: string
        companyCreditReportDialogId: string

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService', '$sce']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService,
            private $sce: ng.ISCEService) {
            super(ntechComponentService, $http, $q);

            this.decisionDetailsDialogId = modalDialogService.generateDialogId()
            this.companyCreditReportDialogId = modalDialogService.generateDialogId()
            window[this.componentName() + '_debug_' + this.decisionDetailsDialogId] = this
        }

        componentName(): string {
            return 'companyLoanInitialCreditCheckRecommendation'
        }

        getCreditUrl(creditNr: string) {
            return this.initialData.creditUrlPattern.replace('NNN', creditNr)
        }

        onChanges() {
            this.m = null
            if (!this.initialData) {
                return
            }

            let r = NTechPreCreditApi.FetchApplicationDataSourceRequestItem.createCreditApplicationItemSource(
                ['application.companyAgeInMonths', 'application.companyYearlyRevenue', 'application.companyYearlyResult', 'application.companyCurrentDebtAmount', 'application.loanPurposeCode',
                    'application.forceExternalScoring'],
                false, true, '-', true)

            this.initialData.apiClient.fetchApplicationDataSourceItems(
                this.initialData.applicationNr, [r]).then(x => {
                this.m = {
                    applicationNr: this.initialData.applicationNr,
                    recommendation: this.initialData.recommendation,
                    loadCreditReportDocument: false,
                    creditApplicationItems: NTechPreCreditApi.FetchApplicationDataSourceRequestItem.resultAsDictionary(x.Results[0].Items),
                    changedCreditApplicationItemNames: x.Results[0].ChangedNames
                }
            })
        }

        getCreditApplicationItemDisplayValue(groupedName: string, dataType: string) {
            let v = this.m && this.m.creditApplicationItems ? this.m.creditApplicationItems[groupedName] : '-'
            if (v === '-') {
                return null
            }

            if (dataType === 'int') {
                return Math.round(this.parseDecimalOrNull(v))
            } else if (dataType == 'decimal') {
                return this.parseDecimalOrNull(v)
            } else {
                return v
            }
        }

        isCreditApplicationItemEdited(groupedName: string) {
            if (!this.m || !this.m.changedCreditApplicationItemNames) {
                return false
            }
            return this.m.changedCreditApplicationItemNames.indexOf(groupedName) >= 0
        }

        getEditApplicationItemUrl(groupedName: string) {
            if (!this.initialData) {
                return null
            }
            return `/Ui/CompanyLoan/Application/EditItem?applicationNr=${this.initialData.applicationNr}&dataSourceName=CreditApplicationItem&itemName=${groupedName}&ro=${this.initialData.isEditAllowed ? 'False' : 'True'}&backTarget=${this.initialData.navigationTargetToHere}`
        }

        hasManulControlReasons() {
            return this.m && this.m.recommendation.ManualAttentionRuleNames && this.m.recommendation.ManualAttentionRuleNames.length > 0
        }

        getScoringDataStr(name: string, defaultValue?: string): string {
            let v = this.m && this.m.recommendation && this.m.recommendation.ScoringData && this.m.recommendation.ScoringData.ApplicationItems[name]
            return (!v && defaultValue) ? defaultValue : v            
        }

        getRecommendationRejectionReasonDisplayNames() {
            if (!this.initialData) {
                return null
            }
            if (!this.m || !this.m.recommendation) {
                return null
            }

            if (this.m.recommendation.WasAccepted) {
                return []
            }

            let reasonsPre = {} //dedupe
            for (let ruleName of this.m.recommendation.RejectionRuleNames) {
                let reasonName = this.initialData.rejectionRuleToReasonNameMapping[ruleName]
                if (reasonName) {
                    reasonsPre[reasonName] = this.initialData.rejectionReasonToDisplayNameMapping[reasonName]
                }
            }

            let reasons = []
            for (let reasonName of Object.keys(reasonsPre)) {
                reasons.push(reasonsPre[reasonName])
            }
            return reasons
        }

        showDetails(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            this.modalDialogService.openDialog(this.decisionDetailsDialogId)
        }

        getRejectionReasonDisplayNameByRuleName(ruleName: string) {
            if (!this.initialData) {
                return ''
            }
            let reasonName = this.initialData.rejectionRuleToReasonNameMapping[ruleName]
            let displayName = this.initialData.rejectionReasonToDisplayNameMapping[reasonName]
            return displayName ? displayName : reasonName
        }

        getScorePointRuleNames() {
            if (!this.m) {
                return []
            }
            return Object.keys(this.m.recommendation.ScorePointsByRuleName)
        }

        getScorePointDebugData(ruleName: string): WeightedPointRuleDebugData {
            if (!this.m.recommendation.DebugDataByRuleNames) {
                return null
            }
            let dRaw = this.m.recommendation.DebugDataByRuleNames[ruleName]
            if (!dRaw) {
                return null
            }
            let d: WeightedPointRuleDebugData = JSON.parse(dRaw)
            return d
        }

        getRejectionRuleDebugData(ruleName: string): string {
            if (!this.m.recommendation.DebugDataByRuleNames) {
                return null
            }
            return this.m.recommendation.DebugDataByRuleNames[ruleName]
        }

        getNonScorePointDebugDataRuleNames() {
            let n = []
            if (!this.m || !this.m.recommendation || !this.m.recommendation.DebugDataByRuleNames) {
                return n
            }
            let s = this.m.recommendation.ScorePointsByRuleName ? this.m.recommendation.ScorePointsByRuleName : {}
            for (let r of Object.keys(this.m.recommendation.DebugDataByRuleNames)) {
                if (!(r in s)) {
                    n.push(r)
                }    
            }
            return n
        }

        toggleRuleDebugDetails(ruleName: string, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.m) {
                return
            }
            this.m.debugDetailsRuleName = this.m.debugDetailsRuleName === ruleName ? null : ruleName
        }

        getScoringDataItemNames() {
            if (!this.m) {
                return []
            }
            return Object.keys(this.m.recommendation.ScoringData.ApplicationItems)
        }

        showCreditReport(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }            
            this.m.loadCreditReportDocument = true
            this.modalDialogService.openDialog(this.companyCreditReportDialogId)
        }

        getCreditReportIFrameHtml() {
            if (!this.initialData) {
                return null
            }
            let html = `<iframe src="/CreditManagement/ArchiveDocument?key=${this.getScoringDataStr('companyCreditReportHtmlArchiveKey', '')}" allowfullscreen></iframe>`
            return this.$sce.trustAsHtml(html)
        }
    }

    export class CompanyLoanInitialCreditCheckRecommendationComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanInitialCreditCheckRecommendationController;
            this.templateUrl = 'company-loan-initial-credit-check-recommendation.html';
        }
    }

    export class InitialData {
        isTest: true
        apiClient: NTechPreCreditApi.ApiClient
        companyLoanApiClient: NTechCompanyLoanPreCreditApi.ApiClient
        rejectionReasonToDisplayNameMapping: NTechPreCreditApi.IStringDictionary<string>
        rejectionRuleToReasonNameMapping: NTechPreCreditApi.IStringDictionary<string>
        creditUrlPattern: string
        applicationNr: string
        recommendation: NTechCompanyLoanPreCreditApi.CompanyLoanScoringRecommendationModel
        isEditAllowed: boolean
        navigationTargetToHere: string
    }

    export class Model {
        applicationNr: string
        recommendation: NTechCompanyLoanPreCreditApi.CompanyLoanScoringRecommendationModel
        loadCreditReportDocument: boolean
        debugDetailsRuleName?: string
        creditApplicationItems: NTechPreCreditApi.IStringDictionary<string>
        changedCreditApplicationItemNames: string[]
    }
    
    export interface WeightedPointRuleDebugData {
        weight: number
        unweightedPoints: number
        totalWeight: number
    }
}

angular.module('ntech.components').component('companyLoanInitialCreditCheckRecommendation', new CompanyLoanInitialCreditCheckRecommendationComponentNs.CompanyLoanInitialCreditCheckRecommendationComponent())