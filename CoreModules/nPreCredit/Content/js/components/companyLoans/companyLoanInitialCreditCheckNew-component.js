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
var CompanyLoanInitialCreditCheckNewComponentNs;
(function (CompanyLoanInitialCreditCheckNewComponentNs) {
    var CompanyLoanInitialCreditCheckNewController = /** @class */ (function (_super) {
        __extends(CompanyLoanInitialCreditCheckNewController, _super);
        function CompanyLoanInitialCreditCheckNewController($http, $q, ntechComponentService, $scope) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.$scope = $scope;
            _this.onBack = function (evt) {
                if (evt) {
                    evt.preventDefault();
                }
                var target = _this.initialData.backTarget
                    ? NavigationTargetHelper.createCodeTarget(_this.initialData.backTarget)
                    : NavigationTargetHelper.createCrossModule('CompanyLoanApplication', { applicationNr: initialData.applicationNr });
                NavigationTargetHelper.handleBack(target, _this.apiClient, _this.$q, { applicationNr: initialData.applicationNr });
            };
            return _this;
        }
        CompanyLoanInitialCreditCheckNewController.prototype.componentName = function () {
            return 'companyLoanInitialCreditCheckNew';
        };
        CompanyLoanInitialCreditCheckNewController.prototype.getRejectionReasonsFromScoringResult = function (result, onMissing) {
            var reasons = null;
            if (!result.WasAccepted && result.RejectionRuleNames && result.RejectionRuleNames.length > 0) {
                reasons = [];
                for (var _i = 0, _a = result.RejectionRuleNames; _i < _a.length; _i++) {
                    var rejectionRuleName = _a[_i];
                    var reasonName = this.initialData.rejectionRuleToReasonNameMapping[rejectionRuleName];
                    if (reasonName) {
                        reasons.push(reasonName);
                    }
                    else {
                        if (onMissing) {
                            onMissing(rejectionRuleName);
                        }
                    }
                }
            }
            return reasons;
        };
        CompanyLoanInitialCreditCheckNewController.prototype.createRejectModelFromScoringResult = function (result) {
            var r = {
                otherReason: '',
                reasons: {},
                rejectModelCheckboxesCol1: [],
                rejectModelCheckboxesCol2: [],
                initialReasons: this.getRejectionReasonsFromScoringResult(result, function (x) { return toastr.warning('Unmapped rejection rule: ' + x + '. Check the rejection reasons by hand!'); })
            };
            for (var _i = 0, _a = Object.keys(this.initialData.rejectionReasonToDisplayNameMapping); _i < _a.length; _i++) {
                var reasonName = _a[_i];
                var displayName = this.initialData.rejectionReasonToDisplayNameMapping[reasonName];
                if (r.rejectModelCheckboxesCol1.length > r.rejectModelCheckboxesCol2.length) {
                    r.rejectModelCheckboxesCol2.push(new RejectionCheckboxModel(reasonName, displayName));
                }
                else {
                    r.rejectModelCheckboxesCol1.push(new RejectionCheckboxModel(reasonName, displayName));
                }
            }
            if (r.initialReasons) {
                for (var _b = 0, _c = r.initialReasons; _b < _c.length; _b++) {
                    var reasonName = _c[_b];
                    r.reasons[reasonName] = true;
                }
            }
            return r;
        };
        CompanyLoanInitialCreditCheckNewController.prototype.init = function (applicationNr, result) {
            var _this = this;
            var setModel = function (offer) {
                _this.m = {
                    applicationNr: applicationNr,
                    recommendationServerKey: result.TempCopyStorageKey,
                    mode: result.Offer ? 'acceptNewLoan' : 'reject',
                    acceptModel: {
                        offer: offer,
                        isPendingValidation: false,
                        validationResult: {
                            handledAcceptedOverLimit: false,
                            isAllowedToOverrideHandlerLimit: false,
                            isOverHandlerLimit: false
                        }
                    },
                    rejectModel: _this.createRejectModelFromScoringResult(result),
                    recommendationInitialData: {
                        applicationNr: applicationNr,
                        recommendation: result,
                        apiClient: _this.initialData.apiClient,
                        companyLoanApiClient: _this.initialData.companyLoanApiClient,
                        creditUrlPattern: _this.initialData.creditUrlPattern,
                        isTest: _this.initialData.isTest,
                        rejectionReasonToDisplayNameMapping: _this.initialData.rejectionReasonToDisplayNameMapping,
                        rejectionRuleToReasonNameMapping: _this.initialData.rejectionRuleToReasonNameMapping,
                        isEditAllowed: true,
                        navigationTargetToHere: NTechNavigationTarget.createCrossModuleNavigationTargetCode("CompanyLoanNewCreditCheck", { applicationNr: applicationNr })
                    }
                };
            };
            if (result.Offer) {
                setModel(this.createAcceptModelFromOffer(result.Offer));
            }
            else {
                this.apiClient.fetchCurrentReferenceInterestRate().then(function (referenceInterestRate) {
                    setModel({
                        initialFeeAmount: '',
                        loanAmount: '',
                        monthlyFeeAmount: '',
                        nominalInterestRatePercent: '',
                        referenceInterestRatePercent: _this.formatNumberForEdit(referenceInterestRate),
                        repaymentTimeInMonths: ''
                    });
                });
            }
        };
        CompanyLoanInitialCreditCheckNewController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.initialData.companyLoanApiClient.initialCreditCheck(this.initialData.applicationNr, true).then(function (x) {
                _this.init(_this.initialData.applicationNr, x);
            });
        };
        CompanyLoanInitialCreditCheckNewController.prototype.createAcceptModelFromOffer = function (offer) {
            return {
                loanAmount: this.formatNumberForEdit(offer.LoanAmount),
                initialFeeAmount: this.formatNumberForEdit(offer.InitialFeeAmount),
                monthlyFeeAmount: this.formatNumberForEdit(offer.MonthlyFeeAmount),
                nominalInterestRatePercent: this.formatNumberForEdit(offer.NominalInterestRatePercent),
                repaymentTimeInMonths: this.formatNumberForEdit(offer.RepaymentTimeInMonths),
                referenceInterestRatePercent: this.formatNumberForEdit(offer.ReferenceInterestRatePercent)
            };
        };
        CompanyLoanInitialCreditCheckNewController.prototype.wasAcceptedRecommendationChanged = function () {
            if (!this.m.recommendationInitialData.recommendation.WasAccepted) {
                return true;
            }
            var initial = this.createAcceptModelFromOffer(this.m.recommendationInitialData.recommendation.Offer);
            var current = this.m.acceptModel.offer;
            return (JSON.stringify(initial) !== JSON.stringify(current));
        };
        CompanyLoanInitialCreditCheckNewController.prototype.onAcceptModelChanged = function () {
            var _this = this;
            if (!this.m || !this.$scope.acceptform) {
                return;
            }
            if (this.$scope.acceptform.$invalid) {
                return;
            }
            if (this.wasAcceptedRecommendationChanged()) {
                this.m.acceptModel.isPendingValidation = false;
                this.m.acceptModel.validationResult = {
                    handledAcceptedOverLimit: false,
                    isAllowedToOverrideHandlerLimit: false,
                    isOverHandlerLimit: false
                };
            }
            else {
                this.m.acceptModel.isPendingValidation = true;
                this.m.acceptModel.validationResult = null;
                this.initialData.apiClient.checkIfOverHandlerLimit(this.m.applicationNr, this.parseDecimalOrNull(this.m.acceptModel.offer.loanAmount), true).then(function (result) {
                    _this.m.acceptModel.isPendingValidation = false;
                    _this.m.acceptModel.validationResult = {
                        handledAcceptedOverLimit: false,
                        isAllowedToOverrideHandlerLimit: result.isAllowedToOverrideHandlerLimit,
                        isOverHandlerLimit: result.isOverHandlerLimit
                    };
                });
            }
        };
        CompanyLoanInitialCreditCheckNewController.prototype.acceptNewLoan = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (this.$scope.acceptform.$invalid) {
                return;
            }
            var offerRaw = this.m.acceptModel.offer;
            var offer = {
                LoanAmount: this.parseDecimalOrNull(offerRaw.loanAmount),
                AnnuityAmount: null,
                InitialFeeAmount: this.parseDecimalOrNull(offerRaw.initialFeeAmount),
                MonthlyFeeAmount: this.parseDecimalOrNull(offerRaw.monthlyFeeAmount),
                NominalInterestRatePercent: this.parseDecimalOrNull(offerRaw.nominalInterestRatePercent),
                ReferenceInterestRatePercent: this.parseDecimalOrNull(offerRaw.referenceInterestRatePercent),
                RepaymentTimeInMonths: this.parseDecimalOrNull(offerRaw.repaymentTimeInMonths)
            };
            this.initialData.companyLoanApiClient.commitInitialCreditCheckDecisionAccept(this.m.applicationNr, this.m.recommendationServerKey, offer).then(function (x) {
                _this.onBack(null);
            });
        };
        CompanyLoanInitialCreditCheckNewController.prototype.totalInterestRatePercent = function () {
            if (!this.m || !this.m.acceptModel || !this.m.acceptModel.offer) {
                return null;
            }
            return this.parseDecimalOrNull(this.m.acceptModel.offer.nominalInterestRatePercent) + this.parseDecimalOrNull(this.m.acceptModel.offer.referenceInterestRatePercent);
        };
        CompanyLoanInitialCreditCheckNewController.prototype.reject = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var reasons = null;
            reasons = this.getRejectionReasons();
            this.initialData.companyLoanApiClient.commitInitialCreditCheckDecisionReject(this.m.applicationNr, this.m.recommendationServerKey, reasons).then(function (x) {
                _this.onBack(null);
            });
        };
        CompanyLoanInitialCreditCheckNewController.prototype.setAcceptRejectMode = function (mode, evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (this.m) {
                this.m.mode = mode;
            }
        };
        CompanyLoanInitialCreditCheckNewController.prototype.getRejectionReasons = function () {
            if (!this.m || !this.m.rejectModel) {
                return null;
            }
            var reasons = [];
            for (var _i = 0, _a = Object.keys(this.m.rejectModel.reasons); _i < _a.length; _i++) {
                var key = _a[_i];
                if (this.m.rejectModel.reasons[key] === true) {
                    reasons.push(key);
                }
            }
            if (!this.isNullOrWhitespace(this.m.rejectModel.otherReason)) {
                reasons.push('other: ' + this.m.rejectModel.otherReason);
            }
            return reasons;
        };
        CompanyLoanInitialCreditCheckNewController.prototype.anyRejectionReasonGiven = function () {
            var reasons = this.getRejectionReasons();
            return reasons && reasons.length > 0;
        };
        CompanyLoanInitialCreditCheckNewController.$inject = ['$http', '$q', 'ntechComponentService', '$scope', '$timeout'];
        return CompanyLoanInitialCreditCheckNewController;
    }(NTechComponents.NTechComponentControllerBase));
    CompanyLoanInitialCreditCheckNewComponentNs.CompanyLoanInitialCreditCheckNewController = CompanyLoanInitialCreditCheckNewController;
    var CompanyLoanInitialCreditCheckNewComponent = /** @class */ (function () {
        function CompanyLoanInitialCreditCheckNewComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanInitialCreditCheckNewController;
            this.templateUrl = 'company-loan-initial-credit-check-new.html';
        }
        return CompanyLoanInitialCreditCheckNewComponent;
    }());
    CompanyLoanInitialCreditCheckNewComponentNs.CompanyLoanInitialCreditCheckNewComponent = CompanyLoanInitialCreditCheckNewComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    CompanyLoanInitialCreditCheckNewComponentNs.Model = Model;
    var EditOfferModel = /** @class */ (function () {
        function EditOfferModel() {
        }
        return EditOfferModel;
    }());
    CompanyLoanInitialCreditCheckNewComponentNs.EditOfferModel = EditOfferModel;
    var AcceptNewLoanModel = /** @class */ (function () {
        function AcceptNewLoanModel() {
        }
        return AcceptNewLoanModel;
    }());
    CompanyLoanInitialCreditCheckNewComponentNs.AcceptNewLoanModel = AcceptNewLoanModel;
    var RejectModel = /** @class */ (function () {
        function RejectModel() {
        }
        return RejectModel;
    }());
    CompanyLoanInitialCreditCheckNewComponentNs.RejectModel = RejectModel;
    var AcceptNewLoanModelValidationModel = /** @class */ (function () {
        function AcceptNewLoanModelValidationModel() {
        }
        return AcceptNewLoanModelValidationModel;
    }());
    CompanyLoanInitialCreditCheckNewComponentNs.AcceptNewLoanModelValidationModel = AcceptNewLoanModelValidationModel;
    var RejectionCheckboxModel = /** @class */ (function () {
        function RejectionCheckboxModel(reason, displayName) {
            this.reason = reason;
            this.displayName = displayName;
        }
        return RejectionCheckboxModel;
    }());
    CompanyLoanInitialCreditCheckNewComponentNs.RejectionCheckboxModel = RejectionCheckboxModel;
})(CompanyLoanInitialCreditCheckNewComponentNs || (CompanyLoanInitialCreditCheckNewComponentNs = {}));
angular.module('ntech.components').component('companyLoanInitialCreditCheckNew', new CompanyLoanInitialCreditCheckNewComponentNs.CompanyLoanInitialCreditCheckNewComponent());
