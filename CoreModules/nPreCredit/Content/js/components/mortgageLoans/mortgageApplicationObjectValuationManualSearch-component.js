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
var MortgageApplicationObjectValuationManualSearchComponentNs;
(function (MortgageApplicationObjectValuationManualSearchComponentNs) {
    var MortgageApplicationObjectValuationManualSearchController = /** @class */ (function (_super) {
        __extends(MortgageApplicationObjectValuationManualSearchController, _super);
        function MortgageApplicationObjectValuationManualSearchController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        MortgageApplicationObjectValuationManualSearchController.prototype.componentName = function () {
            return 'mortgageApplicationObjectValuationManualSearch';
        };
        MortgageApplicationObjectValuationManualSearchController.prototype.onChanges = function () {
            var _this = this;
            //Search address
            this.ucbvSearchAddressInput = {
                city: '',
                streetAddress: '',
                municipality: '',
                zipcode: ''
            };
            this.ucbvSearchForm = {
                modelBase: this.ucbvSearchAddressInput,
                items: [
                    SimpleFormComponentNs.textField({ labelText: 'Street address', model: 'streetAddress', required: true }),
                    SimpleFormComponentNs.textField({ labelText: 'Zipcode', model: 'zipcode' }),
                    SimpleFormComponentNs.textField({ labelText: 'City', model: 'city' }),
                    SimpleFormComponentNs.textField({ labelText: 'Municipality', model: 'municipality' }),
                    SimpleFormComponentNs.button({ buttonText: 'Search', onClick: function () { _this.ucbvSearchAddress(_this.ucbvSearchAddressInput, null); } })
                ]
            };
            this.ucbvSearchAddressHits = null;
            //Fetch object
            this.ucbvFetchObjectInput = {
                objectId: ''
            };
            this.ucbvFetchObjectForm = {
                modelBase: this.ucbvFetchObjectInput,
                items: [
                    SimpleFormComponentNs.textField({ labelText: 'Object Id', model: 'objectId', required: true }),
                    SimpleFormComponentNs.button({ buttonText: 'Fetch', onClick: function () { _this.ucbvFetchObject(_this.ucbvFetchObjectInput, null); } })
                ]
            };
            //Vardera bostadsratt
            this.ucbvVarderaBostadsrattHit = null;
            this.ucbvVarderaBostadsrattInput = {
                objektID: ''
            };
            this.ucbvVarderaBostadsrattForm = {
                modelBase: this.ucbvVarderaBostadsrattInput,
                items: [
                    SimpleFormComponentNs.textField({ labelText: 'objektID', model: 'objektID', required: true }),
                    SimpleFormComponentNs.textField({ labelText: 'yta', model: 'yta' }),
                    SimpleFormComponentNs.button({ buttonText: 'Vardera', onClick: function () { _this.ucbvVarderaBostadsratt(_this.ucbvVarderaBostadsrattInput, null); } })
                ]
            };
        };
        MortgageApplicationObjectValuationManualSearchController.prototype.ucbvSearchAddress = function (input, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.ucbvSokAddress(input.streetAddress, input.zipcode, input.city, input.municipality).then(function (result) {
                _this.ucbvSearchAddressHits = result;
            });
        };
        MortgageApplicationObjectValuationManualSearchController.prototype.ucbvFetchObject = function (input, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.ucbvHamtaObjekt(input.objectId).then(function (result) {
                if (result) {
                    _this.ucbvFetchObjectHit = {
                        hit: result
                    };
                }
                else {
                    _this.ucbvFetchObjectHit = { hit: null };
                }
            });
        };
        MortgageApplicationObjectValuationManualSearchController.prototype.ucbvVarderaBostadsratt = function (input, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.ucbvVarderaBostadsratt(input).then(function (result) {
                if (result) {
                    _this.ucbvVarderaBostadsrattHit = {
                        hit: result
                    };
                }
                else {
                    _this.ucbvVarderaBostadsrattHit = { hit: null };
                }
            });
        };
        MortgageApplicationObjectValuationManualSearchController.prototype.ucbvVarderaBostadsrattJson = function (hit) {
            if (hit) {
                return JSON.parse(hit.RawJson);
            }
            else {
                return null;
            }
        };
        MortgageApplicationObjectValuationManualSearchController.prototype.arrayToCommaList = function (a) {
            if (!a) {
                return null;
            }
            else {
                var s_1 = '';
                angular.forEach(a, function (x) {
                    if (s_1.length > 0) {
                        s_1 += ', ';
                    }
                    s_1 += x;
                });
                return s_1;
            }
        };
        MortgageApplicationObjectValuationManualSearchController.prototype.brfSignalToCode = function (value) {
            if (value === 0) {
                return "Okand";
            }
            else if (value === 1) {
                return "Ok";
            }
            else if (value === 2) {
                return "Varning";
            }
            else if (!value) {
                return null;
            }
            else {
                return 'Kod' + value.toString();
            }
        };
        MortgageApplicationObjectValuationManualSearchController.$inject = ['$http', '$q', 'ntechComponentService'];
        return MortgageApplicationObjectValuationManualSearchController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageApplicationObjectValuationManualSearchComponentNs.MortgageApplicationObjectValuationManualSearchController = MortgageApplicationObjectValuationManualSearchController;
    var MortgageApplicationObjectValuationManualSearchComponent = /** @class */ (function () {
        function MortgageApplicationObjectValuationManualSearchComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationObjectValuationManualSearchController;
            this.templateUrl = 'mortgage-application-object-valuation-manual-search.html';
        }
        return MortgageApplicationObjectValuationManualSearchComponent;
    }());
    MortgageApplicationObjectValuationManualSearchComponentNs.MortgageApplicationObjectValuationManualSearchComponent = MortgageApplicationObjectValuationManualSearchComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    MortgageApplicationObjectValuationManualSearchComponentNs.InitialData = InitialData;
    var UcbvSearchAddressInputModel = /** @class */ (function () {
        function UcbvSearchAddressInputModel() {
        }
        return UcbvSearchAddressInputModel;
    }());
    MortgageApplicationObjectValuationManualSearchComponentNs.UcbvSearchAddressInputModel = UcbvSearchAddressInputModel;
    var UcbvFetchObjectInputModel = /** @class */ (function () {
        function UcbvFetchObjectInputModel() {
        }
        return UcbvFetchObjectInputModel;
    }());
    MortgageApplicationObjectValuationManualSearchComponentNs.UcbvFetchObjectInputModel = UcbvFetchObjectInputModel;
    var UcbvFetchObjectResult = /** @class */ (function () {
        function UcbvFetchObjectResult() {
        }
        return UcbvFetchObjectResult;
    }());
    MortgageApplicationObjectValuationManualSearchComponentNs.UcbvFetchObjectResult = UcbvFetchObjectResult;
    var UcbvVarderaBostadsResult = /** @class */ (function () {
        function UcbvVarderaBostadsResult() {
        }
        return UcbvVarderaBostadsResult;
    }());
    MortgageApplicationObjectValuationManualSearchComponentNs.UcbvVarderaBostadsResult = UcbvVarderaBostadsResult;
})(MortgageApplicationObjectValuationManualSearchComponentNs || (MortgageApplicationObjectValuationManualSearchComponentNs = {}));
angular.module('ntech.components').component('mortgageApplicationObjectValuationManualSearch', new MortgageApplicationObjectValuationManualSearchComponentNs.MortgageApplicationObjectValuationManualSearchComponent());
