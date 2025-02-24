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
var __assign = (this && this.__assign) || function () {
    __assign = Object.assign || function(t) {
        for (var s, i = 1, n = arguments.length; i < n; i++) {
            s = arguments[i];
            for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p))
                t[p] = s[p];
        }
        return t;
    };
    return __assign.apply(this, arguments);
};
var CompanyLoanApplicationComponentNs;
(function (CompanyLoanApplicationComponentNs) {
    var CompanyLoanApplicationController = /** @class */ (function (_super) {
        __extends(CompanyLoanApplicationController, _super);
        function CompanyLoanApplicationController($http, $q, ntechComponentService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.onBack = function (evt) {
                if (evt) {
                    evt.preventDefault();
                }
                NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, _this.apiClient, _this.$q, { applicationNr: initialData.applicationNr }, NavigationTargetHelper.NavigationTargetCode.CompanyLoanSearch);
            };
            ntechComponentService.subscribeToNTechEvents(function (x) {
                if (x.eventName == 'companyLoanInitialCreditCheckBack' && x.eventData == _this.initialData.applicationNr) {
                    _this.f = null;
                }
                else if (x.eventName == 'companyLoanInitialCreditCheckCompleted' && x.eventData == _this.initialData.applicationNr) {
                    _this.f = null;
                    _this.reload();
                }
            });
            ntechComponentService.subscribeToReloadRequired(function (x) {
                _this.reload();
            });
            return _this;
        }
        CompanyLoanApplicationController.prototype.componentName = function () {
            return 'companyLoanApplication';
        };
        CompanyLoanApplicationController.prototype.onChanges = function () {
            this.reload();
        };
        CompanyLoanApplicationController.prototype.isCancelApplicationAllowed = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m) {
                return false;
            }
            var ai = this.m.applicationInfo;
            return ai.IsActive && !ai.IsCancelled && !ai.IsFinalDecisionMade;
        };
        CompanyLoanApplicationController.prototype.isApproveApplicationAllowed = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m) {
                return false;
            }
            var ai = this.m.applicationInfo;
            var lastStepName = this.initialData.workflowModel.Steps[this.initialData.workflowModel.Steps.length - 1].Name;
            var isAtLastStepOrPartiallyApproved = WorkflowHelper.isStepInitial(lastStepName, ai) || (ai.IsPartiallyApproved && WorkflowHelper.isStepAccepted(lastStepName, ai));
            return ai.IsActive
                && !ai.IsFinalDecisionMade
                && WorkflowHelper.areAllStepBeforeThisAccepted(lastStepName, this.m.workflowStepOrder, ai)
                && isAtLastStepOrPartiallyApproved;
        };
        CompanyLoanApplicationController.prototype.cancelApplication = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.isCancelApplicationAllowed()) {
                return;
            }
            this.apiClient.cancelApplication(this.initialData.applicationNr).then(function () {
                _this.reload();
            });
        };
        CompanyLoanApplicationController.prototype.approveApplication = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.isApproveApplicationAllowed()) {
                return;
            }
            this.initialData.companyLoanApiClient.approveApplication(this.initialData.applicationNr).then(function () {
                _this.reload();
            });
        };
        CompanyLoanApplicationController.prototype.isReactivateApplicationAllowed = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m) {
                return false;
            }
            var ai = this.m.applicationInfo;
            return !ai.IsActive && ai.IsCancelled && !ai.IsFinalDecisionMade;
        };
        CompanyLoanApplicationController.prototype.reactivateApplication = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.isReactivateApplicationAllowed()) {
                return;
            }
            this.apiClient.reactivateCancelledApplication(this.initialData.applicationNr).then(function () {
                _this.reload();
            });
        };
        CompanyLoanApplicationController.prototype.reload = function () {
            var _this = this;
            this.m = null;
            this.f = null;
            if (!this.initialData) {
                return;
            }
            this.apiClient.fetchApplicationInfoWithCustom(this.initialData.applicationNr, false, true).then(function (x) {
                _this.init(x.Info);
            });
        };
        CompanyLoanApplicationController.prototype.init = function (ai) {
            var _this = this;
            if (this.initialData.workflowModel.WorkflowVersion.toString() != ai.WorkflowVersion) {
                this.f = {
                    fullScreenModeName: 'invalidWorkflowVersion',
                    invalidWorkflowVersionData: {
                        applicationVersion: ai.WorkflowVersion ? ai.WorkflowVersion : 'Unknown',
                        serverVersion: this.initialData.workflowModel.WorkflowVersion.toString()
                    },
                    initialCreditCheckViewInitialData: null
                };
                return;
            }
            var createStepInitialData = function (s) {
                var d = __assign({}, _this.initialData);
                d.rejectionReasonToDisplayNameMapping = _this.initialData.rejectionReasonToDisplayNameMapping;
                d.applicationInfo = ai;
                d.step = new WorkflowHelper.WorkflowStepModel(_this.initialData.workflowModel, s);
                return d;
            };
            this.m = new Model(ai, this.initialData.workflowModel, createStepInitialData);
            this.m.commentsInitialData = {
                applicationInfo: ai,
                hideAdditionalInfoToggle: true
            };
            this.m.companyCustomerInitialData = {
                applicationNr: ai.ApplicationNr,
                customerIdCompoundItemName: 'application.companyCustomerId',
                applicantNr: null,
                showKycBlock: false,
                onkycscreendone: null,
                backTarget: this.initialData.navigationTargetCodeToHere
            };
            this.m.applicantCustomerInitialData = {
                applicationNr: ai.ApplicationNr,
                customerIdCompoundItemName: 'application.applicantCustomerId',
                applicantNr: null,
                showKycBlock: false,
                onkycscreendone: null,
                backTarget: this.initialData.navigationTargetCodeToHere
            };
            this.m.checkpointsInitialData = {
                applicationNr: ai.ApplicationNr,
                applicationType: 'companyLoan'
            };
            if (this.initialData.isTest) {
                var tf = this.initialData.testFunctions;
                var testScope = this.initialData.testFunctions.generateUniqueScopeName();
                tf.addLink(testScope, 'Show testemails', '/TestLatestEmails/List');
            }
        };
        CompanyLoanApplicationController.$inject = ['$http', '$q', 'ntechComponentService'];
        return CompanyLoanApplicationController;
    }(NTechComponents.NTechComponentControllerBase));
    CompanyLoanApplicationComponentNs.CompanyLoanApplicationController = CompanyLoanApplicationController;
    var CompanyLoanApplicationComponent = /** @class */ (function () {
        function CompanyLoanApplicationComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanApplicationController;
            this.templateUrl = 'company-loan-application.html';
        }
        return CompanyLoanApplicationComponent;
    }());
    CompanyLoanApplicationComponentNs.CompanyLoanApplicationComponent = CompanyLoanApplicationComponent;
    var FullScreenModel = /** @class */ (function () {
        function FullScreenModel() {
        }
        return FullScreenModel;
    }());
    CompanyLoanApplicationComponentNs.FullScreenModel = FullScreenModel;
    var Model = /** @class */ (function () {
        function Model(applicationInfo, workflowModel, createStepInitialData) {
            this.applicationInfo = applicationInfo;
            this.workflowStepOrder = _.map(workflowModel.Steps, function (x) { return x.Name; });
            var i = applicationInfo;
            this.statusBlocksInitialData = {};
            this.stepsInitialData = {};
            var hasExpandedStep = false;
            for (var _i = 0, _a = workflowModel.Steps; _i < _a.length; _i++) {
                var s = _a[_i];
                if (s.ComponentName) {
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
        }
        return Model;
    }());
    CompanyLoanApplicationComponentNs.Model = Model;
})(CompanyLoanApplicationComponentNs || (CompanyLoanApplicationComponentNs = {}));
angular.module('ntech.components').component('companyLoanApplication', new CompanyLoanApplicationComponentNs.CompanyLoanApplicationComponent());
