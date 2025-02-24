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
var MortgageLoanApplicationDualCreditCheckComponentNs;
(function (MortgageLoanApplicationDualCreditCheckComponentNs) {
    var MortgageApplicationRawController = /** @class */ (function (_super) {
        __extends(MortgageApplicationRawController, _super);
        function MortgageApplicationRawController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.modalDialogService = modalDialogService;
            return _this;
        }
        MortgageApplicationRawController.prototype.componentName = function () {
            return 'mortgageLoanApplicationDualCreditCheck';
        };
        MortgageApplicationRawController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData || !this.initialData.applicationInfo) {
                return;
            }
            var ai = this.initialData.applicationInfo;
            var wfc = this.initialData.workflowModel.getCustomStepData();
            var setup = function (d, rr, doesDecisionHistoryAllowNewCreditCheck, applicationtypeName) {
                _this.m = {
                    decision: d,
                    rejectionReasonToDisplayNameMapping: rr,
                    customWorkflowStepData: wfc,
                    doesDecisionHistoryAllowNewCreditCheck: doesDecisionHistoryAllowNewCreditCheck,
                    applicationtypeName: applicationtypeName
                };
            };
            var r = {
                DataSourceName: 'CreditApplicationItem',
                ErrorIfMissing: false,
                IncludeEditorModel: true,
                IncludeIsChanged: false,
                MissingItemReplacementValue: ApplicationDataSourceHelper.MissingItemReplacementValue,
                Names: ['application.mortgageLoanApplicationType'],
                ReplaceIfMissing: true
            };
            this.apiClient.fetchItemBasedCreditDecision({ ApplicationNr: ai.ApplicationNr, OnlyDecisionType: wfc.DecisionType, MaxCount: 1, MustBeCurrent: false, IncludeRejectionReasonToDisplayNameMapping: true }).then(function (x) {
                _this.apiClient.fetchApplicationDataSourceItems(_this.initialData.applicationInfo.ApplicationNr, [r]).then(function (y) {
                    var applicationtypename = '';
                    for (var i = 0; i < y.Results[0].Items[0].EditorModel.DropdownRawDisplayTexts.length; i++) {
                        if (x.Decisions.length > 0 && y.Results[0].Items[0].EditorModel.DropdownRawOptions[i] == x.Decisions[0].UniqueItems.applicationType)
                            applicationtypename = y.Results[0].Items[0].EditorModel.DropdownRawDisplayTexts[i];
                    }
                    var isFinal = wfc.IsFinal === 'yes';
                    var doesDecisionHistoryAllowNewCreditCheck;
                    if (x.Decisions && x.Decisions.length > 0) {
                        var d = x.Decisions[0];
                        doesDecisionHistoryAllowNewCreditCheck = !d.ExistsLaterDecisionOfDifferentType //Later decisons must be remove first to repeat previous
                            && (!isFinal || d.ExistsEarlierDecisionOfDifferentType); //Final has the additional demand of requiring at least one earlier
                    }
                    else {
                        doesDecisionHistoryAllowNewCreditCheck = !isFinal || (isFinal && x.ExistsEarlierDecisionOfDifferentType === true);
                    }
                    if (!_this.initialData.workflowModel.isStatusInitial(ai)) {
                        if (x.Decisions && x.Decisions.length > 0) {
                            setup(x.Decisions[0], x.RejectionReasonToDisplayNameMapping, doesDecisionHistoryAllowNewCreditCheck, applicationtypename);
                        }
                        else {
                            setup(null, x.RejectionReasonToDisplayNameMapping, doesDecisionHistoryAllowNewCreditCheck, applicationtypename);
                        }
                    }
                    else {
                        setup(null, x.RejectionReasonToDisplayNameMapping, doesDecisionHistoryAllowNewCreditCheck, applicationtypename);
                    }
                });
            });
        };
        MortgageApplicationRawController.prototype.isNewCreditCheckPossibleButNeedsReactivate = function () {
            if (!this.initialData || !this.initialData.applicationInfo) {
                return false;
            }
            var a = this.initialData.applicationInfo;
            return !a.IsActive && !a.IsFinalDecisionMade && (a.IsCancelled || a.IsRejected)
                && !a.HasLockedAgreement
                && this.m.doesDecisionHistoryAllowNewCreditCheck;
        };
        MortgageApplicationRawController.prototype.isNewCreditCheckPossible = function () {
            if (!this.initialData || !this.initialData.applicationInfo) {
                return false;
            }
            var appInfo = this.initialData.applicationInfo;
            var isActiveAndOk = appInfo.IsActive
                && !appInfo.HasLockedAgreement
                && this.m.doesDecisionHistoryAllowNewCreditCheck
                && this.initialData.workflowModel.areAllStepBeforeThisAccepted(appInfo);
            return isActiveAndOk || this.isNewCreditCheckPossibleButNeedsReactivate();
        };
        MortgageApplicationRawController.prototype.isViewCreditCheckDetailsPossible = function () {
            if (!this.initialData) {
                return false;
            }
            return !this.initialData.workflowModel.isStatusInitial(this.initialData.applicationInfo);
        };
        MortgageApplicationRawController.prototype.a = function (name) {
            if (!this.m || !this.m.decision || !this.m.decision.IsAccepted) {
                return null;
            }
            return this.m.decision.UniqueItems[name];
        };
        MortgageApplicationRawController.prototype.an = function (name) {
            return this.parseDecimalOrNull(this.a(name));
        };
        MortgageApplicationRawController.prototype.newCreditCheck = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.isNewCreditCheckPossible()) {
                return;
            }
            var doCheck = function () {
                var url = _this.getLocalModuleUrl('Ui/MortgageLoan/NewCreditCheck', [
                    ['applicationNr', _this.initialData.applicationInfo.ApplicationNr],
                    ['scoringWorkflowStepName', _this.initialData.workflowModel.currentStep.Name]
                ]);
                location.href = url;
            };
            if (this.isNewCreditCheckPossibleButNeedsReactivate()) {
                this.apiClient.reactivateCancelledApplication(this.initialData.applicationInfo.ApplicationNr).then(function () {
                    doCheck();
                });
            }
            else {
                doCheck();
            }
        };
        MortgageApplicationRawController.prototype.viewCreditCheckDetails = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (!this.isViewCreditCheckDetailsPossible()) {
                return;
            }
            var url = this.getLocalModuleUrl('Ui/MortgageLoan/ViewCreditCheckDetails', [
                ['applicationNr', this.initialData.applicationInfo.ApplicationNr],
                ['scoringWorkflowStepName', this.initialData.workflowModel.currentStep.Name]
            ]);
            location.href = url;
        };
        MortgageApplicationRawController.prototype.getRejectionReasonDisplayName = function (reasonName) {
            var r = '';
            if (this.m && this.m.rejectionReasonToDisplayNameMapping) {
                r = this.m.rejectionReasonToDisplayNameMapping[reasonName];
            }
            if (!r) {
                r = reasonName;
            }
            return r;
        };
        MortgageApplicationRawController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return MortgageApplicationRawController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanApplicationDualCreditCheckComponentNs.MortgageApplicationRawController = MortgageApplicationRawController;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageLoanApplicationDualCreditCheckComponentNs.Model = Model;
    var MortgageLoanApplicationDualInitialCreditCheckComponent = /** @class */ (function () {
        function MortgageLoanApplicationDualInitialCreditCheckComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationRawController;
            this.template = "<div ng-if=\"$ctrl.m\">\n        <div class=\"text-right\" ng-if=\"$ctrl.isViewCreditCheckDetailsPossible()\">\n            <a class=\"n-anchor\" ng-click=\"$ctrl.viewCreditCheckDetails($event)\">View details</a>\n        </div>\n        <div ng-if=\"$ctrl.m.decision\">\n            <div ng-if=\"!$ctrl.m.decision.IsAccepted\" class=\"form-horizontal pb-3\">\n                <div class=\"form-group\">\n                    <label class=\"col-xs-3 control-label\">Reasons</label>\n                    <div class=\"col-xs-9 form-control-static\">\n                        <span ng-repeat=\"r in $ctrl.m.decision.RepeatingItems['rejectionReason']\"><span ng-hide=\"$first\">, </span>{{$ctrl.getRejectionReasonDisplayName(r)}}</span>\n                    </div>\n                </div>\n            </div>\n            <div ng-if=\"$ctrl.m.decision.IsAccepted\" class=\"pb-3\">\n                <h3>{{$ctrl.m.applicationtypeName}}</h3>\n                <h3>Mortgage loan</h3>\n                <div class=\"row pb-2\">\n                    <div class=\"col-xs-6\">\n                        <div class=\"form-horizontal\">\n                            <div class=\"form-group\">\n                                <label class=\"col-xs-6 control-label\">Loan amount</label>\n                                <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('mainLoanAmount') | currency}}</div>\n                            </div>\n                            <div class=\"form-group\">\n                                <label class=\"col-xs-6 control-label\">Repayment time</label>\n                                <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('mainRepaymentTimeInMonths') }} months</div>\n                            </div>\n                            <div class=\"form-group\">\n                                <label class=\"col-xs-6 control-label\">Interest rate</label>\n                                <div class=\"col-xs-6 form-control-static\">\n                                    <span>{{$ctrl.an('mainMarginInterestRatePercent') | number}} %</span>\n                                    <span ng-if=\"$ctrl.an('mainReferenceInterestRatePercent')\">&nbsp;({{($ctrl.an('mainReferenceInterestRatePercent') > 0 ? '+' : '') + ($ctrl.an('mainReferenceInterestRatePercent') | number:2)}} %)</span>\n                                </div>\n                            </div>\n                        </div>\n                    </div>\n                    <div class=\"col-xs-6\">\n                        <div class=\"form-horizontal\">\n                            <div class=\"form-group\">\n                                <label class=\"col-xs-6 control-label\">Initial fee</label>\n                                <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('mainTotalInitialFeeAmount') | currency}}</div>\n                            </div>\n                            <div class=\"form-group\">\n                                <label class=\"col-xs-6 control-label\">Monthly fee</label>\n                                <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('mainNotificationFeeAmount') | currency}}</div>\n                            </div>\n                            <div class=\"form-group\">\n                                <label class=\"col-xs-6 control-label\">Eff. interest rate</label>\n                                <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('mainEffectiveInterestRatePercent') | number:2}} %</div>\n                            </div>\n                            <div class=\"form-group\">\n                                <label class=\"col-xs-6 control-label\">Monthly amount</label>\n                                <div class=\"col-xs-6 form-control-static\">{{($ctrl.an('mainAnnuityAmount') + $ctrl.an('mainNotificationFeeAmount')) | currency}}</div>\n                            </div>\n                        </div>\n                    </div>\n                </div>\n                <hr class=\"hr-section dotted\"/>\n                <h3>Loan with collateral</h3>\n                <div class=\"row\">\n                    <div class=\"col-xs-6\">\n                        <div class=\"form-horizontal\">\n                            <div class=\"form-group\">\n                                <label class=\"col-xs-6 control-label\">Loan amount</label>\n                                <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('childLoanAmount') | currency}}</div>\n                            </div>\n                            <div class=\"form-group\">\n                                <label class=\"col-xs-6 control-label\">Repayment time</label>\n                                <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('childRepaymentTimeInMonths') }} months</div>\n                            </div>\n                            <div class=\"form-group\">\n                                <label class=\"col-xs-6 control-label\">Interest rate</label>\n                                <div class=\"col-xs-6 form-control-static\">\n                                    <span>{{$ctrl.an('childMarginInterestRatePercent') | number}} %</span>\n                                    <span ng-if=\"$ctrl.an('childReferenceInterestRatePercent')\">&nbsp;({{($ctrl.an('childReferenceInterestRatePercent') > 0 ? '+' : '') + ($ctrl.an('childReferenceInterestRatePercent') | number:2)}} %)</span>\n                                </div>\n                            </div>\n                        </div>\n                    </div>\n                    <div class=\"col-xs-6\">\n                        <div class=\"form-horizontal\">\n                            <div class=\"form-group\">\n                                <label class=\"col-xs-6 control-label\">Initial fee</label>\n                                <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('childTotalInitialFeeAmount') | currency}}</div>\n                            </div>\n                            <div class=\"form-group\">\n                                <label class=\"col-xs-6 control-label\">Monthly fee</label>\n                                <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('childNotificationFeeAmount') | currency}}</div>\n                            </div>\n                            <div class=\"form-group\">\n                                <label class=\"col-xs-6 control-label\">Eff. interest rate</label>\n                                <div class=\"col-xs-6 form-control-static\">{{$ctrl.an('childEffectiveInterestRatePercent') | number:2}} %</div>\n                            </div>\n                            <div class=\"form-group\">\n                                <label class=\"col-xs-6 control-label\">Monthly amount</label>\n                                <div class=\"col-xs-6 form-control-static\">{{($ctrl.an('childAnnuityAmount') + $ctrl.an('childNotificationFeeAmount')) | currency}}</div>\n                            </div>\n                        </div>\n                    </div>\n                </div>\n            </div>\n        </div>\n        <div class=\"pt-3\" ng-if=\"$ctrl.isNewCreditCheckPossible()\">\n            <a class=\"n-main-btn n-blue-btn\" ng-click=\"$ctrl.newCreditCheck($event)\">\n                New credit check <span class=\"glyphicon glyphicon-arrow-right\"></span>\n            </a>\n        </div></div>";
        }
        return MortgageLoanApplicationDualInitialCreditCheckComponent;
    }());
    MortgageLoanApplicationDualCreditCheckComponentNs.MortgageLoanApplicationDualInitialCreditCheckComponent = MortgageLoanApplicationDualInitialCreditCheckComponent;
})(MortgageLoanApplicationDualCreditCheckComponentNs || (MortgageLoanApplicationDualCreditCheckComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanApplicationDualCreditCheck', new MortgageLoanApplicationDualCreditCheckComponentNs.MortgageLoanApplicationDualInitialCreditCheckComponent());
