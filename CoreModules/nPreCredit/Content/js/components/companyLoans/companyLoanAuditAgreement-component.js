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
var CompanyLoanAuditAgreementComponentNs;
(function (CompanyLoanAuditAgreementComponentNs) {
    var CompanyLoanAuditAgreementController = /** @class */ (function (_super) {
        __extends(CompanyLoanAuditAgreementController, _super);
        function CompanyLoanAuditAgreementController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.modalDialogService = modalDialogService;
            return _this;
        }
        CompanyLoanAuditAgreementController.prototype.componentName = function () {
            return 'companyLoanAuditAgreement';
        };
        CompanyLoanAuditAgreementController.prototype.getUserDisplayNameByUserId = function (userId) {
            if (this.initialData) {
                var d = this.initialData.userDisplayNameByUserId[userId];
                if (d) {
                    return d;
                }
            }
            return "User ".concat(userId);
        };
        CompanyLoanAuditAgreementController.prototype.cancel = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var ai = this.initialData.applicationInfo;
            var step = this.initialData.step;
            this.initialData.apiClient.removeLockedAgreement(ai.ApplicationNr).then(function (x) {
                _this.initialData.companyLoanApiClient.setApplicationWorkflowStatus(ai.ApplicationNr, step.stepName, 'Initial', 'Agreement unlocked').then(function () {
                    _this.signalReloadRequired();
                });
            });
        };
        CompanyLoanAuditAgreementController.prototype.approve = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var ai = this.initialData.applicationInfo;
            var step = this.initialData.step;
            this.initialData.companyLoanApiClient.createLockedAgreement(ai.ApplicationNr).then(function (x) {
                _this.initialData.companyLoanApiClient.setApplicationWorkflowStatus(ai.ApplicationNr, step.stepName, 'Accepted', 'Agreement audited and locked').then(function () {
                    _this.signalReloadRequired();
                });
            });
        };
        CompanyLoanAuditAgreementController.prototype.initLocked = function (a) {
            var ai = this.initialData.applicationInfo;
            var s = this.initialData.step;
            this.m = {
                preview: null,
                lockedAgreement: a && a.LockedAgreement ? {
                    agreementUrl: "/CreditManagement/ArchiveDocument?key=".concat(a.LockedAgreement.UnsignedAgreementArchiveKey),
                    auditedByUserId: a.LockedAgreement.LockedByUserId,
                    auditedDate: a.LockedAgreement.LockedDate,
                    loanAmount: a.LockedAgreement.LoanAmount,
                    isCancelAllowed: s.isStatusAccepted(ai) && s.areAllStepsAfterInitial(ai) && ai.IsActive
                } : null
            };
        };
        CompanyLoanAuditAgreementController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            var s = this.initialData.step;
            var ai = this.initialData.applicationInfo;
            if (!s.areAllStepBeforeThisAccepted(ai)) {
                this.m = {
                    lockedAgreement: null,
                    preview: null
                };
            }
            else if (s.isStatusAccepted(ai)) {
                this.apiClient.getLockedAgreement(ai.ApplicationNr).then(function (x) {
                    _this.initLocked(x);
                });
            }
            else {
                this.initialData.companyLoanApiClient.fetchCurrentCreditDecision(ai.ApplicationNr).then(function (decision) {
                    _this.initialData.companyLoanApiClient.checkHandlerLimits(_this.initialData.currentUserId, decision.Decision.CompanyLoanOffer.LoanAmount).then(function (approvedHandlerLimits) {
                        _this.m = {
                            preview: {
                                loanAmount: decision && decision.Decision && decision.Decision.CompanyLoanOffer ? decision.Decision.CompanyLoanOffer.LoanAmount : null,
                                agreementUrl: "/api/CompanyLoan/Create-Agreement-Pdf?ApplicationNr=".concat(_this.initialData.applicationInfo.ApplicationNr),
                                isApprovedAllowd: ai.IsActive && s.areAllStepBeforeThisAccepted(ai) && approvedHandlerLimits.Approved
                            },
                            lockedAgreement: null
                        };
                    });
                });
            }
        };
        CompanyLoanAuditAgreementController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return CompanyLoanAuditAgreementController;
    }(NTechComponents.NTechComponentControllerBase));
    CompanyLoanAuditAgreementComponentNs.CompanyLoanAuditAgreementController = CompanyLoanAuditAgreementController;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    CompanyLoanAuditAgreementComponentNs.Model = Model;
    var PreviewModel = /** @class */ (function () {
        function PreviewModel() {
        }
        return PreviewModel;
    }());
    CompanyLoanAuditAgreementComponentNs.PreviewModel = PreviewModel;
    var LockedAgreementModel = /** @class */ (function () {
        function LockedAgreementModel() {
        }
        return LockedAgreementModel;
    }());
    CompanyLoanAuditAgreementComponentNs.LockedAgreementModel = LockedAgreementModel;
    var CompanyLoanAuditAgreementComponent = /** @class */ (function () {
        function CompanyLoanAuditAgreementComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanAuditAgreementController;
            this.templateUrl = 'company-loan-audit-agreement.html';
        }
        return CompanyLoanAuditAgreementComponent;
    }());
    CompanyLoanAuditAgreementComponentNs.CompanyLoanAuditAgreementComponent = CompanyLoanAuditAgreementComponent;
})(CompanyLoanAuditAgreementComponentNs || (CompanyLoanAuditAgreementComponentNs = {}));
angular.module('ntech.components').component('companyLoanAuditAgreement', new CompanyLoanAuditAgreementComponentNs.CompanyLoanAuditAgreementComponent());
