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
var CompanyLoanApproveApplicationsComponentNs;
(function (CompanyLoanApproveApplicationsComponentNs) {
    var CompanyLoanApproveApplicationsController = /** @class */ (function (_super) {
        __extends(CompanyLoanApproveApplicationsController, _super);
        function CompanyLoanApproveApplicationsController($http, $q, ntechComponentService, $timeout) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$timeout = $timeout;
            return _this;
        }
        CompanyLoanApproveApplicationsController.prototype.componentName = function () {
            return 'companyLoanApproveApplications';
        };
        CompanyLoanApproveApplicationsController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.initialData.companyLoanApiClient.fetchApplicationsPendingFinalDecision().then(function (x) {
                var apps = [];
                for (var _i = 0, _a = x.Applications; _i < _a.length; _i++) {
                    var a = _a[_i];
                    var url = "/Ui/CompanyLoan/Application?applicationNr=".concat(a.ApplicationNr);
                    if (_this.initialData.backUrl) {
                        url += "&backUrl=".concat(encodeURIComponent(_this.initialData.backUrl));
                    }
                    var un = _this.initialData.userNameByUserId[a.ApprovedByUserId.toString()];
                    apps.push({
                        amount: a.OfferedAmount,
                        applicationNr: a.ApplicationNr,
                        isApproved: true,
                        applicationUrl: url,
                        handlerDisplayName: un ? un : 'User ' + a.ApprovedByUserId
                    });
                }
                var today = function () { return moment(_this.initialData.today, 'YYYY-MM-DD', true); };
                _this.m = {
                    backUrl: _this.initialData.backUrl,
                    applications: apps,
                    historyFromDate: today().subtract(30, 'days').format('YYYY-MM-DD'),
                    historyToDate: today().format('YYYY-MM-DD')
                };
            });
        };
        CompanyLoanApproveApplicationsController.prototype.onBack = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m || !this.m.backUrl) {
                return;
            }
            this.initialData.setIsLoading(true);
            this.$timeout(function () {
                document.location.href = _this.m.backUrl;
            });
        };
        CompanyLoanApproveApplicationsController.prototype.newLoanCountToApprove = function () {
            if (!this.m) {
                return null;
            }
            var count = 0;
            for (var _i = 0, _a = this.m.applications; _i < _a.length; _i++) {
                var a = _a[_i];
                count += a.isApproved ? 1 : 0;
            }
            return count;
        };
        CompanyLoanApproveApplicationsController.prototype.newLoanAmountToApprove = function () {
            if (!this.m) {
                return null;
            }
            var sum = 0;
            for (var _i = 0, _a = this.m.applications; _i < _a.length; _i++) {
                var a = _a[_i];
                sum += a.isApproved ? a.amount : 0;
            }
            return sum;
        };
        CompanyLoanApproveApplicationsController.prototype.createCredits = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var applicationNrs = [];
            var stillPendingApplications = [];
            for (var _i = 0, _a = this.m.applications; _i < _a.length; _i++) {
                var a = _a[_i];
                if (a.isApproved) {
                    applicationNrs.push(a.applicationNr);
                }
                else {
                    stillPendingApplications.push(a);
                }
            }
            this.initialData.companyLoanApiClient.createLoans(applicationNrs).then(function (x) {
                _this.m.applications = stillPendingApplications;
                //TODO: Close/Reset the batch list
            });
        };
        CompanyLoanApproveApplicationsController.prototype.filterHistory = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.initialData.companyLoanApiClient.fetchFinalDecisionBatches(this.m.historyFromDate, this.m.historyToDate).then(function (x) {
                _this.m.historyBatches = x.Batches;
            });
        };
        CompanyLoanApproveApplicationsController.prototype.loadBatchDetails = function (batch, evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.initialData.companyLoanApiClient.fetchFinalDecisionBatchItems(batch.Id).then(function (x) {
                batch.batchDetails = x.Items;
            });
        };
        CompanyLoanApproveApplicationsController.$inject = ['$http', '$q', 'ntechComponentService', '$timeout'];
        return CompanyLoanApproveApplicationsController;
    }(NTechComponents.NTechComponentControllerBase));
    CompanyLoanApproveApplicationsComponentNs.CompanyLoanApproveApplicationsController = CompanyLoanApproveApplicationsController;
    var CompanyLoanApproveApplicationsComponent = /** @class */ (function () {
        function CompanyLoanApproveApplicationsComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanApproveApplicationsController;
            this.templateUrl = 'company-loan-approve-applications.html';
        }
        return CompanyLoanApproveApplicationsComponent;
    }());
    CompanyLoanApproveApplicationsComponentNs.CompanyLoanApproveApplicationsComponent = CompanyLoanApproveApplicationsComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    CompanyLoanApproveApplicationsComponentNs.Model = Model;
    var ApplicationModel = /** @class */ (function () {
        function ApplicationModel() {
        }
        return ApplicationModel;
    }());
    CompanyLoanApproveApplicationsComponentNs.ApplicationModel = ApplicationModel;
})(CompanyLoanApproveApplicationsComponentNs || (CompanyLoanApproveApplicationsComponentNs = {}));
angular.module('ntech.components').component('companyLoanApproveApplications', new CompanyLoanApproveApplicationsComponentNs.CompanyLoanApproveApplicationsComponent());
