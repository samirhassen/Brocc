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
var UnsecuredApplicationCreditCheckStatusComponentNs;
(function (UnsecuredApplicationCreditCheckStatusComponentNs) {
    var UnsecuredApplicationCreditCheckStatusController = /** @class */ (function (_super) {
        __extends(UnsecuredApplicationCreditCheckStatusController, _super);
        function UnsecuredApplicationCreditCheckStatusController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        UnsecuredApplicationCreditCheckStatusController.prototype.componentName = function () {
            return 'unsecuredApplicationCreditCheckStatus';
        };
        UnsecuredApplicationCreditCheckStatusController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.apiClient.fetchUnsecuredLoanCreditCheckStatus(this.initialData.applicationInfo.ApplicationNr, null, null, false, true).then(function (x) {
                var r = {};
                for (var _i = 0, _a = x.RejectionReasonDisplayNames; _i < _a.length; _i++) {
                    var y = _a[_i];
                    r[y.Name] = y.Value;
                }
                _this.m = {
                    acceptedCreditDecision: x.CurrentCreditDecision && x.CurrentCreditDecision.AcceptedDecisionModel ? JSON.parse(x.CurrentCreditDecision.AcceptedDecisionModel) : null,
                    rejectedCreditDecision: x.CurrentCreditDecision && x.CurrentCreditDecision.RejectedDecisionModel ? JSON.parse(x.CurrentCreditDecision.RejectedDecisionModel) : null,
                    NewCreditCheckUrl: x.NewCreditCheckUrl,
                    ViewCreditDecisionUrl: x.ViewCreditDecisionUrl,
                    RejectionReasonToDisplayNameMapping: r
                };
            });
        };
        UnsecuredApplicationCreditCheckStatusController.prototype.headerClassFromStatus = function (status) {
            var isAccepted = status === 'Accepted';
            var isRejected = status === 'Rejected';
            return { 'text-success': isAccepted, 'text-danger': isRejected };
        };
        UnsecuredApplicationCreditCheckStatusController.prototype.iconClassFromStatus = function (status) {
            var isAccepted = status === 'Accepted';
            var isRejected = status === 'Rejected';
            var isOther = !isAccepted && !isRejected;
            return { 'glyphicon-ok': isAccepted, 'glyphicon-remove': isRejected, 'glyphicon-minus': isOther, 'glyphicon': true, 'text-success': isAccepted, 'text-danger': isRejected };
        };
        UnsecuredApplicationCreditCheckStatusController.prototype.getRejectionReasonDisplayName = function (reason) {
            if (this.m && this.m.RejectionReasonToDisplayNameMapping[reason]) {
                return this.m.RejectionReasonToDisplayNameMapping[reason];
            }
            else {
                return reason;
            }
        };
        UnsecuredApplicationCreditCheckStatusController.$inject = ['$http', '$q', 'ntechComponentService'];
        return UnsecuredApplicationCreditCheckStatusController;
    }(NTechComponents.NTechComponentControllerBase));
    UnsecuredApplicationCreditCheckStatusComponentNs.UnsecuredApplicationCreditCheckStatusController = UnsecuredApplicationCreditCheckStatusController;
    var UnsecuredApplicationCreditCheckStatusComponent = /** @class */ (function () {
        function UnsecuredApplicationCreditCheckStatusComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = UnsecuredApplicationCreditCheckStatusController;
            this.templateUrl = 'unsecured-application-credit-check-status.html';
        }
        return UnsecuredApplicationCreditCheckStatusComponent;
    }());
    UnsecuredApplicationCreditCheckStatusComponentNs.UnsecuredApplicationCreditCheckStatusComponent = UnsecuredApplicationCreditCheckStatusComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    UnsecuredApplicationCreditCheckStatusComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    UnsecuredApplicationCreditCheckStatusComponentNs.Model = Model;
})(UnsecuredApplicationCreditCheckStatusComponentNs || (UnsecuredApplicationCreditCheckStatusComponentNs = {}));
angular.module('ntech.components').component('unsecuredApplicationCreditCheckStatus', new UnsecuredApplicationCreditCheckStatusComponentNs.UnsecuredApplicationCreditCheckStatusComponent());
