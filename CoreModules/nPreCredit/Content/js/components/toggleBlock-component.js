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
var ToggleBlockComponentNs;
(function (ToggleBlockComponentNs) {
    var ToggleBlockController = /** @class */ (function (_super) {
        __extends(ToggleBlockController, _super);
        function ToggleBlockController($http, $q, ntechComponentService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.service = new Service(_this);
            ntechComponentService.subscribeToNTechEvents(function (x) {
                if (x.eventName === ExpandEventName && _this.eventId && x.eventData == _this.eventId) {
                    if (!_this.isExpanded) {
                        _this.toggleExpanded(null);
                    }
                }
            });
            return _this;
        }
        ToggleBlockController.prototype.componentName = function () {
            return 'toggleBlock';
        };
        ToggleBlockController.prototype.onChanges = function () {
        };
        ToggleBlockController.prototype.toggleExpanded = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.isExpanded = !this.isExpanded;
            if (this.isExpanded && this.onExpanded) {
                this.onExpanded(this.service);
            }
        };
        ToggleBlockController.$inject = ['$http', '$q', 'ntechComponentService'];
        return ToggleBlockController;
    }(NTechComponents.NTechComponentControllerBase));
    ToggleBlockComponentNs.ToggleBlockController = ToggleBlockController;
    var ToggleBlockComponent = /** @class */ (function () {
        function ToggleBlockComponent() {
            this.transclude = true;
            this.bindings = {
                headerText: '<',
                onExpanded: '<',
                isLocked: '<',
                floatedHeaderText: '<',
                eventId: '<'
            };
            this.controller = ToggleBlockController;
            this.template = "<div class=\"block\">\n        <div class=\"row\" ng-if=\"$ctrl.isLocked && !$ctrl.isExpanded\">\n            <div class=\"col-xs-1\">\n                <span class=\"n-unlock\" ng-click=\"$ctrl.toggleExpanded($event)\"><a href=\"#\"><span class=\"glyphicon glyphicon-chevron-right\"></span><span class=\"glyphicon glyphicon-lock\"></span></a></span>\n            </div>\n            <div class=\"col-xs-11\"><h2>{{$ctrl.headerText}}<span class=\"pull-right\" ng-if=\"$ctrl.floatedHeaderText\">{{$ctrl.floatedHeaderText}}</span></h2></div>\n        </div>\n        <div class=\"row\" ng-if=\"!$ctrl.isLocked || $ctrl.isExpanded\">\n            <div class=\"col-xs-1\">\n                <span ng-click=\"$ctrl.toggleExpanded($event)\" class=\"glyphicon\" ng-class=\"{ 'chevron-bg glyphicon-chevron-down' : $ctrl.isExpanded, 'chevron-bg glyphicon-chevron-right' : !$ctrl.isExpanded }\"></span>\n            </div>\n            <div class=\"col-xs-11\"><h2>{{$ctrl.headerText}}<span class=\"pull-right\" ng-if=\"$ctrl.floatedHeaderText\">{{$ctrl.floatedHeaderText}}</span></h2></div>\n        </div>\n        <div ng-if=\"$ctrl.isExpanded\" style=\"padding-bottom: 70px; border-top:1px solid #000000; margin-top: 3px;\" class=\"pt-2\">\n            <div ng-transclude></div>\n        </div>\n    </div>";
        }
        return ToggleBlockComponent;
    }());
    ToggleBlockComponentNs.ToggleBlockComponent = ToggleBlockComponent;
    var ExpandEventName = "c9f18700-db4e-46ac-8f64-630902cc1a31";
    function EmitExpandEvent(eventId, ntechComponentService) {
        ntechComponentService.emitNTechEvent(ExpandEventName, eventId);
    }
    ToggleBlockComponentNs.EmitExpandEvent = EmitExpandEvent;
    var Service = /** @class */ (function () {
        function Service(ctrl) {
            this.ctrl = ctrl;
        }
        Service.prototype.setLocked = function (isLocked) {
            this.ctrl.isLocked = isLocked;
        };
        return Service;
    }());
    ToggleBlockComponentNs.Service = Service;
})(ToggleBlockComponentNs || (ToggleBlockComponentNs = {}));
angular.module('ntech.components').component('toggleBlock', new ToggleBlockComponentNs.ToggleBlockComponent());
