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
var ApplicationAssignedHandlersComponentNs;
(function (ApplicationAssignedHandlersComponentNs) {
    var ApplicationAssignedHandlersController = /** @class */ (function (_super) {
        __extends(ApplicationAssignedHandlersController, _super);
        function ApplicationAssignedHandlersController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        ApplicationAssignedHandlersController.prototype.componentName = function () {
            return 'applicationAssignedHandlers';
        };
        ApplicationAssignedHandlersController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.apiClient.fetchApplicationAssignedHandlers({
                applicationNr: this.initialData.applicationNr,
                returnPossibleHandlers: true,
                returnAssignedHandlers: true
            }).then(function (x) {
                var m = new Model(false, x.PossibleHandlers);
                m.setAssignedHandlers(x.AssignedHandlers);
                _this.m = m;
            });
        };
        ApplicationAssignedHandlersController.prototype.toggleExpanded = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.m.isExpanded = !this.m.isExpanded;
        };
        ApplicationAssignedHandlersController.prototype.removeAssignedHandler = function (h, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.setApplicationAssignedHandlers(this.initialData.applicationNr, null, [h.UserId]).then(function (x) {
                _this.m.setAssignedHandlers(x.AllAssignedHandlers);
            });
        };
        ApplicationAssignedHandlersController.prototype.beginEdit = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.m.isAddUserMode = true;
        };
        ApplicationAssignedHandlersController.prototype.commitEdit = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m.selectedAddHandlerUserId) {
                return;
            }
            this.apiClient.setApplicationAssignedHandlers(this.initialData.applicationNr, [parseInt(this.m.selectedAddHandlerUserId)], null).then(function (x) {
                _this.m.setAssignedHandlers(x.AllAssignedHandlers);
                _this.m.selectedAddHandlerUserId = null;
                _this.m.isAddUserMode = false;
            });
        };
        ApplicationAssignedHandlersController.prototype.cancelEdit = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.m.selectedAddHandlerUserId = null;
            this.m.isAddUserMode = false;
        };
        ApplicationAssignedHandlersController.$inject = ['$http', '$q', 'ntechComponentService'];
        return ApplicationAssignedHandlersController;
    }(NTechComponents.NTechComponentControllerBase));
    ApplicationAssignedHandlersComponentNs.ApplicationAssignedHandlersController = ApplicationAssignedHandlersController;
    var ApplicationAssignedHandlersComponent = /** @class */ (function () {
        function ApplicationAssignedHandlersComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ApplicationAssignedHandlersController;
            this.template = "<div ng-if=\"$ctrl.m\">\n<div>\n    <div class=\"row pt-1\">\n        <div class=\"col-xs-2\">\n            <span ng-click=\"$ctrl.toggleExpanded($event)\" class=\"glyphicon\" ng-class=\"{ 'chevron-bg glyphicon-chevron-down' : $ctrl.m.isExpanded, 'chevron-bg glyphicon-chevron-right' : !$ctrl.m.isExpanded }\"></span>\n        </div>\n        <div class=\"col-xs-4 text-right\">\n            <span>Assigned handler</span>\n        </div>\n        <div class=\"col-xs-6\">\n            <span><b>{{ $ctrl.m.firstAssignedHandler ? $ctrl.m.firstAssignedHandler.UserDisplayName : 'None' }}</b></span>\n        </div>\n    </div>\n    <div ng-if=\"$ctrl.m.isExpanded\">\n        <hr class=\"hr-section dotted\">\n        <div>\n            <div class=\"form-horizontal\">\n                <div class=\"form-group pt-1\" ng-repeat=\"h in $ctrl.m.assignedHandlers\">\n                    <div>\n                        <label class=\"control-label col-xs-9\">{{h.UserDisplayName}}</label>\n                        <div class=\"col-xs-3\">\n                            <span style=\"float:right\"><button ng-click=\"$ctrl.removeAssignedHandler(h, $event)\" class=\"n-icon-btn n-red-btn\"><span class=\"glyphicon glyphicon-remove\"></span></button></span>                            \n                        </div>\n                    </div>\n                </div>\n                <div class=\"form-group\" ng-if=\"!$ctrl.m.isAddUserMode\">\n                    <div class=\"pt-2\">\n                        <div class=\"col-xs-9\"><a ng-click=\"$ctrl.beginEdit($event)\" class=\"n-icon-btn n-blue-btn pull-right\"><span class=\"glyphicon glyphicon-plus\"></span></a></div>\n                    </div>\n                </div>\n                <div class=\"form-group\" ng-if=\"$ctrl.m.isAddUserMode\">\n                    <div class=\"pt-2\">\n                        <label class=\"col-xs-9\">\n                            <select class=\"form-control\" ng-model=\"$ctrl.m.selectedAddHandlerUserId\">\n                                <option value=\"\">Select user</option>\n                                <option ng-repeat=\"h in $ctrl.m.addableHandlers\" value=\"{{h.UserId}}\">{{h.UserDisplayName}}</option>\n                            </select>\n                        </label>\n                        <div class=\"col-xs-3\">\n                            <span style=\"float:right\">\n                                <button ng-click=\"$ctrl.cancelEdit($event)\" class=\"n-icon-btn n-white-btn\"><span class=\"glyphicon glyphicon-remove\"></span></button> <button ng-click=\"$ctrl.commitEdit($event)\" ng-disabled=\"!$ctrl.m.selectedAddHandlerUserId\" class=\"n-icon-btn n-green-btn\"><span class=\"glyphicon glyphicon-ok\"></span></button>\n                            </span>\n                        </div>\n                    </div>\n                </div>\n            </div>\n        </div>\n    </div>\n</div>\n</div>";
        }
        return ApplicationAssignedHandlersComponent;
    }());
    ApplicationAssignedHandlersComponentNs.ApplicationAssignedHandlersComponent = ApplicationAssignedHandlersComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    ApplicationAssignedHandlersComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model(isExpanded, allPossibleHandlers) {
            this.isExpanded = isExpanded;
            this.allPossibleHandlers = allPossibleHandlers;
        }
        Model.prototype.setAssignedHandlers = function (assignedHandlers) {
            this.assignedHandlers = assignedHandlers ? assignedHandlers : [];
            this.firstAssignedHandler = assignedHandlers && assignedHandlers.length > 0 ? assignedHandlers[0] : null;
            var ah = [];
            var _loop_1 = function (h) {
                if (!NTechLinq.any(this_1.assignedHandlers, function (x) { return x.UserId === h.UserId; })) {
                    ah.push(h);
                }
            };
            var this_1 = this;
            for (var _i = 0, _a = this.allPossibleHandlers; _i < _a.length; _i++) {
                var h = _a[_i];
                _loop_1(h);
            }
            this.addableHandlers = ah;
        };
        return Model;
    }());
    ApplicationAssignedHandlersComponentNs.Model = Model;
})(ApplicationAssignedHandlersComponentNs || (ApplicationAssignedHandlersComponentNs = {}));
angular.module('ntech.components').component('applicationAssignedHandlers', new ApplicationAssignedHandlersComponentNs.ApplicationAssignedHandlersComponent());
