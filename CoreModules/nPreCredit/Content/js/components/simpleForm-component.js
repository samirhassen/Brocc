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
var SimpleFormComponentNs;
(function (SimpleFormComponentNs) {
    var SimpleFormController = /** @class */ (function (_super) {
        __extends(SimpleFormController, _super);
        function SimpleFormController($http, $q, $scope, ntechComponentService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$scope = $scope;
            _this.formName = 'f' + NTechComponents.generateUniqueId(6);
            return _this;
        }
        SimpleFormController.prototype.componentName = function () {
            return 'simpleForm';
        };
        SimpleFormController.prototype.onChanges = function () {
        };
        SimpleFormController.prototype.items = function () {
            if (this.initialData) {
                return this.initialData.items;
            }
            return [];
        };
        SimpleFormController.prototype.model = function (i) {
            return this.initialData.modelBase[i.modelPropertyName];
        };
        SimpleFormController.prototype.onClick = function (i, evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (i.onClick) {
                i.onClick();
            }
        };
        SimpleFormController.prototype.form = function () {
            return this.$scope[this.formName];
        };
        SimpleFormController.prototype.isFormInvalid = function () {
            return !this.form() || this.form().$invalid;
        };
        SimpleFormController.$inject = ['$http', '$q', '$scope', 'ntechComponentService'];
        return SimpleFormController;
    }(NTechComponents.NTechComponentControllerBase));
    SimpleFormComponentNs.SimpleFormController = SimpleFormController;
    var SimpleFormComponent = /** @class */ (function () {
        function SimpleFormComponent() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = SimpleFormController;
            this.templateUrl = 'simple-form.html';
        }
        return SimpleFormComponent;
    }());
    SimpleFormComponentNs.SimpleFormComponent = SimpleFormComponent;
    var SimpleFormItemType;
    (function (SimpleFormItemType) {
        SimpleFormItemType["TextField"] = "input";
        SimpleFormItemType["Button"] = "button";
        SimpleFormItemType["TextView"] = "textview";
    })(SimpleFormItemType = SimpleFormComponentNs.SimpleFormItemType || (SimpleFormComponentNs.SimpleFormItemType = {}));
    var ButtonType;
    (function (ButtonType) {
        ButtonType["NotAButton"] = "notAButton";
        ButtonType["Default"] = "defaultButton";
        ButtonType["Accept"] = "acceptButton";
    })(ButtonType = SimpleFormComponentNs.ButtonType || (SimpleFormComponentNs.ButtonType = {}));
    var SimpleFormItem = /** @class */ (function () {
        function SimpleFormItem() {
        }
        return SimpleFormItem;
    }());
    SimpleFormComponentNs.SimpleFormItem = SimpleFormItem;
    function textField(opt) {
        return {
            itemType: SimpleFormItemType.TextField,
            labelText: opt.labelText,
            required: !!opt.required,
            modelPropertyName: opt.model,
            onClick: null,
            buttonType: ButtonType.NotAButton
        };
    }
    SimpleFormComponentNs.textField = textField;
    function textView(opt) {
        return {
            itemType: SimpleFormItemType.TextView,
            labelText: opt.labelText,
            required: false,
            modelPropertyName: opt.model,
            onClick: null,
            buttonType: ButtonType.NotAButton
        };
    }
    SimpleFormComponentNs.textView = textView;
    function button(opt) {
        return {
            itemType: SimpleFormItemType.Button,
            labelText: opt.buttonText,
            onClick: opt.onClick,
            required: null,
            modelPropertyName: null,
            buttonType: opt.buttonType || ButtonType.Default
        };
    }
    SimpleFormComponentNs.button = button;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    SimpleFormComponentNs.InitialData = InitialData;
})(SimpleFormComponentNs || (SimpleFormComponentNs = {}));
angular.module('ntech.components').component('simpleForm', new SimpleFormComponentNs.SimpleFormComponent());
