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
var MortgageLoanOtherConnectedApplicationsCompactComponentNs;
(function (MortgageLoanOtherConnectedApplicationsCompactComponentNs) {
    var MortgageLoanOtherConnectedApplicationsCompactController = /** @class */ (function (_super) {
        __extends(MortgageLoanOtherConnectedApplicationsCompactController, _super);
        function MortgageLoanOtherConnectedApplicationsCompactController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.modalDialogService = modalDialogService;
            return _this;
        }
        MortgageLoanOtherConnectedApplicationsCompactController.prototype.componentName = function () {
            return 'mortgageLoanOtherConnectedApplicationsCompactComponentNsCompact';
        };
        MortgageLoanOtherConnectedApplicationsCompactController.prototype.onChanges = function () {
            this.reload();
        };
        MortgageLoanOtherConnectedApplicationsCompactController.prototype.reload = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.loadModel().then(function (x) { return _this.m = x; });
        };
        MortgageLoanOtherConnectedApplicationsCompactController.prototype.loadApplicationData = function () {
            var _this = this;
            var rolesByCustomerId = {};
            return this.apiClient.fetchApplicationInfoWithApplicants(this.initialData.applicationNr).then(function (ai2) {
                for (var _i = 0, _a = Object.keys(ai2.CustomerIdByApplicantNr); _i < _a.length; _i++) {
                    var applicantNr = _a[_i];
                    rolesByCustomerId[ai2.CustomerIdByApplicantNr[applicantNr]] = ['Applicant'];
                }
                return ComplexApplicationListHelper.getAllCustomerIds(_this.initialData.applicationNr, ['ApplicationObject'], _this.apiClient, rolesByCustomerId).then(function (listCustomers) {
                    var customerIds = NTechPreCreditApi.getNumberDictionarKeys(listCustomers);
                    return _this.apiClient.fetchCustomerItemsBulk(customerIds, ['firstName', 'birthDate']).then(function (customerItems) {
                        return { rolesByCustomerId: rolesByCustomerId, listCustomers: listCustomers, customerItems: customerItems, customerIds: customerIds, ai2: ai2 };
                    });
                });
            });
        };
        MortgageLoanOtherConnectedApplicationsCompactController.prototype.loadModel = function () {
            var _this = this;
            return this.loadApplicationData().then(function (a) {
                var listCustomers = a.listCustomers;
                var customerItems = a.customerItems;
                var customerIds = a.customerIds;
                var ai2 = a.ai2;
                var firstNameAndBirthDateByCustomerId = {};
                for (var _i = 0, customerIds_1 = customerIds; _i < customerIds_1.length; _i++) {
                    var customerId = customerIds_1[_i];
                    firstNameAndBirthDateByCustomerId[customerId] = {
                        birthDate: customerItems[customerId]['birthDate'],
                        firstName: customerItems[customerId]['firstName'],
                        customerId: customerId
                    };
                }
                return _this.apiClient.fetchotherApplicationsByCustomerId(customerIds, _this.initialData.applicationNr, true).then(function (otherApps) {
                    var doForEachApplicantApplicationPair = function (action) {
                        otherApps.Applicants.forEach(function (app) {
                            app.Applications.forEach(function (x) {
                                action(app, x);
                            });
                        });
                    };
                    var applicationsNrsWithDupes = [];
                    doForEachApplicantApplicationPair(function (_, x) { return applicationsNrsWithDupes.push(x.ApplicationNr); });
                    return _this.apiClient.fetchApplicationInfoBulk(NTechLinq.distinct(applicationsNrsWithDupes)).then(function (applicationInfoByApplicationNr) {
                        var m;
                        m = {
                            customerIds: [],
                            rolesByCustomerId: [],
                            customerIdByApplicantNr: [],
                            firstNameAndBirthDateByCustomerId: [],
                            applicantInfo: null,
                            otherApplications: []
                        };
                        m.customerIds = customerIds;
                        m.rolesByCustomerId = listCustomers;
                        m.customerIdByApplicantNr = ai2.CustomerIdByApplicantNr;
                        m.firstNameAndBirthDateByCustomerId = firstNameAndBirthDateByCustomerId;
                        doForEachApplicantApplicationPair(function (app, x) {
                            m.otherApplications.push({
                                "CustomerId": app.CustomerId,
                                "ApplicationDate": moment(applicationInfoByApplicationNr[x.ApplicationNr].ApplicationDate).format("YYYY-MM-DD"),
                                "ApplicationNr": x.ApplicationNr,
                                "IsActive": applicationInfoByApplicationNr[x.ApplicationNr].IsActive,
                                "Status": _this.getApplicationStatus(applicationInfoByApplicationNr[x.ApplicationNr])
                            });
                        });
                        return m;
                    });
                });
            });
        };
        MortgageLoanOtherConnectedApplicationsCompactController.prototype.getApplicationStatus = function (appInfo) {
            var stat = NTechPreCreditApi.ApplicationStatusItem;
            if (appInfo.IsCancelled)
                return stat.cancelled;
            if (appInfo.IsRejected)
                return stat.rejected;
            if (appInfo.IsFinalDecisionMade)
                return stat.finalDecisionMade;
            return null;
        };
        MortgageLoanOtherConnectedApplicationsCompactController.prototype.getToggleBlockText = function (data) {
            var applicationCount = this.m.otherApplications.filter(function (x) { return x["CustomerId"] == data.customerId; }).length;
            var applicantRole = this.m.rolesByCustomerId[data.customerId];
            var applicantRoleText = applicantRole[0] === "Applicant" ? "Applicant" : "Other";
            var txt = "".concat(applicantRoleText, ": ").concat(data.firstName, ", ").concat(data.birthDate, " (").concat(applicationCount, ")");
            return txt;
        };
        MortgageLoanOtherConnectedApplicationsCompactController.prototype.isCustomerHasOtherApplications = function (customerId) {
            return this.m.otherApplications.some(function (x) { return x["CustomerId"] === customerId; });
        };
        MortgageLoanOtherConnectedApplicationsCompactController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return MortgageLoanOtherConnectedApplicationsCompactController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanOtherConnectedApplicationsCompactComponentNs.MortgageLoanOtherConnectedApplicationsCompactController = MortgageLoanOtherConnectedApplicationsCompactController;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageLoanOtherConnectedApplicationsCompactComponentNs.Model = Model;
    var MortgageLoanOtherConnectedApplicationsCompactComponent = /** @class */ (function () {
        function MortgageLoanOtherConnectedApplicationsCompactComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanOtherConnectedApplicationsCompactController;
            this.template = "<div ng-if=\"$ctrl.m\">\n    <div class=\"otherApplicationsBlock\" ng-repeat=\"cust in $ctrl.m.firstNameAndBirthDateByCustomerId track by $index\">\n        <toggle-block header-text=\"$ctrl.getToggleBlockText(cust)\" >\n            <table class=\"table\" ng-if=\"$ctrl.isCustomerHasOtherApplications(cust.customerId)\">\n                <thead>\n                    <tr> \n                        <th>Date</th>\n                        <th class=\"text-right\">Status</th>\n                    </tr>\n                </thead>\n                <tbody>\n                    <tr ng-repeat=\"x in $ctrl.m.otherApplications track by $index\" ng-if=\"x.CustomerId === cust.customerId\">\n                        <td>                          \n                            <a class=\"n-anchor\" ng-class=\"{ 'inactive': x.IsActive !== true }\"  target=\"_blank\" ng-href=\"{{'/Ui/MortgageLoan/Application?applicationNr=' + x.ApplicationNr}}\">\n                            {{ x.ApplicationDate }} <span class=\"glyphicon glyphicon-new-window\"></span>\n                            </a>\n                        </td>\n                        <td class=\"text-right\"> \n                            {{ x.Status ? x.Status : 'Active' }}\n                        </td>\n                    </tr>\n                </tbody>\n            </table>\n        </toggle-block>\n    </div>\n </div> ";
        }
        return MortgageLoanOtherConnectedApplicationsCompactComponent;
    }());
    MortgageLoanOtherConnectedApplicationsCompactComponentNs.MortgageLoanOtherConnectedApplicationsCompactComponent = MortgageLoanOtherConnectedApplicationsCompactComponent;
})(MortgageLoanOtherConnectedApplicationsCompactComponentNs || (MortgageLoanOtherConnectedApplicationsCompactComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanOtherConnectedApplicationsCompact', new MortgageLoanOtherConnectedApplicationsCompactComponentNs.MortgageLoanOtherConnectedApplicationsCompactComponent());
