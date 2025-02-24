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
var MortgageLoanScoringResultComponentNs;
(function (MortgageLoanScoringResultComponentNs) {
    var MortgageLoanScoringResultController = /** @class */ (function (_super) {
        __extends(MortgageLoanScoringResultController, _super);
        function MortgageLoanScoringResultController($http, $q, $filter, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$filter = $filter;
            _this.modalDialogService = modalDialogService;
            _this.leftToLiveOnExpanded = function () {
                _this.apiClient.fetchLeftToLiveOnRequiredItemNames().then(function (result) {
                    if (!_this.m) {
                        return;
                    }
                    var d = {};
                    var id = {
                        modelBase: d,
                        items: []
                    };
                    var b = angular.copy(_this.initialData.scoringResult.ScoringBasis);
                    var nrOfApplicants = parseInt(b.ApplicationItems['nrOfApplicants']);
                    for (var _i = 0, _a = result.RequiredApplicationItems; _i < _a.length; _i++) {
                        var itemName = _a[_i];
                        var itemValue = b.ApplicationItems[itemName];
                        var modelName = "n_".concat(itemName);
                        d[modelName] = itemValue;
                        if (itemName == 'nrOfApplicants') {
                            id.items.push(SimpleFormComponentNs.textView({ labelText: itemName, model: modelName }));
                        }
                        else {
                            id.items.push(SimpleFormComponentNs.textField({ labelText: itemName, model: modelName, required: true }));
                        }
                    }
                    for (var _b = 0, _c = result.RequiredApplicantItems; _b < _c.length; _b++) {
                        var itemName = _c[_b];
                        for (var applicantNr = 1; applicantNr <= nrOfApplicants; applicantNr++) {
                            var itemValue = b.ApplicantItems[applicantNr][itemName];
                            var modelName = "a_".concat(applicantNr, "_").concat(itemName);
                            d[modelName] = itemValue;
                            id.items.push(SimpleFormComponentNs.textField({ labelText: "Applicant ".concat(applicantNr, " - ").concat(itemName), model: modelName, required: true }));
                        }
                    }
                    if (_this.initialData.scoringResult.IsAccepted && _this.initialData.scoringResult.AcceptedOffer) {
                        d['i_interestRatePercent'] = _this.initialData.scoringResult.AcceptedOffer.NominalInterestRatePercent.toString();
                    }
                    else {
                        d['i_interestRatePercent'] = '';
                    }
                    id.items.push(SimpleFormComponentNs.textField({ labelText: 'interestRatePercent', model: 'i_interestRatePercent', required: true }));
                    var onClick = function () {
                        var scoringBasis = angular.copy(_this.initialData.scoringResult.ScoringBasis);
                        var interestRatePercent = null;
                        for (var keyName in d) {
                            var newValue = d[keyName];
                            if (keyName[0] == 'n') {
                                scoringBasis.ApplicationItems[keyName.substring(2)] = newValue;
                            }
                            else if (keyName[0] == 'a') {
                                var applicantNr_1 = parseInt(keyName.substring(2, 3));
                                scoringBasis.ApplicantItems[applicantNr_1][keyName.substring(4)] = newValue;
                            }
                            else if (keyName[0] == 'i') {
                                interestRatePercent = parseFloat(newValue);
                            }
                        }
                        _this.apiClient.computeLeftToLiveOn(scoringBasis, interestRatePercent).then(function (ltlResult) {
                            var rows = [];
                            rows.push(['Main', '', '']);
                            rows.push(['', 'Left to live on', _this.$filter('currency')(ltlResult.LeftToLiveOnAmount)]);
                            rows.push(['', 'Debt/Income multiplier', _this.$filter('number')(ltlResult.DebtMultiplier) + 'x']);
                            rows.push(['', 'Loan fraction', _this.$filter('number')(ltlResult.LoanFraction * 100) + '%']);
                            rows.push(['Parts', '', '']);
                            for (var _i = 0, _a = ltlResult.LeftToLiveOnParts; _i < _a.length; _i++) {
                                var p = _a[_i];
                                rows.push(['', p.Name, p.Value.toString()]);
                            }
                            _this.m.leftToLiveOnResultInitialData = {
                                columns: [{ className: 'col-xs-2', labelText: 'Type' }, { className: 'col-xs-4', labelText: 'Name' }, { className: 'cols-xs-6', labelText: 'Value' }],
                                tableRows: rows
                            };
                        });
                    };
                    id.items.push(SimpleFormComponentNs.button({ buttonText: 'Calculate', onClick: onClick }));
                    _this.m.leftToLiveOnFormInitialData = id;
                });
            };
            _this.decisionDetailsDialogId = modalDialogService.generateDialogId();
            _this.householdIncomeDialogId = modalDialogService.generateDialogId();
            _this.modalDialogService.subscribeToDialogEvents(function (e) {
                if (e.dialogId !== _this.householdIncomeDialogId || !e.isClosed) {
                    return;
                }
                var newIncome = _this.m.householdIncomeDialogModel.newIncome;
                _this.m.householdIncomeDialogModel = null;
                if (newIncome != null) {
                    if (_this.initialData.onBasisDataChanged) {
                        _this.initialData.onBasisDataChanged();
                    }
                    else {
                        _this.signalReloadRequired();
                    }
                }
            });
            return _this;
        }
        MortgageLoanScoringResultController.prototype.componentName = function () {
            return 'mortgageLoanScoringResult';
        };
        MortgageLoanScoringResultController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            var s = this.initialData.scoringResult;
            if (!s) {
                return;
            }
            var getReasonNameByRuleName = function (x) { return _this.initialData.rejectionReasonNameByScoringRuleName[x] || ('DirectlyByRule' + x); };
            this.m = {
                isAccepted: s.IsAccepted,
                riskClass: s.RiskClass,
                loanFraction: this.computeLoanFractionPercent(s.ScoringBasis),
                offer: s.AcceptedOffer,
                rejectionReasonNames: s.IsAccepted ? null : _.uniq(_.map(s.RejectionRuleNames, getReasonNameByRuleName)),
                rejectionReasonDetailItems: s.IsAccepted ? null : this.toRejectionDetails(_.groupBy(s.RejectionRuleNames, getReasonNameByRuleName)),
                scorePointsInitialData: s.ScorePointsByRuleName ? this.createScorePointsInitialData(s.ScorePointsByRuleName) : null,
                scoreModelDataInitialData: s.ScoringBasis ? {
                    columns: [{ className: 'col-xs-5', labelText: 'Name' }, { className: 'col-xs-2', labelText: 'Level' }, { className: 'col-xs-5', labelText: 'Value' }],
                    tableRows: NTechPreCreditApi.ScoringDataModel.toDataTable(s.ScoringBasis)
                } : null,
                manualAttentionInitialData: s.ManualAttentionRules ? {
                    columns: [{ className: 'col-xs-12', labelText: 'Rule' }],
                    tableRows: _.map(s.ManualAttentionRules, function (ruleName) { return [ruleName]; })
                } : null,
                applicantCreditReports: this.createApplicantCreditReports(s.ScoringBasis),
                ucTemplateRejectionCodes: this.createUcTemplateRejectionCodes(s.ScoringBasis)
            };
        };
        MortgageLoanScoringResultController.prototype.showDecisionDetails = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.modalDialogService.openDialog(this.decisionDetailsDialogId);
        };
        MortgageLoanScoringResultController.prototype.createScorePointsInitialData = function (s) {
            var rows = [];
            var sum = 0;
            for (var _i = 0, _a = Object.keys(s); _i < _a.length; _i++) {
                var ruleName = _a[_i];
                var rulePoints = s[ruleName];
                sum = sum + rulePoints;
                rows.push([ruleName, rulePoints.toString()]);
            }
            rows.push(['  Sum', sum.toString()]);
            return {
                columns: [{ className: 'col-xs-8', labelText: 'Rule' }, { className: 'col-xs-4', labelText: 'Points' }],
                tableRows: rows
            };
        };
        MortgageLoanScoringResultController.prototype.createApplicantCreditReports = function (s) {
            if (!s || !s.ApplicantItems) {
                return [];
            }
            var r = [];
            var _loop_1 = function () {
                var key = s.ApplicantItems[applicantNr]['creditReportHtmlReportArchiveKey'];
                if (key != null) {
                    var rr_1 = { applicantNr: parseInt(applicantNr), htmlArchiveKey: key, creditReportDialogId: this_1.modalDialogService.generateDialogId(), loadCreditReportDocument: false };
                    r.push(rr_1);
                    this_1.modalDialogService.subscribeToDialogEvents(function (e) {
                        if (e.dialogId === rr_1.creditReportDialogId && e.isOpenRequest) {
                            rr_1.loadCreditReportDocument = true;
                        }
                    });
                }
            };
            var this_1 = this;
            for (var applicantNr in s.ApplicantItems) {
                _loop_1();
            }
            return r;
        };
        MortgageLoanScoringResultController.prototype.getHouseholdGrossMonthlyIncome = function () {
            if (!this.initialData || !this.initialData.scoringResult || !this.initialData.scoringResult.ScoringBasis) {
                return null;
            }
            var s = this.initialData.scoringResult.ScoringBasis;
            var income = 0;
            for (var applicantNr = 1; applicantNr <= this.initialData.applicationInfo.NrOfApplicants; applicantNr++) {
                var a = s.ApplicantItems[applicantNr];
                if (a) {
                    var applicantIncome = this.parseDecimalOrNull(a['grossMonthlyIncome']);
                    income += applicantIncome;
                }
            }
            return income;
        };
        MortgageLoanScoringResultController.prototype.createUcTemplateRejectionCodes = function (s) {
            if (!s.ApplicantItems) {
                return null;
            }
            var codes = [];
            for (var applicantNr in s.ApplicantItems) {
                var c = s.ApplicantItems[applicantNr]['creditReportTemplateReasonCode'];
                if (c) {
                    //Split into groups of length three            
                    for (var _i = 0, _a = c.match(/.{3}/g); _i < _a.length; _i++) {
                        var code = _a[_i];
                        codes.push(code);
                    }
                }
            }
            return _.uniq(codes);
        };
        MortgageLoanScoringResultController.prototype.computeLoanFractionPercent = function (s) {
            if (!s) {
                return null;
            }
            var loanAmountStr = s.ApplicationItems['loanAmount'];
            var objectValueStr = s.ApplicationItems['objectValue'];
            if (!(loanAmountStr && objectValueStr)) {
                return null;
            }
            var loanAmount = parseFloat(loanAmountStr);
            var objectValue = parseFloat(objectValueStr);
            if (objectValue > 0) {
                return 100.0 * (loanAmount / objectValue);
            }
            else {
                return null;
            }
        };
        MortgageLoanScoringResultController.prototype.toRejectionDetails = function (rejectionRuleNamesGroupedByReasonName) {
            var items = [];
            for (var key in rejectionRuleNamesGroupedByReasonName) {
                items.push({ reasonName: key, ruleNames: rejectionRuleNamesGroupedByReasonName[key] });
            }
            return items;
        };
        MortgageLoanScoringResultController.prototype.getRejectionReasonDisplayName = function (name) {
            if (!this.initialData) {
                return null;
            }
            var dn = this.initialData.rejectionReasonDisplayNameName[name];
            if (dn) {
                return dn;
            }
            else {
                return name;
            }
        };
        MortgageLoanScoringResultController.prototype.getUcTemplateRejectionReason = function (code) {
            if (!this.initialData || !this.initialData.ucTemplateRejectionReasons) {
                return code;
            }
            var d = this.initialData.ucTemplateRejectionReasons[code];
            if (!d) {
                return code;
            }
            return d;
        };
        MortgageLoanScoringResultController.prototype.gotoHouseholdIncome = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m) {
                return;
            }
            this.m.householdIncomeDialogModel = {
                newIncome: null,
                initialData: {
                    applicationInfo: this.initialData.applicationInfo,
                    onIncomeChanged: function (newIncome) {
                        if (newIncome != null) {
                            _this.m.householdIncomeDialogModel.newIncome = newIncome;
                        }
                    },
                    hideHeader: true
                }
            };
            this.modalDialogService.openDialog(this.householdIncomeDialogId);
        };
        MortgageLoanScoringResultController.prototype.showCreditReport = function (r, evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.modalDialogService.openDialog(r.creditReportDialogId);
        };
        MortgageLoanScoringResultController.$inject = ['$http', '$q', '$filter', 'ntechComponentService', 'modalDialogService'];
        return MortgageLoanScoringResultController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanScoringResultComponentNs.MortgageLoanScoringResultController = MortgageLoanScoringResultController;
    var MortgageLoanScoringResultComponent = /** @class */ (function () {
        function MortgageLoanScoringResultComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanScoringResultController;
            this.templateUrl = 'mortgage-loan-scoring-result.html';
        }
        return MortgageLoanScoringResultComponent;
    }());
    MortgageLoanScoringResultComponentNs.MortgageLoanScoringResultComponent = MortgageLoanScoringResultComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    MortgageLoanScoringResultComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageLoanScoringResultComponentNs.Model = Model;
    var ApplicantCreditReport = /** @class */ (function () {
        function ApplicantCreditReport() {
        }
        return ApplicantCreditReport;
    }());
    MortgageLoanScoringResultComponentNs.ApplicantCreditReport = ApplicantCreditReport;
    var RejectionReasonDetailItem = /** @class */ (function () {
        function RejectionReasonDetailItem() {
        }
        return RejectionReasonDetailItem;
    }());
    MortgageLoanScoringResultComponentNs.RejectionReasonDetailItem = RejectionReasonDetailItem;
    var HouseholdIncomeDialogModel = /** @class */ (function () {
        function HouseholdIncomeDialogModel() {
        }
        return HouseholdIncomeDialogModel;
    }());
    MortgageLoanScoringResultComponentNs.HouseholdIncomeDialogModel = HouseholdIncomeDialogModel;
})(MortgageLoanScoringResultComponentNs || (MortgageLoanScoringResultComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanScoringResult', new MortgageLoanScoringResultComponentNs.MortgageLoanScoringResultComponent());
