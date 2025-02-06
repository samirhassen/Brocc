module NTechCustomerPagesApi {
    export class ApiClient {
        constructor(private onError: ((errorMessage: string) => void),
            private $http: ng.IHttpService,
            private $q: ng.IQService) {
        }

        private activePostCount: number = 0;
        public loggingContext: string = null;

        private post<TRequest, TResult>(url: string, data: TRequest): ng.IPromise<TResult> {
            let startTimeMs = performance.now();
            this.activePostCount++;
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

        createTestApplication(overrides: { [index: string]: string }): ng.IPromise<any> {
            return this.post('/api/mortgage-loan/create-test-application', { overrides: overrides });
        }

        acceptTestApplication(applicationNr: string): ng.IPromise<any> {
            return this.post('/api/mortgage-loan/accept-test-application', { applicationNr: applicationNr });
        }
        
        createTestPerson(isAccepted: boolean): ng.IPromise<{ [index: string]: string }> {
            return this.post('/api/mortgageloan/generate-testperson', { isAccepted: isAccepted });
        }

        validateBankAccountNr(bankAccountNr: string): ng.IPromise<ValidateBankAccountResult> {
            return this.post('/api/v1/mortgage-loan/validate-bankaccount-nr', { bankAccountNr: bankAccountNr })
        }

        submitAdditionalQuestionsAnswers(request: SubmitAdditionalQuestionsAnswersRequest): ng.IPromise<SubmitAdditionalQuestionsAnswersResult> { //TODO: Type
            return this.post('/api/v1/mortgage-loan/answer-additional-question', request)
        }
        
        savingsStandardApplicationApply(application: ApplicationApplyModel): ng.IPromise<ApplicationApplyResult> {
            return this.post('/savings/standard-application-apply', { application: application })
        }

        createSecureMessage(request: { Text: string, TextFormat:string, ChannelType: string, ChannelId: string }): ng.IPromise<{ CreatedMessage: GetMessagesResponseMessage }> {
            return this.post('/Api/CustomerMessage/CreateMessage', request)
        }

        getSecureMessages(request: { SkipCount?: number, TakeCount?: number, IncludeChannels?: boolean, IncludeMessageTexts?: boolean }): ng.IPromise<GetMessagesResponse> {
            return this.post('/Api/CustomerMessage/GetMessages', request)
        }

        attachMessageDocument(request: { MessageId: number, AttachedFileAsDataUrl: string, AttachedFileName: string })
            : ng.IPromise<{ Id: number }> {
            return this.post('/Api/CustomerMessage/AttachMessageDocument', request)
        }
    }

    export interface GetMessagesResponse {
        TotalMessageCount: number
        Messages: GetMessagesResponseMessage[]
        CustomerChannels: {
            ChannelType: string
            ChannelId: string
            IsRelation: boolean
        }[]
    }

    export interface GetMessagesResponseMessage {
        Id: number
        Text: string
        TextFormat: string
        IsFromCustomer: boolean
        CreationDate: Date
        ChannelId: string
        ChannelType: string
    }

    export interface ApplicationApplyModel {
        ContactInfoLookupResultEncryptionKey: string;
        ApplicationItems: ApplicationItem[];
        ExternalApplicationVariables: ApplicationItem[];
    }

    export interface ApplicationItem {
        Name: string;
        Value: string;
    }

    export interface ApplicationApplyResult {
        questionsUrl : string
    }

    export class MortageLoanObjectModel {
        PropertyType: number
        PropertyEstimatedValue: number
        PropertyMunicipality: string
        CondominiumPropertyDetails: MortageLoanObjectCondominiumDetailsModel
    }

    export class MortageLoanObjectCondominiumDetailsModel {
        Address: string
        PostalCode: string
        City: string
        NumberOfRooms: number
        LivingArea: number
        Floor: number
        AssociationName: string
        AssociationNumber: string
        MonthlyCost: number
        ApartmentNumber: number
        Elevator: boolean
        PatioType: number
        NewConstruction: boolean
    }

    export class ValidateBankAccountResult {
        type: string
        bankName: string
        clearingNr: string
        accountNr: string
        normalizedNr: string
        bic: string
    }

    export class SubmitAdditionalQuestionsAnswersRequest {
        Token: string
        QuestionsAndAnswers: SubmitAdditionalQuestionsAnswersRequestItem[]
        BankAccountNr: string
        RequestedAmortizationAmount?: number
        Loans: ICurrentMortgageLoanModel[]
    }
    export class SubmitAdditionalQuestionsAnswersRequestItem {
        QuestionCode: string
        QuestionText: string
        ApplicantNr?: number
        AnswerCode: string
        AnswerText: string
        QuestionGroup: string
    }

    export interface ICurrentMortgageLoanModel {
        bankName: string
        monthlyAmortizationAmount: number
        currentBalance: number
        loanNr: string
    }

    export class SubmitAdditionalQuestionsAnswersResult {
        status: AdditionalQuestionsStatusModel
    }

    export class AdditionalQuestionsStatusModel {
        CurrentLoanAmount: number
        Applicants: AdditionalQuestionsApplicantStatusModel[]
        HasAlreadyAnswered: boolean
        IsPossibleToAnswer: boolean
        IsTokenExpired: boolean
    }

    export class AdditionalQuestionsApplicantStatusModel {
        ApplicantNr: number
        FirstName: string
        BirthDate: string
    }
}