namespace CompanyLoanAdditionalQuestionsComponentNs {
    
    export class CompanyLoanAdditionalQuestionsController extends NTechComponents.NTechComponentControllerBase {
        initialData: CompanyLoanApplicationComponentNs.StepInitialData;
        m: NTechCompanyLoanPreCreditApi.FetchCompanyLoanAdditionalQuestionsStatusResponse;
        a: AnswersModel        
        isWaitingForPreviousSteps: boolean
        answersDialogId: string
        linkDialogId: string
        
        applicationNr(): string {
            if (this.initialData) {
                return this.initialData.applicationInfo.ApplicationNr
            } else {
                return null;
            }
        }

        backUrl() {
            if (this.initialData) {
                return this.initialData.backUrl
            } else {
                return null;
            }                        
        }       

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);

            this.answersDialogId = modalDialogService.generateDialogId()
            this.linkDialogId = modalDialogService.generateDialogId()
        }

        showDirectLink(evt: Event) {
            if (evt) {
                evt.preventDefault();
            }
            this.modalDialogService.openDialog(this.linkDialogId)
        }

        getSwitchableAnswerCode(a: AnswerTableItem) {
            if (!a) {
                return ''
            }
            if (a.QuestionCode === 'beneficialOwnerPercentCount' || a.QuestionCode === 'beneficialOwnerConnectionCount') {
                return a.AnswerCode === '0' ? 'false' : 'true'
            }
            return a.AnswerCode
        }

        showAnswers(key: string, evt: Event) {
            if (evt) {
                evt.preventDefault();
            }
            if (this.a) {
                this.modalDialogService.openDialog(this.answersDialogId)
            } else {
                this.initialData.companyLoanApiClient.fetchAdditionalQuestionsAnswers(this.initialData.applicationInfo.ApplicationNr).then(r => {
                    let customerIds = NTechLinq.distinct(r.Document.Items.filter(x => !!x.CustomerId).map(x => x.CustomerId))
                    this.apiClient.fetchCustomerItemsBulk(customerIds, ['firstName']).then(customerData => {
                        let names: NTechPreCreditApi.INumberDictionary<string> = {}
                        for (let customerId of customerIds) {
                            names[customerId] = customerData[customerId] ? customerData[customerId]['firstName'] : 'customer'
                        }

                        let answers: AnswerTableItem[] = []

                        for (let q of r.Document.Items.filter(x => !x.CustomerId)) {
                            answers.push({
                                Type: 'answer',
                                AnswerCode: q.AnswerCode,
                                AnswerText: q.AnswerText,
                                QuestionCode: q.QuestionCode,
                                QuestionText: q.QuestionText
                            })
                        }
                        answers.push({
                            Type: 'separator'
                        })

                        for (let customerId of customerIds) {
                            answers.push({
                                Type: 'customer',
                                CustomerId: customerId,
                                CustomerCardUrl: this.initialData.customerCardUrlPattern.replace('[[[CUSTOMER_ID]]]', customerId.toString()).replace('[[[BACK_TARGET]]]', this.initialData.navigationTargetCodeToHere)
                            })
                            for (let q of r.Document.Items.filter(x => x.CustomerId === customerId)) {
                                answers.push({
                                    Type: 'answer',
                                    CustomerId: q.CustomerId,
                                    AnswerCode: q.AnswerCode,
                                    AnswerText: q.AnswerText,
                                    QuestionCode: q.QuestionCode,
                                    QuestionText: q.QuestionText
                                })
                            }
                            answers.push({
                                Type: 'separator'
                            })
                        }

                        this.a = {
                            Answers: answers,
                            CustomerFirstNameByCustomerId: names
                        }
                        this.modalDialogService.openDialog(this.answersDialogId)
                    })
                })
            }
        }
        
        componentName(): string {
            return 'companyLoanAdditionalQuestions'
        }
        
        onChanges() {
            this.m = null
            this.a = null
            this.isWaitingForPreviousSteps = false

            if (!this.initialData) {
                return
            }

            if (!this.initialData.step.areAllStepBeforeThisAccepted(this.initialData.applicationInfo)) {
                this.isWaitingForPreviousSteps = true
                return
            }

            this.initialData.companyLoanApiClient.fetchAdditionalQuestionsStatus(this.initialData.applicationInfo.ApplicationNr).then(result => {
                this.m = result;
            })
        }

        // To avoid onclick as inline-script due to CSP. 
        focusAndSelect(evt: any) {
            evt.currentTarget.focus();
            evt.currentTarget.select();
        }
    }

    export class CompanyLoanAdditionalQuestionsComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanAdditionalQuestionsController;
            this.templateUrl = 'company-loan-additional-questions.html';
        }
    }

    class AnswersModel {
        Answers: AnswerTableItem[]
        CustomerFirstNameByCustomerId: NTechPreCreditApi.INumberDictionary<string>
    }

    class AnswerTableItem {
        Type: string
        CustomerId?: number
        QuestionCode?: string;
        AnswerCode?: string;
        QuestionText?: string;
        AnswerText?: string;
        CustomerCardUrl?: string
    }
}

angular.module('ntech.components').component('companyLoanAdditionalQuestions', new CompanyLoanAdditionalQuestionsComponentNs.CompanyLoanAdditionalQuestionsComponent())