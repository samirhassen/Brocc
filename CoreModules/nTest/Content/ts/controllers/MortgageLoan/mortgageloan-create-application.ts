var app = angular.module('app', ['ntech.forms', 'angular-jsoneditor'])

class MortgageLoanCreateApplicationCtr {
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
        window['ntechDebug']['mortgageLoanCreateApplication'] = this

        this.apiClient = new NTechTestApi.ApiClient(msg => toastr.error(msg), $http, $q, x => this.isLoading = x)
        this.a = new MortgageLoanCreateApplicationNs.ApplicationModel()

        let h = localStorage.getItem(MortgageLoanCreateApplicationNs.HistoryItemsKey)
        if (h) {
            this.historyItems = JSON.parse(h)
        } else {
            this.historyItems = []
        }
    }
    state: string = 'initial'
    backUrl: string
    a: MortgageLoanCreateApplicationNs.ApplicationModel
    apiClient: NTechTestApi.ApiClient
    isLoading: boolean = false
    initialDataTyped: MortgageLoanCreateApplicationNs.IInitialData
    createResult: NTechTestApi.CreateApplicationResponse
    historyItems: MortgageLoanCreateApplicationNs.HistoryItem[]

    submitInitial(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }

        let handleCoApplicant = () => {
            if (this.a.coApplicantMode === 'New') {
                this.apiClient.getOrGenerateTestPerson(null, this.a.scoringMode === 'Accepted').then(x => {
                    this.a.coApplicantCivicRegNr = x.CivicRegNr
                    this.a.newCoApplicantEditorData = x.Properties
                    this.state = 'applicants'
                })
            } else {
                this.a.coApplicantCivicRegNr = ''
                this.a.newCoApplicantEditorData = null
                this.state = 'applicants'
            }
        }

        if (this.a.applicantMode === 'New') {
            this.apiClient.getOrGenerateTestPerson(null, this.a.scoringMode === 'Accepted').then(x => {
                this.a.applicantCivicRegNr = x.CivicRegNr
                this.a.newApplicantEditorData = x.Properties
                handleCoApplicant()
            })
        } else {
            this.a.applicantCivicRegNr = ''
            this.a.newApplicantEditorData = null
            handleCoApplicant()
        }
    }

    submitApplicants(evt: Event) {
        let persons: string[] = []

        if (this.a.applicantMode === 'New') {
            persons.push(JSON.stringify(this.a.newApplicantEditorData))
        }
        if (this.a.coApplicantMode === 'New') {
            persons.push(JSON.stringify(this.a.newCoApplicantEditorData))
        }

        let submit = () => {
            this.apiClient.createOrUpdateTestPersons(persons, true).then(() => {
                let isAccepted = this.a.scoringMode === 'Accepted'

                let mortgageLoanApplication: any
                if (this.initialDataTyped.clientName === 'bluestepFi') {
                    let useLeads: boolean = null
                    if (this.a.leadsSetting === 'Always') {
                        useLeads = true
                    } else if (this.a.leadsSetting === 'Never') {
                        useLeads = false
                    }
                    mortgageLoanApplication = this.createBluestepFiApplicationModel(isAccepted, useLeads)
                } else {
                    mortgageLoanApplication = this.createStandardApplicationModel(isAccepted)
                }

                this.a.applicationEditorData = mortgageLoanApplication
                this.state = 'application'
            })
        }
        submit()
    }

    createBluestepFiApplicationModel(isAccepted: boolean, useLeads: boolean): any {
        let hasCoApplicant = !!this.a.coApplicantCivicRegNr
        let campaignParameters : any[] = []
        if (Math.random() < 0.3) {
            campaignParameters = [
                  {
                    Name: "utm_medium",
                    Value: "content-test"
                  },
                  {
                    Name: "utm_source",
                    Value: "nTest"
                  },
                  {
                    Name: "utm_campaign",
                    Value: "bluestep-fi-test-monthly-" + moment().format('YYYY-MM')
                  },
                  {
                    Name: "utm_content",
                    Value: "bluestep-fi-test"
                  }
                ]
        }
        let a = {
            Application: {
                ApplicationType: "MoveExistingLoan",
                RequestedLoanAmount: 123000,
                RequestedContactDateAndTime: moment(this.initialDataTyped.currentTime).format('YYYY-MM-DDTHH:mm'),
                OwnSavingsAmount: 1200,
                ExistingMortgageLoanAmount: 99000,
                ProviderToken: null as string,
                CampaignParameters: campaignParameters
            },
            Applicants: [] as any[],
            Object: {
                City: "Helsinki",
                CurrentValueAmount: 135000,
                PriceAmount: null as number,
                Url: null as string
            },
            Meta: {
                ProviderName: this.a.providerName,
                DisableAutomation: true,
                CustomerExternalIpAddress: null as string,
                UseLeads: (useLeads === true ? true : (useLeads === false ? false : null))
            }
        }

        let addApplicant = (d: NTechTestApi.StringDictionary) => {
            a.Applicants.push({
                FirstName: d['firstName'],
                LastName: d['lastName'],
                CivicRegNr: d['civicRegNr'],
                PhoneNr: d['phone'],
                Email: d['email'],
                HasApprovedCreditCheck: true
            })
        }

        addApplicant(this.a.newApplicantEditorData)

        if (hasCoApplicant) {
            addApplicant(this.a.newCoApplicantEditorData)
        }

        return a
    }

    createStandardApplicationModel(isAccepted: boolean): any {
        let hasCoApplicant = !!this.a.coApplicantCivicRegNr

        let items: { Group: string, Name: string, Value: string }[] = []

        let a = (n: string, v: string) => items.push({
            Group: 'application',
            Name: n,
            Value: v
        })

        let p = (i: number, n: string, v: string) => items.push({
            Group: 'applicant' + i,
            Name: n,
            Value: v
        })

        let addApplicant = (nr: number, d: NTechTestApi.StringDictionary) => {
            p(nr, 'civicRegNr', d['civicRegNr'])
            p(nr, 'civicRegNrCountry', d['civicRegNrCountry'])
            p(nr, 'firstName', d['firstName'])
            p(nr, 'lastName', d['lastName'])
            p(nr, 'addressStreet', d['addressStreet'])
            p(nr, 'addressZipcode', d['addressZipcode'])
            p(nr, 'addressCity', d['addressCity'])
            p(nr, 'phone', d['phone'])
            p(nr, 'email', d['email'])
            p(nr, 'incomePerMonthAmount', isAccepted ? '90000' : '8800')
            p(nr, 'nrOfChildren', isAccepted ? '1' : '11')
            p(nr, 'employedSinceMonth', '2008-12')
            p(nr, 'savingsAmount', isAccepted ? '599800' : '0')
            p(nr, 'employment2', isAccepted ? 'fulltime' : 'unemployed')
            p(nr, 'marriage2', 'married')
            p(nr, 'carOrBoatLoanAmount', '0')
            p(nr, 'carOrBoatLoanCostPerMonthAmount', '0')
            p(nr, 'creditCardAmount', '0')
            p(nr, 'creditCardCostPerMonthAmount', '0')
            p(nr, 'studentLoanAmount', '0')
            p(nr, 'studentLoanCostPerMonthAmount', '0')
            p(nr, 'otherLoanAmount', '0')
            p(nr, 'otherLoanCostPerMonthAmount', '0')
            p(nr, 'mortgageLoanAmount', '0')
            p(nr, 'mortgageLoanCostPerMonthAmount', '0')
            p(nr, 'mortgageLoanOtherPropertyTypes', '[\"house\"]')
        }

        addApplicant(1, this.a.newApplicantEditorData)

        if (hasCoApplicant) {
            addApplicant(2, this.a.newCoApplicantEditorData)
        }

        a('applicationType', 'mortgageLoan')
        a('mortgageLoanLoanType', 'moveExistingLoan')
        a('mortgageLoanHouseType', 'apartment')
        a('mortgageLoanHouseZipcode', '14400')
        a('mortgageLoanHouseMonthlyFeeAmount', '2048')
        a('mortgageLoanHouseValueAmount', '3720195')
        a('mortgageLoanCurrentLoanAmount', '592691')
        a('bankAccountNr', 'FI6740567584718747')
        a('bankAccountNrOwnerApplicantNr', '1')
        let mortgageLoanApplication = {
            NrOfApplicants: hasCoApplicant ? 2 : 1,
            Items: items,
            ProviderName: "self",
            MortageLoanObject: {
                PropertyType: 1,
                PropertyEstimatedValue: 3720195,
                PropertyMunicipality: "Stockholm",
                CondominiumPropertyDetails: {
                    Address: "Klörupsvägen 62408",
                    PostalCode: "14400",
                    City: "EKENÄSSJÖN",
                    NumberOfRooms: 1,
                    LivingArea: 64,
                    Floor: 1,
                    AssociationName: "Brf Klörupsvägen",
                    AssociationNumber: "5590406483",
                    MonthlyCost: 2048,
                    ApartmentNumber: 1148,
                    Elevator: false,
                    PatioType: 1,
                    NewConstruction: false
                }
            },
            MortgageLoanCurrentLoans: {
                RequestedAmortizationAmount: 0,
                Loans: [
                    {
                        BankName: "SBAB",
                        MonthlyAmortizationAmount: 989,
                        CurrentBalance: 395332,
                        LoanNr: "L-589301"
                    },
                    {
                        BankName: "SBAB",
                        MonthlyAmortizationAmount: 1156,
                        CurrentBalance: 462203,
                        LoanNr: "L-136024"
                    }
                ]
            }
        }

        return {
            request: mortgageLoanApplication,
            disableAutomation: true,
            skipDirectScoring: false
        }
    }

    submitApplication(evt: Event) {
        this.apiClient.proxyCreateMortgageLoanApplication(this.a.applicationEditorData).then(x => {
            this.addHistoryItem({
                applicantCivicRegNr: this.a.applicantCivicRegNr,
                coApplicantCivicRegNr: this.a.coApplicantCivicRegNr,
                applicationNr: x.ApplicationNr,
                date: moment().format('YYYY-MM-DD HH:mm')
            })
            this.a = new MortgageLoanCreateApplicationNs.ApplicationModel()
            this.state = 'initial'
        });
    }

    private addHistoryItem(item: MortgageLoanCreateApplicationNs.HistoryItem) {
        if (this.historyItems.length > 19) {
            //.Skip(1) with lots of code...
            let tmp = []
            for (var i = 1; i < this.historyItems.length; i++) {
                tmp.push(this.historyItems[i])
            }
            this.historyItems = tmp
        }
        this.historyItems.push(item)
        localStorage.setItem(MortgageLoanCreateApplicationNs.HistoryItemsKey, JSON.stringify(this.historyItems))
    }
}

app.controller('mortgageLoanCreateApplicationCtr', MortgageLoanCreateApplicationCtr)

module MortgageLoanCreateApplicationNs {
    export const HistoryItemsKey = "MortgageLoanApplicationHistoryV2"

    export interface IInitialData {
        backUrl: string
        currentTime: Date
        baseCountry: string
        defaultProviderName: string
        applicationUrlPrefix: string
        clientName: string
        providers: { ProviderName: string, IsSelf: boolean, UseLeads: boolean, DisplayToEnduserName: string }[]
    }

    export class ApplicationModel {
        constructor() {
            this.applicantMode = 'New'
            this.coApplicantMode = 'New'
            this.scoringMode = 'Accepted'
            this.skipInitialScoring = 'Yes'
            this.leadsSetting = 'ByProvider'
        }
        applicantMode: string
        coApplicantMode: string
        scoringMode: string
        leadsSetting: string
        skipInitialScoring: string
        coApplicantCivicRegNr: string
        applicantCivicRegNr: string
        bankAccountType: string
        newApplicantEditorData: NTechTestApi.StringDictionary
        newCoApplicantEditorData: NTechTestApi.StringDictionary
        providerName: string
        applicationEditorData: any
    }

    export interface HistoryItem {
        applicationNr: string
        applicantCivicRegNr: string
        coApplicantCivicRegNr: string
        date: string
    }
}