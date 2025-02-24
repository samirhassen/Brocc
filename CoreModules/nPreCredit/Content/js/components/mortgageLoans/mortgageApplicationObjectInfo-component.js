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
var MortgageApplicationObjectInfoComponentNs;
(function (MortgageApplicationObjectInfoComponentNs) {
    var MortgageApplicationObjectInfoController = /** @class */ (function (_super) {
        __extends(MortgageApplicationObjectInfoController, _super);
        function MortgageApplicationObjectInfoController($http, $q, $filter, $translate, ntechComponentService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$filter = $filter;
            _this.$translate = $translate;
            ntechComponentService.subscribeToNTechEvents(function (evt) {
                if (evt.eventName === 'showMortgageObjectValuationDetails' && _this.initialData && evt.eventData === _this.initialData.applicationInfo.ApplicationNr) {
                    _this.reload(false);
                }
            });
            return _this;
        }
        MortgageApplicationObjectInfoController.prototype.componentName = function () {
            return 'mortgageApplicationObjectInfo';
        };
        MortgageApplicationObjectInfoController.prototype.reload = function (isCompactMode) {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.apiClient.fetchCreditApplicationItemSimple(this.initialData.applicationInfo.ApplicationNr, ['mortageLoanObject.*'], '').then(function (x) {
                var d = {};
                for (var _i = 0, _a = Object.keys(x); _i < _a.length; _i++) {
                    var name_1 = _a[_i];
                    d[name_1.substr('mortageLoanObject.'.length)] = x[name_1];
                }
                _this.m = {
                    isCompactMode: isCompactMode,
                    infoBlockInitialData: new TwoColumnInformationBlockComponentNs.InitialDataFromObjectBuilder(d, null, Object.keys(d), [])
                        .buildInitialData()
                };
            });
        };
        MortgageApplicationObjectInfoController.prototype.onChanges = function () {
            this.reload(true);
        };
        MortgageApplicationObjectInfoController.$inject = ['$http', '$q', '$filter', '$translate', 'ntechComponentService'];
        return MortgageApplicationObjectInfoController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageApplicationObjectInfoComponentNs.MortgageApplicationObjectInfoController = MortgageApplicationObjectInfoController;
    var MortgageApplicationObjectInfoComponent = /** @class */ (function () {
        function MortgageApplicationObjectInfoComponent() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationObjectInfoController;
            this.templateUrl = 'mortgage-application-object-info.html';
        }
        return MortgageApplicationObjectInfoComponent;
    }());
    MortgageApplicationObjectInfoComponentNs.MortgageApplicationObjectInfoComponent = MortgageApplicationObjectInfoComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    MortgageApplicationObjectInfoComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageApplicationObjectInfoComponentNs.Model = Model;
})(MortgageApplicationObjectInfoComponentNs || (MortgageApplicationObjectInfoComponentNs = {}));
angular.module('ntech.components').component('mortgageApplicationObjectInfo', new MortgageApplicationObjectInfoComponentNs.MortgageApplicationObjectInfoComponent());
