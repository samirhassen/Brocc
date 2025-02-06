module NTechTestApi {
    export class ApiClient {
        constructor(private onError: ((errorMessage: string) => void),
            private $http: ng.IHttpService,
            private $q: ng.IQService,
            private changeIsLoading?: (isLoading: boolean) => void) {
        }

        private activePostCount: number = 0;
        public loggingContext: string = null;

        public postUsingApiGateway<TRequest, TResult>(seviceName: string, serviceLocalUrl: string, data: TRequest): ng.IPromise<TResult> {
            return this.post<TRequest, TResult>(`/Api/Gateway/${seviceName}${serviceLocalUrl[0] === '/' ? '' : '/'}${serviceLocalUrl}`, data)
        }

        public getUserModuleUrl(moduleName: string, serviceLocalUrl: string, parameters?: IStringDictionary<string>): ng.IPromise<{ Url: string }> {
            return this.post('/Api/GetUserModuleUrl', { moduleName: moduleName, moduleLocalUrl: serviceLocalUrl, parameters: parameters })
        }

        private post<TRequest, TResult>(url: string, data: TRequest): ng.IPromise<TResult> {
            let startTimeMs = performance.now();
            this.activePostCount++;
            this.changeIsLoading(true)
            let d: ng.IDeferred<TResult> = this.$q.defer()
            this.$http.post(url, data).then((result: ng.IHttpResponse<TResult>) => {
                d.resolve(result.data)
            }, err => {
                if (this.onError) {
                    this.onError(err.statusText)
                }
                d.reject(err.statusText)
            }).finally(() => {
                this.activePostCount--;
                if (!this.isLoading()) {
                    this.changeIsLoading(false)
                }
                let totalTimeMs = performance.now() - startTimeMs;
                let c = this.loggingContext == null ? '' : (this.loggingContext + ': ')
                if (console) {
                    console.log(`${c}post - ${url}: ${totalTimeMs}ms`);
                }
            })
            return d.promise
        }

        isLoading() {
            return this.activePostCount > 0;
        }

        getOrGenerateTestCompany(orgnr: string, orgnrCountry: string, isAccepted: boolean, addToCustomerModule: boolean, bankAccountType: string): ng.IPromise<GetOrGenerateTestCompanyResponseModel> {
            return this.post('/Api/Company/TestCompany/GetOrGenerate', { orgnr: orgnr, orgnrCountry: orgnrCountry, isAccepted: isAccepted, addToCustomerModule: addToCustomerModule, bankAccountType: bankAccountType })
        }

        getOrGenerateTestPerson(civicRegNr: string, isAccepted: boolean, addToCustomerModule?: boolean): ng.IPromise<GetOrGenerateTestPersonResponseModel> {
            return this.post('/Api/TestPerson/GetOrGenerateSingle', { civicRegNr: civicRegNr, isAccepted: isAccepted })
        }

        getTestPerson(request: { civicRegNr: string, civicRegNrCountry: string, requestedProperties: string[], allWithThisPrefix?: string }): ng.IPromise<IStringDictionary<string>> {
            return this.post('/Api/TestPerson/Get', request)
        }

        getTestCompany(request: { orgnr: string, orgnrCountry: string, generateIfNotExists: boolean }): ng.IPromise<IStringDictionary<string>> {
            return this.post('/Api/Company/TestCompany/Get', request)
        }

        createOrUpdateTestPersons(persons: string[], clearCache: boolean): ng.IPromise<void> {
            return this.post('/Api/TestPerson/CreateOrUpdate', { persons: persons, clearCache: clearCache })
        }

        createOrUpdateTestCompanies(companies: string[], clearCache: boolean): ng.IPromise<void> {
            return this.post('/Api/Company/TestCompany/CreateOrUpdate', { companies: companies, clearCache: clearCache })
        }

        createCompanyLoanCredit(request: CreateCompanyLoanRequestModel): ng.IPromise<CreateCompanyLoanResponseModel> {
            return this.post('/Api/Gateway/nCredit/api/CompanyCredit/Create', request)
        }

        generateNewCreditNumber(): ng.IPromise<GenerateNewCreditNumberResponseModel> {
            return this.post('/Api/Gateway/nCredit/Api/NewCreditNumber', {})
        }

        generateReferenceNumbers(request: { CreditNrCount?: number, OcrNrCount?: number }): ng.IPromise<{ CreditNrs: string[], OcrNrs: string[] }> {
            return this.post('/Api/Gateway/nCredit/api/Credit/Generate-Reference-Numbers', request)
        }

        proxyCreateApplication(application: any): ng.IPromise<CreateApplicationResponse> {
            return this.post('/Api/Gateway/nPreCredit/api/CompanyLoan/Create-Application', application)
        }

        proxyCreateCompanyLoan(request: any): ng.IPromise<CreateCompanyLoanResponseModel> {
            return this.postUsingApiGateway('nCredit', '/api/CompanyCredit/Create', request)
        }

        proxyCreateMortgageLoanApplication(request: any): ng.IPromise<CreateMortgageLoanApplicationResponseModel> {
            return this.postUsingApiGateway('nPreCredit', '/api/mortgageloan/create-application', request)
        }

        getCompanyCustomerId(orgnr: string): ng.IPromise<CustomerIdResult> {
            return this.postUsingApiGateway('nCustomer', '/api/CustomerIdByOrgnr', { orgnr: orgnr })
        }

        getPersonCustomerId(civicRegNr: string): ng.IPromise<CustomerIdResult> {
            return this.postUsingApiGateway('nCustomer', '/api/CustomerIdByCivicRegNr', { civicRegNr: civicRegNr })
        }

        addCompanyToCustomerModule(orgnr: string, companyName: string, additionalProperties: StringDictionary, expectedCustomerId?: number): ng.IPromise<CustomerIdResult> {
            let p = []
            if (additionalProperties) {
                for (let k of Object.keys(additionalProperties)) {
                    p.push({ name: k, value: additionalProperties[k] })
                }
            }

            return this.postUsingApiGateway('nCustomer', '/api/CompanyCustomer/CreateOrUpdate', {
                orgnr: orgnr,
                companyName: companyName,
                expectedCustomerId: expectedCustomerId,
                properties: p
            })
        }

        addPersonToCustomerModule(civicRegNr: string, additionalProperties: StringDictionary, expectedCustomerId?: number): ng.IPromise<CustomerIdResult> {
            let p = []
            if (additionalProperties) {
                for (let k of Object.keys(additionalProperties)) {
                    p.push({ name: k, value: additionalProperties[k] })
                }
            }

            return this.postUsingApiGateway('nCustomer', '/api/PersonCustomer/CreateOrUpdate', {
                civicRegNr: civicRegNr,
                expectedCustomerId: expectedCustomerId,
                properties: p
            })
        }

        getCreditKeyValueItem(keySpace: string, key: string): ng.IPromise<{ Value: string }> {
            return this.postUsingApiGateway('nCredit', '/api/KeyValueStore/Get', { KeySpace: keySpace, Key: key })
        }

        setCreditKeyValueItem(keySpace: string, key: string, value: string): ng.IPromise<void> {
            return this.postUsingApiGateway('nCredit', '/api/KeyValueStore/Set', { KeySpace: keySpace, Key: key, Value: value })
        }
    }

    export interface CreateMortgageLoanApplicationResponseModel {
        ApplicationNr: string
        DirectScoringResult: {
            IsAccepted: boolean
            AcceptedOffer: {
                LoanAmount: number
                MonthlyAmortizationAmount: number
                NominalInterestRatePercent: number
                MonthlyFeeAmount: number
                InitialFeeAmount: number
                ValidUntilDate: Date
            }
            RejectedDetails: {
                RejectionReasons: string[]
            }
        }
    }

    export interface IStringDictionary<T> {
        [key: string]: T
    }

    export interface CustomerIdResult {
        CustomerId: number
    }

    export interface CreateApplicationResponse {
        ApplicationNr: string
        DecisionStatus: string
    }

    export interface GenerateNewCreditNumberResponseModel {
        nr: string
    }

    export interface CreateCompanyLoanRequestModel {
        AnnuityAmount: number,
        CreditNr: string,
        Iban: string,
        BankAccountNr: string,
        BankAccountNrType: string,
        ProviderName: string,
        CreditAmount: number,
        CapitalizedInitialFeeAmount: number,
        DrawnFromLoanAmountInitialFeeAmount: number,
        NotificationFee: number,
        MarginInterestRatePercent: number,
        AgreementPdfArchiveKey: string,
        CompanyCustomerId: number,
        ProviderApplicationId: string,
        ApplicationNr: string,
        CampaignCode: string,
        SourceChannel: string
    }

    export interface CreateCompanyLoanResponseModel {
        CreditNr: string
    }

    export interface GetOrGenerateTestCompanyResponseModel {
        Orgnr: string
        Properties: StringDictionary
        WasGenerated: boolean
        CustomerId?: number
    }

    export interface GetOrGenerateTestPersonResponseModel {
        CivicRegNr: string
        Properties: StringDictionary
        WasGenerated: boolean
        CustomerId?: number
    }

    export interface StringDictionary {
        [key: string]: string
    }
}