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
var MortgageLoanAmortizationComponentNs;
(function (MortgageLoanAmortizationComponentNs) {
    var MortgageLoanAmortizationController = /** @class */ (function (_super) {
        __extends(MortgageLoanAmortizationController, _super);
        function MortgageLoanAmortizationController($http, $q, $scope, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$scope = $scope;
            _this.modalDialogService = modalDialogService;
            _this.u = NTechComponents.generateUniqueId(6);
            _this.editDialogId = modalDialogService.generateDialogId();
            return _this;
        }
        MortgageLoanAmortizationController.prototype.componentName = function () {
            return 'mortgageLoanAmortization';
        };
        MortgageLoanAmortizationController.prototype.showOverview = function (model, isNewAllowed, allowSetAndEdit) {
            var _this = this;
            this.m = {
                nm: null,
                om: {
                    basis: model,
                    loanFractionPercent: 100 * (model.AmortizationBasisLoanAmount / model.AmortizationBasisObjectValue),
                    loanIncomeRatio: model.CurrentCombinedTotalLoanAmount / model.CurrentCombinedYearlyIncomeAmount,
                    isNewAllowed: isNewAllowed,
                    isSetAndEditAllowed: allowSetAndEdit,
                    showLoanIncomeRatioDetails: false,
                    onNew: isNewAllowed ? ((function (evt) {
                        if (evt) {
                            evt.preventDefault();
                        }
                        _this.reload(false);
                    })) : (function (evt) { })
                },
                showFuturePossibleMessage: false
            };
        };
        MortgageLoanAmortizationController.prototype.onChanges = function () {
            this.reload(true);
        };
        MortgageLoanAmortizationController.prototype.reload = function (isOverview) {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            var ai = this.initialData.applicationInfo;
            var areAllStepsBeforeThisCompleted = this.initialData.workflowModel.areAllStepBeforeThisAccepted(ai);
            var isNewAllowed = ai.IsActive && areAllStepsBeforeThisCompleted;
            if (!areAllStepsBeforeThisCompleted) {
                this.m = {
                    om: null,
                    nm: null,
                    showFuturePossibleMessage: true
                };
            }
            else if (isOverview) {
                this.apiClient.fetchMortgageLoanAmortizationBasis(this.initialData.applicationInfo.ApplicationNr).then(function (x) {
                    _this.showOverview(x, isNewAllowed, false);
                });
            }
            else {
                this.apiClient.fetchApplicationDocuments(this.initialData.applicationInfo.ApplicationNr, ['MortgageLoanCustomerAmortizationPlan']).then(function (documents) {
                    _this.apiClient.fetchMortageLoanApplicationInitialCreditCheckStatus(_this.initialData.applicationInfo.ApplicationNr, null).then(function (initialCreditCheckStatus) {
                        _this.m = {
                            nm: {
                                mortgageLoanCustomerAmortizationPlanDownloadUrl: documents && documents.length == 1 ? NTechPreCreditApi.ApplicationDocument.GetDownloadUrl(documents[0]) : null,
                                initialOfferLoanAmount: initialCreditCheckStatus.AcceptedDecision.Offer.LoanAmount,
                                initialOfferMonthlyAmortizationAmount: initialCreditCheckStatus.AcceptedDecision.Offer.MonthlyAmortizationAmount,
                                c: {},
                                p: null
                            },
                            om: null,
                            showFuturePossibleMessage: false
                        };
                    });
                });
            }
        };
        MortgageLoanAmortizationController.prototype.calculate = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var c = this.m.nm.c;
            this.apiClient.calculateMortgageLoanAmortizationSuggestionBasedOnStandardBankForm(this.initialData.applicationInfo.ApplicationNr, {
                AlternateRuleAmortizationAmount: this.parseDecimalOrNull(c.totalAmortizationAlternateAmount),
                AmortizationBasisDate: NTechDates.DateOnly.parseDateOrNull(c.amortizationBasisDate),
                AmortizationBasisLoanAmount: this.parseDecimalOrNull(c.amortizationBasisLoanAmount),
                AmortizationBasisObjectValue: this.parseDecimalOrNull(c.amortizationBasisObjectValue),
                RuleAlternateCurrentAmount: this.parseDecimalOrNull(c.ruleAlternateCurrentAmount),
                RuleNoneCurrentAmount: this.parseDecimalOrNull(c.ruleNoneCurrentAmount),
                RuleR201616CurrentAmount: this.parseDecimalOrNull(c.ruleR201616CurrentAmount),
                RuleR201723CurrentAmount: this.parseDecimalOrNull(c.ruleR201723CurrentAmount)
            }).then(function (x) {
                _this.showOverview(x, false, true);
            });
        };
        MortgageLoanAmortizationController.prototype.setBasis = function (basis, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.setMortgageLoanAmortizationBasis(this.initialData.applicationInfo.ApplicationNr, basis).then(function () {
                _this.signalReloadRequired();
            });
        };
        MortgageLoanAmortizationController.prototype.editManually = function (basis, evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m.om || !this.m.om.basis) {
                return;
            }
            var b = this.m.om.basis;
            var currentExceptions = b.AmortizationExceptionReasons || [];
            var possibleExceptions = ['Nyproduktion', 'Lantbruksenhet', 'Sjukdom', 'Arbetsl\u00f6shet', 'D\u00f6dsfall'];
            var exceptions = [];
            for (var _i = 0, possibleExceptions_1 = possibleExceptions; _i < possibleExceptions_1.length; _i++) {
                var e = possibleExceptions_1[_i];
                exceptions.push({
                    name: e,
                    checked: currentExceptions.indexOf(e) >= 0
                });
            }
            this.m.om.e = {
                actualAmortizationAmount: this.formatNumberForEdit(b.ActualAmortizationAmount),
                amortizationExceptionReasons: exceptions,
                hasAmortizationFree: !!b.AmortizationFreeUntilDate,
                amortizationExceptionUntilDate: this.formatDateOnlyForEdit(b.AmortizationExceptionUntilDate),
                amortizationFreeUntilDate: this.formatDateOnlyForEdit(b.AmortizationFreeUntilDate),
                exceptionAmortizationAmount: this.formatNumberForEdit(b.ExceptionAmortizationAmount),
                hasException: !!b.AmortizationExceptionUntilDate
            };
            this.modalDialogService.openDialog(this.editDialogId);
        };
        MortgageLoanAmortizationController.prototype.cancelEdit = function (evt) {
            this.m.om.e = null;
            this.modalDialogService.closeDialog(this.editDialogId);
        };
        MortgageLoanAmortizationController.prototype.saveEdit = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m.om || !this.m.om.e) {
                return;
            }
            var b = angular.copy(this.m.om.basis);
            var e = this.m.om.e;
            b.ActualAmortizationAmount = this.parseDecimalOrNull(e.actualAmortizationAmount);
            b.AmortizationFreeUntilDate = e.hasAmortizationFree ? NTechDates.DateOnly.parseDateOrNull(e.amortizationFreeUntilDate) : null;
            if (e.hasException) {
                b.ExceptionAmortizationAmount = this.parseDecimalOrNull(e.exceptionAmortizationAmount);
                b.AmortizationExceptionUntilDate = NTechDates.DateOnly.parseDateOrNull(e.amortizationExceptionUntilDate);
                b.AmortizationExceptionReasons = [];
                for (var _i = 0, _a = e.amortizationExceptionReasons; _i < _a.length; _i++) {
                    var r = _a[_i];
                    if (r.checked) {
                        b.AmortizationExceptionReasons.push(r.name);
                    }
                }
            }
            else {
                b.ExceptionAmortizationAmount = null;
                b.AmortizationExceptionUntilDate = null;
                b.AmortizationExceptionReasons = [];
            }
            this.m.om.basis = b;
            this.m.om.e = null;
            this.modalDialogService.closeDialog(this.editDialogId);
        };
        MortgageLoanAmortizationController.prototype.toggleArrayItem = function (item, items, evt) {
            if (evt) {
                evt.stopPropagation();
                evt.preventDefault();
            }
            var index = items.indexOf(item);
            if (index >= 0) {
                items.splice(index, 1);
            }
            else {
                items.push(item);
            }
        };
        MortgageLoanAmortizationController.prototype.isAtLeastOnceExceptionChecked = function () {
            if (!this.m.om || !this.m.om.e || !this.m.om.e.amortizationExceptionReasons) {
                return false;
            }
            return this.m.om.e.amortizationExceptionReasons.some(function (x) { return x.checked; });
        };
        MortgageLoanAmortizationController.$inject = ['$http', '$q', '$scope', 'ntechComponentService', 'modalDialogService'];
        return MortgageLoanAmortizationController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanAmortizationComponentNs.MortgageLoanAmortizationController = MortgageLoanAmortizationController;
    var MortgageLoanAmortizationComponent = /** @class */ (function () {
        function MortgageLoanAmortizationComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanAmortizationController;
            this.templateUrl = 'mortgage-loan-amortization.html';
        }
        return MortgageLoanAmortizationComponent;
    }());
    MortgageLoanAmortizationComponentNs.MortgageLoanAmortizationComponent = MortgageLoanAmortizationComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    MortgageLoanAmortizationComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageLoanAmortizationComponentNs.Model = Model;
    var OverviewModel = /** @class */ (function () {
        function OverviewModel() {
        }
        return OverviewModel;
    }());
    MortgageLoanAmortizationComponentNs.OverviewModel = OverviewModel;
    var NewModel = /** @class */ (function () {
        function NewModel() {
        }
        return NewModel;
    }());
    MortgageLoanAmortizationComponentNs.NewModel = NewModel;
    var CalculateModel = /** @class */ (function () {
        function CalculateModel() {
        }
        return CalculateModel;
    }());
    MortgageLoanAmortizationComponentNs.CalculateModel = CalculateModel;
    var PreviewModel = /** @class */ (function () {
        function PreviewModel() {
        }
        return PreviewModel;
    }());
    MortgageLoanAmortizationComponentNs.PreviewModel = PreviewModel;
    var MortgageLoanAmortizationBasisEditModel = /** @class */ (function () {
        function MortgageLoanAmortizationBasisEditModel() {
        }
        return MortgageLoanAmortizationBasisEditModel;
    }());
    MortgageLoanAmortizationComponentNs.MortgageLoanAmortizationBasisEditModel = MortgageLoanAmortizationBasisEditModel;
    var ExceptionEditModel = /** @class */ (function () {
        function ExceptionEditModel() {
        }
        return ExceptionEditModel;
    }());
    MortgageLoanAmortizationComponentNs.ExceptionEditModel = ExceptionEditModel;
})(MortgageLoanAmortizationComponentNs || (MortgageLoanAmortizationComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanAmortization', new MortgageLoanAmortizationComponentNs.MortgageLoanAmortizationComponent());
