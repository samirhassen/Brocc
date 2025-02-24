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
var MortgageApplicationSettlementComponentNs;
(function (MortgageApplicationSettlementComponentNs) {
    var MortgageApplicationSettlementController = /** @class */ (function (_super) {
        __extends(MortgageApplicationSettlementController, _super);
        function MortgageApplicationSettlementController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        MortgageApplicationSettlementController.prototype.componentName = function () {
            return 'mortgageApplicationSettlement';
        };
        MortgageApplicationSettlementController.prototype.isSettlementAllowed = function () {
            return this.initialData
                && this.initialData.applicationInfo.IsActive
                && this.initialData.workflowModel.areAllStepBeforeThisAccepted(this.initialData.applicationInfo)
                && !this.initialData.workflowModel.isStatusAccepted(this.initialData.applicationInfo);
        };
        MortgageApplicationSettlementController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            if (!(this.isSettlementAllowed() || this.initialData.workflowModel.isStatusAccepted(this.initialData.applicationInfo))) {
                return;
            }
            this.apiClient.fetchMortgageLoanSettlementData(this.initialData.applicationInfo).then(function (result) {
                var amd = result.AmortizationPlanDocument ? angular.copy(result.AmortizationPlanDocument) : null;
                if (amd) {
                    amd.DownloadUrl = NTechPreCreditApi.ApplicationDocument.GetDownloadUrl(amd);
                }
                _this.m = {
                    AmortizationModel: result.AmortizationModel,
                    AmortizationPlanDocument: amd,
                    FinalOffer: result.FinalOffer,
                    LoanTypeCode: result.LoanTypeCode
                };
                if (result.PendingSettlementPayment) {
                    _this.m.PendingSettlementPayment = result.PendingSettlementPayment;
                }
                else {
                    _this.m.Edit = {
                        CurrentLoans: []
                    };
                    for (var _i = 0, _a = result.CurrentLoansModel.Loans; _i < _a.length; _i++) {
                        var loan = _a[_i];
                        _this.m.Edit.CurrentLoans.push({
                            BankName: loan.BankName,
                            MonthlyAmortizationAmount: loan.MonthlyAmortizationAmount,
                            LastKnownCurrentBalance: loan.CurrentBalance,
                            LoanNr: loan.LoanNr,
                            ActualLoanAmount: '',
                            InterestDifferenceAmount: ''
                        });
                    }
                }
            });
        };
        MortgageApplicationSettlementController.prototype.calculateSettlementSuggestion = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            var actualLoanAmount = this.getEditActualLoanAmount();
            var interestDifferenceAmount = this.getEditInterestDifferenceAmount();
            if (actualLoanAmount == null || interestDifferenceAmount == null) {
                return;
            }
            this.m.Preview = {
                applicationNr: this.initialData.applicationInfo.ApplicationNr,
                interestDifferenceAmount: interestDifferenceAmount,
                actualLoanAmount: actualLoanAmount,
                grantedLoanAmount: this.getGrantedLoanAmount(),
                totalPaidAmount: interestDifferenceAmount + actualLoanAmount,
                actualVsGrantedDifferenceAmount: this.getGrantedLoanAmount() - actualLoanAmount
            };
        };
        MortgageApplicationSettlementController.prototype.scheduleOutgoingPayment = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.scheduleMortgageLoanOutgoingSettlementPayment(this.m.Preview.applicationNr, this.m.Preview.interestDifferenceAmount, this.m.Preview.actualLoanAmount).then(function () {
                toastr.info('Ok');
                _this.signalReloadRequired();
            });
        };
        MortgageApplicationSettlementController.prototype.cancelScheduledOutgoingPayment = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.cancelScheduledMortgageLoanOutgoingSettlementPayment(this.initialData.applicationInfo.ApplicationNr).then(function () {
                toastr.info('Ok');
                _this.signalReloadRequired();
            });
        };
        MortgageApplicationSettlementController.prototype.createNewLoan = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m || !this.m.PendingSettlementPayment) {
                return;
            }
            this.apiClient.createMortgageLoan(this.initialData.applicationInfo.ApplicationNr).then(function (result) {
                toastr.info('Ok');
                _this.signalReloadRequired();
            });
        };
        MortgageApplicationSettlementController.prototype.getEditSum = function (getFieldValue) {
            if (!this.m || !this.m.Edit || !this.m.Edit.CurrentLoans) {
                return null;
            }
            var sum = 0;
            for (var _i = 0, _a = this.m.Edit.CurrentLoans; _i < _a.length; _i++) {
                var loan = _a[_i];
                var amount = this.parseDecimalOrNull(getFieldValue(loan));
                if (amount === null) {
                    return null;
                }
                sum = sum + amount;
            }
            return sum;
        };
        MortgageApplicationSettlementController.prototype.getGrantedLoanAmount = function () {
            if (!this.m || !this.m.AmortizationModel) {
                return null;
            }
            return this.m.AmortizationModel.CurrentLoanAmount;
        };
        MortgageApplicationSettlementController.prototype.getEditActualLoanAmount = function () {
            return this.getEditSum(function (x) { return x.ActualLoanAmount; });
        };
        MortgageApplicationSettlementController.prototype.getEditInterestDifferenceAmount = function () {
            return this.getEditSum(function (x) { return x.InterestDifferenceAmount; });
        };
        MortgageApplicationSettlementController.$inject = ['$http', '$q', 'ntechComponentService'];
        return MortgageApplicationSettlementController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageApplicationSettlementComponentNs.MortgageApplicationSettlementController = MortgageApplicationSettlementController;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    var EditModel = /** @class */ (function () {
        function EditModel() {
        }
        return EditModel;
    }());
    var EditMortageLoanCurrentLoansLoanModel = /** @class */ (function () {
        function EditMortageLoanCurrentLoansLoanModel() {
        }
        return EditMortageLoanCurrentLoansLoanModel;
    }());
    var PreviewModel = /** @class */ (function () {
        function PreviewModel() {
        }
        return PreviewModel;
    }());
    var MortgageApplicationSettlementComponent = /** @class */ (function () {
        function MortgageApplicationSettlementComponent() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationSettlementController;
            this.templateUrl = 'mortgage-application-settlement.html';
        }
        return MortgageApplicationSettlementComponent;
    }());
    MortgageApplicationSettlementComponentNs.MortgageApplicationSettlementComponent = MortgageApplicationSettlementComponent;
})(MortgageApplicationSettlementComponentNs || (MortgageApplicationSettlementComponentNs = {}));
angular.module('ntech.components').component('mortgageApplicationSettlement', new MortgageApplicationSettlementComponentNs.MortgageApplicationSettlementComponent());
