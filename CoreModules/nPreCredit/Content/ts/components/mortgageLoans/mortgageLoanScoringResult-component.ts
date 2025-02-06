namespace MortgageLoanScoringResultComponentNs {

    export class MortgageLoanScoringResultController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model        
        decisionDetailsDialogId: string
        householdIncomeDialogId: string

        static $inject = ['$http', '$q', '$filter', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            private $filter: ng.IFilterService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);

            this.decisionDetailsDialogId = modalDialogService.generateDialogId()

            this.householdIncomeDialogId = modalDialogService.generateDialogId()            
            this.modalDialogService.subscribeToDialogEvents(e => {
                if (e.dialogId !== this.householdIncomeDialogId || !e.isClosed) {
                    return
                }

                let newIncome = this.m.householdIncomeDialogModel.newIncome
                this.m.householdIncomeDialogModel = null
                if (newIncome != null) {
                    if (this.initialData.onBasisDataChanged) {
                        this.initialData.onBasisDataChanged()
                    } else {
                        this.signalReloadRequired()
                    }
                }                
            })
        }

        componentName(): string {
            return 'mortgageLoanScoringResult'
        }

        onChanges() {
            this.m = null;
            if (!this.initialData) {
                return
            }
            let s = this.initialData.scoringResult
            if (!s) {
                return
            }

            let getReasonNameByRuleName = x => this.initialData.rejectionReasonNameByScoringRuleName[x] || ('DirectlyByRule' + x)

            this.m = {
                isAccepted: s.IsAccepted,
                riskClass: s.RiskClass,
                loanFraction: this.computeLoanFractionPercent(s.ScoringBasis),
                offer: s.AcceptedOffer,
                rejectionReasonNames: s.IsAccepted ? null : _.uniq(_.map(s.RejectionRuleNames, getReasonNameByRuleName)),
                rejectionReasonDetailItems: s.IsAccepted ? null : this.toRejectionDetails(_.groupBy<string>(s.RejectionRuleNames, getReasonNameByRuleName)),
                scorePointsInitialData: s.ScorePointsByRuleName ? this.createScorePointsInitialData(s.ScorePointsByRuleName) : null,
                scoreModelDataInitialData: s.ScoringBasis ? {
                    columns: [{ className: 'col-xs-5', labelText: 'Name' }, { className: 'col-xs-2', labelText: 'Level' }, { className: 'col-xs-5', labelText: 'Value' }],
                    tableRows: NTechPreCreditApi.ScoringDataModel.toDataTable(s.ScoringBasis)
                } : null,
                manualAttentionInitialData: s.ManualAttentionRules ? {
                    columns: [{ className: 'col-xs-12', labelText: 'Rule' }],
                    tableRows: _.map(s.ManualAttentionRules, ruleName => [ruleName])
                } : null,
                applicantCreditReports: this.createApplicantCreditReports(s.ScoringBasis),
                ucTemplateRejectionCodes: this.createUcTemplateRejectionCodes(s.ScoringBasis)
            }
        }

        showDecisionDetails(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.modalDialogService.openDialog(this.decisionDetailsDialogId)
        }

        private createScorePointsInitialData(s: { [index: string]: number }): SimpleTableComponentNs.InitialData {
            let rows: string[][] = []
            let sum = 0
            for (let ruleName of Object.keys(s)) {                
                let rulePoints = s[ruleName]
                sum = sum + rulePoints
                rows.push([ruleName, rulePoints.toString()])
            }
            rows.push(['  Sum', sum.toString()])
            
            return {
                columns: [{ className: 'col-xs-8', labelText: 'Rule' }, { className: 'col-xs-4', labelText: 'Points' }],
                tableRows: rows
            }
        }

        private createApplicantCreditReports(s: NTechPreCreditApi.ScoringDataModel): ApplicantCreditReport[] {            
            if (!s || !s.ApplicantItems) {
                return []
            }

            let r: ApplicantCreditReport[] = []
            for (var applicantNr in s.ApplicantItems) {
                let key = s.ApplicantItems[applicantNr]['creditReportHtmlReportArchiveKey']
                if (key != null) {
                    let rr = { applicantNr: parseInt(applicantNr), htmlArchiveKey: key, creditReportDialogId: this.modalDialogService.generateDialogId(), loadCreditReportDocument: false }
                    r.push(rr)
                    this.modalDialogService.subscribeToDialogEvents(e => {
                        if (e.dialogId === rr.creditReportDialogId && e.isOpenRequest) {
                            rr.loadCreditReportDocument = true
                        }
                    })
                }
            }

            return r
        }

        getHouseholdGrossMonthlyIncome() {
            if (!this.initialData || !this.initialData.scoringResult || !this.initialData.scoringResult.ScoringBasis) {
                return null
            }
            let s = this.initialData.scoringResult.ScoringBasis

            let income = 0
            for (let applicantNr = 1; applicantNr <= this.initialData.applicationInfo.NrOfApplicants; applicantNr++) {
                let a = s.ApplicantItems[applicantNr]
                if (a) {
                    let applicantIncome = this.parseDecimalOrNull(a['grossMonthlyIncome'])
                    income += applicantIncome
                }
            }

            return income
        }

        private createUcTemplateRejectionCodes(s: NTechPreCreditApi.ScoringDataModel): string[] {
            if (!s.ApplicantItems) {
                return null
            }
            let codes: string[] = []
            for (var applicantNr in s.ApplicantItems) {
                let c = s.ApplicantItems[applicantNr]['creditReportTemplateReasonCode']
                if (c) {
                     //Split into groups of length three            
                    for (let code of c.match(/.{3}/g)) {
                        codes.push(code)
                    }
                }
            }
            
            return _.uniq(codes)
        }

        private computeLoanFractionPercent(s: NTechPreCreditApi.ScoringDataModel): number {
            if (!s) {
                return null
            }
            let loanAmountStr = s.ApplicationItems['loanAmount']
            let objectValueStr = s.ApplicationItems['objectValue']
            if (!(loanAmountStr && objectValueStr)) {
                return null
            }
            let loanAmount = parseFloat(loanAmountStr)
            let objectValue = parseFloat(objectValueStr)
            if (objectValue > 0) {
                return 100.0 * (loanAmount / objectValue)
            } else {
                return null
            }
        }

        private toRejectionDetails(rejectionRuleNamesGroupedByReasonName: _.Dictionary<string[]>): RejectionReasonDetailItem[] {
            let items : RejectionReasonDetailItem[] = []
            for (var key in rejectionRuleNamesGroupedByReasonName) {
                items.push({ reasonName : key, ruleNames: rejectionRuleNamesGroupedByReasonName[key]})
            }
            return items
        }

        getRejectionReasonDisplayName(name: string) {
            if (!this.initialData) {
                return null
            }
            var dn = this.initialData.rejectionReasonDisplayNameName[name]
            if (dn) {
                return dn
            } else {
                return name
            }
        }

        getUcTemplateRejectionReason(code: string) {
            if (!this.initialData || !this.initialData.ucTemplateRejectionReasons) {
                return code
            }
            var d = this.initialData.ucTemplateRejectionReasons[code]
            if (!d) {
                return code
            }
            return d
        }
        
        leftToLiveOnExpanded = () => {
            this.apiClient.fetchLeftToLiveOnRequiredItemNames().then(result => {
                if (!this.m) {
                    return
                }
                let d: { [key: string]: string } = {}

                let id: SimpleFormComponentNs.InitialData = {
                    modelBase: d,
                    items: []
                }

                let b = angular.copy(this.initialData.scoringResult.ScoringBasis)

                let nrOfApplicants = parseInt(b.ApplicationItems['nrOfApplicants'])

                for (let itemName of result.RequiredApplicationItems) {
                    let itemValue = b.ApplicationItems[itemName]
                    let modelName = `n_${itemName}`
                    d[modelName] = itemValue                        
                    if (itemName == 'nrOfApplicants') {                        
                        id.items.push(SimpleFormComponentNs.textView({ labelText: itemName, model: modelName }))
                    } else {
                        id.items.push(SimpleFormComponentNs.textField({ labelText: itemName, model: modelName, required: true }))
                    }
                }

                for (let itemName of result.RequiredApplicantItems) {
                    for (var applicantNr = 1; applicantNr <= nrOfApplicants; applicantNr++) {
                        let itemValue = b.ApplicantItems[applicantNr][itemName]
                        let modelName = `a_${applicantNr}_${itemName}`
                        d[modelName] = itemValue
                        id.items.push(SimpleFormComponentNs.textField({ labelText: `Applicant ${applicantNr} - ${itemName}`, model: modelName, required: true }))
                    }
                }

                if (this.initialData.scoringResult.IsAccepted && this.initialData.scoringResult.AcceptedOffer) {
                    d['i_interestRatePercent'] = this.initialData.scoringResult.AcceptedOffer.NominalInterestRatePercent.toString()
                } else {
                    d['i_interestRatePercent'] = ''
                }
                
                id.items.push(SimpleFormComponentNs.textField({ labelText: 'interestRatePercent', model: 'i_interestRatePercent', required: true }))

                let onClick = () => {
                    let scoringBasis = angular.copy(this.initialData.scoringResult.ScoringBasis)
                    let interestRatePercent: number = null
                    for (var keyName in d) {
                        let newValue = d[keyName]
                        if (keyName[0] == 'n') {
                            scoringBasis.ApplicationItems[keyName.substring(2)] = newValue
                        } else if (keyName[0] == 'a') {
                            let applicantNr = parseInt(keyName.substring(2, 3))
                            scoringBasis.ApplicantItems[applicantNr][keyName.substring(4)] = newValue
                        } else if (keyName[0] == 'i') {
                            interestRatePercent = parseFloat(newValue)
                        }
                    }
                    this.apiClient.computeLeftToLiveOn(scoringBasis, interestRatePercent).then(ltlResult => {
                        let rows: string[][] = []

                        rows.push(['Main', '', ''])
                        rows.push(['', 'Left to live on', this.$filter('currency')(ltlResult.LeftToLiveOnAmount)])
                        rows.push(['', 'Debt/Income multiplier', this.$filter('number')(ltlResult.DebtMultiplier) + 'x'])
                        rows.push(['', 'Loan fraction', this.$filter('number')(ltlResult.LoanFraction * 100) + '%'])
                        rows.push(['Parts', '', ''])
                        for (let p of ltlResult.LeftToLiveOnParts) {
                            rows.push(['', p.Name, p.Value.toString()])
                        }
                        this.m.leftToLiveOnResultInitialData = {
                            columns: [{ className: 'col-xs-2', labelText: 'Type' }, { className: 'col-xs-4', labelText: 'Name' }, { className: 'cols-xs-6', labelText: 'Value' }],
                            tableRows: rows
                        }
                    })
                }

                id.items.push(SimpleFormComponentNs.button({ buttonText: 'Calculate', onClick: onClick }))

                this.m.leftToLiveOnFormInitialData = id
            })            
        }

        gotoHouseholdIncome(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.m) {
                return
            }
            this.m.householdIncomeDialogModel = {
                newIncome: null,
                initialData: {
                    applicationInfo: this.initialData.applicationInfo,
                    onIncomeChanged: (newIncome) => {
                        if (newIncome != null) {
                            this.m.householdIncomeDialogModel.newIncome = newIncome
                        }                        
                    },
                    hideHeader: true
                }
            }
            this.modalDialogService.openDialog(this.householdIncomeDialogId)
        }

        showCreditReport(r: ApplicantCreditReport, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.modalDialogService.openDialog(r.creditReportDialogId)
        }        
    }
    
    export class MortgageLoanScoringResultComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanScoringResultController;
            this.templateUrl = 'mortgage-loan-scoring-result.html';
        }
    }

    export class InitialData {
        applicationInfo: NTechPreCreditApi.ApplicationInfoModel
        scoringResult: NTechPreCreditApi.MortageLoanScoringResult
        rejectionReasonNameByScoringRuleName: {[key: string] : string}
        rejectionReasonDisplayNameName: { [key: string]: string }
        ucTemplateRejectionReasons?: { [key: string]: string }
        onBasisDataChanged?: () => void
        isBasisChangeAllowed?: boolean
    }

    export class Model {
        isAccepted: boolean
        riskClass: string
        loanFraction: number
        offer: NTechPreCreditApi.MortageLoanOffer
        rejectionReasonNames: string[]
        rejectionReasonDetailItems: RejectionReasonDetailItem[]
        scorePointsInitialData: SimpleTableComponentNs.InitialData
        scoreModelDataInitialData: SimpleTableComponentNs.InitialData
        manualAttentionInitialData: SimpleTableComponentNs.InitialData
        leftToLiveOnFormInitialData?: SimpleFormComponentNs.InitialData
        leftToLiveOnResultInitialData?: SimpleTableComponentNs.InitialData
        applicantCreditReports: ApplicantCreditReport[]
        ucTemplateRejectionCodes: string[]
        householdIncomeDialogModel?: HouseholdIncomeDialogModel
    }

    export class ApplicantCreditReport {
        applicantNr: number
        htmlArchiveKey: string
        creditReportDialogId: string
        loadCreditReportDocument: boolean
    }

    export class RejectionReasonDetailItem {
        reasonName: string
        ruleNames: string[]
        isDetailsExpanded?: boolean
    }

    export class HouseholdIncomeDialogModel {
        initialData?: MortgageLoanApplicationHouseholdIncomeComponentNs.InitialData
        newIncome: number
    }
}

angular.module('ntech.components').component('mortgageLoanScoringResult', new MortgageLoanScoringResultComponentNs.MortgageLoanScoringResultComponent())