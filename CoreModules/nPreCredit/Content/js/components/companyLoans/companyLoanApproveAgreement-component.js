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
var CompanyLoanApproveAgreementComponentNs;
(function (CompanyLoanApproveAgreementComponentNs) {
    var CompanyLoanApproveAgreementController = /** @class */ (function (_super) {
        __extends(CompanyLoanApproveAgreementController, _super);
        function CompanyLoanApproveAgreementController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.modalDialogService = modalDialogService;
            return _this;
        }
        CompanyLoanApproveAgreementController.prototype.componentName = function () {
            return 'companyLoanApproveAgreement';
        };
        CompanyLoanApproveAgreementController.prototype.approve = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.approveInternal(false);
        };
        CompanyLoanApproveAgreementController.prototype.cancel = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.initialData.companyLoanApiClient.cancelAgreementSignatureSession(this.initialData.applicationInfo.ApplicationNr).then(function (x) {
                var msg = 'Agreement approval reversed';
                if (x.WasCancelled) {
                    msg += ' and pending signature session cancelled';
                }
                _this.initialData.companyLoanApiClient.setApplicationWorkflowStatus(_this.initialData.applicationInfo.ApplicationNr, _this.initialData.step.stepName, 'Initial', msg).then(function () {
                    _this.signalReloadRequired();
                });
            });
        };
        CompanyLoanApproveAgreementController.prototype.approveInternal = function (requestOverrideDuality) {
            var _this = this;
            this.apiClient.approveLockedAgreement(this.initialData.applicationInfo.ApplicationNr, requestOverrideDuality).then(function (x) {
                if (x.WasApproved) {
                    _this.initialData.companyLoanApiClient.setApplicationWorkflowStatus(_this.initialData.applicationInfo.ApplicationNr, _this.initialData.step.stepName, 'Accepted', 'Agreement approved').then(function () {
                        _this.initialData.companyLoanApiClient.getOrCreateAgreementSignatureSession(_this.initialData.applicationInfo.ApplicationNr).then(function (x) {
                            _this.signalReloadRequired();
                        });
                    });
                }
                else {
                    toastr.warning('Agreement could not be approved');
                }
            });
        };
        CompanyLoanApproveAgreementController.prototype.getUserDisplayNameByUserId = function (userId) {
            if (this.initialData) {
                var d = this.initialData.userDisplayNameByUserId[userId];
                if (d) {
                    return d;
                }
            }
            return "User ".concat(userId);
        };
        CompanyLoanApproveAgreementController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            var s = this.initialData.step;
            var ai = this.initialData.applicationInfo;
            if (!s.areAllStepBeforeThisAccepted(ai)) {
                this.m = {
                    approved: null,
                    pendingApproval: null
                };
            }
            else if (s.isStatusAccepted(ai)) {
                this.apiClient.getLockedAgreement(ai.ApplicationNr).then(function (x) {
                    var ai = _this.initialData.applicationInfo;
                    var s = _this.initialData.step;
                    _this.m = {
                        approved: x && x.LockedAgreement ? {
                            agreementUrl: "/CreditManagement/ArchiveDocument?key=".concat(x.LockedAgreement.UnsignedAgreementArchiveKey),
                            auditedByUserId: x.LockedAgreement.LockedByUserId,
                            auditedDate: x.LockedAgreement.LockedDate,
                            loanAmount: x.LockedAgreement.LoanAmount,
                            isCancelAllowed: ai.IsActive && s.isStatusAccepted(ai) && s.areAllStepsAfterInitial(ai),
                            approvedByUserId: x.LockedAgreement.ApprovedByUserId,
                            approvedDate: x.LockedAgreement.ApprovedDate
                        } : null,
                        pendingApproval: null
                    };
                    if (_this.initialData.isTest) {
                        var tf = _this.initialData.testFunctions;
                        tf.addFunctionCall(tf.generateUniqueScopeName(), 'Force approve', function () {
                            _this.approveInternal(true);
                        });
                    }
                });
            }
            else {
                this.apiClient.getLockedAgreement(ai.ApplicationNr).then(function (x) {
                    _this.initialData.companyLoanApiClient.checkHandlerLimits(_this.initialData.currentUserId, x.LockedAgreement.LoanAmount).then(function (approvedHandlerLimits) {
                        var ai = _this.initialData.applicationInfo;
                        var s = _this.initialData.step;
                        var isApprovePossible = ai.IsActive && s.areAllStepBeforeThisAccepted(ai);
                        _this.m = {
                            pendingApproval: x && x.LockedAgreement ? {
                                agreementUrl: "/CreditManagement/ArchiveDocument?key=".concat(x.LockedAgreement.UnsignedAgreementArchiveKey),
                                auditedByUserId: x.LockedAgreement.LockedByUserId,
                                auditedDate: x.LockedAgreement.LockedDate,
                                loanAmount: x.LockedAgreement.LoanAmount,
                                isApprovePossible: isApprovePossible,
                                isApproveAllowed: isApprovePossible && x.LockedAgreement.LockedByUserId !== _this.initialData.currentUserId,
                                isLoanAmountToLimitApproved: isApprovePossible && x.LockedAgreement.LockedByUserId !== _this.initialData.currentUserId && approvedHandlerLimits.Approved,
                                showWaitingForApproval: isApprovePossible && x.LockedAgreement.LockedByUserId == _this.initialData.currentUserId,
                            } : null,
                            approved: null
                        };
                        if (_this.initialData.isTest) {
                            var tf = _this.initialData.testFunctions;
                            tf.addFunctionCall(tf.generateUniqueScopeName(), 'Force approve', function () {
                                _this.approveInternal(true);
                            });
                        }
                    });
                });
            }
        };
        CompanyLoanApproveAgreementController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return CompanyLoanApproveAgreementController;
    }(NTechComponents.NTechComponentControllerBase));
    CompanyLoanApproveAgreementComponentNs.CompanyLoanApproveAgreementController = CompanyLoanApproveAgreementController;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    CompanyLoanApproveAgreementComponentNs.Model = Model;
    var PendingApprovalModel = /** @class */ (function () {
        function PendingApprovalModel() {
        }
        return PendingApprovalModel;
    }());
    CompanyLoanApproveAgreementComponentNs.PendingApprovalModel = PendingApprovalModel;
    var ApprovedModel = /** @class */ (function () {
        function ApprovedModel() {
        }
        return ApprovedModel;
    }());
    CompanyLoanApproveAgreementComponentNs.ApprovedModel = ApprovedModel;
    var CompanyLoanApproveAgreementComponent = /** @class */ (function () {
        function CompanyLoanApproveAgreementComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanApproveAgreementController;
            this.templateUrl = 'company-loan-approve-agreement.html';
        }
        return CompanyLoanApproveAgreementComponent;
    }());
    CompanyLoanApproveAgreementComponentNs.CompanyLoanApproveAgreementComponent = CompanyLoanApproveAgreementComponent;
})(CompanyLoanApproveAgreementComponentNs || (CompanyLoanApproveAgreementComponentNs = {}));
angular.module('ntech.components').component('companyLoanApproveAgreement', new CompanyLoanApproveAgreementComponentNs.CompanyLoanApproveAgreementComponent());
