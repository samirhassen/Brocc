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
var NTechWsDocsComponentNs;
(function (NTechWsDocsComponentNs) {
    var NTechWsDocsController = /** @class */ (function (_super) {
        __extends(NTechWsDocsController, _super);
        function NTechWsDocsController($http, $q, $timeout, ntechComponentService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$http = $http;
            _this.$timeout = $timeout;
            _this.filterText = '';
            _this.lb = '\n';
            return _this;
        }
        NTechWsDocsController.prototype.componentName = function () {
            return 'ntechWsDocs';
        };
        NTechWsDocsController.prototype.onChanges = function () {
            this.filterText = '';
        };
        NTechWsDocsController.prototype.getFilteredMethods = function () {
            if (!this.filterText) {
                return this.initialData.methods;
            }
            var methods = [];
            //TODO: Use double metaphone or some other text index. Also maybe index into objects?
            for (var _i = 0, _a = this.initialData.methods; _i < _a.length; _i++) {
                var m = _a[_i];
                if (m.Path.toLowerCase().indexOf(this.filterText.toLowerCase()) >= 0) {
                    methods.push(m);
                }
            }
            return methods;
        };
        NTechWsDocsController.prototype.getTypeDesc = function (t) {
            var b = '';
            b += "".concat(t.Name, ": ") + this.lb;
            for (var _i = 0, _a = t.PrimtiveProperties; _i < _a.length; _i++) {
                var p = _a[_i];
                b += "    ".concat(p.Name, ": ").concat(p.TypeCode).concat((p.IsArray ? "[]" : "")) + this.lb;
            }
            for (var _b = 0, _c = t.CompoundProperties; _b < _c.length; _b++) {
                var p = _c[_b];
                b += "    ".concat(p.Name, ": ").concat(p.Type.Name).concat((p.IsArray ? "[]" : "")) + this.lb;
            }
            return b.substr(0, b.length - 1);
        };
        NTechWsDocsController.prototype.getFullMethodPath = function (m) {
            //TODO: Normalize '/'-es
            var s = location.origin + this.initialData.apiRootPath + '/' + m.Path;
            if (m.Method == 'GET') {
                s = s + m.RequestExample;
            }
            return s;
        };
        NTechWsDocsController.prototype.getSampleHeaders = function (m) {
            var b = '';
            if (m.Method == 'POST') {
                b += 'Content-Type: application/json' + this.lb;
            }
            b += "Authorization: Bearer ".concat(this.initialData.testingToken ? this.initialData.testingToken : '<ACCESS_TOKEN_GOES_HERE>');
            return b;
        };
        NTechWsDocsController.prototype.getMethodType = function (m) {
        };
        NTechWsDocsController.$inject = ['$http', '$q', '$timeout', 'ntechComponentService'];
        return NTechWsDocsController;
    }(NTechComponents.NTechComponentControllerBase));
    NTechWsDocsComponentNs.NTechWsDocsController = NTechWsDocsController;
    var NTechWsDocsComponent = /** @class */ (function () {
        function NTechWsDocsComponent() {
            this.transclude = true;
            this.bindings = {
                initialData: '<',
            };
            this.controller = NTechWsDocsController;
            this.template = "<div>\n                <div class=\"pt-1 pb-2\">\n                    <div class=\"pull-left\"><a class=\"n-back\" ng-if=\"$ctrl.initialData.whiteListedReturnUrl\" ng-href=\"{{$ctrl.initialData.whiteListedReturnUrl}}\"><span class=\"glyphicon glyphicon-arrow-left\"></span></a></div>\n                    <h1 class=\"adjusted\" ng-class=\"\">Api documentation</h1>\n                </div>\n                <div class=\"pt-2\">\n                    <form>\n                        <div class=\"form-group\">\n                            <label>Filter methods</label>\n                            <input type=\"text\" class=\"form-control\" placeholder=\"Name part\" ng-model=\"$ctrl.filterText\">\n                        </div>\n                    </form>                \n                </div>\n                <hr>\n                <div ng-repeat=\"m in $ctrl.getFilteredMethods() track by $index\" class=\"pt-2\">\n                    <h2><span ng-click=\"m.isExpanded = !m.isExpanded\" class=\"glyphicon\" ng-class=\"{ 'chevron-bg glyphicon-chevron-down' : m.isExpanded, 'chevron-bg glyphicon-chevron-right' : !m.isExpanded }\"></span><span class=\"copyable\">{{m.Path}}</span></h2>\n                    <div ng-if=\"m.isExpanded\">\n                        <div>\n                            <h3>Request template</h3>\n                            <div>\n                                <pre class=\"copyable col-xs-1\" style=\"overflow:hidden; white-space: pre;\">{{m.Method}}</pre>\n                                <pre class=\"copyable col-xs-11\" style=\"overflow:hidden; white-space: pre\">{{$ctrl.getFullMethodPath(m)}}</pre>\n                            </div>\n                            <pre class=\"copyable\" style=\"overflow:hidden; white-space: pre\">{{$ctrl.getSampleHeaders(m)}}</pre>\n                            <pre class=\"copyable\" ng-if=\"m.Method == 'POST'\">{{m.RequestExample}}</pre>\n                            <h3>Response template</h3>\n                            <pre class=\"copyable\">{{m.ResponseExample}}</pre>                        \n                        </div>\n                        <div>\n                            <h3>Types</h3>                            \n                            <pre>{{$ctrl.getTypeDesc(m.RequestType)}}</pre>\n                            <pre ng-if=\"m.ResponseType.Name != 'FileResponseType'\">{{$ctrl.getTypeDesc(m.ResponseType)}}</pre>\n                            <pre ng-repeat=\"t in m.OtherTypes track by $index\">{{$ctrl.getTypeDesc(t)}}</pre>                        \n                        </div>\n                    </div>\n                </div>            \n            </div>";
        }
        return NTechWsDocsComponent;
    }());
    NTechWsDocsComponentNs.NTechWsDocsComponent = NTechWsDocsComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    NTechWsDocsComponentNs.InitialData = InitialData;
    var ServiceMethodDocumentation = /** @class */ (function () {
        function ServiceMethodDocumentation() {
        }
        return ServiceMethodDocumentation;
    }());
    NTechWsDocsComponentNs.ServiceMethodDocumentation = ServiceMethodDocumentation;
    var CompoundType = /** @class */ (function () {
        function CompoundType() {
        }
        return CompoundType;
    }());
    NTechWsDocsComponentNs.CompoundType = CompoundType;
    var PrimtiveProperty = /** @class */ (function () {
        function PrimtiveProperty() {
        }
        return PrimtiveProperty;
    }());
    NTechWsDocsComponentNs.PrimtiveProperty = PrimtiveProperty;
    var CompoundProperty = /** @class */ (function () {
        function CompoundProperty() {
        }
        return CompoundProperty;
    }());
    NTechWsDocsComponentNs.CompoundProperty = CompoundProperty;
})(NTechWsDocsComponentNs || (NTechWsDocsComponentNs = {}));
angular.module('ntech.components').component('ntechWsDocs', new NTechWsDocsComponentNs.NTechWsDocsComponent());
