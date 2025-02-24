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
var MortgageApplicationObjectValuationNewComponentNs;
(function (MortgageApplicationObjectValuationNewComponentNs) {
    var MortgageApplicationObjectValuationNewController = /** @class */ (function (_super) {
        __extends(MortgageApplicationObjectValuationNewController, _super);
        function MortgageApplicationObjectValuationNewController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        MortgageApplicationObjectValuationNewController.prototype.componentName = function () {
            return 'mortgageApplicationObjectValuationNew';
        };
        MortgageApplicationObjectValuationNewController.prototype.onChanges = function () {
            var _this = this;
            this.applicationObjectInitialData = null;
            this.ucbvSearchInitialData = null;
            if (this.initialData) {
                this.applicationObjectInitialData = {
                    applicationInfo: this.initialData.applicationInfo
                };
                this.ucbvSearchInitialData = {
                    applicationInfo: this.initialData.applicationInfo,
                    backUrl: this.initialData.backUrl
                };
            }
            //Result
            this.setupManualEditResult(new NTechPreCreditApi.MortgageApplicationValutionResult());
            if (this.initialData.callAutomateCustomerOnInit) {
                this.apiClient.tryAutomateMortgageApplicationValution(this.initialData.applicationInfo.ApplicationNr).then(function (result) {
                    _this.setupAutomatedResult(result);
                    if (_this.initialData.autoAcceptSuggestion) {
                        _this.acceptResult(_this.resultData, null);
                    }
                });
            }
        };
        MortgageApplicationObjectValuationNewController.prototype.setupAutomatedResult = function (result) {
            var _this = this;
            if (result.IsSuccess) {
                var data = result.SuccessData;
                this.isResultExpanded = true;
                this.resultData = data;
                this.resultForm = {
                    modelBase: this.resultData,
                    items: [
                        SimpleFormComponentNs.textView({ labelText: 'Ucbv - ObjektId', model: 'ucbvObjektId' }),
                        SimpleFormComponentNs.textView({ labelText: 'Skatteverket lgh-nr', model: 'brfLghSkvLghNr' }),
                        SimpleFormComponentNs.textView({ labelText: 'Foreningsnamn', model: 'brfNamn' }),
                        SimpleFormComponentNs.textView({ labelText: 'Yta', model: 'brfLghYta' }),
                        SimpleFormComponentNs.textView({ labelText: 'Vaning', model: 'brfLghVaning' }),
                        SimpleFormComponentNs.textView({ labelText: 'Antal rum', model: 'brfLghAntalRum' }),
                        SimpleFormComponentNs.textView({ labelText: 'Varde', model: 'brfLghVarde' }),
                        SimpleFormComponentNs.textView({ labelText: 'Brf signal - ar', model: 'brfSignalAr' }),
                        SimpleFormComponentNs.textView({ labelText: 'Brf signal - belaning', model: 'brfSignalBelaning' }),
                        SimpleFormComponentNs.textView({ labelText: 'Brf signal - likviditet', model: 'brfSignalLikviditet' }),
                        SimpleFormComponentNs.textView({ labelText: 'Brf signal - Sjalvforsorjningsgrad', model: 'brfSignalSjalvforsorjningsgrad' }),
                        SimpleFormComponentNs.textView({ labelText: 'Brf signal - Rantekanslighet', model: 'brfSignalRantekanslighet' }),
                        SimpleFormComponentNs.textView({ labelText: 'Lghs andel av brfs skulder (kr)', model: 'brfLghDebtAmount' }),
                        SimpleFormComponentNs.button({ buttonText: 'Approve', onClick: function () { _this.acceptResult(_this.resultData, null); }, buttonType: SimpleFormComponentNs.ButtonType.Accept })
                    ]
                };
                this.automationFailedMessage = null;
            }
            else {
                this.isResultExpanded = false;
                this.resultData = null;
                this.resultForm = null;
                this.automationFailedMessage = result.FailedMessage;
            }
        };
        MortgageApplicationObjectValuationNewController.prototype.setupManualEditResult = function (data) {
            var _this = this;
            this.manualResultData = data;
            this.manualResultForm = {
                modelBase: this.manualResultData,
                items: [
                    SimpleFormComponentNs.textField({ labelText: 'Ucbv - ObjektId', model: 'ucbvObjektId', required: true }),
                    SimpleFormComponentNs.textField({ labelText: 'Skatteverket lgh-nr', model: 'brfLghSkvLghNr', required: true }),
                    SimpleFormComponentNs.textField({ labelText: 'Foreningsnamn', model: 'brfNamn', required: true }),
                    SimpleFormComponentNs.textField({ labelText: 'Yta', model: 'brfLghYta', required: true }),
                    SimpleFormComponentNs.textField({ labelText: 'Vaning', model: 'brfLghVaning', required: true }),
                    SimpleFormComponentNs.textField({ labelText: 'Antal rum', model: 'brfLghAntalRum', required: true }),
                    SimpleFormComponentNs.textField({ labelText: 'Varde', model: 'brfLghVarde', required: true }),
                    SimpleFormComponentNs.textField({ labelText: 'Brf signal - ar', model: 'brfSignalAr' }),
                    SimpleFormComponentNs.textField({ labelText: 'Brf signal - belaning', model: 'brfSignalBelaning' }),
                    SimpleFormComponentNs.textField({ labelText: 'Brf signal - likviditet', model: 'brfSignalLikviditet' }),
                    SimpleFormComponentNs.textField({ labelText: 'Brf signal - Sjalvforsorjningsgrad', model: 'brfSignalSjalvforsorjningsgrad' }),
                    SimpleFormComponentNs.textField({ labelText: 'Brf signal - Rantekanslighet', model: 'brfSignalRantekanslighet' }),
                    SimpleFormComponentNs.textField({ labelText: 'Lghs andel av brfs skulder (kr)', model: 'brfLghDebtAmount' }),
                    SimpleFormComponentNs.button({ buttonText: 'Approve', onClick: function () { _this.acceptResult(_this.manualResultData, null); }, buttonType: SimpleFormComponentNs.ButtonType.Accept })
                ]
            };
        };
        MortgageApplicationObjectValuationNewController.prototype.acceptResult = function (result, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.acceptMortgageLoanUcbvValuation(this.initialData.applicationInfo.ApplicationNr, result).then(function () {
                document.location.href = _this.initialData.backUrl;
            });
        };
        MortgageApplicationObjectValuationNewController.prototype.arrayToCommaList = function (a) {
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
        MortgageApplicationObjectValuationNewController.prototype.brfSignalToCode = function (value) {
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
        MortgageApplicationObjectValuationNewController.$inject = ['$http', '$q', 'ntechComponentService'];
        return MortgageApplicationObjectValuationNewController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageApplicationObjectValuationNewComponentNs.MortgageApplicationObjectValuationNewController = MortgageApplicationObjectValuationNewController;
    var MortgageApplicationObjectValuationNewComponent = /** @class */ (function () {
        function MortgageApplicationObjectValuationNewComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationObjectValuationNewController;
            this.templateUrl = 'mortgage-application-object-valuation-new.html';
        }
        return MortgageApplicationObjectValuationNewComponent;
    }());
    MortgageApplicationObjectValuationNewComponentNs.MortgageApplicationObjectValuationNewComponent = MortgageApplicationObjectValuationNewComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    MortgageApplicationObjectValuationNewComponentNs.InitialData = InitialData;
})(MortgageApplicationObjectValuationNewComponentNs || (MortgageApplicationObjectValuationNewComponentNs = {}));
angular.module('ntech.components').component('mortgageApplicationObjectValuationNew', new MortgageApplicationObjectValuationNewComponentNs.MortgageApplicationObjectValuationNewComponent());
