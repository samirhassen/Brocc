var app = angular.module('app', ['angular-jsoneditor']);

let clientCountry: string = initialData.clientCountry

function hasChildLoan(): boolean {
    return clientCountry === 'FI'
}

//TODO: Compute better annuity
function createLoanRequest(apiClient: NTechTestApi.ApiClient, civicRegNr1: string, civicRegNr2: string, providerName: string,
    creditNr: string, sharedOcrPaymentReference: string, withRequest: (request: any) => void): void {
    let r = {
        CreditNr: creditNr,
        SharedOcrPaymentReference: sharedOcrPaymentReference,
        MonthlyFeeAmount: 0,
        NominalInterestRatePercent: 1.3,
        ReferenceInterestRate: 0.2,
        NextInterestRebindDate: moment(initialData.currentTime).add('days', 1).format('YYYY-MM-DDT00:00:00'), //Normally defaults to three months but making this very soon makes testing way easier.
        NrOfApplicants: civicRegNr2 ? 2 : 1,
        ProviderName: providerName,
        ProviderApplicationId: null as string,
        ApplicationNr: null as string,
        SettlementDate: moment(initialData.currentTime).format('YYYY-MM-DDTHH:mm:00'),
        EndDate: moment(initialData.currentTime).add('years', 10).format('YYYY-MM-DDT00:00:00'),
        LoanAmount: 2650000,
        ActualAmortizationAmount: clientCountry === "SE" ? 2300 : null,
        AnnuityAmount: clientCountry === "SE" ? null : 2650000 / 100,
        ExceptionAmortizationAmount: null as number,
        AmortizationExceptionReasons: [] as string[],
        AmortizationRule: clientCountry === "SE" ? 'r201723' : 'none',
        AmortizationBasisLoanAmount: 2900000,
        AmortizationBasisObjectValue: 4125000,
        DebtIncomeRatioBasisAmount: 155000,
        CurrentCombinedYearlyIncomeAmount: 1200000,
        RequiredAlternateAmortizationAmount: null as number,
        KycQuestionsJsonDocumentArchiveKey: null as string,
        Applicants: [] as { ApplicantNr: number, CustomerId: number, AgreementPdfArchiveKey: string, OwnershipPercent: number }[],
        Documents: [] as { DocumentType: string, ApplicantNr: number, ArchiveKey: string }[],
        SecurityItems: {} as NTechTestApi.IStringDictionary<string>,
        ActiveDirectDebitAccount: null as { BankAccountNrOwnerApplicantNr: number, BankAccountNr: string, ActiveSinceDate: string },
        AmortizationExceptionUntilDate: null as string,
        AmortizationFreeUntilDate: null as string,
        AmortizationBasisDate: moment(initialData.currentTime).add('years', -2).format('YYYY-MM-DDT00:00:00'),
        HistoricalStartDate: null as string,
        CurrentObjectValue: 3100000,
        CurrentObjectValueDate: null as string,
        IsForNonPropertyUse: false,
        MainCreditCreditNr: null as string,
        NotificationDueDay: 28
    }

    apiClient.getPersonCustomerId(civicRegNr1).then(x => {
        r.Applicants.push({
            ApplicantNr: 1,
            CustomerId: x.CustomerId,
            AgreementPdfArchiveKey: null,
            OwnershipPercent: civicRegNr2 ? 60 : 100
        })
        if (civicRegNr2) {
            apiClient.getPersonCustomerId(civicRegNr2).then(y => {
                r.Applicants.push({
                    ApplicantNr: 2,
                    CustomerId: y.CustomerId,
                    AgreementPdfArchiveKey: null,
                    OwnershipPercent: 40
                })
                withRequest(r)
            })
        } else {
            withRequest(r)
        }
    })
}

class MortgageLoanCreateLoanCtr {
    static $inject = ['$scope', '$http', '$timeout', '$q']
    constructor(
        $scope: MortgageLoanCreateLoanNs.IScope,
        private $http: ng.IHttpService,
        private $timeout: ng.ITimeoutService,
        private $q: ng.IQService
    ) {
        let apiClient = new NTechTestApi.ApiClient(m => toastr.error(m), $http, $q, x => { $scope.isLoading = x })

        $scope.backUrl = initialData.backUrl
        $scope.providerNames = initialData.providerNames

        function setInitial() {
            $scope.state = 'initial'
            $scope.initial = { applicant1: 'New', applicant2: 'New', providerName: initialData.defaultProviderName }
            $scope.applicants = null
            $scope.loan = null
            $scope.newLoanUrl = null
        }

        $scope.restart = (evt: Event) => {
            if (evt) {
                evt.preventDefault()
            }
            setInitial()
        }

        $scope.createLoan = (evt: Event) => {
            if (evt) {
                evt.preventDefault()
            }

            let creditNrs: string[] = []
            for (let r of $scope.loan.model.data) {
                creditNrs.push(r.CreditNr)
            }

            apiClient.postUsingApiGateway('nCredit', '/api/MortgageLoans/Create-Bulk', { Loans: $scope.loan.model.data }).then(x => {
                apiClient.getUserModuleUrl('nCredit', 'Ui/Credit', { creditNr: creditNrs[0] }).then(urlResult => {
                    $scope.state = 'done'
                    $scope.newLoanUrl = urlResult.Url
                })
            })
        }

        $scope.submitInitial = (evt: Event) => {
            evt.preventDefault()

            $scope.state = 'working'

            var newPersonCustomizations = []
            var nrOfTestPersonsToGenerate = 0
            if ($scope.initial.applicant1 === 'New') {
                nrOfTestPersonsToGenerate = nrOfTestPersonsToGenerate + 1
                newPersonCustomizations.push($scope.initial.applicant1Custom)
            }
            if ($scope.initial.applicant2 === 'New') {
                nrOfTestPersonsToGenerate = nrOfTestPersonsToGenerate + 1
                newPersonCustomizations.push($scope.initial.applicant2Custom)
            }

            var afterGeneratePersons = (data: any) => {
                $scope.state = 'applicants'
                var applicants: any = {}

                if ($scope.initial.applicant1 === 'New') {
                    applicants.newApplicant1 = {
                        data: JSON.parse(data.applicants[0]),
                        options: { mode: 'tree' }
                    }
                } else if ($scope.initial.applicant1 === 'Existing') {
                    applicants.existingApplicant1 = {}
                }

                if ($scope.initial.applicant2 === 'New') {
                    applicants.newApplicant2 = {
                        data: JSON.parse(data.applicants[nrOfTestPersonsToGenerate === 1 ? 0 : 1]),
                        options: { mode: 'tree' }
                    }
                } else if ($scope.initial.applicant2 === 'Existing') {
                    applicants.existingApplicant2 = {}
                }

                $scope.applicants = applicants
                $scope.isLoading = false
            }

            $scope.isLoading = true
            if (nrOfTestPersonsToGenerate > 0) {
                $http({
                    method: 'POST',
                    url: '/Api/TestPerson/Generate',
                    data: { isAccepted: true, count: nrOfTestPersonsToGenerate, newPersonCustomizations: newPersonCustomizations }
                }).then((response: any) => {
                    afterGeneratePersons({ applicants: response.data.applicants })
                }, (response) => {
                    $scope.isLoading = false
                    toastr.error('Failed!')
                })
            } else {
                afterGeneratePersons(null)
            }
        }

        function addNewApplicantsToCustomerModule(after: () => void) {
            let add = (d: any) => {
                return apiClient.addPersonToCustomerModule(d.civicRegNr, {
                    firstName: d.firstName,
                    lastName: d.lastName,
                    email: d.email,
                    phone: d.phone,
                    addressStreet: d.addressStreet,
                    addressZipcode: d.addressZipcode,
                    addressCity: d.addressCity
                })
            }
            let promises = []

            if ($scope.initial.applicant1 === 'New') {
                promises.push(add($scope.applicants.newApplicant1.data))
            }

            if ($scope.initial.applicant2 === 'New') {
                promises.push(add($scope.applicants.newApplicant2.data))
            }

            $q.all(promises).then(x => {
                after()
            })
        }

        $scope.submitApplicants = (evt) => {
            evt.preventDefault()

            let applicant1CivicRegNr: string = null
            let applicant2CivicRegNr: string = null

            var afterUpdateApplicants = function () {
                if ($scope.initial.applicant1 === 'New') {
                    applicant1CivicRegNr = $scope.applicants.newApplicant1.data.civicRegNr
                } else if ($scope.initial.applicant1 === 'Existing') {
                    applicant1CivicRegNr = $scope.applicants.existingApplicant1.civicRegNr
                }

                if ($scope.initial.applicant2 === 'New') {
                    applicant2CivicRegNr = $scope.applicants.newApplicant2.data.civicRegNr
                } else if ($scope.initial.applicant2 === 'Existing') {
                    applicant2CivicRegNr = $scope.applicants.existingApplicant2.civicRegNr
                }

                $scope.isLoading = true
                addNewApplicantsToCustomerModule(() => {
                    apiClient.generateReferenceNumbers({ CreditNrCount: hasChildLoan() ? 2 : 1, OcrNrCount: hasChildLoan() ? 1 : 0 }).then(nrs => {
                        createLoanRequest(apiClient, applicant1CivicRegNr, applicant2CivicRegNr, $scope.initial.providerName, nrs.CreditNrs[0], hasChildLoan() ? nrs.OcrNrs[0] : null, r => {
                            let requests = [r]

                            let afterRequests = () => {
                                $scope.isLoading = false
                                $scope.state = 'loan'
                                $scope.loan = {
                                    model: {
                                        data: requests,
                                        options: { mode: 'tree' }
                                    }
                                }
                            }
                            let loanAmount = r.LoanAmount

                            if (hasChildLoan()) {
                                let annuityAmount = r.AnnuityAmount
                                let nonPropertyUseLoanAmount = Math.round(loanAmount / 3)

                                //r.LoanAmount = r.LoanAmount - nonPropertyUseLoanAmount
                                r.LoanAmount = null
                                r.LoanAmountParts = [
                                    { Amount: loanAmount - nonPropertyUseLoanAmount, SubAccountCode: 'loanAmount' },
                                    { Amount: 90, SubAccountCode: 'valuationFee' },
                                    { Amount: 75, SubAccountCode: 'deedFee' },
                                    { Amount: 125, SubAccountCode: 'mortgageApplicationFee' },
                                    { Amount: 100, SubAccountCode: 'initialFee' }
                                ]

                                r.AnnuityAmount = Math.round(2 * annuityAmount / 3)
                                createLoanRequest(apiClient, applicant1CivicRegNr, applicant2CivicRegNr, $scope.initial.providerName, nrs.CreditNrs[1], nrs.OcrNrs[0], r2 => {
                                    //r2.LoanAmount = nonPropertyUseLoanAmount
                                    r2.AnnuityAmount = Math.round(1 * annuityAmount / 3)
                                    r2.LoanAmount = null
                                    r2.LoanAmountParts = [
                                        { Amount: nonPropertyUseLoanAmount, SubAccountCode: 'loanAmount' },
                                        { Amount: 150, SubAccountCode: 'initialFee' }
                                    ]
                                    r2.IsForNonPropertyUse = true
                                    r2.MainCreditCreditNr = r.CreditNr
                                    //r2.CapitalizedInitialFees = [{ Amount: 150, SubAccountCode: 'initialFee' }]

                                    requests.push(r2)
                                    afterRequests()
                                })
                            } else {
                                afterRequests()
                            }
                        })
                    })
                })
            }

            if ($scope.initial.applicant1 === 'New' || $scope.initial.applicant2 === 'New') {
                var persons = []

                if ($scope.initial.applicant1 === 'New') {
                    persons.push(JSON.stringify($scope.applicants.newApplicant1.data))
                }
                if ($scope.initial.applicant2 === 'New') {
                    persons.push(JSON.stringify($scope.applicants.newApplicant2.data))
                }

                $scope.isLoading = true
                $http({
                    method: 'POST',
                    url: '/Api/TestPerson/CreateOrUpdate',
                    data: { persons: persons }
                }).then((response) => {
                    afterUpdateApplicants()
                }, (response) => {
                    $scope.isLoading = false
                    toastr.error('Failed!')
                })
            } else {
                afterUpdateApplicants()
            }
        }

        setInitial()

        window.scope = $scope
    }
}

app.controller('mortgageLoanCreateLoanCtr', MortgageLoanCreateLoanCtr)

module MortgageLoanCreateLoanNs {
    export interface IScope extends ng.IScope {
        submitApplicants: (evt: any) => void;
        isLoading: boolean;
        submitInitial: (evt: Event) => void;
        createLoan: (evt: Event) => void;
        loan: { model: { data: any, options: any } };
        applicants: any;
        initial: { applicant1: any; applicant1Custom?: any; applicant2: any; applicant2Custom?: any; providerName: string; };
        state: any;
        providerNames: string[];
        backUrl: string;
        restart: (evt: any) => void;
        newLoanUrl: string
    }
}