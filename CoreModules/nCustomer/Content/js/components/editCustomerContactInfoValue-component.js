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
var EditCustomerContactInfoValueComponentNs;
(function (EditCustomerContactInfoValueComponentNs) {
    var EditCustomerContactInfoValueController = /** @class */ (function (_super) {
        __extends(EditCustomerContactInfoValueController, _super);
        function EditCustomerContactInfoValueController($http, $q, ntechComponentService, $translate) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$translate = $translate;
            return _this;
        }
        EditCustomerContactInfoValueController.prototype.componentName = function () {
            return 'editCustomerContactInfoValue';
        };
        EditCustomerContactInfoValueController.prototype.getLabelText = function () {
            var _a, _b;
            return (_b = (_a = this === null || this === void 0 ? void 0 : this.initialData) === null || _a === void 0 ? void 0 : _a.labelText) !== null && _b !== void 0 ? _b : 'Edit contact information';
        };
        EditCustomerContactInfoValueController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.apiClient.fetchCustomerContactInfoEditValueData(this.initialData.customerId, this.initialData.itemName).then(function (result) {
                _this.m = {
                    editValue: result.currentValue ? result.currentValue.Value : null,
                    templateName: result.templateName,
                    currentValue: result.currentValue,
                    historicalValues: result.historicalValues
                };
            });
        };
        EditCustomerContactInfoValueController.prototype.saveChange = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.initialData || !this.m) {
                return;
            }
            this.apiClient.changeCustomerContactInfoValue(this.initialData.customerId, this.initialData.itemName, this.m.editValue, false).then(function (result) {
                _this.initialData.onClose(null);
            });
        };
        EditCustomerContactInfoValueController.prototype.getInputType = function () {
            if (!this.m) {
                return 'text';
            }
            var t = this.m.templateName;
            if (t === 'Email') {
                return 'email';
            }
            else if (t === 'Phonenr') {
                return 'phonenr';
            }
            else if (t === 'String') {
                return 'text';
            }
            else if (t === 'Date') {
                return 'date';
            }
            return 'text';
        };
        EditCustomerContactInfoValueController.$inject = ['$http', '$q', 'ntechComponentService', '$translate'];
        return EditCustomerContactInfoValueController;
    }(NTechComponents.NTechComponentControllerBase));
    EditCustomerContactInfoValueComponentNs.EditCustomerContactInfoValueController = EditCustomerContactInfoValueController;
    var EditCustomerContactInfoValueComponent = /** @class */ (function () {
        function EditCustomerContactInfoValueComponent() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = EditCustomerContactInfoValueController;
            this.templateUrl = 'edit-customer-contact-info-value.html';
        }
        return EditCustomerContactInfoValueComponent;
    }());
    EditCustomerContactInfoValueComponentNs.EditCustomerContactInfoValueComponent = EditCustomerContactInfoValueComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    EditCustomerContactInfoValueComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    EditCustomerContactInfoValueComponentNs.Model = Model;
})(EditCustomerContactInfoValueComponentNs || (EditCustomerContactInfoValueComponentNs = {}));
angular.module('ntech.components').component('editCustomerContactInfoValue', new EditCustomerContactInfoValueComponentNs.EditCustomerContactInfoValueComponent());
