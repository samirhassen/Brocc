namespace MortgageApplicationAdditionalQuestionsComponentNs {
    
    export class MortgageApplicationAdditionalQuestionsController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData;
        m: NTechPreCreditApi.MortgageLoanAdditionalQuestionsStatusModel;        
        a: AnswersModel
        documentsInitialData: ApplicationDocumentsComponentNs.InitialData
        answersDialogId: string
        
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

        nrOfApplicants() {
            if (this.initialData) {
                return this.initialData.applicationInfo.NrOfApplicants
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
        }

        showAnswers(key: string, evt: Event) {
            if (evt) {
                evt.preventDefault();
            }
            if (this.a) {
                this.modalDialogService.openDialog(this.answersDialogId)
            } else {
                this.apiClient.fetchMortgageLoanAdditionalQuestionsDocument(key).then(result => {
                    this.apiClient.fetchMortgageLoanCurrentLoans(this.applicationNr()).then(loansResult => {
                        this.a = {
                            answersDocument: result,
                            currentLoansModel: loansResult
                        }
                        this.modalDialogService.openDialog(this.answersDialogId)
                    })
                })
            }
        }
        
        componentName(): string {
            return 'mortgageApplicationAdditionalQuestions'
        }
        
        onChanges() {
            this.m = null
            this.a = null
            this.documentsInitialData = null

            if (!this.initialData) {
                return
            }

            this.apiClient.fetchMortageLoanAdditionalQuestionsStatus(this.initialData.applicationInfo.ApplicationNr).then(result => {
                this.m = result;
            })

            this.documentsInitialData = new ApplicationDocumentsComponentNs.InitialData(this.initialData.applicationInfo)
                .addSharedDocument('MortgageLoanCustomerAmortizationPlan', 'Amortization basis')
                .addSharedDocument('MortgageLoanLagenhetsutdrag', 'L\u00e4genhetsutdrag');
        }

        onDocumentsAddedOrRemoved = () => {
            this.apiClient.updateMortgageLoanAdditionalQuestionsStatus(this.initialData.applicationInfo.ApplicationNr).then(result => {
                if (result.WasStatusChanged) {
                    this.signalReloadRequired()
                }
            })
        }
    }

    export class MortgageApplicationAdditionalQuestionsComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationAdditionalQuestionsController;
            this.templateUrl = 'mortgage-application-additional-questions.html';
        }
    }

    export class InitialData {
        applicationInfo: NTechPreCreditApi.ApplicationInfoModel
        backUrl: string
    }

    class AnswersModel {
        answersDocument: NTechPreCreditApi.MortgageLoanAdditionalQuestionsDocument
        currentLoansModel: any        
    }
}

angular.module('ntech.components').component('mortgageApplicationAdditionalQuestions', new MortgageApplicationAdditionalQuestionsComponentNs.MortgageApplicationAdditionalQuestionsComponent())