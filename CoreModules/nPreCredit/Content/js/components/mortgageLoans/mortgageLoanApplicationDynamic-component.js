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
var MortgageLoanApplicationDynamicComponentNs;
(function (MortgageLoanApplicationDynamicComponentNs) {
    var MortgageLoanApplicationDynamicController = /** @class */ (function (_super) {
        __extends(MortgageLoanApplicationDynamicController, _super);
        function MortgageLoanApplicationDynamicController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.modalDialogService = modalDialogService;
            _this.ntechComponentService.subscribeToReloadRequired(function () {
                _this.reload();
            });
            return _this;
        }
        MortgageLoanApplicationDynamicController.prototype.componentName = function () {
            return 'mortgageLoanApplicationDynamic';
        };
        MortgageLoanApplicationDynamicController.prototype.cancelApplication = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.cancelApplication(this.m.applicationInfo.ApplicationNr).then(function () {
                _this.signalReloadRequired();
            });
        };
        MortgageLoanApplicationDynamicController.prototype.reactivateApplication = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.reactivateCancelledApplication(this.m.applicationInfo.ApplicationNr).then(function () {
                _this.signalReloadRequired();
            });
        };
        MortgageLoanApplicationDynamicController.prototype.isCancelApplicationAllowed = function () {
            if (!this.m || !this.m.applicationInfo) {
                return false;
            }
            var ai = this.m.applicationInfo;
            return ai.IsActive === true && ai.IsFinalDecisionMade === false && ai.IsWaitingForAdditionalInformation === false;
        };
        MortgageLoanApplicationDynamicController.prototype.isReactivateApplicationAllowed = function () {
            if (!this.m || !this.m.applicationInfo) {
                return false;
            }
            var ai = this.m.applicationInfo;
            return ai.IsActive === false && ai.IsCancelled === true && ai.IsWaitingForAdditionalInformation === false;
        };
        MortgageLoanApplicationDynamicController.prototype.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.handleBack(NavigationTargetHelper.createCodeTarget(this.initialData.backTarget || NavigationTargetHelper.NavigationTargetCode.MortgageLoanSearch), this.apiClient, this.$q, { applicationNr: this.initialData.applicationNr });
        };
        MortgageLoanApplicationDynamicController.prototype.onChanges = function () {
            this.reload();
        };
        MortgageLoanApplicationDynamicController.prototype.reload = function () {
            var _this = this;
            this.m = null;
            this.f = null;
            if (!this.initialData) {
                return;
            }
            this.apiClient.fetchApplicationInfo(this.initialData.applicationNr).then(function (x) {
                var commentsInitialData = {
                    applicationInfo: x,
                    hideAdditionalInfoToggle: true,
                    reloadPageOnWaitingForAdditionalInformation: false
                };
                if (_this.initialData.workflowModel.WorkflowVersion.toString() != x.WorkflowVersion) {
                    _this.f = {
                        fullScreenModeName: 'invalidWorkflowVersion',
                        invalidWorkflowVersionData: {
                            applicationVersion: x.WorkflowVersion ? x.WorkflowVersion : 'Unknown',
                            serverVersion: _this.initialData.workflowModel.WorkflowVersion.toString()
                        },
                    };
                    return;
                }
                var createCustomerInfoInitialData = function (applicantNr) {
                    var d = {
                        applicationNr: x.ApplicationNr,
                        applicantNr: applicantNr,
                        customerIdCompoundItemName: null,
                        backTarget: _this.initialData.navigationTargetCodeToHere
                    };
                    return d;
                };
                var createStepInitialData = function (s) {
                    var d = WorkflowHelper.createInitialData(initialData, {
                        applicationInfo: x,
                        workflowModel: new WorkflowHelper.WorkflowStepModel(_this.initialData.workflowModel, s)
                    });
                    return d;
                };
                var assignedHandlersInitialData = {
                    applicationNr: _this.initialData.applicationNr,
                    hostData: _this.initialData
                };
                _this.m = new Model(x, commentsInitialData, createCustomerInfoInitialData(1), x.NrOfApplicants > 1 ? createCustomerInfoInitialData(2) : null, assignedHandlersInitialData, _this.initialData.workflowModel, createStepInitialData);
            });
        };
        MortgageLoanApplicationDynamicController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return MortgageLoanApplicationDynamicController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanApplicationDynamicComponentNs.MortgageLoanApplicationDynamicController = MortgageLoanApplicationDynamicController;
    var MortgageLoanApplicationDynamicComponent = /** @class */ (function () {
        function MortgageLoanApplicationDynamicComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationDynamicController;
            this.templateUrl = 'mortgage-loan-application-dynamic.html';
        }
        return MortgageLoanApplicationDynamicComponent;
    }());
    MortgageLoanApplicationDynamicComponentNs.MortgageLoanApplicationDynamicComponent = MortgageLoanApplicationDynamicComponent;
    var Model = /** @class */ (function () {
        function Model(applicationInfo, commentsInitialData, applicationCustomerInfo1InitialData, applicationCustomerInfo2InitialData, assignedHandlersInitialData, workflowModel, createStepInitialData) {
            this.applicationInfo = applicationInfo;
            this.commentsInitialData = commentsInitialData;
            this.applicationCustomerInfo1InitialData = applicationCustomerInfo1InitialData;
            this.applicationCustomerInfo2InitialData = applicationCustomerInfo2InitialData;
            this.assignedHandlersInitialData = assignedHandlersInitialData;
            var i = applicationInfo;
            this.statusBlocksInitialData = {};
            this.stepsInitialData = {};
            var hasExpandedStep = false;
            for (var _i = 0, _a = workflowModel.Steps; _i < _a.length; _i++) {
                var s = _a[_i];
                var newStep = {
                    title: s.DisplayName,
                    isActive: i.IsActive,
                    status: WorkflowHelper.isStepAccepted(s.Name, i) ? 'Accepted' : (WorkflowHelper.isStepRejected(s.Name, i) ? 'Rejected' : 'Initial'),
                    isInitiallyExpanded: false
                };
                if (!hasExpandedStep && newStep.status === 'Initial' && i.IsActive) {
                    newStep.isInitiallyExpanded = true;
                    hasExpandedStep = true;
                }
                this.statusBlocksInitialData[s.Name] = newStep;
                this.stepsInitialData[s.Name] = createStepInitialData(s.Name);
            }
        }
        return Model;
    }());
    MortgageLoanApplicationDynamicComponentNs.Model = Model;
    var FullScreenModel = /** @class */ (function () {
        function FullScreenModel() {
        }
        return FullScreenModel;
    }());
    MortgageLoanApplicationDynamicComponentNs.FullScreenModel = FullScreenModel;
})(MortgageLoanApplicationDynamicComponentNs || (MortgageLoanApplicationDynamicComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanApplicationDynamic', new MortgageLoanApplicationDynamicComponentNs.MortgageLoanApplicationDynamicComponent());
