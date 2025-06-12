namespace MortgageLoanApplicationDualDocumentsComponentNs {
    export class MortgageLoanApplicationDualDocumentsController extends NTechComponents.NTechComponentControllerBase {
        initialData: MortgageLoanApplicationDynamicComponentNs.StepInitialData;
        m: Model
        linkDialogId: string

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);

            this.linkDialogId = modalDialogService.generateDialogId()
        }

        showDirectLink(evt: Event) {
            if (evt) {
                evt.preventDefault();
            }
            this.modalDialogService.openDialog(this.linkDialogId)
        }

        componentName(): string {
            return 'mortgageLoanApplicationDualDocuments'
        }

        onChanges() {
            if (!this.initialData) {
                return
            }

            let ai = this.initialData.applicationInfo
            let wf = this.initialData.workflowModel

            let setup = (q: AdditionalQuestionsModel) => {
                this.apiClient.fetchApplicationDocuments(ai.ApplicationNr, ['SignedApplication']).then(docs => {
                    this.apiClient.fetchDualApplicationSignatureStatus(ai.ApplicationNr).then(sign => {
                        let id = new ApplicationDocumentsComponentNs.InitialData(this.initialData.applicationInfo)

                        id.onDocumentsAddedOrRemoved = (areAllDocumentsAdded) => {
                            this.signalReloadRequired()
                        }
                        let m: Model = {
                            documentsInitialData: {
                                applicationInfo: ai,
                                isReadOnly: wf.isStatusAccepted(ai)
                            },
                            haveAllApplicantsSigned: false,
                            additionalQuestions: q,
                            unsignedDocuments: [],
                            documentSignedApplicationAndPOAData: id
                        }
                        for (var applicantNr = 1; applicantNr <= ai.NrOfApplicants; applicantNr++) {
                            id.addComplexDocument('SignedApplication', `Signed application applicant ${applicantNr}`, applicantNr, null, null);
                            m.unsignedDocuments.push({
                                title: `Unsigned application applicant ${applicantNr}`,
                                applicantNr: applicantNr,
                                documentType: 'SignedApplication',
                                documentSubType: null,
                                documentUrl: `/api/MortgageLoan/Create-Application-Poa-Pdf?ApplicationNr=${this.initialData.applicationInfo.ApplicationNr}&ApplicantNr=${applicantNr}&OnlyApplication=True`
                            })
                            let bankNames = sign && sign.BankNamesByApplicantNr && sign.BankNamesByApplicantNr[applicantNr] ? sign.BankNamesByApplicantNr[applicantNr] : null
                            if (bankNames) {
                                for (let bankName of bankNames) {
                                    id.addComplexDocument('SignedPowerOfAttorney', `Signed POA applicant ${applicantNr} (${bankName})`, applicantNr, null, bankName)
                                    m.unsignedDocuments.push({
                                        title: `Unsigned POA applicant ${applicantNr} (${bankName})`,
                                        applicantNr: applicantNr,
                                        documentType: 'SignedPowerOfAttorney',
                                        documentSubType: bankName,
                                        documentUrl: `/api/MortgageLoan/Create-Application-Poa-Pdf?ApplicationNr=${this.initialData.applicationInfo.ApplicationNr}&ApplicantNr=${applicantNr}&OnlyPoaForBankName=${encodeURIComponent(bankName)}`
                                    })
                                }
                            }
                        }
                        m.haveAllApplicantsSigned = docs.length >= ai.NrOfApplicants;
                        this.m = m
                        this.setupTest()
                    })
                })
            }

            this.apiClient.fetchCreditApplicationItemSimple(ai.ApplicationNr,
                ['application.additionalQuestionsAnswerDate'],
                ApplicationDataSourceHelper.MissingItemReplacementValue).then(x => {
                    let additionalQuestionsAnswerDate: string = x['application.additionalQuestionsAnswerDate']
                    if (!additionalQuestionsAnswerDate || additionalQuestionsAnswerDate === 'pending' || additionalQuestionsAnswerDate === ApplicationDataSourceHelper.MissingItemReplacementValue) {
                        additionalQuestionsAnswerDate = null
                    } else {
                        additionalQuestionsAnswerDate = moment(additionalQuestionsAnswerDate).format('YYYY-MM-DD')
                    }
                    if (wf.areAllStepBeforeThisAccepted(ai)) {
                        this.apiClient.getUserModuleUrl('nCustomerPages', 'a/#/eid-login', {
                            t: `q_${ai.ApplicationNr}`
                        }).then(x => {
                            setup({
                                linkUrl: wf.isStatusAccepted(ai) ? null : x.UrlExternal,
                                additionalQuestionsAnswerDate: additionalQuestionsAnswerDate
                            })
                        })
                    } else {
                        setup(null)
                    }
                })
        }

        private setupTest() {
            let tf = this.initialData.testFunctions
            if (this.m.additionalQuestions) {
                let testScopeName = tf.generateUniqueScopeName()
                let addAnswerFunction = useExistingCustomerIds => {
                    tf.addFunctionCall(testScopeName, 'Auto answer' + (useExistingCustomerIds ? '' : ' (use invalid customerIds)'), () => {
                        this.apiClient.fetchApplicationInfoWithApplicants(this.initialData.applicationInfo.ApplicationNr).then(applicants => {
                            let handleSignatures = () => {
                                this.apiClient.fetchApplicationDocuments(applicants.Info.ApplicationNr, ['SignedApplicationAndPOA']).then(documents => {
                                    let promises = []
                                    for (var applicantNr = 1; applicantNr <= applicants.Info.NrOfApplicants; applicantNr++) {
                                        let d = NTechLinq.first(documents, x => x.ApplicantNr === applicantNr)
                                        if (!d) {
                                            for (let d of this.m.unsignedDocuments) {
                                                promises.push(this.apiClient.addApplicationDocument(
                                                    applicants.Info.ApplicationNr, d.documentType, applicantNr,
                                                    tf.generateTestPdfDataUrl(`Signed application for applicant ${applicantNr}`), `${d.documentType}{applicantNr}.pdf`, null, d.documentSubType))
                                            }
                                        }
                                    }
                                    this.$q.all(promises).then(x => {
                                        this.signalReloadRequired()
                                    })
                                })
                            }
                            if (!this.m.additionalQuestions.additionalQuestionsAnswerDate || this.m.additionalQuestions.additionalQuestionsAnswerDate == 'pending') {
                                let d: NTechPreCreditApi.MortgageLoanAdditionalQuestionsDocument = {
                                    Items: []
                                }
                                for (var applicantNr = 1; applicantNr <= applicants.Info.NrOfApplicants; applicantNr++) {
                                    let customerId = applicants.CustomerIdByApplicantNr[applicantNr] + (useExistingCustomerIds ? 0 : 1000000)
                                    d.Items.push({
                                        ApplicantNr: applicantNr,
                                        CustomerId: customerId,
                                        IsCustomerQuestion: true,
                                        QuestionGroup: 'customer',
                                        QuestionCode: 'taxCountries',
                                        QuestionText: 'Vilka \u00E4r dina skatter\u00E4ttsliga hemvister?',
                                        AnswerCode: 'FI',
                                        AnswerText: 'Finland'
                                    })
                                    d.Items.push({
                                        ApplicantNr: applicantNr,
                                        CustomerId: customerId,
                                        IsCustomerQuestion: true,
                                        QuestionGroup: 'customer',
                                        QuestionCode: 'isPep',
                                        QuestionText: 'Har du en h\u00F6g politisk befattning inom staten, \u00E4r en n\u00E4ra sl\u00E4kting eller medarbetare till en s\u00E5dan person?',
                                        AnswerCode: 'no',
                                        AnswerText: 'Nej'
                                    })
                                    d.Items.push({
                                        ApplicantNr: applicantNr,
                                        CustomerId: customerId,
                                        IsCustomerQuestion: true,
                                        QuestionGroup: 'customer',
                                        QuestionCode: 'pepRole',
                                        QuestionText: 'Ange vilka roller du, n\u00E5gon i din familj, eller n\u00E4rst\u00E5ende, har haft',
                                        AnswerCode: 'none',
                                        AnswerText: '-'
                                    })
                                    d.Items.push({
                                        ApplicantNr: applicantNr,
                                        CustomerId: customerId,
                                        IsCustomerQuestion: true,
                                        QuestionGroup: 'customer',
                                        QuestionCode: 'pepWho',
                                        QuestionText: 'Vem eller vilka har i s\u00E5 fall haft rollerna?',
                                        AnswerCode: null,
                                        AnswerText: null
                                    })
                                }
                                let consumerBankAccountNr = 'FI6740567584718747'
                                
                                this.apiClient.submitAdditionalQuestions(this.initialData.applicationInfo.ApplicationNr, d, consumerBankAccountNr).then(x => {
                                    handleSignatures()
                                })
                            } else {
                                handleSignatures()
                            }
                        })
                    })
                }

                addAnswerFunction(true)
                addAnswerFunction(false)
            }
        }

        getMortgageLoanDocumentCheckStatus() {
            if (!this.initialData) {
                return null
            }
            return this.initialData.workflowModel.getStepStatus(this.initialData.applicationInfo)
        }

        isToggleMortgageLoanDocumentCheckStatusAllowed() {
            if (!this.initialData || !this.m) {
                return false
            }

            let ai = this.initialData.applicationInfo
            let wf = this.initialData.workflowModel

            return this.m.haveAllApplicantsSigned && ai.IsActive && !ai.IsPartiallyApproved && !ai.HasLockedAgreement
                && wf.areAllStepBeforeThisAccepted(ai) && wf.areAllStepsAfterInitial(ai)
        }

        toggleMortgageLoanDocumentCheckStatus() {
            this.toggleMortgageLoanListBasedStatus()
        }

        toggleMortgageLoanListBasedStatus() {
            if (!this.initialData) {
                return
            }
            let ai = this.initialData.applicationInfo
            let step = this.initialData.workflowModel

            this.initialData.apiClient.setMortgageApplicationWorkflowStatus(ai.ApplicationNr, step.stepName, step.isStatusAccepted(ai) ? 'Initial' : 'Accepted').then(() => {
                this.signalReloadRequired()
            })
        }
        checkStatusUnsignedApplicationButton() {
            if (!this.initialData || !this.m) {
                return false
            }

            let ai = this.initialData.applicationInfo
            let step = this.initialData.workflowModel

            if (step.getStepStatus(ai) == 'Initial' && step.areAllStepBeforeThisAccepted(ai) && (this.m.additionalQuestions.additionalQuestionsAnswerDate && this.m.additionalQuestions.additionalQuestionsAnswerDate != 'pending'))
                return true
            else
                return false
        }

        // To avoid onclick as inline-script due to CSP. 
        focusAndSelect(evt: any) {
            evt.currentTarget.focus();
            evt.currentTarget.select();
        }
    }

    export class Model {
        documentsInitialData: ApplicationFreeformDocumentsComponentNs.InitialData
        additionalQuestions: AdditionalQuestionsModel
        unsignedDocuments: UnsignedDocumentModel[]
        documentSignedApplicationAndPOAData: ApplicationDocumentsComponentNs.InitialData
        haveAllApplicantsSigned: boolean
    }

    export class UnsignedDocumentModel {
        title: string
        applicantNr: number
        documentType: string
        documentSubType: string
        documentUrl: string
    }

    export class AdditionalQuestionsModel {
        additionalQuestionsAnswerDate: string
        linkUrl: string
    }

    export class MortgageLoanApplicationDualDocumentsComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationDualDocumentsController;
            this.template = `<div ng-if="$ctrl.m" class="container">
                    <div class="row pb-2" ng-if="$ctrl.m.additionalQuestions">
                        <div class="form-horizontal">
                            <div class="col-xs-6">
                                <div class="form-group">
                                    <label class="col-xs-6 control-label">Additional questions</label>
                                    <div class="col-xs-6 form-control-static">
                                        <button ng-if="$ctrl.m.additionalQuestions.linkUrl" ng-click="$ctrl.showDirectLink($event)" class="n-direct-btn n-turquoise-btn">Link <span class="glyphicon glyphicon-resize-full"></span></button>
                                    </div>
                                </div>
                            </div>
                            <div class="col-xs-6">
                                <div class="form-group">
                                    <label class="col-xs-6 control-label">Answered</label>
                                    <div class="col-xs-6 form-control-static">{{$ctrl.m.additionalQuestions.additionalQuestionsAnswerDate}}</div>
                                </div>
                            </div>
                        </div>

                        <modal-dialog dialog-title="'Additional questions link'" dialog-id="$ctrl.linkDialogId">
                            <div class="modal-body">
                                <textarea rows="1" style="width:100%;resize: none" ng-click="$ctrl.focusAndSelect($event)" readonly="readonly">{{$ctrl.m.additionalQuestions.linkUrl}}</textarea>
                            </div>
                        </modal-dialog>
                    </div>

                    <div class="row pb-2" ng-if="$ctrl.m.unsignedDocuments" ng-show="$ctrl.checkStatusUnsignedApplicationButton()">
                        <div class="col-xs-6">
                            <div class="form-group row" ng-repeat="d in $ctrl.m.unsignedDocuments">
                                <label class="col-xs-6 control-label">{{d.title}}</label>
                                <div class="col-xs-6 form-control-static">
                                    <a ng-href="{{d.documentUrl}}" target="_blank" class="n-direct-btn n-purple-btn"> File<span class="glyphicon glyphicon-save"></span></a>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="row pb-2">
                        <application-documents initial-data="$ctrl.m.documentSignedApplicationAndPOAData" >
                        </application-documents>
                    </div>

                    <div class="row">
                        <application-freeform-documents initial-data="$ctrl.m.documentsInitialData">
                        </application-freeform-documents>
                    </div>

                    <div class="row">
                        <div class="pt-3" ng-show="$ctrl.isToggleMortgageLoanDocumentCheckStatusAllowed()">
                            <label class="pr-2">Document control {{$ctrl.getMortgageLoanDocumentCheckStatus() === 'Accepted' ? 'done' : 'not done'}}</label>
                            <label class="n-toggle">
                                <input type="checkbox" ng-checked="$ctrl.getMortgageLoanDocumentCheckStatus() === 'Accepted'" ng-click="$ctrl.toggleMortgageLoanDocumentCheckStatus()" />
                                <span class="n-slider"></span>
                            </label>
                        </div>
                    </div>

                    </div>`
        }
    }
}

angular.module('ntech.components').component('mortgageLoanApplicationDualDocuments', new MortgageLoanApplicationDualDocumentsComponentNs.MortgageLoanApplicationDualDocumentsComponent())