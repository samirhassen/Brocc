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
var CompanyLoanInitialCreditCheckRecommendationComponentNs;
(function (CompanyLoanInitialCreditCheckRecommendationComponentNs) {
    var CompanyLoanInitialCreditCheckRecommendationController = /** @class */ (function (_super) {
        __extends(CompanyLoanInitialCreditCheckRecommendationController, _super);
        function CompanyLoanInitialCreditCheckRecommendationController($http, $q, ntechComponentService, modalDialogService, $sce) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.modalDialogService = modalDialogService;
            _this.$sce = $sce;
            _this.decisionDetailsDialogId = modalDialogService.generateDialogId();
            _this.companyCreditReportDialogId = modalDialogService.generateDialogId();
            window[_this.componentName() + '_debug_' + _this.decisionDetailsDialogId] = _this;
            return _this;
        }
        CompanyLoanInitialCreditCheckRecommendationController.prototype.componentName = function () {
            return 'companyLoanInitialCreditCheckRecommendation';
        };
        CompanyLoanInitialCreditCheckRecommendationController.prototype.getCreditUrl = function (creditNr) {
            return this.initialData.creditUrlPattern.replace('NNN', creditNr);
        };
        CompanyLoanInitialCreditCheckRecommendationController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            var r = NTechPreCreditApi.FetchApplicationDataSourceRequestItem.createCreditApplicationItemSource(['application.companyAgeInMonths', 'application.companyYearlyRevenue', 'application.companyYearlyResult', 'application.companyCurrentDebtAmount', 'application.loanPurposeCode',
                'application.forceExternalScoring'], false, true, '-', true);
            this.initialData.apiClient.fetchApplicationDataSourceItems(this.initialData.applicationNr, [r]).then(function (x) {
                _this.m = {
                    applicationNr: _this.initialData.applicationNr,
                    recommendation: _this.initialData.recommendation,
                    loadCreditReportDocument: false,
                    creditApplicationItems: NTechPreCreditApi.FetchApplicationDataSourceRequestItem.resultAsDictionary(x.Results[0].Items),
                    changedCreditApplicationItemNames: x.Results[0].ChangedNames
                };
            });
        };
        CompanyLoanInitialCreditCheckRecommendationController.prototype.getCreditApplicationItemDisplayValue = function (groupedName, dataType) {
            var v = this.m && this.m.creditApplicationItems ? this.m.creditApplicationItems[groupedName] : '-';
            if (v === '-') {
                return null;
            }
            if (dataType === 'int') {
                return Math.round(this.parseDecimalOrNull(v));
            }
            else if (dataType == 'decimal') {
                return this.parseDecimalOrNull(v);
            }
            else {
                return v;
            }
        };
        CompanyLoanInitialCreditCheckRecommendationController.prototype.isCreditApplicationItemEdited = function (groupedName) {
            if (!this.m || !this.m.changedCreditApplicationItemNames) {
                return false;
            }
            return this.m.changedCreditApplicationItemNames.indexOf(groupedName) >= 0;
        };
        CompanyLoanInitialCreditCheckRecommendationController.prototype.getEditApplicationItemUrl = function (groupedName) {
            if (!this.initialData) {
                return null;
            }
            return "/Ui/CompanyLoan/Application/EditItem?applicationNr=".concat(this.initialData.applicationNr, "&dataSourceName=CreditApplicationItem&itemName=").concat(groupedName, "&ro=").concat(this.initialData.isEditAllowed ? 'False' : 'True', "&backTarget=").concat(this.initialData.navigationTargetToHere);
        };
        CompanyLoanInitialCreditCheckRecommendationController.prototype.hasManulControlReasons = function () {
            return this.m && this.m.recommendation.ManualAttentionRuleNames && this.m.recommendation.ManualAttentionRuleNames.length > 0;
        };
        CompanyLoanInitialCreditCheckRecommendationController.prototype.getScoringDataStr = function (name, defaultValue) {
            var v = this.m && this.m.recommendation && this.m.recommendation.ScoringData && this.m.recommendation.ScoringData.ApplicationItems[name];
            return (!v && defaultValue) ? defaultValue : v;
        };
        CompanyLoanInitialCreditCheckRecommendationController.prototype.getRecommendationRejectionReasonDisplayNames = function () {
            if (!this.initialData) {
                return null;
            }
            if (!this.m || !this.m.recommendation) {
                return null;
            }
            if (this.m.recommendation.WasAccepted) {
                return [];
            }
            var reasonsPre = {}; //dedupe
            for (var _i = 0, _a = this.m.recommendation.RejectionRuleNames; _i < _a.length; _i++) {
                var ruleName = _a[_i];
                var reasonName = this.initialData.rejectionRuleToReasonNameMapping[ruleName];
                if (reasonName) {
                    reasonsPre[reasonName] = this.initialData.rejectionReasonToDisplayNameMapping[reasonName];
                }
            }
            var reasons = [];
            for (var _b = 0, _c = Object.keys(reasonsPre); _b < _c.length; _b++) {
                var reasonName = _c[_b];
                reasons.push(reasonsPre[reasonName]);
            }
            return reasons;
        };
        CompanyLoanInitialCreditCheckRecommendationController.prototype.showDetails = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.modalDialogService.openDialog(this.decisionDetailsDialogId);
        };
        CompanyLoanInitialCreditCheckRecommendationController.prototype.getRejectionReasonDisplayNameByRuleName = function (ruleName) {
            if (!this.initialData) {
                return '';
            }
            var reasonName = this.initialData.rejectionRuleToReasonNameMapping[ruleName];
            var displayName = this.initialData.rejectionReasonToDisplayNameMapping[reasonName];
            return displayName ? displayName : reasonName;
        };
        CompanyLoanInitialCreditCheckRecommendationController.prototype.getScorePointRuleNames = function () {
            if (!this.m) {
                return [];
            }
            return Object.keys(this.m.recommendation.ScorePointsByRuleName);
        };
        CompanyLoanInitialCreditCheckRecommendationController.prototype.getScorePointDebugData = function (ruleName) {
            if (!this.m.recommendation.DebugDataByRuleNames) {
                return null;
            }
            var dRaw = this.m.recommendation.DebugDataByRuleNames[ruleName];
            if (!dRaw) {
                return null;
            }
            var d = JSON.parse(dRaw);
            return d;
        };
        CompanyLoanInitialCreditCheckRecommendationController.prototype.getRejectionRuleDebugData = function (ruleName) {
            if (!this.m.recommendation.DebugDataByRuleNames) {
                return null;
            }
            return this.m.recommendation.DebugDataByRuleNames[ruleName];
        };
        CompanyLoanInitialCreditCheckRecommendationController.prototype.getNonScorePointDebugDataRuleNames = function () {
            var n = [];
            if (!this.m || !this.m.recommendation || !this.m.recommendation.DebugDataByRuleNames) {
                return n;
            }
            var s = this.m.recommendation.ScorePointsByRuleName ? this.m.recommendation.ScorePointsByRuleName : {};
            for (var _i = 0, _a = Object.keys(this.m.recommendation.DebugDataByRuleNames); _i < _a.length; _i++) {
                var r = _a[_i];
                if (!(r in s)) {
                    n.push(r);
                }
            }
            return n;
        };
        CompanyLoanInitialCreditCheckRecommendationController.prototype.toggleRuleDebugDetails = function (ruleName, evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m) {
                return;
            }
            this.m.debugDetailsRuleName = this.m.debugDetailsRuleName === ruleName ? null : ruleName;
        };
        CompanyLoanInitialCreditCheckRecommendationController.prototype.getScoringDataItemNames = function () {
            if (!this.m) {
                return [];
            }
            return Object.keys(this.m.recommendation.ScoringData.ApplicationItems);
        };
        CompanyLoanInitialCreditCheckRecommendationController.prototype.showCreditReport = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.m.loadCreditReportDocument = true;
            this.modalDialogService.openDialog(this.companyCreditReportDialogId);
        };
        CompanyLoanInitialCreditCheckRecommendationController.prototype.getCreditReportIFrameHtml = function () {
            if (!this.initialData) {
                return null;
            }
            var html = "<iframe src=\"/CreditManagement/ArchiveDocument?key=".concat(this.getScoringDataStr('companyCreditReportHtmlArchiveKey', ''), "\" allowfullscreen></iframe>");
            return this.$sce.trustAsHtml(html);
        };
        CompanyLoanInitialCreditCheckRecommendationController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService', '$sce'];
        return CompanyLoanInitialCreditCheckRecommendationController;
    }(NTechComponents.NTechComponentControllerBase));
    CompanyLoanInitialCreditCheckRecommendationComponentNs.CompanyLoanInitialCreditCheckRecommendationController = CompanyLoanInitialCreditCheckRecommendationController;
    var CompanyLoanInitialCreditCheckRecommendationComponent = /** @class */ (function () {
        function CompanyLoanInitialCreditCheckRecommendationComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanInitialCreditCheckRecommendationController;
            this.templateUrl = 'company-loan-initial-credit-check-recommendation.html';
        }
        return CompanyLoanInitialCreditCheckRecommendationComponent;
    }());
    CompanyLoanInitialCreditCheckRecommendationComponentNs.CompanyLoanInitialCreditCheckRecommendationComponent = CompanyLoanInitialCreditCheckRecommendationComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    CompanyLoanInitialCreditCheckRecommendationComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    CompanyLoanInitialCreditCheckRecommendationComponentNs.Model = Model;
})(CompanyLoanInitialCreditCheckRecommendationComponentNs || (CompanyLoanInitialCreditCheckRecommendationComponentNs = {}));
angular.module('ntech.components').component('companyLoanInitialCreditCheckRecommendation', new CompanyLoanInitialCreditCheckRecommendationComponentNs.CompanyLoanInitialCreditCheckRecommendationComponent());
