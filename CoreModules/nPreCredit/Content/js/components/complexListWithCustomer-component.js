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
var ComplexListWithCustomerComponentNs;
(function (ComplexListWithCustomerComponentNs) {
    var ComplexListWithCustomerController = /** @class */ (function (_super) {
        __extends(ComplexListWithCustomerController, _super);
        function ComplexListWithCustomerController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.modalDialogService = modalDialogService;
            return _this;
        }
        ComplexListWithCustomerController.prototype.componentName = function () {
            return 'complexListWithCustomer';
        };
        ComplexListWithCustomerController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            var i = this.initialData;
            var a = ApplicationEditorComponentNs.createInitialData(i.ai.ApplicationNr, i.ai.ApplicationType, NavigationTargetHelper.createCodeTarget(i.backTarget), i.apiClient, this.$q, function (x) {
                for (var _i = 0, _a = i.fieldNames; _i < _a.length; _i++) {
                    var fieldName = _a[_i];
                    x.addComplexApplicationListItem("".concat(i.listName, "#").concat(i.listNr, "#u#").concat(fieldName));
                }
            }, {
                isInPlaceEditAllowed: !i.isReadonly,
                afterInPlaceEditsCommited: function (x) {
                    _this.ntechComponentService.emitNTechCustomDataEvent(ComplexListWithCustomerComponentNs.AfterEditEventName, _this.initialData);
                }
            });
            this.m = {
                a: a,
                c: {
                    applicationInfo: i.ai,
                    backUrl: encodeURIComponent(decodeURIComponent(i.urlToHereFromOtherModule)),
                    backTarget: this.initialData.navigationTargetCodeToHere,
                    isEditable: i.ai.IsActive && !i.ai.IsFinalDecisionMade && !i.isReadonly,
                    editorService: new ApplicationCustomerListComponentNs.ComplexApplicationListEditorService(i.ai.ApplicationNr, i.listName, i.listNr, this.apiClient)
                }
            };
        };
        ComplexListWithCustomerController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return ComplexListWithCustomerController;
    }(NTechComponents.NTechComponentControllerBase));
    ComplexListWithCustomerComponentNs.ComplexListWithCustomerController = ComplexListWithCustomerController;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    ComplexListWithCustomerComponentNs.Model = Model;
    ComplexListWithCustomerComponentNs.AfterEditEventName = 'complexListWithCustomerAfterInPlaceEditsCommited';
    var ComplexListWithCustomerComponent = /** @class */ (function () {
        function ComplexListWithCustomerComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ComplexListWithCustomerController;
            this.template = "<div ng-if=\"$ctrl.m\">\n<div class=\"row\">\n    <div class=\"col-sm-5\"><div class=\"editblock\"><application-editor initial-data=\"$ctrl.m.a\"></application-editor></div></div>\n    <div class=\"col-sm-7\"><application-customer-list initial-data=\"$ctrl.m.c\"></application-customer-list></div>\n</div>";
        }
        return ComplexListWithCustomerComponent;
    }());
    ComplexListWithCustomerComponentNs.ComplexListWithCustomerComponent = ComplexListWithCustomerComponent;
})(ComplexListWithCustomerComponentNs || (ComplexListWithCustomerComponentNs = {}));
angular.module('ntech.components').component('complexListWithCustomer', new ComplexListWithCustomerComponentNs.ComplexListWithCustomerComponent());
