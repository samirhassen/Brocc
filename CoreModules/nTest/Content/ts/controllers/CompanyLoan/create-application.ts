var app = angular.module('app', ['ntech.forms', 'angular-jsoneditor'])

class CompanyLoanCreateApplicationCtr {
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
        window['ntechDebug']['companyLoanCreateApplication'] = this

        this.apiClient = new NTechTestApi.ApiClient(msg => toastr.error(msg), $http, $q, x => this.isLoading = x)
        this.a = new CompanyLoanCreateApplicationNs.ApplicationModel()

        let h = localStorage.getItem(CompanyLoanCreateApplicationNs.HistoryItemsKey)
        if (h) {
            this.historyItems = JSON.parse(h)
        } else {
            this.historyItems = []
        }        
    }
    state: string = 'initial'
    backUrl: string
    a: CompanyLoanCreateApplicationNs.ApplicationModel
    apiClient: NTechTestApi.ApiClient
    isLoading: boolean = false
    initialDataTyped: CompanyLoanCreateApplicationNs.IInitialData
    createResult: NTechTestApi.CreateApplicationResponse
    historyItems: CompanyLoanCreateApplicationNs.HistoryItem[]

    submitInitial(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }

        let handleApplicant = () => {
            if (this.a.applicantMode === 'New') {
                this.apiClient.getOrGenerateTestPerson(null, this.a.scoringMode === 'Accepted').then(x => {
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
            this.apiClient.getOrGenerateTestCompany(null, null, this.a.scoringMode === 'Accepted', false, this.a.bankAccountType).then(x => {
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
        let companies: string[] = []
        let persons: string[] = []

        if (this.a.companyMode === 'New') {
            companies.push(JSON.stringify(this.a.newCompanyEditorData))
        }


        let submit = () => {
            this.apiClient.createOrUpdateTestCompanies(companies, true).then(() => {
                this.apiClient.createOrUpdateTestPersons(persons, true).then(() => {
                    let isAccepted = this.a.scoringMode === 'Accepted'
                    let companyLoanApplication = {
                        RequestedAmount: 75000,
                        SkipInitialScoring: this.a.skipInitialScoring === 'Yes',
                        RequestedRepaymentTimeInMonths: 25,
                        ProviderName: "self",
                        Applicant: {
                            CivicRegNr: this.a.applicantCivicRegNr,
                            FirstName: this.a.newApplicantEditorData['firstName'],
                            LastName: this.a.newApplicantEditorData['lastName'],
                            Email: this.a.newApplicantEditorData['email'],
                            Phone: this.a.newApplicantEditorData['phone']
                        },
                        Customer: {
                            Orgnr: this.a.companyOrgnr
                        },
                        AdditionalApplicationProperties: {
                            companyAgeInMonths: 16,
                            companyYearlyRevenue: 800000,
                            companyYearlyResult: 300000,
                            companyCurrentDebtAmount: isAccepted ? 19000 : 2100000,
                            loanPurposeCode: "Förvärv"
                        }
                    }
                    this.a.applicationEditorData = companyLoanApplication
                    this.state = 'application'
                })
            })
        }
        
        if (this.a.applicantMode === 'New') {
            persons.push(JSON.stringify(this.a.newApplicantEditorData))
            submit()
        } else {
            this.apiClient.getOrGenerateTestPerson(this.a.applicantCivicRegNr, this.a.scoringMode === 'Accepted').then(x => {
                this.a.applicantCivicRegNr = x.CivicRegNr
                this.a.newApplicantEditorData = x.Properties
                submit()
            })
        }
    }

    submitApplication(evt: Event) {
        this.apiClient.proxyCreateApplication(this.a.applicationEditorData).then(x => {
            this.addHistoryItem({
                applicantCivicRegNr: this.a.applicantCivicRegNr,
                companyOrgNr: this.a.companyOrgnr,
                applicationNr: x.ApplicationNr,
                date: moment().format('YYYY-MM-DD HH:mm')
            })
            this.a = new CompanyLoanCreateApplicationNs.ApplicationModel()
            this.state = 'initial'
        });
    }

    private addHistoryItem(item: CompanyLoanCreateApplicationNs.HistoryItem) {
        if (this.historyItems.length > 19) {
            //.Skip(1) with lots of code...
            let tmp = [] 
            for (var i = 1; i < this.historyItems.length; i++) {
                tmp.push(this.historyItems[i])
            }
            this.historyItems = tmp
        }
        this.historyItems.push(item)
        localStorage.setItem(CompanyLoanCreateApplicationNs.HistoryItemsKey, JSON.stringify(this.historyItems))        
    }
}

app.controller('companyLoanCreateApplicationCtr', CompanyLoanCreateApplicationCtr)

module CompanyLoanCreateApplicationNs {
    export const HistoryItemsKey = "CompanyLoanApplicationHistoryV2"

    export interface IInitialData {
        backUrl: string
        currentTime: Date
        baseCountry: string
        defaultProviderName: string
        applicationUrlPrefix: string
    }

    export class ApplicationModel {
        constructor() {
            this.companyMode = 'New'
            this.applicantMode = 'New'
            this.scoringMode = 'Accepted'
            this.skipInitialScoring = 'Yes'
        }
        companyMode: string
        applicantMode: string
        scoringMode: string
        skipInitialScoring: string
        companyOrgnr: string
        applicantCivicRegNr: string
        bankAccountType: string
        newCompanyEditorData: NTechTestApi.StringDictionary
        newApplicantEditorData: NTechTestApi.StringDictionary
        applicationEditorData: any
    }

    export interface HistoryItem {
        applicationNr: string
        companyOrgNr: string
        applicantCivicRegNr: string
        date: string
    }
}