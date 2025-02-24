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
var MortgageLoanApplicationDualSignAgreementComponentNs;
(function (MortgageLoanApplicationDualSignAgreementComponentNs) {
    var MortgageLoanApplicationDualSignAgreementController = /** @class */ (function (_super) {
        __extends(MortgageLoanApplicationDualSignAgreementController, _super);
        function MortgageLoanApplicationDualSignAgreementController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.modalDialogService = modalDialogService;
            _this.linkDialogId = modalDialogService.generateDialogId();
            return _this;
        }
        MortgageLoanApplicationDualSignAgreementController.prototype.componentName = function () {
            return 'mortgageLoanApplicationDualSignAgreement';
        };
        MortgageLoanApplicationDualSignAgreementController.prototype.onChanges = function () {
            this.reload();
        };
        MortgageLoanApplicationDualSignAgreementController.prototype.reload = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData || !this.initialData.applicationInfo) {
                return;
            }
            var ai = this.initialData.applicationInfo;
            var wf = this.initialData.workflowModel;
            var init = function (customersWithRoles, lockedAgreement, aia, docs) {
                if (!wf.areAllStepBeforeThisAccepted(ai)) {
                    _this.m = null;
                    return;
                }
                _this.apiClient.fetchDualAgreementSignatureStatus(ai.ApplicationNr).then(function (s) {
                    var i = new ApplicationDocumentsComponentNs.InitialData(ai);
                    i.onDocumentsAddedOrRemoved = function (areAllDocumentsAdded) {
                        _this.reload();
                    };
                    i.forceReadonly = wf.isStatusAccepted(ai);
                    _this.m = {
                        isApproveAllowed: false,
                        documentsInitialData: i,
                        customers: [],
                        isPendingSignatures: s.IsPendingSignatures,
                        currentSignatureLink: null
                    };
                    var isAnyMissingDocuments = false;
                    var _loop_1 = function (customerId) {
                        var firstName = customersWithRoles.firstNameAndBirthDateByCustomerId[customerId]['firstName'];
                        var birthDate = customersWithRoles.firstNameAndBirthDateByCustomerId[customerId]['birthDate'];
                        //---- Document -----
                        var roles = '';
                        for (var _b = 0, _c = customersWithRoles.rolesByCustomerId[customerId]; _b < _c.length; _b++) {
                            var roleName = _c[_b];
                            if (roles.length > 0) {
                                roles += ', ';
                            }
                            roles += roleName;
                        }
                        var title = "".concat(firstName, ", ").concat(birthDate, " (").concat(roles, ")");
                        var applicantNr = null;
                        if (aia.CustomerIdByApplicantNr[1] == customerId) {
                            applicantNr = 1;
                        }
                        else if (ai.NrOfApplicants > 1 && aia.CustomerIdByApplicantNr[2] == customerId) {
                            applicantNr = 2;
                        }
                        //NOTE: If you add applicantNr here make sure it's also included when the document is saved from the callback or these wont match
                        i.addComplexDocument('SignedAgreement', title, null, customerId, null);
                        var signedAgreementDocument = NTechLinq.first(docs, function (x) { return x.CustomerId === customerId; });
                        if (!signedAgreementDocument) {
                            isAnyMissingDocuments = true;
                        }
                        //--Customer---
                        var archiveKey = lockedAgreement && lockedAgreement.UnsignedAgreementArchiveKeyByCustomerId ? lockedAgreement.UnsignedAgreementArchiveKeyByCustomerId[customerId] : null;
                        var unsignedAgreementUrl = archiveKey ? "/CreditManagement/ArchiveDocument?key=".concat(archiveKey) : null;
                        _this.m.customers.push({
                            customerId: customerId,
                            firstName: firstName,
                            birthDate: birthDate,
                            roleNames: customersWithRoles.rolesByCustomerId[customerId],
                            unsignedAgreementUrl: unsignedAgreementUrl,
                            signedAgreement: signedAgreementDocument ? { date: signedAgreementDocument.DocumentDate, url: "/CreditManagement/ArchiveDocument?key=".concat(signedAgreementDocument.DocumentArchiveKey) } : null,
                            signatureToken: s.IsPendingSignatures ? s.SignatureTokenByCustomerId[customerId] : null
                        });
                    };
                    for (var _i = 0, _a = customersWithRoles.customerIds; _i < _a.length; _i++) {
                        var customerId = _a[_i];
                        _loop_1(customerId);
                    }
                    _this.m.isApproveAllowed = ai.IsActive && ai.HasLockedAgreement && !isAnyMissingDocuments
                        && wf.areAllStepBeforeThisAccepted(ai)
                        && wf.isStatusInitial(ai);
                    if (_this.initialData.isTest) {
                        var tf_1 = _this.initialData.testFunctions;
                        tf_1.addFunctionCall(tf_1.generateUniqueScopeName(), 'Add mock signed agreements', function () {
                            //Small pdf from https://stackoverflow.com/questions/17279712/what-is-the-smallest-possible-valid-pdf
                            var promises = [];
                            for (var _i = 0, _a = _this.m.customers; _i < _a.length; _i++) {
                                var c = _a[_i];
                                promises.push(_this.apiClient.addApplicationDocument(_this.initialData.applicationInfo.ApplicationNr, 'SignedAgreement', null, tf_1.generateTestPdfDataUrl("Signed agreement on ".concat(_this.initialData.applicationInfo.ApplicationNr, " for ").concat(c.firstName, ", ").concat(c.birthDate)), "SignedAgreement".concat(c.customerId, ".pdf"), c.customerId, null));
                            }
                            _this.$q.all(promises).then(function (x) {
                                _this.signalReloadRequired();
                            });
                        });
                    }
                });
            };
            this.apiClient.getLockedAgreement(ai.ApplicationNr).then(function (lockedAgreement) {
                _this.apiClient.fetchApplicationInfoWithApplicants(ai.ApplicationNr).then(function (aia) {
                    MortgageLoanDualCustomerRoleHelperNs.getApplicationCustomerRolesByCustomerId(ai.ApplicationNr, _this.apiClient).then(function (x) {
                        _this.apiClient.fetchApplicationDocuments(ai.ApplicationNr, ['SignedAgreement']).then(function (docs) {
                            init(x, lockedAgreement.LockedAgreement, aia, docs);
                        });
                    });
                });
            });
        };
        MortgageLoanApplicationDualSignAgreementController.prototype.showSignatureLink = function (c, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.m.currentSignatureLink = null;
            this.apiClient.getUserModuleUrl('nCustomerPages', '/a/#/token-login', {
                t: "st_".concat(c.signatureToken)
            }).then(function (x) {
                _this.m.currentSignatureLink = {
                    linkTitle: "Signature link for ".concat(c.firstName, ", ").concat(c.birthDate),
                    linkUrl: x.UrlExternal
                };
                _this.modalDialogService.openDialog(_this.linkDialogId);
            });
        };
        MortgageLoanApplicationDualSignAgreementController.prototype.approve = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.setMortgageApplicationWorkflowStatus(this.initialData.applicationInfo.ApplicationNr, this.initialData.workflowModel.currentStep.Name, 'Accepted', 'Agreements signed').then(function (y) {
                _this.signalReloadRequired();
            });
        };
        MortgageLoanApplicationDualSignAgreementController.prototype.glyphIconClassFromBoolean = function (isAccepted, isRejected) {
            return ApplicationStatusBlockComponentNs.getIconClass(isAccepted, isRejected);
        };
        // To avoid onclick as inline-script due to CSP. 
        MortgageLoanApplicationDualSignAgreementController.prototype.focusAndSelect = function (evt) {
            evt.currentTarget.focus();
            evt.currentTarget.select();
        };
        MortgageLoanApplicationDualSignAgreementController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return MortgageLoanApplicationDualSignAgreementController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanApplicationDualSignAgreementComponentNs.MortgageLoanApplicationDualSignAgreementController = MortgageLoanApplicationDualSignAgreementController;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageLoanApplicationDualSignAgreementComponentNs.Model = Model;
    var CustomerModel = /** @class */ (function () {
        function CustomerModel() {
        }
        return CustomerModel;
    }());
    MortgageLoanApplicationDualSignAgreementComponentNs.CustomerModel = CustomerModel;
    var MortgageLoanApplicationDualSignAgreementCheckComponent = /** @class */ (function () {
        function MortgageLoanApplicationDualSignAgreementCheckComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationDualSignAgreementController;
            this.template = "<div class=\"container\" ng-if=\"$ctrl.m\">\n\n<div class=\"row\" ng-if=\"$ctrl.m.customers\">\n    <table class=\"table col-sm-12\">\n        <tbody>\n            <tr ng-repeat=\"c in $ctrl.m.customers\">\n                <td class=\"col-xs-5\">{{c.firstName}}, {{c.birthDate}} (<span ng-repeat=\"r in c.roleNames\" class=\"comma\">{{r}}</span>)</td>\n                <td class=\"col-xs-1\"><span class=\"glyphicon\" ng-class=\"{{$ctrl.glyphIconClassFromBoolean(!!c.signedAgreement, false)}}\"></span></td>\n                <td class=\"col-xs-3 text-right\" ng-if=\"!c.signedAgreement\">\n                    <a ng-if=\"c.unsignedAgreementUrl\" ng-href=\"{{c.unsignedAgreementUrl}}\" target=\"_blank\" class=\"n-direct-btn n-purple-btn\">UNSIGNED <span class=\"glyphicon glyphicon-save\"></span></a>\n                    <span ng-if=\"!c.unsignedAgreementUrl\">Missing</span>\n                </td>\n                <td class=\"col-xs-3 text-right\" ng-if=\"!c.signedAgreement\">\n                    <button ng-if=\"$ctrl.m.isPendingSignatures\" ng-click=\"$ctrl.showSignatureLink(c, $event)\" class=\"n-direct-btn n-turquoise-btn\">Signature link <span class=\"glyphicon glyphicon-resize-full\"></span></button>\n               </td>\n                <td class=\"col-xs-6 text-right\" colspan=\"2\" ng-if=\"c.signedAgreement\">\n                    <spa >Signed {{c.signedAgreement.date | date:'short'}}</span>\n                </td>\n            </tr>\n        </tbody>\n    </table>\n\n    <modal-dialog dialog-title=\"$ctrl.m.currentSignatureLink.linkTitle\" dialog-id=\"$ctrl.linkDialogId\">\n        <div class=\"modal-body\">\n            <textarea ng-if=\"$ctrl.m.currentSignatureLink\" rows=\"1\" style=\"width:100%;resize: none\" ng-click=\"$ctrl.focusAndSelect($event)\" readonly=\"readonly\">{{$ctrl.m.currentSignatureLink.linkUrl}}</textarea>\n        </div>\n    </modal-dialog>\n</div>\n\n<div class=\"row\">\n    <application-documents initial-data=\"$ctrl.m.documentsInitialData\">\n\n    </application-documents>\n</div>\n\n<div class=\"row\" ng-if=\"$ctrl.m.isApproveAllowed\">\n    <div class=\"text-center pt-3\">\n        <button class=\"n-main-btn n-green-btn\" ng-click=\"$ctrl.approve($event)\">\n            Approve\n        </button>\n    </div>\n</div>\n\n</div>";
        }
        return MortgageLoanApplicationDualSignAgreementCheckComponent;
    }());
    MortgageLoanApplicationDualSignAgreementComponentNs.MortgageLoanApplicationDualSignAgreementCheckComponent = MortgageLoanApplicationDualSignAgreementCheckComponent;
})(MortgageLoanApplicationDualSignAgreementComponentNs || (MortgageLoanApplicationDualSignAgreementComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanApplicationDualSignAgreement', new MortgageLoanApplicationDualSignAgreementComponentNs.MortgageLoanApplicationDualSignAgreementCheckComponent());
