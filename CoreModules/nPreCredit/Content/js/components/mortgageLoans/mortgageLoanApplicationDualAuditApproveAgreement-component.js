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
var MortgageLoanApplicationDualAuditApproveAgreementComponentNs;
(function (MortgageLoanApplicationDualAuditApproveAgreementComponentNs) {
    var MortgageApplicationRawController = /** @class */ (function (_super) {
        __extends(MortgageApplicationRawController, _super);
        function MortgageApplicationRawController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.modalDialogService = modalDialogService;
            return _this;
        }
        MortgageApplicationRawController.prototype.componentName = function () {
            return 'mortgageLoanApplicationDualAuditApproveAgreement';
        };
        MortgageApplicationRawController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData || !this.initialData.applicationInfo) {
                return;
            }
            var ai = this.initialData.applicationInfo;
            var setup = function (lockedAgreement) {
                MortgageLoanDualCustomerRoleHelperNs.getApplicationCustomerRolesByCustomerId(ai.ApplicationNr, _this.apiClient).then(function (x) {
                    var stepCode = _this.getStepCode();
                    if (!(stepCode === StepCode.Audit || stepCode === StepCode.Approve)) {
                        return;
                    }
                    var isAudit = stepCode === StepCode.Audit;
                    if (wf.areAllStepBeforeThisAccepted(ai)) {
                        var m = {
                            customers: [],
                            isApproveAllowed: false,
                            isCancelAllowed: false,
                            lockedAgreement: lockedAgreement,
                            isApproveStep: stepCode === StepCode.Approve,
                            showWaitingForApproval: false,
                            isApproved: false
                        };
                        for (var _i = 0, _a = x.customerIds; _i < _a.length; _i++) {
                            var customerId = _a[_i];
                            var agreementUrl = void 0;
                            if (ai.HasLockedAgreement) {
                                var archiveKey = lockedAgreement && lockedAgreement.UnsignedAgreementArchiveKeyByCustomerId ? lockedAgreement.UnsignedAgreementArchiveKeyByCustomerId[customerId] : null;
                                agreementUrl = _this.apiClient.getArchiveDocumentUrl(archiveKey);
                            }
                            else {
                                agreementUrl = _this.getLocalModuleUrl('api/MortgageLoan/Create-Dual-Agreement-Pdf', [['ApplicationNr', ai.ApplicationNr],
                                    ['CustomerId', customerId.toString()],
                                    ['DisableTemplateCache', _this.initialData.isTest ? 'True' : null]]);
                            }
                            m.customers.push({
                                customerId: customerId,
                                firstName: x.firstNameAndBirthDateByCustomerId[customerId]['firstName'],
                                birthDate: x.firstNameAndBirthDateByCustomerId[customerId]['birthDate'],
                                roleNames: x.rolesByCustomerId[customerId],
                                agreementUrl: agreementUrl
                            });
                        }
                        m.isCancelAllowed = ai.IsActive && ai.HasLockedAgreement && wf.areAllStepsAfterInitial(ai) && wf.isStatusAccepted(ai);
                        if (isAudit) {
                            m.isApproveAllowed = ai.IsActive && !ai.HasLockedAgreement && wf.isStatusInitial(ai);
                        }
                        else {
                            var isApproveAllowedBeforeUserCheck = ai.IsActive
                                && ai.HasLockedAgreement
                                && wf.isStatusInitial(ai);
                            m.isApproveAllowed = isApproveAllowedBeforeUserCheck && lockedAgreement.LockedByUserId !== _this.initialData.currentUserId;
                            m.showWaitingForApproval = isApproveAllowedBeforeUserCheck && !m.isApproveAllowed;
                            m.isApproved = wf.isStatusAccepted(ai);
                            if (isApproveAllowedBeforeUserCheck && _this.initialData.isTest) {
                                var tf = _this.initialData.testFunctions;
                                tf.addFunctionCall(tf.generateUniqueScopeName(), 'Force approve', function () {
                                    _this.approveInternal(true);
                                });
                            }
                        }
                        _this.m = m;
                    }
                    else {
                        //Show nothing
                    }
                });
            };
            var wf = this.initialData.workflowModel;
            if (ai.HasLockedAgreement) {
                this.apiClient.getLockedAgreement(ai.ApplicationNr).then(function (x) { return setup(x.LockedAgreement); });
            }
            else {
                setup(null);
            }
        };
        MortgageApplicationRawController.prototype.approveInternal = function (requestOverrideDuality, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var s = this.getStepCode();
            var applicationNr = this.initialData.applicationInfo.ApplicationNr;
            if (s === StepCode.Audit) {
                this.apiClient.auditAndCreateMortgageLoanLockedAgreement(applicationNr).then(function (x) {
                    _this.signalReloadRequired();
                });
            }
            else if (s === StepCode.Approve) {
                this.apiClient.approveLockedAgreement(applicationNr, requestOverrideDuality).then(function (x) {
                    if (x.WasApproved) {
                        _this.apiClient.setMortgageApplicationWorkflowStatus(applicationNr, _this.initialData.workflowModel.currentStep.Name, 'Accepted', 'Agreement approved').then(function (y) {
                            _this.signalReloadRequired();
                        });
                    }
                    else {
                        toastr.warning('Approve failed');
                    }
                });
            }
        };
        MortgageApplicationRawController.prototype.approve = function (evt) {
            this.approveInternal(false, evt);
        };
        MortgageApplicationRawController.prototype.cancel = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var ai = this.initialData.applicationInfo;
            var step = this.initialData.workflowModel;
            var s = this.getStepCode();
            if (s === StepCode.Audit) {
                this.apiClient.removeLockedAgreement(ai.ApplicationNr).then(function (x) {
                    _this.apiClient.setMortgageApplicationWorkflowStatus(ai.ApplicationNr, step.stepName, 'Initial', 'Audit agreement cancelled').then(function () {
                        _this.signalReloadRequired();
                    });
                });
            }
            else if (s === StepCode.Approve) {
                this.apiClient.setMortgageApplicationWorkflowStatus(ai.ApplicationNr, step.stepName, 'Initial', 'Approve agreement cancelled').then(function () {
                    _this.signalReloadRequired();
                });
            }
        };
        MortgageApplicationRawController.prototype.getUserDisplayNameByUserId = function (userId) {
            if (!userId) {
                return null;
            }
            if (this.initialData) {
                var d = this.initialData.userDisplayNameByUserId[userId];
                if (d) {
                    return d;
                }
            }
            return "User ".concat(userId);
        };
        MortgageApplicationRawController.prototype.getStepCode = function () {
            if (!this.initialData || !this.initialData.workflowModel) {
                return StepCode.Unknown;
            }
            var c = this.initialData.workflowModel.getCustomStepData();
            if (c && c.IsApproveAgreement == "yes") {
                return StepCode.Approve;
            }
            if (c && c.IsAuditAgreement) {
                return StepCode.Audit;
            }
            return StepCode.Unknown;
        };
        MortgageApplicationRawController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return MortgageApplicationRawController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanApplicationDualAuditApproveAgreementComponentNs.MortgageApplicationRawController = MortgageApplicationRawController;
    var StepCode;
    (function (StepCode) {
        StepCode["Unknown"] = "Unknown";
        StepCode["Audit"] = "Audit";
        StepCode["Approve"] = "Approve";
    })(StepCode = MortgageLoanApplicationDualAuditApproveAgreementComponentNs.StepCode || (MortgageLoanApplicationDualAuditApproveAgreementComponentNs.StepCode = {}));
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageLoanApplicationDualAuditApproveAgreementComponentNs.Model = Model;
    var CustomerModel = /** @class */ (function () {
        function CustomerModel() {
        }
        return CustomerModel;
    }());
    MortgageLoanApplicationDualAuditApproveAgreementComponentNs.CustomerModel = CustomerModel;
    var MortgageLoanApplicationDualInitialCreditCheckComponent = /** @class */ (function () {
        function MortgageLoanApplicationDualInitialCreditCheckComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationRawController;
            this.template = "<div class=\"container frame\" ng-if=\"$ctrl.m\">\n\n<div class=\"row pb-3\" ng-if=\"$ctrl.m.isCancelAllowed\">\n    <div class=\"text-right\">\n        <button class=\"n-main-btn n-white-btn\" ng-click=\"$ctrl.cancel($event)\">\n            Cancel\n        </button>\n    </div>\n</div>\n\n<div class=\"row pb-3\" ng-if=\"$ctrl.m.lockedAgreement\">\n    <div class=\"col-xs-6\">\n        <div class=\"form-horizontal\">\n            <div class=\"form-group\">\n                <label class=\"col-xs-6 control-label\">Audited by</label>\n                <div class=\"col-xs-6 form-control-static\">{{$ctrl.getUserDisplayNameByUserId($ctrl.m.lockedAgreement.LockedByUserId)}}</div>\n            </div>\n            <div class=\"form-group\">\n                <label class=\"col-xs-6 control-label\">Audited date</label>\n                <div class=\"col-xs-6 form-control-static\">{{$ctrl.m.lockedAgreement.LockedDate | date:'short'}}</div>\n            </div>\n        </div>\n    </div>\n    <div class=\"col-xs-6\" ng-if=\"$ctrl.m.isApproveStep\">\n        <div class=\"form-horizontal\">\n            <div class=\"form-group\">\n                <label class=\"col-xs-6 control-label\">Approved by</label>\n                <div class=\"col-xs-6 form-control-static\"><span ng-if=\"$ctrl.m.isApproved\">{{$ctrl.getUserDisplayNameByUserId($ctrl.m.lockedAgreement.ApprovedByUserId)}}</span></div>\n            </div>\n            <div class=\"form-group\">\n                <label class=\"col-xs-6 control-label\">Approved date</label>\n                <div class=\"col-xs-6 form-control-static\"><span ng-if=\"$ctrl.m.isApproved\">{{$ctrl.m.lockedAgreement.ApprovedDate | date:'short'}}</span></div>\n            </div>\n        </div>\n    </div>\n</div>\n\n<div class=\"row\" ng-if=\"$ctrl.m.customers\">\n    <table class=\"table col-sm-12\">\n        <tbody>\n            <tr ng-repeat=\"c in $ctrl.m.customers\">\n                <td class=\"col-xs-9\">{{c.firstName}}, {{c.birthDate}} (<span ng-repeat=\"r in c.roleNames\" class=\"comma\">{{r}}</span>)</td>\n                <td class=\"col-xs-3 text-right\">\n                    <a ng-if=\"c.agreementUrl\" ng-href=\"{{c.agreementUrl}}\" target=\"_blank\" class=\"n-direct-btn n-purple-btn\">File <span class=\"glyphicon glyphicon-save\"></span></a>\n                    <span ng-if=\"!c.agreementUrl\">Missing</span>\n                </td>\n            </tr>\n        </tbody>\n    </table>\n</div>\n\n<div class=\"row\" ng-if=\"$ctrl.m.isApproveAllowed\">\n    <div class=\"text-center pt-3\">\n        <button class=\"n-main-btn n-green-btn\" ng-click=\"$ctrl.approve($event)\">\n            Approve\n        </button>\n    </div>\n</div>\n\n<div class=\"row\" ng-if=\"$ctrl.m.showWaitingForApproval\">\n    <div class=\"text-center pt-3\">\n        <span style=\"font-weight:bold;font-style:italic\">Waiting for approval</span>\n    </div>\n</div>\n\n</div>";
        }
        return MortgageLoanApplicationDualInitialCreditCheckComponent;
    }());
    MortgageLoanApplicationDualAuditApproveAgreementComponentNs.MortgageLoanApplicationDualInitialCreditCheckComponent = MortgageLoanApplicationDualInitialCreditCheckComponent;
})(MortgageLoanApplicationDualAuditApproveAgreementComponentNs || (MortgageLoanApplicationDualAuditApproveAgreementComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanApplicationDualAuditApproveAgreement', new MortgageLoanApplicationDualAuditApproveAgreementComponentNs.MortgageLoanApplicationDualInitialCreditCheckComponent());
