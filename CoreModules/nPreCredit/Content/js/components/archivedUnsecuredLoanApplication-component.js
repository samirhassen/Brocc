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
var ArchivedUnsecuredLoanApplicationComponentNs;
(function (ArchivedUnsecuredLoanApplicationComponentNs) {
    var ArchivedUnsecuredLoanApplicationController = /** @class */ (function (_super) {
        __extends(ArchivedUnsecuredLoanApplicationController, _super);
        function ArchivedUnsecuredLoanApplicationController($http, $q, ntechComponentService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.onBack = function (evt) {
                if (evt) {
                    evt.preventDefault();
                }
                NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, _this.apiClient, _this.$q, { applicationNr: initialData.ApplicationNr }, NavigationTargetHelper.NavigationTargetCode.UnsecuredLoanApplications);
            };
            return _this;
        }
        ArchivedUnsecuredLoanApplicationController.prototype.componentName = function () {
            return 'archivedUnsecuredLoanApplication';
        };
        ArchivedUnsecuredLoanApplicationController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.apiClient.fetchApplicationInfo(this.initialData.applicationNr).then(function (x) {
                var applicants = [];
                for (var applicantNr = 1; applicantNr <= x.NrOfApplicants; applicantNr++) {
                    applicants.push({ applicantNr: applicantNr, applicationNr: _this.initialData.applicationNr, customerIdCompoundItemName: null, backTarget: _this.initialData.backTarget, isArchived: true });
                }
                _this.m = {
                    applicationInfo: x,
                    commentsInitialData: {
                        applicationInfo: x
                    },
                    applicants: applicants
                };
            });
        };
        ArchivedUnsecuredLoanApplicationController.$inject = ['$http', '$q', 'ntechComponentService'];
        return ArchivedUnsecuredLoanApplicationController;
    }(NTechComponents.NTechComponentControllerBase));
    ArchivedUnsecuredLoanApplicationComponentNs.ArchivedUnsecuredLoanApplicationController = ArchivedUnsecuredLoanApplicationController;
    var ArchivedUnsecuredLoanApplicationComponent = /** @class */ (function () {
        function ArchivedUnsecuredLoanApplicationComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ArchivedUnsecuredLoanApplicationController;
            this.templateUrl = 'archived-unsecured-loan-application.html';
        }
        return ArchivedUnsecuredLoanApplicationComponent;
    }());
    ArchivedUnsecuredLoanApplicationComponentNs.ArchivedUnsecuredLoanApplicationComponent = ArchivedUnsecuredLoanApplicationComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    ArchivedUnsecuredLoanApplicationComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    ArchivedUnsecuredLoanApplicationComponentNs.Model = Model;
})(ArchivedUnsecuredLoanApplicationComponentNs || (ArchivedUnsecuredLoanApplicationComponentNs = {}));
angular.module('ntech.components').component('archivedUnsecuredLoanApplication', new ArchivedUnsecuredLoanApplicationComponentNs.ArchivedUnsecuredLoanApplicationComponent());
