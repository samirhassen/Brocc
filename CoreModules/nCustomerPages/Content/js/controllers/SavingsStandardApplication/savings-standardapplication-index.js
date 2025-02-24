var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms']);
ntech.angular.setupTranslation(app);
var SavingsAccountApplicationCtr = /** @class */ (function () {
    function SavingsAccountApplicationCtr($scope, $http, $q, $timeout, $translate) {
        var _this = this;
        this.$http = $http;
        this.$q = $q;
        this.$timeout = $timeout;
        this.apiClient = new NTechCustomerPagesApi.ApiClient(function (msg) { return toastr.error(msg); }, $http, $q);
        var initialDataTyped = initialData;
        window.scope = $scope;
        $scope.savingsAccountOverviewUrl = initialDataTyped.savingsAccountOverviewUrl;
        $scope.isTest = initialDataTyped.isProduction === false;
        $scope.loggedInCivicRegNr = initialDataTyped.civicRegNr;
        $scope.currentLanguage = function () {
            return $translate.use();
        };
        var getModeBasedOnStatus = function (status) {
            if (status === 'CustomerIsAMinor') {
                return 'rejectedminor';
            }
            else if (status === 'WaitingForClient') {
                return 'beingprocessed';
            }
            else if (status === 'CustomerHasAnActiveAccount') {
                return 'hasactiveaccount';
            }
            else {
                return 'application';
            }
        };
        var isNullOrWhitespace = function (input) {
            if (typeof input === 'undefined' || input == null)
                return true;
            if (typeof input === 'string') {
                return input.trim().length < 1;
            }
            else {
                return false;
            }
        };
        $scope.isValidIBAN = function (value) {
            if (isNullOrWhitespace(value))
                return true;
            return ntech.fi.isValidIBAN(value.replace(" ", ""));
        };
        $scope.isValidEmail = function (value) {
            //Just to help the user in case they mix up the fields. Not trying to ensure it's actually possible to send email here
            if (isNullOrWhitespace(value))
                return true;
            var i = value.indexOf('@');
            return value.length >= 3 && i > 0 && i < (value.length - 1);
        };
        $scope.isValidPhoneNr = function (value) {
            if (isNullOrWhitespace(value))
                return true;
            return !(/[a-z]/i.test(value));
        };
        $scope.mode = getModeBasedOnStatus(initialDataTyped.customerApplicationStatus);
        var isExistingCustomer = false;
        var isTrustedInfoEditable = false;
        if ($scope.mode == 'application') {
            $scope.f = {};
            $scope.hasError = function (n) {
                return $scope.f.applicationform.$submitted && $scope.f.applicationform[n] && $scope.f.applicationform[n].$invalid;
            };
            if (initialDataTyped.existingCustomer) {
                $scope.namesViewModel = {
                    customerFirstName: initialDataTyped.existingCustomer.contact.customerFirstName,
                    customerLastName: initialDataTyped.existingCustomer.contact.customerLastName
                };
                $scope.addressViewModel = {
                    customerAddressStreet: initialDataTyped.existingCustomer.contact.customerAddressStreet,
                    customerAddressZipcode: initialDataTyped.existingCustomer.contact.customerAddressZipcode,
                    customerAddressCity: initialDataTyped.existingCustomer.contact.customerAddressCity
                };
                $scope.contactViewModel = {
                    customerEmail: initialDataTyped.existingCustomer.contact.customerEmail,
                    customerPhone: initialDataTyped.existingCustomer.contact.customerPhone
                };
                $scope.applicationEditModel = {};
                isExistingCustomer = true;
            }
            else if (initialDataTyped.trustedSourceLookupCustomer && initialDataTyped.trustedSourceLookupCustomer.contact) {
                $scope.namesViewModel = {
                    customerFirstName: initialDataTyped.trustedSourceLookupCustomer.contact.customerFirstName,
                    customerLastName: initialDataTyped.trustedSourceLookupCustomer.contact.customerLastName
                };
                $scope.addressViewModel = {
                    customerAddressStreet: initialDataTyped.trustedSourceLookupCustomer.contact.customerAddressStreet,
                    customerAddressZipcode: initialDataTyped.trustedSourceLookupCustomer.contact.customerAddressZipcode,
                    customerAddressCity: initialDataTyped.trustedSourceLookupCustomer.contact.customerAddressCity
                };
                $scope.contactEditModel = {};
                $scope.applicationEditModel = {};
            }
            else {
                $scope.namesEditModel = {};
                $scope.addressEditModel = {};
                $scope.contactEditModel = {};
                $scope.applicationEditModel = {};
                isTrustedInfoEditable = true;
            }
        }
        $scope.cancel = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.isLoading = true;
            $timeout(function () {
                document.location.href = initialDataTyped.cancelUrl;
            });
        };
        $scope.apply = function (applicationModel, evt) {
            if (evt) {
                evt.preventDefault();
            }
            if ($scope.f.applicationform.$invalid) {
                $scope.f.applicationform.$setSubmitted();
                return;
            }
            $scope.isLoading = true;
            var applicationItems = [];
            var addAllFieldsUsingFieldName = function (ii, source) {
                if (!source)
                    return;
                angular.forEach(source, function (v, k) {
                    ii.push({ Name: String(k), Value: v });
                });
            };
            if ($scope.namesEditModel) {
                addAllFieldsUsingFieldName(applicationItems, $scope.namesEditModel);
            }
            if ($scope.addressEditModel) {
                addAllFieldsUsingFieldName(applicationItems, $scope.addressEditModel);
            }
            if ($scope.contactEditModel) {
                addAllFieldsUsingFieldName(applicationItems, $scope.contactEditModel);
            }
            if ($scope.applicationEditModel) {
                addAllFieldsUsingFieldName(applicationItems, $scope.applicationEditModel);
            }
            var contactInfoLookupResultEncryptionKey = "";
            if (initialDataTyped && initialDataTyped.trustedSourceLookupCustomer) {
                contactInfoLookupResultEncryptionKey = initialDataTyped.trustedSourceLookupCustomer.contactInfoLookupResultEncryptionKey;
            }
            var application = {
                UserLanguage: $scope.currentLanguage(),
                ContactInfoLookupResultEncryptionKey: contactInfoLookupResultEncryptionKey,
                ApplicationItems: applicationItems,
                ExternalApplicationVariables: initialDataTyped.externalApplicationVariables
            };
            $scope.isLoading = true;
            _this.apiClient.savingsStandardApplicationApply(application).then(function (x) {
                if (!initialDataTyped.isProduction) {
                    localStorage.setItem(SavingsAccountApplicationNs.getTestApplicationStorageKey(isExistingCustomer, isTrustedInfoEditable), JSON.stringify({
                        namesEditModel: $scope.namesEditModel,
                        addressEditModel: $scope.addressEditModel,
                        contactEditModel: $scope.contactEditModel,
                        applicationEditModel: $scope.applicationEditModel
                    }));
                }
                $scope.isLoading = false;
                document.location.href = x.questionsUrl;
            });
        };
        if (initialDataTyped.isProduction === false) {
            $scope.loadLastSuccessfulTestApplication = function (evt) {
                if (evt) {
                    evt.preventDefault();
                }
                var last = localStorage.getItem(SavingsAccountApplicationNs.getTestApplicationStorageKey(isExistingCustomer, isTrustedInfoEditable));
                if (last) {
                    var a = JSON.parse(last);
                    if (a.namesEditModel) {
                        $scope.namesEditModel = a.namesEditModel;
                    }
                    if (a.addressEditModel) {
                        $scope.addressEditModel = a.addressEditModel;
                    }
                    if (a.contactEditModel) {
                        $scope.contactEditModel = a.contactEditModel;
                    }
                    if (a.applicationEditModel) {
                        $scope.applicationEditModel = a.applicationEditModel;
                    }
                }
            };
        }
    }
    SavingsAccountApplicationCtr.$inject = ['$scope', '$http', '$q', '$timeout', '$translate'];
    return SavingsAccountApplicationCtr;
}());
app.controller('savingsAccountApplicationCtr', SavingsAccountApplicationCtr);
var SavingsAccountApplicationNs;
(function (SavingsAccountApplicationNs) {
    function getTestApplicationStorageKey(isExistingCustomer, isTrustedInfoEditable) {
        var testCacheKeyPrefix = (isExistingCustomer ? 'e' : 'n') + (isTrustedInfoEditable ? 'c' : '_');
        return 'last_successful_application_' + testCacheKeyPrefix + '_v9';
    }
    SavingsAccountApplicationNs.getTestApplicationStorageKey = getTestApplicationStorageKey;
})(SavingsAccountApplicationNs || (SavingsAccountApplicationNs = {}));
//# sourceMappingURL=savings-standardapplication-index.js.map