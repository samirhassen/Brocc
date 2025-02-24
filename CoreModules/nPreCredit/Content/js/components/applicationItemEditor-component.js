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
var ApplicationItemEditorComponentNs;
(function (ApplicationItemEditorComponentNs) {
    function getItemDisplayValueShared(v, e, parseDecimalOrNull) {
        if (v === '-' || v === ApplicationDataSourceHelper.MissingItemReplacementValue || !v) {
            return null;
        }
        if (e.DataSourceName === 'BankAccountTypeAndNr') {
            var i = v.indexOf('#');
            if (i >= 0) {
                var accountType = v.substr(0, i);
                var accountNr = v.substr(i + 1);
                if (accountType === 'IBANFi') {
                    accountType = 'IBAN';
                }
                else if (accountType === 'BankAccountSe') {
                    accountType = 'regular account';
                }
                else if (accountType === 'BankGiroSe') {
                    accountType = 'bankgiro account';
                }
                else if (accountType == 'PlusGiroSe') {
                    accountType = 'plugiro account';
                }
                return "".concat(accountNr, " (").concat(accountType, ")");
            }
            else {
                return v;
            }
        }
        else {
            var dt = e.DataType;
            if (dt == 'positiveInt' || dt == 'dueDay') {
                return Math.round(parseDecimalOrNull(v));
            }
            else if (dt == 'positiveDecimal') {
                return parseDecimalOrNull(v);
            }
            else if (e.EditorType === 'dropdownRaw') {
                return ApplicationDataEditorComponentNs.getDropdownDisplayValue(v, e);
            }
            else if (e.DataType == "url") {
                if (v.length > 30) {
                    return v.substr(0, 30);
                }
                else {
                    return v;
                }
            }
            else if (e.DataType == 'ibanfi') {
                if (v.length === 18) {
                    return "".concat(v.substr(0, 4), " ").concat(v.substr(4, 4), " ").concat(v.substr(8, 4), " ").concat(v.substr(12, 4), " ").concat(v.substr(16, 2));
                }
                else {
                    return v;
                }
            }
            else if (e.DataType == 'iban') {
                return v;
            }
            else if (e.DataType == 'localDateAndTime') {
                return moment(v).format('YYYY-MM-DD HH:mm');
            }
            else {
                return v;
            }
        }
    }
    ApplicationItemEditorComponentNs.getItemDisplayValueShared = getItemDisplayValueShared;
    var ApplicationItemEditorController = /** @class */ (function (_super) {
        __extends(ApplicationItemEditorController, _super);
        function ApplicationItemEditorController(ntechComponentService, $q, $http) {
            var _this = _super.call(this, ntechComponentService) || this;
            _this.$q = $q;
            _this.$http = $http;
            _this.getPlaceholderStandard = function () {
                var e = _this.e();
                if (!e) {
                    return '';
                }
                var et = e.EditorType;
                var dt = e.DataType;
                if (et == 'text') {
                    if (dt == 'month') {
                        return 'YYYY-MM';
                    }
                    else if (dt == 'date') {
                        return 'YYYY-MM-DD';
                    }
                    else if (dt == 'url') {
                        return 'https://somewhere.example.org/test';
                    }
                    else if (dt == 'dueDay') {
                        return '1-28';
                    }
                }
                return '';
            };
            _this.isValidStandard = function (value) {
                var e = _this.e();
                if (!e) {
                    return false;
                }
                var et = e.EditorType;
                var dt = e.DataType;
                if (et == 'text') {
                    if (dt == 'positiveInt') {
                        return _this.isValidPositiveInt(value);
                    }
                    else if (dt == 'positiveDecimal') {
                        return _this.isValidPositiveDecimal(value);
                    }
                    else if (dt == 'month') {
                        return _this.isValidMonth(value);
                    }
                    else if (dt == 'date') {
                        return _this.isValidDate(value);
                    }
                    else if (dt == 'ibanfi') {
                        return _this.isValidIBANFI(value);
                    }
                    else if (dt == 'iban') {
                        return _this.isValidIBAN(value);
                    }
                    else if (dt == 'string') {
                        return true;
                    }
                    else if (dt == 'url') {
                        return _this.isValidURL(value);
                    }
                    else if (dt == 'dueDay') {
                        if (!_this.isValidPositiveInt(value)) {
                            return false;
                        }
                        var v = _this.parseDecimalOrNull(value);
                        return v >= 1 && v <= 28;
                    }
                }
                return false;
            };
            _this.fieldName = 'n' + NTechComponents.generateUniqueId(6);
            _this.apiClient = new NTechPreCreditApi.ApiClient(function (e) { return toastr.error(e); }, _this.$http, _this.$q);
            return _this;
        }
        ApplicationItemEditorController.prototype.componentName = function () {
            return 'applicationItemEditor';
        };
        ApplicationItemEditorController.prototype.onChanges = function () {
            var _this = this;
            if (!this.m) {
                var em = this.e();
                this.m = {
                    dropdownRawOptions: this.createDropdownRawOptions(),
                    b: em.EditorType === 'bankaccountnr'
                        ? new BankAccountEditor(this.v(), this.$q, this.apiClient, function (x) { return _this.directEditModel[_this.name] = x; }, this.isReadOnly())
                        : null
                };
            }
            else {
                this.m.dropdownRawOptions = this.createDropdownRawOptions();
            }
        };
        ApplicationItemEditorController.prototype.e = function () {
            return this.data && this.data.modelByGroupedName ? this.data.modelByGroupedName[this.name] : null;
        };
        ApplicationItemEditorController.prototype.v = function () {
            var v = this.data && this.data.valueByGroupedName ? this.data.valueByGroupedName[this.name] : null;
            return v === ApplicationDataSourceHelper.MissingItemReplacementValue ? null : v;
        };
        ApplicationItemEditorController.prototype.lbl = function () {
            var e = this.e();
            return e ? e.LabelText : null;
        };
        ApplicationItemEditorController.prototype.isChangeTrackingEnabled = function () {
            return this.enableChangeTracking === 'true' || this.enableChangeTracking === true;
        };
        ApplicationItemEditorController.prototype.isReadOnlyDataType = function (dt) {
            return dt == 'localDateAndTime';
        };
        ApplicationItemEditorController.prototype.isReadOnly = function () {
            var e = this.e();
            return !this.data.isEditAllowed || (e && (e.IsReadonly || this.isReadOnlyDataType(e.DataType)));
        };
        ApplicationItemEditorController.prototype.isDirectEditAllowed = function () {
            return !this.isReadOnly() && (this.directEdit === 'true' || this.directEdit === true);
        };
        ApplicationItemEditorController.prototype.isRequired = function () {
            var e = this.e();
            return e ? e.IsRequired === true : false;
        };
        ApplicationItemEditorController.prototype.getCreditApplicationItemDisplayValue = function () {
            var _this = this;
            return getItemDisplayValueShared(this.v(), this.e(), function (x) { return _this.parseDecimalOrNull(x); });
        };
        ApplicationItemEditorController.prototype.getLabelSize = function () {
            if (!this.labelSize) {
                return 6;
            }
            var n = this.parseDecimalOrNull(this.labelSize);
            if (n && n > 0.5 && n < 12.4) {
                return Math.round(n);
            }
            else {
                return 6;
            }
        };
        ApplicationItemEditorController.prototype.getLabelSizeClass = function () {
            return "col-xs-".concat(this.getLabelSize().toFixed(0));
        };
        ApplicationItemEditorController.prototype.getInputSizeClass = function () {
            return "col-xs-".concat((12 - this.getLabelSize()).toFixed(0));
        };
        ApplicationItemEditorController.prototype.createDropdownRawOptions = function () {
            var em = this.e();
            if (!em || !em.DropdownRawOptions) {
                return null;
            }
            var v = em.DropdownRawOptions;
            var t = em.DropdownRawDisplayTexts;
            if (!t) {
                t = [];
            }
            var r = [];
            for (var i = 0; i < v.length; i++) {
                r.push([v[i], t.length > i ? t[i] : v[i]]);
            }
            return r;
        };
        ApplicationItemEditorController.prototype.isNavigableUrl = function () {
            var e = this.e();
            if (e.DataType != 'url') {
                return false;
            }
            var value = this.v();
            if (value) {
                return this.isValidURL(value);
            }
            return false;
        };
        ApplicationItemEditorController.prototype.isCreditApplicationItemEdited = function () {
            return this.data.isEditedByGroupedName[this.name] === true;
        };
        ApplicationItemEditorController.prototype.getEditApplicationItemUrl = function () {
            if (!this.data) {
                return null;
            }
            var e = this.e();
            if (!e) {
                return null;
            }
            var url = '';
            if (this.data.applicationType === 'mortgageLoan') {
                url += "/Ui/MortgageLoan/EditItem";
            }
            else if (this.data.applicationType === 'companyLoan') {
                url += "/Ui/CompanyLoan/Application/EditItem";
            }
            else {
                throw new Error('Not implemented for unsecuredLoans. Needs a controller host');
            }
            url += "?applicationNr=".concat(this.data.applicationNr, "&dataSourceName=").concat(e.DataSourceName, "&itemName=").concat(encodeURIComponent(this.name), "&ro=").concat(this.data.isEditAllowed ? 'False' : 'True');
            url = NavigationTargetHelper.AppendBackNavigationToUrl(url, this.data.navigationOptionToHere);
            return url;
        };
        ApplicationItemEditorController.prototype.getDirectEditForm = function () {
            if (!this.directEditForm) {
                return null;
            }
            return this.directEditForm();
        };
        ApplicationItemEditorController.prototype.getDirectEditErrorClasses = function () {
            var f = this.getDirectEditForm();
            if (!f) {
                return null;
            }
            var e = this.e();
            if (e.EditorType === 'bankaccountnr') {
                var field = f[this.fieldName];
                return {
                    'has-error': !this.m.b.validBankAccountInfo && field.$viewValue && !field.$pending,
                    'has-success': this.m.b.validBankAccountInfo && !field.$pending
                };
            }
            else {
                var field = f[this.fieldName];
                return {
                    'has-error': field && field.$invalid,
                    'has-success': field && field.$dirty && field.$valid
                };
            }
        };
        //Others are just a text input with custom validation and placeholder
        ApplicationItemEditorController.prototype.getEditorTemplate = function () {
            var e = this.e();
            if (!e) {
                return null;
            }
            if (e.EditorType === 'dropdownRaw' && e.DataType === 'string') {
                return 'dropdown';
            }
            else if (e.EditorType === 'bankaccountnr') {
                return 'bankaccountnr';
            }
            return 'standard';
        };
        ApplicationItemEditorController.$inject = ['ntechComponentService', '$q', '$http'];
        return ApplicationItemEditorController;
    }(NTechComponents.NTechComponentControllerBaseTemplate));
    ApplicationItemEditorComponentNs.ApplicationItemEditorController = ApplicationItemEditorController;
    var ApplicationItemEditorComponent = /** @class */ (function () {
        function ApplicationItemEditorComponent() {
            this.bindings = {
                name: '<',
                data: '<',
                directEdit: '<',
                directEditForm: '<',
                directEditModel: '<',
                labelSize: '<',
                enableChangeTracking: '<'
            };
            this.controller = ApplicationItemEditorController;
            var labelTemplate = "<label ng-if=\"!$ctrl.getEditApplicationItemUrl()\" class=\"{{$ctrl.getLabelSizeClass()}} control-label\">{{$ctrl.lbl()}}</label>\n                            <label ng-if=\"$ctrl.getEditApplicationItemUrl()\" class=\"{{$ctrl.getLabelSizeClass()}} control-label\"><a class=\"pull-right n-anchor-neutral\" ng-href=\"{{$ctrl.getEditApplicationItemUrl()}}\">{{$ctrl.lbl()}}</a></label>";
            var inPlaceEditValueTemplate = "\n                            <div class=\"{{$ctrl.getInputSizeClass()}}\" ng-class=\"$ctrl.getDirectEditErrorClasses()\" ng-if=\"$ctrl.isDirectEditAllowed()\">\n                                    <input name=\"{{$ctrl.fieldName}}\" ng-if=\"$ctrl.getEditorTemplate() === 'standard'\" type=\"text\" class=\"form-control\" custom-validate=\"$ctrl.isValidStandard\" ng-model=\"$ctrl.directEditModel[$ctrl.name]\" placeholder=\"{{$ctrl.getPlaceholderStandard()}}\" ng-required=\"$ctrl.isRequired()\">\n\n                                    <select name=\"{{$ctrl.fieldName}}\" ng-if=\"$ctrl.getEditorTemplate() === 'dropdown'\" class=\"form-control\" ng-model=\"$ctrl.directEditModel[$ctrl.name]\"  ng-required=\"$ctrl.isRequired()\">\n                                        <option value=\"\" translate=\"valj\" ng-hide=\"$ctrl.directEditModel[$ctrl.name]\">None</option>\n                                        <option value=\"{{p[0]}}\" ng-repeat=\"p in $ctrl.m.dropdownRawOptions\">{{p[1]}}</option>\n                                    </select>\n\n                                    <div ng-if=\"$ctrl.getEditorTemplate() ==='bankaccountnr'\">\n                                        <div class=\"pb-1\">\n                                            <label style=\"padding-top:7px;\">Account type</label>\n                                            <select name=\"{{$ctrl.fieldName + 'bankAccountNrType'}}\" class=\"form-control\" ng-model=\"$ctrl.m.b.bankAccountNrType\" ng-change=\" $ctrl.m.b.onTypeChanged()\">\n                                                <option ng-repeat=\"b in $ctrl.m.b.bankAccountNrTypes\" value=\"{{b.code}}\">{{b.text}}</option>\n                                            </select>\n                                        </div>\n                                        <label>\n                                            {{$ctrl.m.b.getAccountNrFieldLabel($ctrl.m.b.bankAccountNrType)}}\n                                        </label>\n                                        <input type=\"text\"\n                                               class=\"form-control\"\n                                               autocomplete=\"off\"\n                                               ng-model=\"$ctrl.m.b.bankAccountNr\"\n                                               name=\"{{$ctrl.fieldName}}\"\n                                               custom-validate-async=\"$ctrl.m.b.isValidBankAccount\"\n                                               ng-model-options=\"{ updateOn: 'default blur', debounce: {'default': 300, 'blur': 0} }\"\n                                               required placeholder=\"{{$ctrl.m.b.getAccountNrMask()}}\">\n\n                                        <div ng-if=\"$ctrl.m.b.validBankAccountInfo\">\n                                            <hr style=\"border-color: #fff;\" />\n                                            {{$ctrl.m.b.validBankAccountInfo.displayValue}}\n                                            <hr style=\"border-color: #fff;\" />\n                                        </div>\n                                        <div ng-if=\"!$ctrl.m.b.validBankAccountInfo && $ctrl.getDirectEditForm()[$ctrl.fieldName].$viewValue && !$ctrl.getDirectEditForm()[$ctrl.fieldName].$pending\">\n                                            Invalid\n                                        </div>\n                                        <div ng-if=\"$ctrl.getDirectEditForm()[$ctrl.fieldName].$pending\">...</div>\n\n                                    </div>\n                                </div>";
            var readOnlyValueTemplate = "<div class=\"{{$ctrl.getInputSizeClass()}}\" ng-if=\"!$ctrl.isDirectEditAllowed()\">\n                                <p class=\"form-control-static\" ng-if=\"$ctrl.isNavigableUrl()\" style=\"border-bottom:solid 1px\">\n                                    <a ng-href=\"{{$ctrl.v()}}\" target=\"_blank\" class=\"n-anchor n-longer\">\n                                        {{$ctrl.getCreditApplicationItemDisplayValue()}}\n                                        <span class=\"pull-right n-star\" ng-show=\"$ctrl.isCreditApplicationItemEdited() && $ctrl.isChangeTrackingEnabled()\">*</span>\n                                    </a>\n                                </p>\n                                <p class=\"form-control-static\" ng-if=\"!$ctrl.isNavigableUrl()\" style=\"border-bottom:solid 1px\">\n                                    <span ng-if=\"$ctrl.getCreditApplicationItemDisplayValue()\">{{$ctrl.getCreditApplicationItemDisplayValue()}}</span>\n                                    <span ng-if=\"!$ctrl.getCreditApplicationItemDisplayValue()\">&nbsp;</span> <!-- Prevent collapsing to no height -->\n                                    <span class=\"pull-right n-star\" ng-show=\"$ctrl.isCreditApplicationItemEdited() && $ctrl.isChangeTrackingEnabled()\">*</span>\n                                </p>\n                            </div>";
            this.template = "<div class=\"form-group\">\n\n                            ".concat(labelTemplate, "\n\n                            ").concat(readOnlyValueTemplate, "\n\n                            ").concat(inPlaceEditValueTemplate, "\n                        </div>");
        }
        return ApplicationItemEditorComponent;
    }());
    ApplicationItemEditorComponentNs.ApplicationItemEditorComponent = ApplicationItemEditorComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    ApplicationItemEditorComponentNs.Model = Model;
    var DataModel = /** @class */ (function () {
        function DataModel() {
        }
        return DataModel;
    }());
    ApplicationItemEditorComponentNs.DataModel = DataModel;
    function createDataModelUsingDataSourceResult(applicationNr, applicationType, isEditAllowed, navigationOptionToHere, r) {
        var valueByGroupedName = {};
        var isEditedByGroupedName = {};
        var modelByGroupedName = {};
        for (var _i = 0, _a = r.Items; _i < _a.length; _i++) {
            var i = _a[_i];
            valueByGroupedName[i.Name] = i.Value;
            modelByGroupedName[i.Name] = i.EditorModel;
            isEditedByGroupedName[i.Name] = false;
        }
        for (var _b = 0, _c = r.ChangedNames; _b < _c.length; _b++) {
            var e = _c[_b];
            isEditedByGroupedName[e] = true;
        }
        return {
            applicationNr: applicationNr,
            applicationType: applicationType,
            isEditAllowed: isEditAllowed,
            modelByGroupedName: modelByGroupedName,
            navigationOptionToHere: navigationOptionToHere,
            valueByGroupedName: valueByGroupedName,
            isEditedByGroupedName: isEditedByGroupedName
        };
    }
    ApplicationItemEditorComponentNs.createDataModelUsingDataSourceResult = createDataModelUsingDataSourceResult;
    var BankAccountEditor = /** @class */ (function () {
        function BankAccountEditor(initialValue, $q, apiClient, updateValue, isReadOnly) {
            var _this = this;
            this.$q = $q;
            this.apiClient = apiClient;
            this.updateValue = updateValue;
            this.isReadOnly = isReadOnly;
            this.isValidBankAccount = function (input) {
                var deferred = _this.$q.defer();
                _this.apiClient.isValidAccountNr(input, _this.bankAccountNrType).then(function (x) {
                    if (x.isValid) {
                        deferred.resolve(input);
                        _this.validBankAccountInfo = {
                            displayValue: x.displayValue
                        };
                        _this.updateValue("".concat(_this.bankAccountNrType, "#").concat(x.normalizedValue));
                    }
                    else {
                        deferred.reject(x.message);
                        _this.validBankAccountInfo = null;
                    }
                });
                return deferred.promise;
            };
            this.onTypeChanged = function () {
                _this.bankAccountNr = '';
                _this.validBankAccountInfo = null;
            };
            this.bankAccountNrTypes = [];
            if (ntechClientCountry === 'FI') {
                this.bankAccountNrTypes.push({ code: 'IBANFi', text: 'IBAN' });
            }
            else if (ntechClientCountry === 'SE') {
                this.bankAccountNrTypes.push({ code: 'BankAccountSe', text: 'Bank account nr' });
                this.bankAccountNrTypes.push({ code: 'BankGiroSe', text: 'Bankgiro nr' });
                this.bankAccountNrTypes.push({ code: 'PlusGiroSe', text: 'Plusgiro nr' });
            }
            if (initialValue) {
                var values = initialValue.split('#');
                this.bankAccountNrType = values[0];
                this.bankAccountNr = values[1];
                if (this.isReadOnly) {
                    //Populate validBankAccountInfo
                    this.isValidBankAccount(this.bankAccountNr);
                }
            }
        }
        BankAccountEditor.prototype.getAccountNrFieldLabel = function (nrType) {
            if (!this.bankAccountNrTypes) {
                return nrType;
            }
            for (var _i = 0, _a = this.bankAccountNrTypes; _i < _a.length; _i++) {
                var t = _a[_i];
                if (t.code === nrType) {
                    return t.text;
                }
            }
            return nrType;
        };
        BankAccountEditor.prototype.getAccountNrMask = function () {
            return 'Account nr';
        };
        return BankAccountEditor;
    }());
    ApplicationItemEditorComponentNs.BankAccountEditor = BankAccountEditor;
})(ApplicationItemEditorComponentNs || (ApplicationItemEditorComponentNs = {}));
angular.module('ntech.components').component('applicationItemEditor', new ApplicationItemEditorComponentNs.ApplicationItemEditorComponent());
