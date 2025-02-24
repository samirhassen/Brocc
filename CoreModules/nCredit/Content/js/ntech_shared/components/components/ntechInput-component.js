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
var NTechInputComponentNs;
(function (NTechInputComponentNs) {
    var NTechInputController = /** @class */ (function (_super) {
        __extends(NTechInputController, _super);
        function NTechInputController($http, $q, $scope, $timeout, $filter, ntechComponentService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$scope = $scope;
            _this.$timeout = $timeout;
            _this.$filter = $filter;
            _this.onPaste = function (evt) {
                if (!(_this.t === 'positivedecimal' || _this.t === 'positiveint' || _this.t === 'moneyint')) {
                    return;
                }
                if (evt && evt.originalEvent && evt.originalEvent.clipboardData) {
                    var et = evt.originalEvent;
                    var text = et.clipboardData.getData('text/plain');
                    var trimmedText = text.replace(/[a-z]|\s/gi, '');
                    if (trimmedText !== text) {
                        evt.preventDefault();
                        _this.model = trimmedText;
                    }
                }
            };
            _this.formName = 'f' + NTechComponents.generateUniqueId(6);
            _this.editInputName = 'i' + NTechComponents.generateUniqueId(6);
            _this.inputtype = 'input';
            _this.rows = 1;
            ntechComponentService.subscribeToNTechEvents(function (evt) {
                if (evt.eventName == 'FocusControlByAlias' && _this.alias && evt.eventData == _this.alias) {
                    _this.$timeout(function () {
                        document.getElementsByName(_this.editInputName)[0].focus();
                    });
                }
            });
            return _this;
        }
        NTechInputController.prototype.componentName = function () {
            return 'ntechInput';
        };
        NTechInputController.prototype.onChanges = function () {
        };
        NTechInputController.prototype.getForm = function () {
            if (this.formName && this.$scope[this.formName]) {
                return this.$scope[this.formName];
            }
            return null;
        };
        NTechInputController.prototype.onEdit = function () {
            var f = this.getForm();
            if (f) {
                f.$setValidity(this.editInputName + 'IsValid', this.isValid(this.model), f);
            }
        };
        NTechInputController.prototype.isValid = function (v) {
            var _this = this;
            var isValidIfOptional = function () {
                if (_this.t == 'positiveint' || _this.t == 'moneyint') {
                    return _this.isValidPositiveInt(v);
                }
                else if (_this.t == 'email') {
                    return _this.isValidEmail(v);
                }
                else if (_this.t == 'phonenr') {
                    return _this.isValidPhoneNr(v);
                }
                else if (_this.t == 'date') {
                    return _this.isValidDate(v);
                }
                else if (_this.t == 'positivedecimal') {
                    return _this.isValidPositiveDecimal(v);
                }
                else if (_this.t == 'custom') {
                    return _this.customIsValid(v);
                }
                else {
                    return true; //type = text or missing
                }
            };
            return isValidIfOptional() && (!this.required || v);
        };
        NTechInputController.prototype.isFormInvalid = function () {
            var f = this.getForm();
            if (!f) {
                return true;
            }
            return (f.$invalid && !f.$pristine);
        };
        NTechInputController.$inject = ['$http', '$q', '$scope', '$timeout', '$filter', 'ntechComponentService'];
        return NTechInputController;
    }(NTechComponents.NTechComponentControllerBase));
    NTechInputComponentNs.NTechInputController = NTechInputController;
    var NTechInputComponent = /** @class */ (function () {
        function NTechInputComponent() {
            this.transclude = true;
            this.bindings = {
                required: '<',
                label: '<',
                model: '=',
                t: '<',
                labelClasses: '<',
                inputClasses: '<',
                groupStyle: '<',
                alias: '<',
                customIsValid: '<',
                placeholderText: '<',
                inputtype: '<',
                rows: '<'
            };
            this.controller = NTechInputController;
            this.template = "<div class=\"form-group ntechinput\" ng-form name=\"{{$ctrl.formName}}\" ng-class=\"{ 'has-error' : $ctrl.isFormInvalid() }\" style=\"{{$ctrl.groupStyle}}\">\n        <label class=\"control-label {{$ctrl.labelClasses}}\" ng-if=\"$ctrl.label\">\n            {{$ctrl.label}}\n        </label>\n        <div class=\"{{$ctrl.inputClasses}}\">\n            <div ng-show=\"!$ctrl.inputtype\">\n                <input type=\"text\" class=\"form-control\" autocomplete=\"off\" placeholder=\"{{$ctrl.placeholderText}}\" name=\"{{$ctrl.editInputName}}\" ng-paste=\"$ctrl.onPaste($event)\" ng-change=\"{{$ctrl.onEdit()}}\" ng-required=\"$ctrl.required\" ng-model=\"$ctrl.model\" />\n            </div>\n            <div ng-show=\"$ctrl.inputtype=='textarea'\">\n                <textarea class=\"form-control\" rows=\"{{$ctrl.rows}}\" placeholder=\"{{$ctrl.placeholderText}}\" name=\"{{$ctrl.editInputName}}\" ng-paste=\"$ctrl.onPaste($event)\" ng-change=\"{{$ctrl.onEdit()}}\" ng-required=\"$ctrl.required\" ng-model=\"$ctrl.model\" />\n            </div>\n        </div>\n        <ng-transclude></ng-transclude>\n                </div>";
        }
        return NTechInputComponent;
    }());
    NTechInputComponentNs.NTechInputComponent = NTechInputComponent;
})(NTechInputComponentNs || (NTechInputComponentNs = {}));
angular.module('ntech.components').component('ntechInput', new NTechInputComponentNs.NTechInputComponent());
