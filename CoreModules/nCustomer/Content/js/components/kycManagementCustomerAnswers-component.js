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
var KycManagementCustomerAnswersComponentNs;
(function (KycManagementCustomerAnswersComponentNs) {
    var KycManagementCustomerAnswersController = /** @class */ (function (_super) {
        __extends(KycManagementCustomerAnswersController, _super);
        function KycManagementCustomerAnswersController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        KycManagementCustomerAnswersController.prototype.componentName = function () {
            return 'kycManagementCustomerAnswers';
        };
        KycManagementCustomerAnswersController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (this.initialData == null) {
                return;
            }
            this.apiClient.kycManagementFetchLatestCustomerQuestionsSet(this.initialData.customerId).then(function (questionSet) {
                _this.m = {
                    q: questionSet
                };
            });
        };
        KycManagementCustomerAnswersController.$inject = ['$http', '$q', 'ntechComponentService'];
        return KycManagementCustomerAnswersController;
    }(NTechComponents.NTechComponentControllerBase));
    KycManagementCustomerAnswersComponentNs.KycManagementCustomerAnswersController = KycManagementCustomerAnswersController;
    var KycManagementCustomerAnswersComponent = /** @class */ (function () {
        function KycManagementCustomerAnswersComponent() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = KycManagementCustomerAnswersController;
            this.templateUrl = 'kyc-management-customer-answers.html';
        }
        return KycManagementCustomerAnswersComponent;
    }());
    KycManagementCustomerAnswersComponentNs.KycManagementCustomerAnswersComponent = KycManagementCustomerAnswersComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    KycManagementCustomerAnswersComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    KycManagementCustomerAnswersComponentNs.Model = Model;
})(KycManagementCustomerAnswersComponentNs || (KycManagementCustomerAnswersComponentNs = {}));
angular.module('ntech.components').component('kycManagementCustomerAnswers', new KycManagementCustomerAnswersComponentNs.KycManagementCustomerAnswersComponent());
