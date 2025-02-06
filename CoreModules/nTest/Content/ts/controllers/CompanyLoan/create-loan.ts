var app = angular.module('app', ['ntech.forms', 'angular-jsoneditor'])

class CompanyLoanCreateLoanCtr {
    static $inject = ['$scope', '$http', '$q', '$timeout']
    constructor(
        $scope: ng.IScope,
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $timeout: ng.ITimeoutService
    ) {
        this.initialDataTyped = initialData;
        this.backUrl = this.initialDataTyped.backUrl
        window['ntechDebug'] = window['ntechDebug'] || {}
        window['ntechDebug']['companyLoanCreateLoan'] = this

        this.apiClient = new NTechTestApi.ApiClient(msg => toastr.error(msg), $http, $q, x => this.isLoading = x)
        this.a = new CompanyLoanCreateLoanNs.LoanModel

        let h = localStorage.getItem(CompanyLoanCreateLoanNs.HistoryItemsKey)
        if (h) {
            this.historyItems = JSON.parse(h)
        } else {
            this.historyItems = []
        }        
    }
    state: string = 'initial'
    backUrl: string
    a: CompanyLoanCreateLoanNs.LoanModel
    apiClient: NTechTestApi.ApiClient
    isLoading: boolean = false
    initialDataTyped: CompanyLoanCreateLoanNs.IInitialData
    createResult: NTechTestApi.CreateCompanyLoanResponseModel
    historyItems: CompanyLoanCreateLoanNs.HistoryItem[]

    submitInitial(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }

        let handleApplicant = () => {
            if (this.a.applicantMode === 'New') {
                this.apiClient.getOrGenerateTestPerson(null, true).then(x => {
                    this.a.applicantCivicRegNr = x.CivicRegNr
                    this.a.newApplicantEditorData = x.Properties
                    this.state = 'applicants'
                })
            } else {
                this.a.applicantCivicRegNr = ''
                this.a.newApplicantEditorData = null
                this.state = 'applicants'
            }
        }

        if (this.a.companyMode === 'New') {
            this.apiClient.getOrGenerateTestCompany(null, null, true, false, this.a.bankAccountType).then(x => {
                this.a.companyOrgnr = x.Orgnr
                this.a.newCompanyEditorData = x.Properties
                handleApplicant()
                
            })
        } else {
            this.a.companyOrgnr = ''
            this.a.newCompanyEditorData = null
            handleApplicant()
        }
    }    

    submitApplicants(evt: Event) {
        let persons : string[] = []
        if (this.a.applicantMode === 'New') {
            persons.push(JSON.stringify(this.a.newApplicantEditorData))
        }

        let companies: string[] = []
        if (this.a.companyMode === 'New') {
            companies.push(JSON.stringify(this.a.newCompanyEditorData))
        }

        let handle = (companyEditorData: any) => {
            this.apiClient.createOrUpdateTestCompanies(companies, true).then(() => {
                this.apiClient.createOrUpdateTestPersons(persons, true).then(() => {
                    this.apiClient.generateNewCreditNumber().then(x => {
                        let customerProperties: NTechTestApi.StringDictionary = {}
                        customerProperties['addressStreet'] = companyEditorData['addressStreet']
                        customerProperties['addressZipcode'] = companyEditorData['addressZipcode']
                        customerProperties['addressCity'] = companyEditorData['addressCity']
                        this.apiClient.addCompanyToCustomerModule(
                            this.a.companyOrgnr,
                            companyEditorData['companyName'],
                            customerProperties
                        ).then(companyOrgnrResult => {

                                let createLoanReqeuest: NTechTestApi.CreateCompanyLoanRequestModel = {
                                    AgreementPdfArchiveKey: null,
                                    CreditAmount: 50000,
                                    AnnuityAmount: 2000,
                                    ApplicationNr: null,
                                    BankAccountNr: companyEditorData['bankAccountNr'] ? companyEditorData['bankAccountNr'] : null,
                                    BankAccountNrType: companyEditorData['bankAccountNrType'] ? companyEditorData['bankAccountNrType'] : null,
                                    Iban: companyEditorData['iban'] ? companyEditorData['iban'] : null,
                                    CapitalizedInitialFeeAmount: 0,
                                    DrawnFromLoanAmountInitialFeeAmount: 1000,
                                    ProviderApplicationId: null,
                                    CampaignCode: null,
                                    CompanyCustomerId: companyOrgnrResult.CustomerId,
                                    CreditNr: x.nr,
                                    MarginInterestRatePercent: 10,
                                    NotificationFee: 10,
                                    ProviderName: 'self',
                                    SourceChannel: null
                                }
                                this.a.loanEditorData = createLoanReqeuest
                                this.state = 'loan'
                            })
                    })
                })
            })
        }

        if (this.a.companyMode === 'New') {
            handle(this.a.newCompanyEditorData)
        } else {
            this.apiClient.getOrGenerateTestCompany(this.a.companyOrgnr, null, true, false, this.a.bankAccountType).then(x => {
                handle(x.Properties)
            })
        }
    }

    createLoan(evt: Event) {        
        this.apiClient.createCompanyLoanCredit(this.a.loanEditorData).then(y => {
            this.createResult = y
            this.state = 'done'
            this.addHistoryItem({
                applicantCivicRegNr: this.a.applicantCivicRegNr,
                companyOrgNr: this.a.companyOrgnr,
                creditNr: y.CreditNr,
                date: moment().format('YYYY-MM-DD HH:mm')
            })
        })
    }

    private addHistoryItem(item: CompanyLoanCreateLoanNs.HistoryItem) {
        if (this.historyItems.length > 19) {
            //.Skip(1) with lots of code...
            let tmp = [] 
            for (var i = 1; i <= this.historyItems.length; i++) {
                tmp.push(this.historyItems[i])
            }
            this.historyItems = tmp
        }
        this.historyItems.push(item)
        localStorage.setItem(CompanyLoanCreateLoanNs.HistoryItemsKey, JSON.stringify(this.historyItems))        
    }
}

app.controller('companyLoanCreateLoanCtr', CompanyLoanCreateLoanCtr)

module CompanyLoanCreateLoanNs {
    export const HistoryItemsKey = "CompanyLoanLoanHistoryV1"

    export interface IInitialData {
        backUrl: string
        currentTime: Date
        baseCountry: string
        loanUrlPrefix: string
    }

    export class LoanModel {
        constructor() {
            this.companyMode = 'New'
            this.applicantMode = 'New'
        }
        companyMode: string
        applicantMode: string
        companyOrgnr: string
        applicantCivicRegNr: string
        bankAccountType: string
        newCompanyEditorData: NTechTestApi.StringDictionary
        newApplicantEditorData: NTechTestApi.StringDictionary
        loanEditorData: any
    }

    export interface HistoryItem {
        creditNr: string
        companyOrgNr: string
        applicantCivicRegNr: string
        date: string
    }
}