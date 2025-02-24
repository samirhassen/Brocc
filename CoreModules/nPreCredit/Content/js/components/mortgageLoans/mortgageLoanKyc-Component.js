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
var MortgageLoanApplicationKycComponentNs;
(function (MortgageLoanApplicationKycComponentNs) {
    var MortgageApplicationRawController = /** @class */ (function (_super) {
        __extends(MortgageApplicationRawController, _super);
        function MortgageApplicationRawController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.modalDialogService = modalDialogService;
            return _this;
        }
        MortgageApplicationRawController.prototype.componentName = function () {
            return 'mortgageLoanApplicationKyc';
        };
        MortgageApplicationRawController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData || !this.initialData.applicationInfo) {
                return;
            }
            var ai = this.initialData.applicationInfo;
            var wf = this.initialData.workflowModel;
            var isWaitingForPreviousSteps = !wf.areAllStepBeforeThisAccepted(ai);
            var isReadOnly = isWaitingForPreviousSteps || !ai.IsActive || ai.IsFinalDecisionMade || ai.HasLockedAgreement;
            var isReadOnlyScreening = !ai.IsActive || ai.IsFinalDecisionMade || ai.HasLockedAgreement;
            if (ai.IsCancelled) {
                this.m = {
                    isReadOnly: isReadOnly,
                    isWaitingForPreviousSteps: isWaitingForPreviousSteps,
                    isStepAccepted: wf.isStatusAccepted(ai),
                    status: null
                };
            }
            else {
                MortgageLoanDualCustomerRoleHelperNs.getApplicationCustomerRolesByCustomerId(ai.ApplicationNr, this.apiClient).then(function (x) {
                    var customerIds = x.customerIds;
                    var listCustomers = x.rolesByCustomerId;
                    _this.apiClient.fetchCustomerOnboardingStatuses(customerIds).then(function (kycStatus) {
                        var m = {
                            isWaitingForPreviousSteps: isWaitingForPreviousSteps,
                            isReadOnly: isReadOnly,
                            isStepAccepted: wf.isStatusAccepted(ai),
                            status: {
                                pepSanctionsCustomers: [],
                                isListScreeningDone: true,
                                isListScreeningPossible: false,
                                isToggleAcceptedAllowed: false
                            }
                        };
                        for (var _i = 0, customerIds_1 = customerIds; _i < customerIds_1.length; _i++) {
                            var customerId = customerIds_1[_i];
                            var customerStatus = kycStatus[customerId];
                            if (!customerStatus.LatestScreeningDate) {
                                m.status.isListScreeningDone = false;
                            }
                            var isPepSanctionDone = NTechBooleans.isExactlyTrueOrFalse(customerStatus.IsPep) && NTechBooleans.isExactlyTrueOrFalse(customerStatus.IsSanction);
                            m.status.pepSanctionsCustomers.push({
                                birthDate: x.firstNameAndBirthDateByCustomerId[customerId]['birthDate'],
                                firstName: x.firstNameAndBirthDateByCustomerId[customerId]['firstName'],
                                customerId: customerId,
                                isAccepted: isPepSanctionDone && customerStatus.IsSanction === false,
                                isRejected: isPepSanctionDone && customerStatus.IsSanction === true,
                                roles: listCustomers[customerId],
                                wasScreened: !!customerStatus.LatestScreeningDate
                            });
                        }
                        m.status.isListScreeningPossible = !m.status.isListScreeningDone && !isReadOnlyScreening;
                        m.status.isToggleAcceptedAllowed = !isReadOnly && !isWaitingForPreviousSteps && wf.areAllStepsAfterInitial(ai)
                            && m.status.isListScreeningDone
                            && NTechLinq.all(m.status.pepSanctionsCustomers, function (x) { return x.isAccepted; });
                        _this.m = m;
                    });
                });
            }
        };
        MortgageApplicationRawController.prototype.glyphIconClassFromBoolean = function (isAccepted, isRejected) {
            return ApplicationStatusBlockComponentNs.getIconClass(isAccepted, isRejected);
        };
        MortgageApplicationRawController.prototype.getCustomerKycManagementUrl = function (customerId) {
            if (!customerId || !this.initialData) {
                return null;
            }
            return this.getUiGatewayUrl('nCustomer', 'Ui/KycManagement/Manage', [
                ['customerId', customerId.toString()],
                ['backTarget', this.initialData.navigationTargetCodeToHere]
            ]);
        };
        MortgageApplicationRawController.prototype.toggleStepAccepted = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var i = this.initialData;
            var ai = i.applicationInfo;
            var wf = this.initialData.workflowModel;
            this.initialData.apiClient.setMortgageApplicationWorkflowStatus(ai.ApplicationNr, wf.stepName, wf.isStatusAccepted(ai) ? 'Initial' : 'Accepted').then(function () {
                _this.signalReloadRequired();
            });
        };
        MortgageApplicationRawController.prototype.screenNow = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var customerIds = NTechLinq.select(NTechLinq.where(this.m.status.pepSanctionsCustomers, function (x) { return !x.wasScreened; }), function (x) { return x.customerId; });
            if (customerIds.length == 0) {
                return;
            }
            this.apiClient.kycScreenBatch(customerIds, moment(this.initialData.today).toDate()).then(function (_) {
                _this.signalReloadRequired();
            });
        };
        MortgageApplicationRawController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return MortgageApplicationRawController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanApplicationKycComponentNs.MortgageApplicationRawController = MortgageApplicationRawController;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageLoanApplicationKycComponentNs.Model = Model;
    var MortgageLoanApplicationKycComponent = /** @class */ (function () {
        function MortgageLoanApplicationKycComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationRawController;
            this.template = "<div ng-if=\"$ctrl.m\">\n\n<div ng-if=\"$ctrl.m.status\">\n    <table class=\"table\">\n        <thead>\n            <tr>\n                <th class=\"col-xs-2\">Control</th>\n                <th class=\"col-xs-1\">Status</th>\n                <th class=\"col-xs-6\"></th>\n                <th class=\"col-xs-3 text-right\">Action</th>\n            </tr>\n        </thead>\n        <tbody>\n            <tr>\n                <td>List screening</td>\n                <td><span class=\"glyphicon\" ng-class=\"{{$ctrl.glyphIconClassFromBoolean($ctrl.m.status.isListScreeningDone, false)}}\"></span></td>\n                <td></td>\n                <td class=\"text-right\">\n                    <button ng-if=\"$ctrl.m.status.isListScreeningPossible\" ng-click=\"$ctrl.screenNow($event)\" class=\"n-direct-btn n-green-btn\">Screen now</button>\n                </td>\n            </tr>\n            <tr ng-repeat=\"c in $ctrl.m.status.pepSanctionsCustomers\">\n                <td>PEP &amp; Sanction</td>\n                <td><span class=\"glyphicon\" ng-class=\"{{$ctrl.glyphIconClassFromBoolean(c.isAccepted, c.isRejected)}}\"></span></td>\n                <td>{{c.firstName}}, {{c.birthDate}} (<span ng-repeat=\"r in c.roles\" class=\"comma\">{{r}}</span>)</td>\n                <td class=\"text-right\">\n                    <a class=\"n-anchor\" ng-href=\"{{$ctrl.getCustomerKycManagementUrl(c.customerId)}}\">View details</a>\n                </td>\n            </tr>\n        </tbody>\n    </table>\n</div>\n\n<div class=\"pt-3\" ng-show=\"$ctrl.m.status.isToggleAcceptedAllowed\">\n    <label class=\"pr-2\">Kyc {{$ctrl.m.isStepAccepted ? 'done' : 'not done'}}</label>\n    <label class=\"n-toggle\">\n        <input type=\"checkbox\" ng-checked=\"$ctrl.m.isStepAccepted\" ng-click=\"$ctrl.toggleStepAccepted($event)\" />\n        <span class=\"n-slider\"></span>\n    </label>\n</div>\n</div>";
        }
        return MortgageLoanApplicationKycComponent;
    }());
    MortgageLoanApplicationKycComponentNs.MortgageLoanApplicationKycComponent = MortgageLoanApplicationKycComponent;
})(MortgageLoanApplicationKycComponentNs || (MortgageLoanApplicationKycComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanApplicationKyc', new MortgageLoanApplicationKycComponentNs.MortgageLoanApplicationKycComponent());
