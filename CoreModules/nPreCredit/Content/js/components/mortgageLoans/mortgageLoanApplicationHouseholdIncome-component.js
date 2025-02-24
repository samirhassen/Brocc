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
var MortgageLoanApplicationHouseholdIncomeComponentNs;
(function (MortgageLoanApplicationHouseholdIncomeComponentNs) {
    var MortgageLoanApplicationHouseholdIncomeController = /** @class */ (function (_super) {
        __extends(MortgageLoanApplicationHouseholdIncomeController, _super);
        function MortgageLoanApplicationHouseholdIncomeController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        MortgageLoanApplicationHouseholdIncomeController.prototype.componentName = function () {
            return 'mortgageLoanApplicationHouseholdIncome';
        };
        MortgageLoanApplicationHouseholdIncomeController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (this.initialData == null) {
                return;
            }
            this.apiClient.fetchHouseholdIncomeModel(this.initialData.applicationInfo.ApplicationNr, true).then(function (x) {
                var model = x.model;
                _this.m = {
                    onBack: (_this.initialData.onBack || _this.initialData.backUrl) ? (function (evt) {
                        if (evt) {
                            evt.preventDefault();
                        }
                        if (_this.initialData.onBack) {
                            _this.initialData.onBack(_this.m.wasChanged ? _this.getViewHouseholdGrossTotalMonthlyIncome() : null);
                        }
                        else if (_this.initialData.backUrl) {
                            document.location.href = _this.initialData.backUrl;
                        }
                    }) : null,
                    wasChanged: false,
                    usernames: x.usernames,
                    viewApplicants: model.ApplicantIncomes,
                    documentComments: {
                        applicationInfo: _this.initialData.applicationInfo,
                        reloadPageOnWaitingForAdditionalInformation: false,
                        newCommentEventType: 'HouseholdIncomeEdit',
                        showOnlyTheseEventTypes: ['HouseholdIncomeEdit'],
                        alwaysShowAttachedFiles: true
                    },
                    showHeader: !_this.initialData.hideHeader
                };
            });
        };
        MortgageLoanApplicationHouseholdIncomeController.prototype.edit = function () {
            var e = [];
            for (var _i = 0, _a = this.m.viewApplicants; _i < _a.length; _i++) {
                var a = _a[_i];
                e.push({
                    applicantNr: a.ApplicantNr,
                    capitalGrossMonthlyIncome: this.formatNumberForEdit(a.CapitalGrossMonthlyIncome),
                    employmentGrossMonthlyIncome: this.formatNumberForEdit(a.EmploymentGrossMonthlyIncome),
                    serviceGrossMonthlyIncome: this.formatNumberForEdit(a.ServiceGrossMonthlyIncome)
                });
            }
            this.m.editApplicants = e;
        };
        MortgageLoanApplicationHouseholdIncomeController.prototype.cancel = function () {
            this.m.editApplicants = null;
        };
        MortgageLoanApplicationHouseholdIncomeController.prototype.save = function () {
            var _this = this;
            var a = [];
            for (var _i = 0, _a = this.m.editApplicants; _i < _a.length; _i++) {
                var e = _a[_i];
                a.push({
                    ApplicantNr: e.applicantNr,
                    CapitalGrossMonthlyIncome: this.nullZero(this.parseDecimalOrNull(e.capitalGrossMonthlyIncome)),
                    EmploymentGrossMonthlyIncome: this.nullZero(this.parseDecimalOrNull(e.employmentGrossMonthlyIncome)),
                    ServiceGrossMonthlyIncome: this.nullZero(this.parseDecimalOrNull(e.serviceGrossMonthlyIncome))
                });
            }
            this.apiClient.setHouseholdIncomeModel(this.initialData.applicationInfo.ApplicationNr, {
                ApplicantIncomes: a
            }).then(function () {
                _this.apiClient.fetchHouseholdIncomeModel(_this.initialData.applicationInfo.ApplicationNr, true).then(function (result) {
                    _this.m.viewApplicants = result.model.ApplicantIncomes;
                    _this.m.usernames = result.usernames;
                    _this.m.editApplicants = null;
                    _this.m.wasChanged = true;
                    if (_this.initialData.onIncomeChanged) {
                        _this.initialData.onIncomeChanged(_this.getViewHouseholdGrossTotalMonthlyIncome());
                    }
                });
            });
        };
        MortgageLoanApplicationHouseholdIncomeController.prototype.getUserDisplayName = function (userId) {
            if (this.m && this.m.usernames) {
                for (var _i = 0, _a = this.m.usernames; _i < _a.length; _i++) {
                    var u = _a[_i];
                    if (u.UserId === userId) {
                        return u.DisplayName;
                    }
                }
            }
            return 'User ' + userId;
        };
        MortgageLoanApplicationHouseholdIncomeController.prototype.nullZero = function (n) {
            return n ? n : 0;
        };
        MortgageLoanApplicationHouseholdIncomeController.prototype.getEditApplicantGrossTotalMonthlyIncome = function (a) {
            if (this.m == null || this.m.editApplicants == null) {
                return null;
            }
            var parts = [this.parseDecimalOrNull(a.capitalGrossMonthlyIncome), this.parseDecimalOrNull(a.employmentGrossMonthlyIncome), this.parseDecimalOrNull(a.serviceGrossMonthlyIncome)];
            if (_.some(parts, function (x) { return x === null; })) {
                return null;
            }
            var sum = 0;
            for (var _i = 0, parts_1 = parts; _i < parts_1.length; _i++) {
                var p = parts_1[_i];
                sum += this.nullZero(p);
            }
            return sum;
        };
        MortgageLoanApplicationHouseholdIncomeController.prototype.getEditHouseholdGrossTotalMonthlyIncome = function () {
            if (this.m == null || this.m.editApplicants == null) {
                return null;
            }
            var sum = 0;
            for (var _i = 0, _a = this.m.editApplicants; _i < _a.length; _i++) {
                var a = _a[_i];
                var asum = this.getEditApplicantGrossTotalMonthlyIncome(a);
                if (asum === null) {
                    return null;
                }
                sum += this.nullZero(asum);
            }
            return sum;
        };
        MortgageLoanApplicationHouseholdIncomeController.prototype.getViewHouseholdGrossTotalMonthlyIncome = function () {
            if (this.m == null || this.m.viewApplicants == null) {
                return null;
            }
            var sum = 0;
            for (var _i = 0, _a = this.m.viewApplicants; _i < _a.length; _i++) {
                var a = _a[_i];
                sum += this.nullZero(a.CapitalGrossMonthlyIncome) + this.nullZero(a.EmploymentGrossMonthlyIncome) + this.nullZero(a.ServiceGrossMonthlyIncome);
            }
            return sum;
        };
        MortgageLoanApplicationHouseholdIncomeController.$inject = ['$http', '$q', 'ntechComponentService'];
        return MortgageLoanApplicationHouseholdIncomeController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanApplicationHouseholdIncomeComponentNs.MortgageLoanApplicationHouseholdIncomeController = MortgageLoanApplicationHouseholdIncomeController;
    var MortgageLoanApplicationHouseholdIncomeComponent = /** @class */ (function () {
        function MortgageLoanApplicationHouseholdIncomeComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationHouseholdIncomeController;
            this.templateUrl = 'mortgage-loan-application-household-income.html';
        }
        return MortgageLoanApplicationHouseholdIncomeComponent;
    }());
    MortgageLoanApplicationHouseholdIncomeComponentNs.MortgageLoanApplicationHouseholdIncomeComponent = MortgageLoanApplicationHouseholdIncomeComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    MortgageLoanApplicationHouseholdIncomeComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageLoanApplicationHouseholdIncomeComponentNs.Model = Model;
    var ApplicantEditModel = /** @class */ (function () {
        function ApplicantEditModel() {
        }
        return ApplicantEditModel;
    }());
    MortgageLoanApplicationHouseholdIncomeComponentNs.ApplicantEditModel = ApplicantEditModel;
})(MortgageLoanApplicationHouseholdIncomeComponentNs || (MortgageLoanApplicationHouseholdIncomeComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanApplicationHouseholdIncome', new MortgageLoanApplicationHouseholdIncomeComponentNs.MortgageLoanApplicationHouseholdIncomeComponent());
