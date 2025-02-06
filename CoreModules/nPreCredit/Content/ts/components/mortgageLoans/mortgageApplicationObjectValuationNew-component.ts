namespace MortgageApplicationObjectValuationNewComponentNs {

    export class MortgageApplicationObjectValuationNewController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData;

        applicationObjectInitialData: MortgageApplicationObjectInfoComponentNs.InitialData
        ucbvSearchInitialData: MortgageApplicationObjectValuationManualSearchComponentNs.InitialData

        isResultExpanded: boolean
        resultData: NTechPreCreditApi.MortgageApplicationValutionResult
        resultForm: SimpleFormComponentNs.InitialData

        manualResultData: NTechPreCreditApi.MortgageApplicationValutionResult
        manualResultForm: SimpleFormComponentNs.InitialData
        isManualResultExpanded: boolean
        automationFailedMessage: string

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'mortgageApplicationObjectValuationNew'
        }

        onChanges() {
            this.applicationObjectInitialData = null
            this.ucbvSearchInitialData = null
            if (this.initialData) {
                this.applicationObjectInitialData = {
                    applicationInfo: this.initialData.applicationInfo
                }
                this.ucbvSearchInitialData = {
                    applicationInfo: this.initialData.applicationInfo,
                    backUrl: this.initialData.backUrl
                }
            }
            
            //Result
            this.setupManualEditResult(new NTechPreCreditApi.MortgageApplicationValutionResult())

            if (this.initialData.callAutomateCustomerOnInit) {
                this.apiClient.tryAutomateMortgageApplicationValution(this.initialData.applicationInfo.ApplicationNr).then(result => {
                    this.setupAutomatedResult(result)
                    if (this.initialData.autoAcceptSuggestion) {
                        this.acceptResult(this.resultData, null)
                    }
                })
            }
        }

        setupAutomatedResult(result: NTechPreCreditApi.MortgageApplicationTryValutionResult) {
            if (result.IsSuccess) {                
                let data = result.SuccessData
                this.isResultExpanded = true
                this.resultData = data
                this.resultForm = {
                    modelBase: this.resultData,
                    items: [
                        SimpleFormComponentNs.textView({ labelText: 'Ucbv - ObjektId', model: 'ucbvObjektId' }),
                        SimpleFormComponentNs.textView({ labelText: 'Skatteverket lgh-nr', model: 'brfLghSkvLghNr' }),
                        SimpleFormComponentNs.textView({ labelText: 'Foreningsnamn', model: 'brfNamn' }),
                        SimpleFormComponentNs.textView({ labelText: 'Yta', model: 'brfLghYta' }),
                        SimpleFormComponentNs.textView({ labelText: 'Vaning', model: 'brfLghVaning' }),
                        SimpleFormComponentNs.textView({ labelText: 'Antal rum', model: 'brfLghAntalRum' }),
                        SimpleFormComponentNs.textView({ labelText: 'Varde', model: 'brfLghVarde' }),
                        SimpleFormComponentNs.textView({ labelText: 'Brf signal - ar', model: 'brfSignalAr' }),
                        SimpleFormComponentNs.textView({ labelText: 'Brf signal - belaning', model: 'brfSignalBelaning' }),
                        SimpleFormComponentNs.textView({ labelText: 'Brf signal - likviditet', model: 'brfSignalLikviditet' }),
                        SimpleFormComponentNs.textView({ labelText: 'Brf signal - Sjalvforsorjningsgrad', model: 'brfSignalSjalvforsorjningsgrad' }),
                        SimpleFormComponentNs.textView({ labelText: 'Brf signal - Rantekanslighet', model: 'brfSignalRantekanslighet' }),
                        SimpleFormComponentNs.textView({ labelText: 'Lghs andel av brfs skulder (kr)', model: 'brfLghDebtAmount' }),
                        SimpleFormComponentNs.button({ buttonText: 'Approve', onClick: () => { this.acceptResult(this.resultData, null); }, buttonType: SimpleFormComponentNs.ButtonType.Accept })
                    ]
                }
                this.automationFailedMessage = null
            } else {
                this.isResultExpanded = false
                this.resultData = null
                this.resultForm = null
                this.automationFailedMessage = result.FailedMessage
            }
        }

        setupManualEditResult(data: NTechPreCreditApi.MortgageApplicationValutionResult) {
            this.manualResultData = data
            this.manualResultForm = {
                modelBase: this.manualResultData,
                items: [
                    SimpleFormComponentNs.textField({ labelText: 'Ucbv - ObjektId', model: 'ucbvObjektId', required: true }),
                    SimpleFormComponentNs.textField({ labelText: 'Skatteverket lgh-nr', model: 'brfLghSkvLghNr', required: true }),
                    SimpleFormComponentNs.textField({ labelText: 'Foreningsnamn', model: 'brfNamn', required: true }),
                    SimpleFormComponentNs.textField({ labelText: 'Yta', model: 'brfLghYta', required: true }),
                    SimpleFormComponentNs.textField({ labelText: 'Vaning', model: 'brfLghVaning', required: true }),
                    SimpleFormComponentNs.textField({ labelText: 'Antal rum', model: 'brfLghAntalRum', required: true }),
                    SimpleFormComponentNs.textField({ labelText: 'Varde', model: 'brfLghVarde', required: true }),
                    SimpleFormComponentNs.textField({ labelText: 'Brf signal - ar', model: 'brfSignalAr' }),
                    SimpleFormComponentNs.textField({ labelText: 'Brf signal - belaning', model: 'brfSignalBelaning' }),
                    SimpleFormComponentNs.textField({ labelText: 'Brf signal - likviditet', model: 'brfSignalLikviditet' }),
                    SimpleFormComponentNs.textField({ labelText: 'Brf signal - Sjalvforsorjningsgrad', model: 'brfSignalSjalvforsorjningsgrad' }),
                    SimpleFormComponentNs.textField({ labelText: 'Brf signal - Rantekanslighet', model: 'brfSignalRantekanslighet' }),
                    SimpleFormComponentNs.textField({ labelText: 'Lghs andel av brfs skulder (kr)', model: 'brfLghDebtAmount' }),
                    SimpleFormComponentNs.button({ buttonText: 'Approve', onClick: () => { this.acceptResult(this.manualResultData, null); }, buttonType: SimpleFormComponentNs.ButtonType.Accept })
                ]
            }
        }

        acceptResult(result: NTechPreCreditApi.MortgageApplicationValutionResult, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.acceptMortgageLoanUcbvValuation(this.initialData.applicationInfo.ApplicationNr, result).then(() => {
                document.location.href = this.initialData.backUrl
            });
        }

        arrayToCommaList(a: string[]) {
            if (!a) {
                return null
            } else {
                let s = ''
                angular.forEach(a, x => {
                    if (s.length > 0) {
                        s += ', '
                    }
                    s += x
                })
                return s
            }
        }

        brfSignalToCode(value: number): string {
            if (value === 0) {
                return "Okand"
            } else if (value === 1) {
                return "Ok"
            } else if (value === 2) {
                return "Varning"
            } else if (!value) {
                return null
            } else {
                return 'Kod' + value.toString()
            }
        }
    }

    export class MortgageApplicationObjectValuationNewComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationObjectValuationNewController;
            this.templateUrl = 'mortgage-application-object-valuation-new.html';
        }
    }

    export class InitialData {
        applicationInfo: NTechPreCreditApi.ApplicationInfoModel
        backUrl: string
        callAutomateCustomerOnInit: boolean
        autoAcceptSuggestion?: boolean
    }
}

angular.module('ntech.components').component('mortgageApplicationObjectValuationNew', new MortgageApplicationObjectValuationNewComponentNs.MortgageApplicationObjectValuationNewComponent())