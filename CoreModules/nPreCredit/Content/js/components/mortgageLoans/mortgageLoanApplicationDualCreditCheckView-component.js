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
var MortgageLoanApplicationDualCreditCheckViewComponentNs;
(function (MortgageLoanApplicationDualCreditCheckViewComponentNs) {
    var MortgageLoanApplicationDualInitialCreditCheckViewController = /** @class */ (function (_super) {
        __extends(MortgageLoanApplicationDualInitialCreditCheckViewController, _super);
        function MortgageLoanApplicationDualInitialCreditCheckViewController($http, $q, ntechComponentService, $scope) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.$scope = $scope;
            return _this;
        }
        MortgageLoanApplicationDualInitialCreditCheckViewController.prototype.componentName = function () {
            return 'mortgageLoanApplicationDualCreditCheckView';
        };
        MortgageLoanApplicationDualInitialCreditCheckViewController.prototype.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.handleBack(NavigationTargetHelper.createCodeTarget(NavigationTargetHelper.NavigationTargetCode.MortgageLoanApplication), this.apiClient, this.$q, {
                applicationNr: this.initialData.applicationNr
            });
        };
        MortgageLoanApplicationDualInitialCreditCheckViewController.prototype.getCustomWorkflowData = function () {
            var scoringStepModel = new WorkflowHelper.WorkflowStepModel(this.initialData.workflowModel, this.initialData.scoringWorkflowStepName);
            return scoringStepModel.getCustomStepData();
        };
        MortgageLoanApplicationDualInitialCreditCheckViewController.prototype.getRejectionReasonDisplayName = function (rejectionReasonToDisplayNameMapping, reasonName) {
            var r = '';
            if (rejectionReasonToDisplayNameMapping) {
                r = rejectionReasonToDisplayNameMapping[reasonName];
            }
            if (!r) {
                r = reasonName;
            }
            return r;
        };
        MortgageLoanApplicationDualInitialCreditCheckViewController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            var scoringStepModel = new WorkflowHelper.WorkflowStepModel(this.initialData.workflowModel, this.initialData.scoringWorkflowStepName);
            var cwf = scoringStepModel.getCustomStepData();
            this.apiClient.fetchItemBasedCreditDecision({ ApplicationNr: this.initialData.applicationNr, OnlyDecisionType: cwf.DecisionType, MaxCount: 1, MustBeCurrent: false, IncludeRejectionReasonToDisplayNameMapping: true }).then(function (x) {
                _this.apiClient.fetchApplicationInfo(_this.initialData.applicationNr).then(function (ai) {
                    MortgageLoanApplicationDualCreditCheckSharedNs.getLtvBasisAndLoanListNrs(_this, ai.ApplicationNr, _this.apiClient).then(function (d) {
                        var cwf = _this.getCustomWorkflowData();
                        var isFinal = cwf.IsFinal === 'yes';
                        var backTarget = NavigationTargetHelper.createCodeTarget(isFinal ? NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreditCheckViewFinal : NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreditCheckViewInitial);
                        var decision = x.Decisions && x.Decisions.length > 0 ? x.Decisions[0] : null;
                        var hasMainLoan = !decision.IsAccepted ? false : decision.UniqueItems['mainHasLoan'] === 'true';
                        var hasChildLoan = !decision.IsAccepted ? false : decision.UniqueItems['childHasLoan'] === 'true';
                        var rejectionReasons = '';
                        if (!decision.IsAccepted) {
                            for (var _i = 0, _a = decision.RepeatingItems['rejectionReason']; _i < _a.length; _i++) {
                                var rejectionReason = _a[_i];
                                if (rejectionReasons.length > 0) {
                                    rejectionReasons += ', ';
                                }
                                rejectionReasons += _this.getRejectionReasonDisplayName(x.RejectionReasonToDisplayNameMapping, rejectionReason);
                            }
                        }
                        var calcMonthlyAmount = function (isMain) {
                            return _this.formatNumberForStorage(_this.parseDecimalOrNull(decision.UniqueItems["".concat(isMain ? 'main' : 'child', "AnnuityAmount")]) + _this.parseDecimalOrNull(decision.UniqueItems["".concat(isMain ? 'main' : 'child', "NotificationFeeAmount")]));
                        };
                        MortgageLoanApplicationDualCreditCheckSharedNs.getApplicantDataByApplicantNr(ai.ApplicationNr, ai.NrOfApplicants > 1, _this.apiClient).then(function (applicantDataByApplicantNr) {
                            _this.m = {
                                isFinal: isFinal,
                                headerClass: { 'text-success': decision.IsAccepted, 'text-danger': !decision.IsAccepted },
                                iconClass: { 'glyphicon-ok': decision.IsAccepted, 'glyphicon-remove': !decision.IsAccepted, 'text-success': decision.IsAccepted, 'text-danger': !decision.IsAccepted },
                                decision: decision,
                                b: MortgageLoanApplicationDualCreditCheckSharedNs.createDecisionBasisModel(true, _this, _this.apiClient, _this.$q, ai, ai.NrOfApplicants > 1, false, backTarget, d.mortgageLoanNrs, d.otherLoanNrs, isFinal, applicantDataByApplicantNr),
                                rejectedCommon: !decision.IsAccepted ? new TwoColumnInformationBlockComponentNs.InitialData()
                                    .item(true, rejectionReasons, null, 'Reasons') : null,
                                acceptedCommon: (hasMainLoan || hasChildLoan) ? new TwoColumnInformationBlockComponentNs.InitialData()
                                    .item(true, decision.UniqueItems['applicationType'], null, 'Type') : null,
                                acceptedMainLoan: hasMainLoan ? {
                                    d1: new TwoColumnInformationBlockComponentNs.InitialData()
                                        .item(true, decision.UniqueItems['mainInitialFeeAmount'], null, 'Initial fee', 'currency')
                                        .item(true, decision.UniqueItems['mainNotificationFeeAmount'], null, 'Notification fee', 'currency')
                                        .item(true, decision.UniqueItems['mainValuationFeeAmount'], null, 'Valuation fee', 'currency')
                                        .item(true, decision.UniqueItems['mainDeedFeeAmount'], null, 'Deed fee', 'currency')
                                        .item(true, decision.UniqueItems['mainMortgageApplicationFeeAmount'], null, 'Mortgage app. fee', 'currency')
                                        .item(false, decision.UniqueItems['mainPurchaseAmount'], null, 'Purchase amount', 'currency')
                                        .item(false, decision.UniqueItems['mainDirectToCustomerAmount'], null, 'Payment to customer', 'currency')
                                        .item(false, decision.UniqueItems['mainTotalSettlementAmount'], null, 'Total settlement amount', 'currency')
                                        .item(false, decision.UniqueItems['mainLoanAmount'], null, 'Total amount', 'currency'),
                                    d2: new TwoColumnInformationBlockComponentNs.InitialData()
                                        .item(true, decision.UniqueItems['mainMarginInterestRatePercent'], null, 'Margin interest rate', 'percent', 3)
                                        .item(true, decision.UniqueItems['mainReferenceInterestRatePercent'], null, 'Reference interest rate', 'percent', 3)
                                        .item(true, _this.formatNumberForStorage(_this.parseDecimalOrNull(decision.UniqueItems['mainMarginInterestRatePercent']) + _this.parseDecimalOrNull(decision.UniqueItems['mainReferenceInterestRatePercent'])), null, 'Total interest rate', 'percent', 3)
                                        .item(true, decision.UniqueItems['mainRepaymentTimeInMonths'], null, 'Repayment time', 'number', 3)
                                        .item(true, decision.UniqueItems['mainEffectiveInterestRatePercent'], null, 'Eff. interest rate', 'percent', 3)
                                        .item(true, calcMonthlyAmount(true), null, 'Monthly amount', 'currency', 3)
                                } : null,
                                acceptedChildLoan: hasChildLoan ? {
                                    d1: new TwoColumnInformationBlockComponentNs.InitialData()
                                        .item(true, decision.UniqueItems['childInitialFeeAmount'], null, 'Initial fee', 'currency')
                                        .item(true, decision.UniqueItems['childNotificationFeeAmount'], null, 'Notification fee', 'currency')
                                        .item(false, decision.UniqueItems['childDirectToCustomerAmount'], null, 'Payment to customer', 'currency')
                                        .item(false, decision.UniqueItems['childTotalSettlementAmount'], null, 'Total settlement amount', 'currency')
                                        .item(false, decision.UniqueItems['childLoanAmount'], null, 'Total amount', 'currency'),
                                    d2: new TwoColumnInformationBlockComponentNs.InitialData()
                                        .item(true, decision.UniqueItems['childMarginInterestRatePercent'], null, 'Margin interest rate', 'percent', 3)
                                        .item(true, decision.UniqueItems['childReferenceInterestRatePercent'], null, 'Reference interest rate', 'percent', 3)
                                        .item(true, _this.formatNumberForStorage(_this.parseDecimalOrNull(decision.UniqueItems['childMarginInterestRatePercent']) + _this.parseDecimalOrNull(decision.UniqueItems['childReferenceInterestRatePercent'])), null, 'Total interest rate', 'percent', 3)
                                        .item(true, decision.UniqueItems['childRepaymentTimeInMonths'], null, 'Repayment time', 'number', 3)
                                        .item(true, decision.UniqueItems['childEffectiveInterestRatePercent'], null, 'Eff. interest rate', 'percent', 3)
                                        .item(true, calcMonthlyAmount(false), null, 'Monthly amount', 'currency', 3)
                                } : null
                            };
                        });
                    });
                });
            });
        };
        MortgageLoanApplicationDualInitialCreditCheckViewController.$inject = ['$http', '$q', 'ntechComponentService', '$scope', '$timeout'];
        return MortgageLoanApplicationDualInitialCreditCheckViewController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanApplicationDualCreditCheckViewComponentNs.MortgageLoanApplicationDualInitialCreditCheckViewController = MortgageLoanApplicationDualInitialCreditCheckViewController;
    var MortgageLoanApplicationDualInitialCreditCheckViewComponent = /** @class */ (function () {
        function MortgageLoanApplicationDualInitialCreditCheckViewComponent() {
            this.decisionBasisTemplate = MortgageLoanApplicationDualCreditCheckSharedNs.getDecisionBasisHtmlTemplate(false);
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationDualInitialCreditCheckViewController;
            this.template = "<div ng-if=\"$ctrl.m\">\n\n        <div class=\"pt-1 pb-2\">\n            <div class=\"pull-left\">\n                <a class=\"n-back\" href=\"#\" ng-click=\"$ctrl.onBack($event)\">\n                    <span class=\"glyphicon glyphicon-arrow-left\"></span>\n                </a>\n            </div>\n            <h1 class=\"adjusted\" ng-class=\"$ctrl.m.headerClass\">{{$ctrl.m.isFinal ? 'Final' : 'Initial'}} credit decision <span ng-class=\"$ctrl.m.iconClass\" style=\"font-size:20px; margin-left: 5px;\" class=\"glyphicon\"></span></h1>\n        </div>\n\n        <div class=\"row pb-3 pt-3\" ng-if=\"$ctrl.m.rejectedCommon\">\n            <two-column-information-block class=\"col-sm-8 col-sm-offset-3\" initial-data=\"$ctrl.m.rejectedCommon\"></two-column-information-block>\n        </div>\n\n        <div class=\"row\" ng-if=\"$ctrl.m.acceptedCommon\">\n            <two-column-information-block class=\"col-sm-8\" initial-data=\"$ctrl.m.acceptedCommon\"></two-column-information-block>\n        </div>\n        <div ng-if=\"$ctrl.m.acceptedCommon\">\n            <hr class=\"hr-section dotted\">\n        </div>\n\n        <div ng-if=\"$ctrl.m.acceptedMainLoan\">\n            <h3 class=\"text-center\">Mortgage loan</h3>\n        </div>\n        <div class=\"row\" ng-if=\"$ctrl.m.acceptedMainLoan\">\n            <two-column-information-block class=\"col-sm-8\" initial-data=\"$ctrl.m.acceptedMainLoan.d1\"></two-column-information-block>\n            <two-column-information-block class=\"col-sm-4\" initial-data=\"$ctrl.m.acceptedMainLoan.d2\"></two-column-information-block>\n        </div>\n        <div ng-if=\"$ctrl.m.acceptedMainLoan\">\n            <hr class=\"hr-section dotted\">\n        </div>\n\n        <div ng-if=\"$ctrl.m.acceptedChildLoan\">\n            <h3 class=\"text-center\">Other loan</h3>\n        </div>\n        <div class=\"row\" ng-if=\"$ctrl.m.acceptedChildLoan\">\n            <two-column-information-block class=\"col-sm-8\" initial-data=\"$ctrl.m.acceptedChildLoan.dt\"></two-column-information-block>\n        </div>\n        <div class=\"row\" ng-if=\"$ctrl.m.acceptedChildLoan\">\n            <two-column-information-block class=\"col-sm-8\" initial-data=\"$ctrl.m.acceptedChildLoan.d1\"></two-column-information-block>\n            <two-column-information-block class=\"col-sm-4\" initial-data=\"$ctrl.m.acceptedChildLoan.d2\"></two-column-information-block>\n        </div>\n        <div ng-if=\"$ctrl.m.acceptedChildLoan\">\n            <hr class=\"hr-section dotted\">\n        </div>\n\n        <div class=\"row pb-3\" ng-if=\"$ctrl.m.acceptedMainLoan || $ctrl.m.acceptedChildLoan\">\n\n        </div>\n        ".concat(this.decisionBasisTemplate, "\n</div>");
        }
        return MortgageLoanApplicationDualInitialCreditCheckViewComponent;
    }());
    MortgageLoanApplicationDualCreditCheckViewComponentNs.MortgageLoanApplicationDualInitialCreditCheckViewComponent = MortgageLoanApplicationDualInitialCreditCheckViewComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageLoanApplicationDualCreditCheckViewComponentNs.Model = Model;
})(MortgageLoanApplicationDualCreditCheckViewComponentNs || (MortgageLoanApplicationDualCreditCheckViewComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanApplicationDualCreditCheckView', new MortgageLoanApplicationDualCreditCheckViewComponentNs.MortgageLoanApplicationDualInitialCreditCheckViewComponent());
