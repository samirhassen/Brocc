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
var MortgageApplicationCustomerCheckComponentNs;
(function (MortgageApplicationCustomerCheckComponentNs) {
    var MortgageApplicationCustomerCheckController = /** @class */ (function (_super) {
        __extends(MortgageApplicationCustomerCheckController, _super);
        function MortgageApplicationCustomerCheckController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        MortgageApplicationCustomerCheckController.prototype.componentName = function () {
            return 'mortgageApplicationCustomerCheck';
        };
        MortgageApplicationCustomerCheckController.prototype.toModel = function (dm, applicationInfo) {
            var d = dm.Status;
            dm.CustomerLocalDecisionUrlPattern;
            return {
                isApproveAllowed: d.IsApproveAllowed,
                isKycListScreenAllowed: applicationInfo.IsActive && !applicationInfo.IsFinalDecisionMade && !applicationInfo.IsPartiallyApproved,
                proofOfIdentityIssues: _.filter(d.Issues, function (x) { return x.Code === 'ProofOfIdentityMissing'; }),
                pepQuestionNotAnsweredIssues: _.filter(d.Issues, function (x) { return x.Code === 'PepQuestionNotAnswered'; }),
                kycListScreenNotDoneIssues: _.filter(d.Issues, function (x) { return x.Code === 'KycListScreenNotDone'; }),
                kycCustomerDecisionMissingIssues: _.filter(d.Issues, function (x) { return x.Code === 'KycCustomerDecisionMissing'; }),
                customerLocalDecisionUrlPattern: dm.CustomerLocalDecisionUrlPattern
            };
        };
        MortgageApplicationCustomerCheckController.prototype.isApproved = function () {
            return this.initialData && this.initialData.workflowModel.isStatusAccepted(this.initialData.applicationInfo);
        };
        MortgageApplicationCustomerCheckController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            if (!this.isApproved()) {
                this.apiClient.fetchMortageLoanApplicationCustomerCheckStatus(this.initialData.applicationInfo.ApplicationNr, this.initialData.urlToHereFromOtherModule, this.initialData.applicationInfo.IsActive).then(function (result) {
                    _this.m = _this.toModel(result, _this.initialData.applicationInfo);
                });
            }
        };
        MortgageApplicationCustomerCheckController.prototype.tryKycScreen = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m || this.m.kycListScreenNotDoneIssues.length == 0) {
                return;
            }
            var applicantNrs = _.map(this.m.kycListScreenNotDoneIssues, function (x) { return x.ApplicantNr; });
            this.apiClient.doCustomerCheckKycScreen(this.initialData.applicationInfo.ApplicationNr, applicantNrs).then(function (result) {
                var anySuccess = _.any(result, function (x) { return x.IsSuccess; });
                var anyFailure = _.any(result, function (x) { return !x.IsSuccess; });
                if (anyFailure) {
                    var warningMessage = 'All applicants could not be screened: ';
                    for (var _i = 0, _a = _.filter(result, function (x) { return !x.IsSuccess; }); _i < _a.length; _i++) {
                        var f = _a[_i];
                        warningMessage = warningMessage + " Applicant ".concat(f.ApplicantNr, " - ").concat(f.FailureCode, ".");
                    }
                    toastr.warning(warningMessage);
                }
                else {
                    toastr.info('Ok');
                }
                if (anySuccess) {
                    _this.signalReloadRequired();
                }
            });
        };
        MortgageApplicationCustomerCheckController.prototype.approve = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m || !this.m.isApproveAllowed) {
                return;
            }
            this.apiClient.approveMortageLoanApplicationCustomerCheck(this.initialData.applicationInfo.ApplicationNr).then(function () {
                _this.signalReloadRequired();
            });
        };
        MortgageApplicationCustomerCheckController.prototype.getCustomerLocalDecisionUrl = function (i, evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m || !i.CustomerId) {
                return null;
            }
            return this.m.customerLocalDecisionUrlPattern.replace('{{customerId}}', i.CustomerId.toString());
        };
        MortgageApplicationCustomerCheckController.$inject = ['$http', '$q', 'ntechComponentService'];
        return MortgageApplicationCustomerCheckController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageApplicationCustomerCheckComponentNs.MortgageApplicationCustomerCheckController = MortgageApplicationCustomerCheckController;
    var ApplicationCustomerCheckComponent = /** @class */ (function () {
        function ApplicationCustomerCheckComponent() {
            this.bindings = {
                initialData: '<',
            };
            this.controller = MortgageApplicationCustomerCheckController;
            this.templateUrl = 'mortgage-application-customer-check.html';
        }
        return ApplicationCustomerCheckComponent;
    }());
    MortgageApplicationCustomerCheckComponentNs.ApplicationCustomerCheckComponent = ApplicationCustomerCheckComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageApplicationCustomerCheckComponentNs.Model = Model;
})(MortgageApplicationCustomerCheckComponentNs || (MortgageApplicationCustomerCheckComponentNs = {}));
angular.module('ntech.components').component('mortgageApplicationCustomerCheck', new MortgageApplicationCustomerCheckComponentNs.ApplicationCustomerCheckComponent());
