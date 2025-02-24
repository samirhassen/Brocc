var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (Object.prototype.hasOwnProperty.call(b, p)) d[p] = b[p]; };
        return extendStatics(d, b);
    };
    return function (d, b) {
        if (typeof b !== "function" && b !== null)
            throw new TypeError("Class extends value " + String(b) + " is not a constructor or null");
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
var MortgageLoanApplicationDualDocumentsComponentNs;
(function (MortgageLoanApplicationDualDocumentsComponentNs) {
    var MortgageLoanApplicationDualDocumentsController = /** @class */ (function (_super) {
        __extends(MortgageLoanApplicationDualDocumentsController, _super);
        function MortgageLoanApplicationDualDocumentsController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.modalDialogService = modalDialogService;
            _this.linkDialogId = modalDialogService.generateDialogId();
            return _this;
        }
        MortgageLoanApplicationDualDocumentsController.prototype.showDirectLink = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.modalDialogService.openDialog(this.linkDialogId);
        };
        MortgageLoanApplicationDualDocumentsController.prototype.componentName = function () {
            return 'mortgageLoanApplicationDualDocuments';
        };
        MortgageLoanApplicationDualDocumentsController.prototype.onChanges = function () {
            var _this = this;
            if (!this.initialData) {
                return;
            }
            var ai = this.initialData.applicationInfo;
            var wf = this.initialData.workflowModel;
            var setup = function (q) {
                _this.apiClient.fetchApplicationDocuments(ai.ApplicationNr, ['SignedApplication']).then(function (docs) {
                    _this.apiClient.fetchDualApplicationSignatureStatus(ai.ApplicationNr).then(function (sign) {
                        var id = new ApplicationDocumentsComponentNs.InitialData(_this.initialData.applicationInfo);
                        id.onDocumentsAddedOrRemoved = function (areAllDocumentsAdded) {
                            _this.signalReloadRequired();
                        };
                        var m = {
                            documentsInitialData: {
                                applicationInfo: ai,
                                isReadOnly: wf.isStatusAccepted(ai)
                            },
                            haveAllApplicantsSigned: false,
                            additionalQuestions: q,
                            unsignedDocuments: [],
                            documentSignedApplicationAndPOAData: id
                        };
                        for (var applicantNr = 1; applicantNr <= ai.NrOfApplicants; applicantNr++) {
                            id.addComplexDocument('SignedApplication', "Signed application applicant ".concat(applicantNr), applicantNr, null, null);
                            m.unsignedDocuments.push({
                                title: "Unsigned application applicant ".concat(applicantNr),
                                applicantNr: applicantNr,
                                documentType: 'SignedApplication',
                                documentSubType: null,
                                documentUrl: "/api/MortgageLoan/Create-Application-Poa-Pdf?ApplicationNr=".concat(_this.initialData.applicationInfo.ApplicationNr, "&ApplicantNr=").concat(applicantNr, "&OnlyApplication=True")
                            });
                            var bankNames = sign && sign.BankNamesByApplicantNr && sign.BankNamesByApplicantNr[applicantNr] ? sign.BankNamesByApplicantNr[applicantNr] : null;
                            if (bankNames) {
                                for (var _i = 0, bankNames_1 = bankNames; _i < bankNames_1.length; _i++) {
                                    var bankName = bankNames_1[_i];
                                    id.addComplexDocument('SignedPowerOfAttorney', "Signed POA applicant ".concat(applicantNr, " (").concat(bankName, ")"), applicantNr, null, bankName);
                                    m.unsignedDocuments.push({
                                        title: "Unsigned POA applicant ".concat(applicantNr, " (").concat(bankName, ")"),
                                        applicantNr: applicantNr,
                                        documentType: 'SignedPowerOfAttorney',
                                        documentSubType: bankName,
                                        documentUrl: "/api/MortgageLoan/Create-Application-Poa-Pdf?ApplicationNr=".concat(_this.initialData.applicationInfo.ApplicationNr, "&ApplicantNr=").concat(applicantNr, "&OnlyPoaForBankName=").concat(encodeURIComponent(bankName))
                                    });
                                }
                            }
                        }
                        m.haveAllApplicantsSigned = docs.length >= ai.NrOfApplicants;
                        _this.m = m;
                        _this.setupTest();
                    });
                });
            };
            this.apiClient.fetchCreditApplicationItemSimple(ai.ApplicationNr, ['application.additionalQuestionsAnswerDate'], ApplicationDataSourceHelper.MissingItemReplacementValue).then(function (x) {
                var additionalQuestionsAnswerDate = x['application.additionalQuestionsAnswerDate'];
                if (!additionalQuestionsAnswerDate || additionalQuestionsAnswerDate === 'pending' || additionalQuestionsAnswerDate === ApplicationDataSourceHelper.MissingItemReplacementValue) {
                    additionalQuestionsAnswerDate = null;
                }
                else {
                    additionalQuestionsAnswerDate = moment(additionalQuestionsAnswerDate).format('YYYY-MM-DD');
                }
                if (wf.areAllStepBeforeThisAccepted(ai)) {
                    _this.apiClient.getUserModuleUrl('nCustomerPages', 'a/#/eid-login', {
                        t: "q_".concat(ai.ApplicationNr)
                    }).then(function (x) {
                        setup({
                            linkUrl: wf.isStatusAccepted(ai) ? null : x.UrlExternal,
                            additionalQuestionsAnswerDate: additionalQuestionsAnswerDate
                        });
                    });
                }
                else {
                    setup(null);
                }
            });
        };
        MortgageLoanApplicationDualDocumentsController.prototype.setupTest = function () {
            var _this = this;
            var tf = this.initialData.testFunctions;
            if (this.m.additionalQuestions) {
                var testScopeName_1 = tf.generateUniqueScopeName();
                var addAnswerFunction = function (useExistingCustomerIds) {
                    tf.addFunctionCall(testScopeName_1, 'Auto answer' + (useExistingCustomerIds ? '' : ' (use invalid customerIds)'), function () {
                        _this.apiClient.fetchApplicationInfoWithApplicants(_this.initialData.applicationInfo.ApplicationNr).then(function (applicants) {
                            var handleSignatures = function () {
                                _this.apiClient.fetchApplicationDocuments(applicants.Info.ApplicationNr, ['SignedApplicationAndPOA']).then(function (documents) {
                                    var promises = [];
                                    for (var applicantNr = 1; applicantNr <= applicants.Info.NrOfApplicants; applicantNr++) {
                                        var d = NTechLinq.first(documents, function (x) { return x.ApplicantNr === applicantNr; });
                                        if (!d) {
                                            for (var _i = 0, _a = _this.m.unsignedDocuments; _i < _a.length; _i++) {
                                                var d_1 = _a[_i];
                                                promises.push(_this.apiClient.addApplicationDocument(applicants.Info.ApplicationNr, d_1.documentType, applicantNr, tf.generateTestPdfDataUrl("Signed application for applicant ".concat(applicantNr)), "".concat(d_1.documentType, "{applicantNr}.pdf"), null, d_1.documentSubType));
                                            }
                                        }
                                    }
                                    _this.$q.all(promises).then(function (x) {
                                        _this.signalReloadRequired();
                                    });
                                });
                            };
                            if (!_this.m.additionalQuestions.additionalQuestionsAnswerDate || _this.m.additionalQuestions.additionalQuestionsAnswerDate == 'pending') {
                                var d = {
                                    Items: []
                                };
                                for (var applicantNr = 1; applicantNr <= applicants.Info.NrOfApplicants; applicantNr++) {
                                    var customerId = applicants.CustomerIdByApplicantNr[applicantNr] + (useExistingCustomerIds ? 0 : 1000000);
                                    d.Items.push({
                                        ApplicantNr: applicantNr,
                                        CustomerId: customerId,
                                        IsCustomerQuestion: true,
                                        QuestionGroup: 'customer',
                                        QuestionCode: 'taxCountries',
                                        QuestionText: 'Vilka \u00E4r dina skatter\u00E4ttsliga hemvister?',
                                        AnswerCode: 'FI',
                                        AnswerText: 'Finland'
                                    });
                                    d.Items.push({
                                        ApplicantNr: applicantNr,
                                        CustomerId: customerId,
                                        IsCustomerQuestion: true,
                                        QuestionGroup: 'customer',
                                        QuestionCode: 'isPep',
                                        QuestionText: 'Har du en h\u00F6g politisk befattning inom staten, \u00E4r en n\u00E4ra sl\u00E4kting eller medarbetare till en s\u00E5dan person?',
                                        AnswerCode: 'no',
                                        AnswerText: 'Nej'
                                    });
                                    d.Items.push({
                                        ApplicantNr: applicantNr,
                                        CustomerId: customerId,
                                        IsCustomerQuestion: true,
                                        QuestionGroup: 'customer',
                                        QuestionCode: 'pepRole',
                                        QuestionText: 'Ange vilka roller du, n\u00E5gon i din familj, eller n\u00E4rst\u00E5ende, har haft',
                                        AnswerCode: 'none',
                                        AnswerText: '-'
                                    });
                                    d.Items.push({
                                        ApplicantNr: applicantNr,
                                        CustomerId: customerId,
                                        IsCustomerQuestion: true,
                                        QuestionGroup: 'customer',
                                        QuestionCode: 'pepWho',
                                        QuestionText: 'Vem eller vilka har i s\u00E5 fall haft rollerna?',
                                        AnswerCode: null,
                                        AnswerText: null
                                    });
                                }
                                var consumerBankAccountNr = 'FI6740567584718747';
                                _this.apiClient.submitAdditionalQuestions(_this.initialData.applicationInfo.ApplicationNr, d, consumerBankAccountNr).then(function (x) {
                                    handleSignatures();
                                });
                            }
                            else {
                                handleSignatures();
                            }
                        });
                    });
                };
                addAnswerFunction(true);
                addAnswerFunction(false);
            }
        };
        MortgageLoanApplicationDualDocumentsController.prototype.getMortgageLoanDocumentCheckStatus = function () {
            if (!this.initialData) {
                return null;
            }
            return this.initialData.workflowModel.getStepStatus(this.initialData.applicationInfo);
        };
        MortgageLoanApplicationDualDocumentsController.prototype.isToggleMortgageLoanDocumentCheckStatusAllowed = function () {
            if (!this.initialData || !this.m) {
                return false;
            }
            var ai = this.initialData.applicationInfo;
            var wf = this.initialData.workflowModel;
            return this.m.haveAllApplicantsSigned && ai.IsActive && !ai.IsPartiallyApproved && !ai.HasLockedAgreement
                && wf.areAllStepBeforeThisAccepted(ai) && wf.areAllStepsAfterInitial(ai);
        };
        MortgageLoanApplicationDualDocumentsController.prototype.toggleMortgageLoanDocumentCheckStatus = function () {
            this.toggleMortgageLoanListBasedStatus();
        };
        MortgageLoanApplicationDualDocumentsController.prototype.toggleMortgageLoanListBasedStatus = function () {
            var _this = this;
            if (!this.initialData) {
                return;
            }
            var ai = this.initialData.applicationInfo;
            var step = this.initialData.workflowModel;
            this.initialData.apiClient.setMortgageApplicationWorkflowStatus(ai.ApplicationNr, step.stepName, step.isStatusAccepted(ai) ? 'Initial' : 'Accepted').then(function () {
                _this.signalReloadRequired();
            });
        };
        MortgageLoanApplicationDualDocumentsController.prototype.checkStatusUnsignedApplicationButton = function () {
            if (!this.initialData || !this.m) {
                return false;
            }
            var ai = this.initialData.applicationInfo;
            var step = this.initialData.workflowModel;
            if (step.getStepStatus(ai) == 'Initial' && step.areAllStepBeforeThisAccepted(ai) && (this.m.additionalQuestions.additionalQuestionsAnswerDate && this.m.additionalQuestions.additionalQuestionsAnswerDate != 'pending'))
                return true;
            else
                return false;
        };
        // To avoid onclick as inline-script due to CSP. 
        MortgageLoanApplicationDualDocumentsController.prototype.focusAndSelect = function (evt) {
            evt.currentTarget.focus();
            evt.currentTarget.select();
        };
        MortgageLoanApplicationDualDocumentsController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return MortgageLoanApplicationDualDocumentsController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanApplicationDualDocumentsComponentNs.MortgageLoanApplicationDualDocumentsController = MortgageLoanApplicationDualDocumentsController;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageLoanApplicationDualDocumentsComponentNs.Model = Model;
    var UnsignedDocumentModel = /** @class */ (function () {
        function UnsignedDocumentModel() {
        }
        return UnsignedDocumentModel;
    }());
    MortgageLoanApplicationDualDocumentsComponentNs.UnsignedDocumentModel = UnsignedDocumentModel;
    var AdditionalQuestionsModel = /** @class */ (function () {
        function AdditionalQuestionsModel() {
        }
        return AdditionalQuestionsModel;
    }());
    MortgageLoanApplicationDualDocumentsComponentNs.AdditionalQuestionsModel = AdditionalQuestionsModel;
    var MortgageLoanApplicationDualDocumentsComponent = /** @class */ (function () {
        function MortgageLoanApplicationDualDocumentsComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationDualDocumentsController;
            this.template = "<div ng-if=\"$ctrl.m\" class=\"container\">\n                    <div class=\"row pb-2\" ng-if=\"$ctrl.m.additionalQuestions\">\n                        <div class=\"form-horizontal\">\n                            <div class=\"col-xs-6\">\n                                <div class=\"form-group\">\n                                    <label class=\"col-xs-6 control-label\">Additional questions</label>\n                                    <div class=\"col-xs-6 form-control-static\">\n                                        <button ng-if=\"$ctrl.m.additionalQuestions.linkUrl\" ng-click=\"$ctrl.showDirectLink($event)\" class=\"n-direct-btn n-turquoise-btn\">Link <span class=\"glyphicon glyphicon-resize-full\"></span></button>\n                                    </div>\n                                </div>\n                            </div>\n                            <div class=\"col-xs-6\">\n                                <div class=\"form-group\">\n                                    <label class=\"col-xs-6 control-label\">Answered</label>\n                                    <div class=\"col-xs-6 form-control-static\">{{$ctrl.m.additionalQuestions.additionalQuestionsAnswerDate}}</div>\n                                </div>\n                            </div>\n                        </div>\n\n                        <modal-dialog dialog-title=\"'Additional questions link'\" dialog-id=\"$ctrl.linkDialogId\">\n                            <div class=\"modal-body\">\n                                <textarea rows=\"1\" style=\"width:100%;resize: none\" ng-click=\"$ctrl.focusAndSelect($event)\" readonly=\"readonly\">{{$ctrl.m.additionalQuestions.linkUrl}}</textarea>\n                            </div>\n                        </modal-dialog>\n                    </div>\n\n                    <div class=\"row pb-2\" ng-if=\"$ctrl.m.unsignedDocuments\" ng-show=\"$ctrl.checkStatusUnsignedApplicationButton()\">\n                        <div class=\"col-xs-6\">\n                            <div class=\"form-group row\" ng-repeat=\"d in $ctrl.m.unsignedDocuments\">\n                                <label class=\"col-xs-6 control-label\">{{d.title}}</label>\n                                <div class=\"col-xs-6 form-control-static\">\n                                    <a ng-href=\"{{d.documentUrl}}\" target=\"_blank\" class=\"n-direct-btn n-purple-btn\"> File<span class=\"glyphicon glyphicon-save\"></span></a>\n                                </div>\n                            </div>\n                        </div>\n                    </div>\n\n                    <div class=\"row pb-2\">\n                        <application-documents initial-data=\"$ctrl.m.documentSignedApplicationAndPOAData\" >\n                        </application-documents>\n                    </div>\n\n                    <div class=\"row\">\n                        <application-freeform-documents initial-data=\"$ctrl.m.documentsInitialData\">\n                        </application-freeform-documents>\n                    </div>\n\n                    <div class=\"row\">\n                        <div class=\"pt-3\" ng-show=\"$ctrl.isToggleMortgageLoanDocumentCheckStatusAllowed()\">\n                            <label class=\"pr-2\">Document control {{$ctrl.getMortgageLoanDocumentCheckStatus() === 'Accepted' ? 'done' : 'not done'}}</label>\n                            <label class=\"n-toggle\">\n                                <input type=\"checkbox\" ng-checked=\"$ctrl.getMortgageLoanDocumentCheckStatus() === 'Accepted'\" ng-click=\"$ctrl.toggleMortgageLoanDocumentCheckStatus()\" />\n                                <span class=\"n-slider\"></span>\n                            </label>\n                        </div>\n                    </div>\n\n                    </div>";
        }
        return MortgageLoanApplicationDualDocumentsComponent;
    }());
    MortgageLoanApplicationDualDocumentsComponentNs.MortgageLoanApplicationDualDocumentsComponent = MortgageLoanApplicationDualDocumentsComponent;
})(MortgageLoanApplicationDualDocumentsComponentNs || (MortgageLoanApplicationDualDocumentsComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanApplicationDualDocuments', new MortgageLoanApplicationDualDocumentsComponentNs.MortgageLoanApplicationDualDocumentsComponent());
