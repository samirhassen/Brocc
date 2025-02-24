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
var WorkflowHelper;
(function (WorkflowHelper) {
    WorkflowHelper.InitialName = 'Initial';
    WorkflowHelper.AcceptedName = 'Accepted';
    WorkflowHelper.RejectedName = 'Rejected';
    function isStepAccepted(stepName, ai) {
        if (!ai) {
            return false;
        }
        return ai.ListNames.indexOf(stepName + "_".concat(WorkflowHelper.AcceptedName)) >= 0;
    }
    WorkflowHelper.isStepAccepted = isStepAccepted;
    function isStepRejected(stepName, ai) {
        if (!ai) {
            return false;
        }
        return ai.ListNames.indexOf(stepName + "_".concat(WorkflowHelper.RejectedName)) >= 0;
    }
    WorkflowHelper.isStepRejected = isStepRejected;
    function isStepInitial(stepName, ai) {
        if (!ai) {
            return false;
        }
        return !isStepAccepted(stepName, ai) && !isStepRejected(stepName, ai);
    }
    WorkflowHelper.isStepInitial = isStepInitial;
    function areAllStepBeforeThisAccepted(stepName, allStepNames, ai) {
        for (var _i = 0, allStepNames_1 = allStepNames; _i < allStepNames_1.length; _i++) {
            var s = allStepNames_1[_i];
            if (s === stepName) {
                return true;
            }
            else if (!isStepAccepted(s, ai)) {
                return false;
            }
        }
        throw new Error('The step does not exist in the workflow: ' + stepName);
    }
    WorkflowHelper.areAllStepBeforeThisAccepted = areAllStepBeforeThisAccepted;
    function getStepStatus(stepName, ai) {
        return isStepAccepted(stepName, ai) ? WorkflowHelper.AcceptedName : (isStepRejected(stepName, ai) ? WorkflowHelper.RejectedName : WorkflowHelper.InitialName);
    }
    WorkflowHelper.getStepStatus = getStepStatus;
    function areAllStepsAccepted(allStepNames, ai, forceDoneStepName) {
        for (var _i = 0, allStepNames_2 = allStepNames; _i < allStepNames_2.length; _i++) {
            var sn = allStepNames_2[_i];
            if (!isStepAccepted(sn, ai) && (!forceDoneStepName || sn !== forceDoneStepName)) {
                return false;
            }
        }
        return true;
    }
    WorkflowHelper.areAllStepsAccepted = areAllStepsAccepted;
    function createInitialData(baseData, extData) {
        var b = __assign({}, baseData);
        //{ ...baseData, ...extData } should work but doesnt yet in this version of typescript hence the below hack
        for (var _i = 0, _a = Object.keys(extData); _i < _a.length; _i++) { //The any cast here is just to please the compiler
            var p = _a[_i];
            b[p] = extData[p];
        }
        return b;
    }
    WorkflowHelper.createInitialData = createInitialData;
    function getStepModelByCustomData(serverModel, predicate) {
        for (var _i = 0, _a = serverModel.Steps; _i < _a.length; _i++) {
            var step = _a[_i];
            if (step && step.CustomData && predicate(step.CustomData)) {
                return new WorkflowHelper.WorkflowStepModel(serverModel, step.Name);
            }
        }
        return null;
    }
    WorkflowHelper.getStepModelByCustomData = getStepModelByCustomData;
    var WorkflowStepModel = /** @class */ (function () {
        function WorkflowStepModel(serverModel, stepName) {
            this.serverModel = serverModel;
            this.stepName = stepName;
            this.allStepNames = [];
            for (var _i = 0, _a = serverModel.Steps; _i < _a.length; _i++) {
                var s = _a[_i];
                this.allStepNames.push(s.Name);
                if (s.Name == stepName) {
                    this.currentStep = s;
                }
            }
        }
        WorkflowStepModel.prototype.areAllStepBeforeThisAccepted = function (ai) {
            return WorkflowHelper.areAllStepBeforeThisAccepted(this.stepName, this.allStepNames, ai);
        };
        WorkflowStepModel.prototype.getStepStatus = function (ai) {
            return WorkflowHelper.getStepStatus(this.stepName, ai);
        };
        WorkflowStepModel.prototype.isStatusAccepted = function (ai) {
            return WorkflowHelper.isStepAccepted(this.stepName, ai);
        };
        WorkflowStepModel.prototype.areAllStepsAfterInitial = function (ai) {
            var passed = false;
            for (var _i = 0, _a = this.allStepNames; _i < _a.length; _i++) {
                var stepName = _a[_i];
                if (this.currentStep.Name == stepName) {
                    passed = true;
                }
                else if (passed && !WorkflowHelper.isStepInitial(stepName, ai)) {
                    return false;
                }
            }
            return true;
        };
        WorkflowStepModel.prototype.isStatusRejected = function (ai) {
            return WorkflowHelper.isStepRejected(this.stepName, ai);
        };
        WorkflowStepModel.prototype.getAcceptedListName = function () {
            return "".concat(this.stepName, "_").concat(WorkflowHelper.AcceptedName);
        };
        WorkflowStepModel.prototype.getRejectedListName = function () {
            return "".concat(this.stepName, "_").concat(WorkflowHelper.RejectedName);
        };
        WorkflowStepModel.prototype.getInitialListName = function () {
            return "".concat(this.stepName, "_").concat(WorkflowHelper.InitialName);
        };
        WorkflowStepModel.prototype.isStatusInitial = function (ai) {
            return WorkflowHelper.isStepInitial(this.stepName, ai);
        };
        WorkflowStepModel.prototype.getCustomStepData = function () {
            return this.currentStep.CustomData;
        };
        return WorkflowStepModel;
    }());
    WorkflowHelper.WorkflowStepModel = WorkflowStepModel;
})(WorkflowHelper || (WorkflowHelper = {}));
