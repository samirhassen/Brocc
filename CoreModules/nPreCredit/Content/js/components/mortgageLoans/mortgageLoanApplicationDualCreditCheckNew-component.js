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
var MortgageLoanApplicationDualCreditCheckNewComponentNs;
(function (MortgageLoanApplicationDualCreditCheckNewComponentNs) {
    var MortgageLoanApplicationDualCreditCheckNewController = /** @class */ (function (_super) {
        __extends(MortgageLoanApplicationDualCreditCheckNewController, _super);
        function MortgageLoanApplicationDualCreditCheckNewController($http, $q, ntechComponentService, $scope) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$http = $http;
            _this.$q = $q;
            _this.$scope = $scope;
            _this.ntechComponentService.subscribeToReloadRequired(function () {
                _this.reload();
            });
            return _this;
        }
        MortgageLoanApplicationDualCreditCheckNewController.prototype.componentName = function () {
            return 'mortgageLoanApplicationDualCreditCheckNew';
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.handleBack(NavigationTargetHelper.createCodeTarget(NavigationTargetHelper.NavigationTargetCode.MortgageLoanApplication), this.apiClient, this.$q, {
                applicationNr: this.initialData.applicationNr
            });
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.setAcceptRejectMode = function (mode, evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (this.m) {
                this.m.mode = mode;
            }
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.calculateLtv = function () {
            if (!this.initialData || !this.m || !this.m.ltvBasis)
                return null;
            var valuationAmount = this.calculateValuationAmount();
            var l = this.calculateLoanAmount();
            var ltv = 0;
            if (valuationAmount > 0.00001)
                ltv = l / valuationAmount;
            return ltv;
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.calculateValuationAmount = function () {
            var _this = this;
            if (!this.initialData || !this.m || !this.m.ltvBasis)
                return null;
            var valuationAmounts = [];
            this.m.ltvBasis.valuationAmount.forEach(function (element) { return _this.getArrWithElement(valuationAmounts, element); });
            this.m.ltvBasis.statValuationAmount.forEach(function (element) { return _this.getArrWithElement(valuationAmounts, element); });
            this.m.ltvBasis.priceAmount.forEach(function (element) { return _this.getArrWithElement(valuationAmounts, element); });
            var sumValuationAmounts = valuationAmounts.reduce(function (a, b) {
                return a + b["value"];
            }, 0);
            return sumValuationAmounts;
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.getArrWithElement = function (arr, element) {
            var exists = arr.some(function (o) { return o["key"] === element.key; });
            if (!exists)
                arr.push({ key: element.key, value: element.value });
            return arr;
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.calculateMainTotalInitialFeesAmount = function () {
            var m = this.m;
            if (!m)
                return null;
            var totalFeesAmount = 0;
            for (var _i = 0, _a = ['Initial', 'Valuation', 'Deed', 'MortgageApplication']; _i < _a.length; _i++) {
                var feeTypeName = _a[_i];
                totalFeesAmount += this.parseDecimalOrNull(m.acceptModel["main".concat(feeTypeName, "FeeAmount")]);
            }
            return totalFeesAmount;
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.calculateChildTotalInitialFeesAmount = function () {
            var m = this.m;
            if (!m) {
                return null;
            }
            return this.parseDecimalOrNull(m.acceptModel.childInitialFeeAmount);
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.calculateMainLoanAmount = function () {
            var m = this.m;
            if (!m) {
                return null;
            }
            return m.decisionBasis.mainTotalSettlementAmount
                + this.parseDecimalOrNull(m.acceptModel.mainPurchaseAmount)
                + this.parseDecimalOrNull(m.acceptModel.mainDirectToCustomerAmount);
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.calculateSecurityElsewhereAmount = function () {
            var m = this.m;
            if (!m)
                return null;
            if (m.ltvBasis.securityElsewhereAmount.length === 0)
                return 0;
            var sumSecurityElsewhereAmounts = m.ltvBasis.securityElsewhereAmount
                .reduce(function (a, b) { return a + b; }, 0);
            return this.parseDecimalOrNull(sumSecurityElsewhereAmounts);
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.calculateHousingCompanyLoans = function () {
            var m = this.m;
            if (!m)
                return null;
            if (m.ltvBasis.housingCompanyLoans.length === 0)
                return 0;
            var sumHousingCompanyLoans = m.ltvBasis.housingCompanyLoans
                .reduce(function (a, b) { return a + b; }, 0);
            return this.parseDecimalOrNull(sumHousingCompanyLoans);
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.calculateNewLoansAmount = function () {
            var m = this.m;
            if (!m)
                return null;
            var mainTotalAmount = (this.calculateMainLoanAmount()
                + this.calculateMainTotalInitialFeesAmount());
            var childTotalAmount = (this.calculateChildLoanAmount()
                + this.calculateChildTotalInitialFeesAmount());
            return (mainTotalAmount
                + childTotalAmount);
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.calculateExistingLoansAmount = function () {
            var m = this.m;
            if (!m)
                return null;
            var sumCustomerCreditCapitalBalance = m.ltvBasis.customerCredits.reduce(function (a, b) {
                return a + b["CapitalBalance"];
            }, 0);
            return sumCustomerCreditCapitalBalance;
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.calculateChildLoanAmount = function () {
            var m = this.m;
            if (!m) {
                return null;
            }
            return m.decisionBasis.childTotalSettlementAmount
                + this.parseDecimalOrNull(m.acceptModel.childDirectToCustomerAmount);
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.calculateTotalAmount = function (isMain) {
            if (isMain) {
                return this.calculateMainLoanAmount() + this.calculateMainTotalInitialFeesAmount();
            }
            else {
                return this.calculateChildLoanAmount() + this.calculateChildTotalInitialFeesAmount();
            }
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.calculateLoanAmount = function () {
            var m = this.m;
            if (!m)
                return null;
            var collateralLoansAmount = (this.calculateSecurityElsewhereAmount()
                + this.calculateHousingCompanyLoans());
            var l = (this.calculateNewLoansAmount()
                + this.calculateExistingLoansAmount()
                + collateralLoansAmount);
            return this.parseDecimalOrNull(l);
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.getRejectionReasons = function () {
            if (!this.m || !this.m.rejectModel) {
                return null;
            }
            var reasons = [];
            var displayNames = [];
            for (var _i = 0, _a = Object.keys(this.m.rejectModel.reasons); _i < _a.length; _i++) {
                var key = _a[_i];
                if (this.m.rejectModel.reasons[key] === true) {
                    reasons.push(key);
                    displayNames.push(this.initialData.rejectionReasonToDisplayNameMapping[key]);
                }
            }
            if (!this.isNullOrWhitespace(this.m.rejectModel.otherReason)) {
                var otherReason = 'other: ' + this.m.rejectModel.otherReason;
                reasons.push(otherReason);
                displayNames.push(otherReason);
            }
            return { rejectionReasons: reasons, rejectionReasonDisplayNames: displayNames };
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.anyRejectionReasonGiven = function () {
            var reasons = this.getRejectionReasons();
            return reasons && reasons.rejectionReasons && reasons.rejectionReasons.length > 0;
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.buyNewSatReport = function (applicantNr) {
            console.log(this.initialData);
            this.$http({
                method: 'POST',
                url: initialData.buyNewSatReportUrl,
                data: {
                    applicationNr: this.initialData.applicationNr,
                    applicantNr: applicantNr,
                    forceBuyNew: false,
                    requestedCreditReportFields: ['c01', 'c03', 'c04', 'count']
                }
            }).then(function successCallback(response) {
                console.log("Success!");
            }, function errorCallback(response) {
                //$scope.satUi['applicant' + applicantNr] = { isLoading: false }
                toastr.error(response.statusText);
            });
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.createAcceptModel = function (d, isFinal) {
            if (d) {
                return d.UniqueItems;
            }
            else {
                return {
                    applicationType: isFinal ? '' : 'LoanPromise'
                };
            }
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.createRejectModel = function (initialReasons, isFinal) {
            var r = {
                otherReason: '',
                reasons: {},
                rejectModelCheckboxesCol1: [],
                rejectModelCheckboxesCol2: [],
                initialReasons: initialReasons
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
        MortgageLoanApplicationDualCreditCheckNewController.prototype.withAi = function (a) {
            this.apiClient.fetchApplicationInfo(this.initialData.applicationNr).then(function (ai) {
                a(ai);
            });
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.reject = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.withAi(function (ai) {
                var reasonsData = _this.getRejectionReasons();
                _this.apiClient.createItemBasedCreditDecision({
                    ApplicationNr: _this.initialData.applicationNr,
                    IsAccepted: false,
                    SetAsCurrent: true,
                    DecisionType: _this.m.decisionType,
                    RepeatingItems: {
                        rejectionReason: reasonsData.rejectionReasons,
                        rejectionReasonText: reasonsData.rejectionReasonDisplayNames
                    },
                    UniqueItems: {
                        decisionType: _this.m.decisionType
                    },
                    RejectionReasonsItemName: 'rejectionReason'
                }).then(function (x) {
                    _this.apiClient.setMortgageApplicationWorkflowStatus(ai.ApplicationNr, _this.initialData.scoringWorkflowStepName, WorkflowHelper.RejectedName, "".concat(_this.m.decisionType, " credit check rejected")).then(function (y) {
                        _this.onBack(null);
                    });
                });
            });
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.calculatePaymentPlan = function (a, isMain) {
            var loanAmount = isMain ? this.calculateMainLoanAmount() : this.calculateChildLoanAmount();
            var initialFeeAmount = isMain ? this.calculateMainTotalInitialFeesAmount() : this.calculateChildTotalInitialFeesAmount();
            if (loanAmount === null || loanAmount < 0.0001) {
                var p = this.$q.defer();
                p.resolve({
                    AnnuityAmount: 0,
                    InitialCapitalDebtAmount: 0,
                    TotalPaidAmount: 0,
                    FlatAmortizationAmount: 0,
                    EffectiveInterestRatePercent: 0,
                    Payments: null
                });
                return p.promise;
            }
            else {
                return this.apiClient.calculatePaymentPlan({
                    LoanAmount: loanAmount,
                    RepaymentTimeInMonths: this.parseDecimalOrNull(isMain ? a.mainRepaymentTimeInMonths : a.childRepaymentTimeInMonths),
                    IsFlatAmortization: false,
                    CapitalizedInitialFeeAmount: initialFeeAmount,
                    MonthlyFeeAmount: this.parseDecimalOrNull(isMain ? a.mainNotificationFeeAmount : a.childNotificationFeeAmount),
                    TotalInterestRatePercent: this.currentReferenceInterestRate.value + this.parseDecimalOrNull(isMain ? a.mainMarginInterestRatePercent : a.childMarginInterestRatePercent),
                    IncludePayments: false,
                    MonthCountCapEvenIfNotFullyPaid: null
                });
            }
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.acceptNewLoan = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.withAi(function (ai) {
                var a = JSON.parse(JSON.stringify(_this.m.acceptModel)); //clone
                a.mainMarginInterestRatePercent = _this.formatNumberForStorage(_this.parseDecimalOrNull(a.mainMarginInterestRatePercent));
                a.childMarginInterestRatePercent = _this.formatNumberForStorage(_this.parseDecimalOrNull(a.childMarginInterestRatePercent));
                var uItems = a;
                _this.normalizeDecimalIfExists('mainNotificationFeeAmount', uItems);
                _this.normalizeDecimalIfExists('childNotificationFeeAmount', uItems);
                uItems['decisionType'] = _this.m.decisionType;
                _this.calculatePaymentPlan(a, true).then(function (mainPaymentPlan) {
                    uItems['mainHasLoan'] = (mainPaymentPlan.TotalPaidAmount > 0.0001) ? 'true' : 'false';
                    uItems['mainReferenceInterestRatePercent'] = _this.formatNumberForStorage(_this.referenceInterestRatePercent(true));
                    uItems['mainLoanAmount'] = _this.formatNumberForStorage(_this.calculateMainLoanAmount());
                    uItems['mainTotalInitialFeeAmount'] = _this.formatNumberForStorage(_this.calculateMainTotalInitialFeesAmount());
                    uItems['mainTotalSettlementAmount'] = _this.formatNumberForStorage(_this.m.decisionBasis.mainTotalSettlementAmount);
                    uItems['mainAnnuityAmount'] = _this.formatNumberForStorage(mainPaymentPlan.AnnuityAmount);
                    uItems['mainTotalPaidAmount'] = _this.formatNumberForStorage(mainPaymentPlan.TotalPaidAmount);
                    uItems['mainEffectiveInterestRatePercent'] = _this.formatNumberForStorage(mainPaymentPlan.EffectiveInterestRatePercent);
                    _this.calculatePaymentPlan(a, false).then(function (childPaymentPlan) {
                        uItems['childHasLoan'] = (childPaymentPlan.TotalPaidAmount > 0.0001) ? 'true' : 'false';
                        uItems['childReferenceInterestRatePercent'] = _this.formatNumberForStorage(_this.referenceInterestRatePercent(true));
                        uItems['childLoanAmount'] = _this.formatNumberForStorage(_this.calculateChildLoanAmount());
                        uItems['childTotalInitialFeeAmount'] = _this.formatNumberForStorage(_this.parseDecimalOrNull(_this.m.acceptModel.childInitialFeeAmount));
                        uItems['childTotalSettlementAmount'] = _this.formatNumberForStorage(_this.m.decisionBasis.childTotalSettlementAmount);
                        uItems['childAnnuityAmount'] = _this.formatNumberForStorage(childPaymentPlan.AnnuityAmount);
                        uItems['childTotalPaidAmount'] = _this.formatNumberForStorage(childPaymentPlan.TotalPaidAmount);
                        uItems['childEffectiveInterestRatePercent'] = _this.formatNumberForStorage(childPaymentPlan.EffectiveInterestRatePercent);
                        uItems['internalLoanToValue'] = _this.formatNumberForStorage(_this.calculateLtv());
                        _this.apiClient.createItemBasedCreditDecision({
                            ApplicationNr: _this.initialData.applicationNr,
                            IsAccepted: true,
                            SetAsCurrent: true,
                            DecisionType: _this.m.decisionType,
                            UniqueItems: uItems,
                            RepeatingItems: null
                        }).then(function (x) {
                            var isLoanPromise = a.applicationType === 'LoanPromise';
                            if (_this.m.isFinal && isLoanPromise) {
                                throw new Error("Loan promise is not allowed for the final step");
                            }
                            _this.apiClient.setMortgageApplicationWorkflowStatus(ai.ApplicationNr, _this.initialData.scoringWorkflowStepName, WorkflowHelper.AcceptedName, isLoanPromise ? 'Loan promise created' : "".concat(_this.m.decisionType, " credit check accepted"), null, isLoanPromise ? 'AddToApplicationList:LoanPromise' : 'RemoveFromApplicationList:LoanPromise').then(function (y) {
                                _this.onBack(null);
                            });
                        });
                    });
                });
            });
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.normalizeDecimalIfExists = function (name, d) {
            if (d && d[name]) {
                d[name] = this.formatNumberForStorage(this.parseDecimalOrNull(d[name]));
            }
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.onChanges = function () {
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.reload();
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.fetchCreditDecisionToStartFrom = function (cwf) {
            var _this = this;
            var d = this.$q.defer();
            this.apiClient.fetchItemBasedCreditDecision({ ApplicationNr: this.initialData.applicationNr, OnlyDecisionType: cwf.DecisionType, MaxCount: 1 }).then(function (x) {
                if (x.Decisions && x.Decisions.length > 0) {
                    //There is a stored decision of the current type, always use that in this case
                    d.resolve(x.Decisions[0]);
                }
                else if (!cwf.CopyFromDecisionType) {
                    //There is nothing to copy from so start from scratch
                    d.resolve(null);
                }
                else {
                    _this.apiClient.fetchItemBasedCreditDecision({ ApplicationNr: _this.initialData.applicationNr, OnlyDecisionType: cwf.CopyFromDecisionType, MaxCount: 1 }).then(function (y) {
                        if (y.Decisions && y.Decisions.length > 0) {
                            //Start from a decision from an earlier step
                            d.resolve(y.Decisions[0]);
                        }
                        else {
                            //No decision exists in the earlier step so start from scratch
                            d.resolve(null);
                        }
                    });
                }
            });
            return d.promise;
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.reload = function () {
            var _this = this;
            var cwf = this.getCustomWorkflowData();
            var isFinal = cwf.IsFinal === 'yes';
            var setModel = function () {
                _this.fetchCreditDecisionToStartFrom(cwf).then(function (d) {
                    var showAcceptedTab = true;
                    var reject = _this.createRejectModel([], isFinal);
                    var accept = _this.createAcceptModel(null, isFinal);
                    if (d) {
                        if (d.IsAccepted) {
                            showAcceptedTab = true;
                            accept = _this.createAcceptModel(d, isFinal);
                        }
                        else {
                            showAcceptedTab = false;
                            reject = _this.createRejectModel(d.RepeatingItems['rejectionReason'], isFinal);
                        }
                    }
                    _this.withAi(function (ai) { return _this.init(showAcceptedTab, reject, accept, ai); });
                });
            };
            if (this.currentReferenceInterestRate) {
                setModel();
            }
            else {
                this.apiClient.fetchCurrentReferenceInterestRate().then(function (x) {
                    _this.currentReferenceInterestRate = {
                        value: x
                    };
                    setModel();
                });
            }
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.getCustomWorkflowData = function () {
            var scoringStepModel = new WorkflowHelper.WorkflowStepModel(this.initialData.workflowModel, this.initialData.scoringWorkflowStepName);
            return scoringStepModel.getCustomStepData();
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.init = function (showAcceptedTab, reject, accept, ai) {
            var _this = this;
            var cwf = this.getCustomWorkflowData();
            var isFinal = cwf.IsFinal === 'yes';
            var backTarget = NavigationTargetHelper.createCodeTarget(isFinal ? NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreditCheckNewFinal : NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreditCheckNewInitial);
            MortgageLoanApplicationDualCreditCheckSharedNs.getLtvBasisAndLoanListNrs(this, ai.ApplicationNr, this.apiClient).then(function (d) {
                var hasCoApplicant = ai.NrOfApplicants > 1;
                var isEditAllowed = ai.IsActive && !ai.IsFinalDecisionMade && !ai.HasLockedAgreement;
                var reportsModel = [{ applicationNrAndApplicantNr: { applicationNr: ai.ApplicationNr, applicantNr: 1 }, customerId: null, creditReportProviderName: ai.CreditReportProviderName, listProviders: ai.ListCreditReportProviders }];
                if (hasCoApplicant) {
                    reportsModel.push({ applicationNrAndApplicantNr: { applicationNr: ai.ApplicationNr, applicantNr: 2 }, customerId: null, creditReportProviderName: ai.CreditReportProviderName, listProviders: ai.ListCreditReportProviders });
                }
                MortgageLoanApplicationDualCreditCheckSharedNs.getApplicantDataByApplicantNr(ai.ApplicationNr, hasCoApplicant, _this.apiClient).then(function (applicantDataByApplicantNr) {
                    MortgageLoanApplicationDualCreditCheckSharedNs.getCustomerCreditHistoryByApplicationNr(ai.ApplicationNr, _this.apiClient).then(function (customerCreditHistory) {
                        var m = {
                            isFinal: isFinal,
                            decisionType: cwf.DecisionType,
                            mode: showAcceptedTab ? 'acceptNewLoan' : 'reject',
                            isCalculating: false,
                            rejectModel: reject,
                            acceptModel: accept,
                            hasCoApplicant: hasCoApplicant,
                            isEditAllowed: ai.IsActive && !ai.IsFinalDecisionMade && !ai.HasLockedAgreement,
                            ltvBasis: {
                                customerCredits: customerCreditHistory,
                                valuationAmount: d.valuationAmount,
                                statValuationAmount: d.statValuationAmount,
                                priceAmount: d.priceAmount,
                                securityElsewhereAmount: d.securityElsewhereAmount,
                                housingCompanyLoans: d.housingCompanyLoans
                            },
                            decisionBasis: {
                                mainTotalSettlementAmount: d.mortgageLoansToSettleAmount,
                                childTotalSettlementAmount: d.otherLoansToSettleAmount
                            },
                            acceptModelComputed: null,
                            backTarget: backTarget,
                            b: MortgageLoanApplicationDualCreditCheckSharedNs.createDecisionBasisModel(false, _this, _this.apiClient, _this.$q, ai, hasCoApplicant, isEditAllowed, backTarget, d.mortgageLoanNrs, d.otherLoanNrs, isFinal, applicantDataByApplicantNr),
                            customerCreditReports: reportsModel
                        };
                        _this.m = m;
                        _this.registerTestFunctions();
                        if (showAcceptedTab) {
                            _this.updateAcceptModelComputed();
                        }
                    });
                });
            });
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.onAcceptModelChanged = function () {
            if (!this.m || !this.m.acceptModel) {
                return;
            }
            var a = this.m.acceptModel;
            var mainPurchaseAmount = this.parseDecimalOrNull(a.mainPurchaseAmount);
            if (a.applicationType === 'MoveExistingLoan' && mainPurchaseAmount && Math.abs(mainPurchaseAmount) > 0.00001) {
                a.mainPurchaseAmount = '';
            }
            this.updateAcceptModelComputed();
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.referenceInterestRatePercent = function (isMain) {
            if (!this.currentReferenceInterestRate) {
                return null;
            }
            return this.currentReferenceInterestRate.value;
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.totalInterestRatePercent = function (isMain) {
            if (!this.m || !this.m.acceptModel) {
                return null;
            }
            var refR = this.referenceInterestRatePercent(isMain);
            if (refR === null) {
                return null;
            }
            var r;
            if (isMain) {
                r = this.parseDecimalOrNull(this.m.acceptModel.mainMarginInterestRatePercent);
            }
            else {
                r = this.parseDecimalOrNull(this.m.acceptModel.childMarginInterestRatePercent);
            }
            if (r === null) {
                return null;
            }
            return r + refR;
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.updateAcceptModelComputed = function () {
            var _this = this;
            if (!this.m) {
                return;
            }
            var n = function (s) { var v = _this.parseDecimalOrNull(s); return v === null ? 0 : v; };
            var a = this.m.acceptModel;
            var mainEffectiveInterstRatePercent = null;
            var childEffectiveInterstRatePercent = null;
            var mainMonthlyAmount = null;
            var childMonthlyAmount = null;
            var ps = [];
            var mainLoanAmount = this.calculateMainLoanAmount();
            if (mainLoanAmount && mainLoanAmount > 0) {
                var r_1 = {
                    LoanAmount: mainLoanAmount,
                    RepaymentTimeInMonths: n(a.mainRepaymentTimeInMonths),
                    MonthlyFeeAmount: n(a.mainNotificationFeeAmount),
                    CapitalizedInitialFeeAmount: this.calculateMainTotalInitialFeesAmount(),
                    TotalInterestRatePercent: this.totalInterestRatePercent(true),
                    IsFlatAmortization: false
                };
                if (r_1.TotalInterestRatePercent > 0 && r_1.RepaymentTimeInMonths > 0) {
                    ps.push(this.apiClient.calculatePaymentPlan(r_1).then(function (x) {
                        mainEffectiveInterstRatePercent = x.EffectiveInterestRatePercent;
                        mainMonthlyAmount = x.AnnuityAmount + r_1.MonthlyFeeAmount;
                    }));
                }
            }
            var childLoanAmount = this.calculateChildLoanAmount();
            if (childLoanAmount && childLoanAmount > 0) {
                var r_2 = {
                    LoanAmount: childLoanAmount,
                    RepaymentTimeInMonths: n(a.childRepaymentTimeInMonths),
                    MonthlyFeeAmount: n(a.childNotificationFeeAmount),
                    CapitalizedInitialFeeAmount: this.calculateChildTotalInitialFeesAmount(),
                    TotalInterestRatePercent: this.totalInterestRatePercent(false),
                    IsFlatAmortization: false
                };
                if (r_2.TotalInterestRatePercent > 0 && r_2.RepaymentTimeInMonths > 0) {
                    ps.push(this.apiClient.calculatePaymentPlan(r_2).then(function (x) {
                        childEffectiveInterstRatePercent = x.EffectiveInterestRatePercent;
                        childMonthlyAmount = x.AnnuityAmount + r_2.MonthlyFeeAmount;
                    }));
                }
            }
            this.$q.all(ps).then(function (_) {
                _this.m.acceptModelComputed = {
                    mainEffectiveInterstRatePercent: mainEffectiveInterstRatePercent,
                    mainMonthlyAmount: mainMonthlyAmount,
                    childEffectiveInterstRatePercent: childEffectiveInterstRatePercent,
                    childMonthlyAmount: childMonthlyAmount,
                };
            });
        };
        MortgageLoanApplicationDualCreditCheckNewController.prototype.registerTestFunctions = function () {
            var _this = this;
            if (!this.initialData || !this.initialData.isTest) {
                return;
            }
            var t = this.initialData.testFunctions;
            var scopeName = t.generateUniqueScopeName();
            t.addFunctionCall(scopeName, 'Fill offer', function () {
                var isCurrentlyNewLoan = _this.m.acceptModel && _this.m.acceptModel.applicationType === 'NewLoan';
                _this.m.acceptModel = {
                    applicationType: isCurrentlyNewLoan ? 'MoveExistingLoan' : 'NewLoan',
                    mainInitialFeeAmount: '150',
                    mainNotificationFeeAmount: '2,5',
                    mainValuationFeeAmount: '35',
                    mainDeedFeeAmount: '40',
                    mainMortgageApplicationFeeAmount: '45',
                    mainPurchaseAmount: isCurrentlyNewLoan ? '' : '135000',
                    mainDirectToCustomerAmount: '0',
                    mainMarginInterestRatePercent: '4.1',
                    mainRepaymentTimeInMonths: '240',
                    childInitialFeeAmount: '120',
                    childNotificationFeeAmount: '2,5',
                    childDirectToCustomerAmount: '7500',
                    childMarginInterestRatePercent: '4.1',
                    childRepaymentTimeInMonths: '240'
                };
                _this.onAcceptModelChanged();
            });
            t.addFunctionCall(scopeName, 'Add other loans', function () {
                if (_this.m.b.additionalCurrentOtherLoans.length > 0 || _this.m.b.currentMortgageLoans.length > 0) {
                    return;
                }
                var ps = [];
                var add = function (name, value) {
                    ps.push(_this.apiClient.setApplicationEditItemData(_this.initialData.applicationNr, 'ComplexApplicationList', name, value, false));
                };
                for (var _i = 0, _a = [true, false]; _i < _a.length; _i++) {
                    var isMortgage = _a[_i];
                    var listName = isMortgage ? 'CurrentMortgageLoans' : 'CurrentOtherLoans';
                    add("".concat(listName, "#").concat(isMortgage ? 1 : 3, "#u#exists"), 'true');
                    add("".concat(listName, "#").concat(isMortgage ? 1 : 3, "#u#bankName"), "".concat(listName, " settled loan bank"));
                    add("".concat(listName, "#").concat(isMortgage ? 1 : 3, "#u#loanTotalAmount"), isMortgage ? '23000' : '24000');
                    add("".concat(listName, "#").concat(isMortgage ? 1 : 3, "#u#loanMonthlyAmount"), isMortgage ? '146' : '147');
                    add("".concat(listName, "#").concat(isMortgage ? 1 : 3, "#u#loanShouldBeSettled"), 'true');
                    add("".concat(listName, "#").concat(isMortgage ? 1 : 3, "#u#loanApplicant1IsParty"), 'true');
                    add("".concat(listName, "#").concat(isMortgage ? 1 : 3, "#u#loanApplicant2IsParty"), 'false');
                    add("".concat(listName, "#").concat(isMortgage ? 2 : 4, "#u#exists"), 'true');
                    add("".concat(listName, "#").concat(isMortgage ? 2 : 4, "#u#bankName"), "".concat(listName, " not settled loan bank"));
                    add("".concat(listName, "#").concat(isMortgage ? 2 : 4, "#u#loanTotalAmount"), isMortgage ? '21000' : '22000');
                    add("".concat(listName, "#").concat(isMortgage ? 2 : 4, "#u#loanMonthlyAmount"), isMortgage ? '144' : '145');
                    add("".concat(listName, "#").concat(isMortgage ? 2 : 4, "#u#loanShouldBeSettled"), 'false');
                    add("".concat(listName, "#").concat(isMortgage ? 2 : 4, "#u#loanApplicant1IsParty"), 'true');
                    add("".concat(listName, "#").concat(isMortgage ? 2 : 4, "#u#loanApplicant2IsParty"), 'false');
                }
                _this.$q.all(ps).then(function (x) {
                    _this.signalReloadRequired();
                });
            });
            //Prototype service ONLY
            //SelfContainedModules => PSD2Prototype
            t.addFunctionCall(scopeName, 'Get PSD2 File', function () {
                window.open("https://psd2-prototype.naktergaltech.se");
            });
        };
        MortgageLoanApplicationDualCreditCheckNewController.$inject = ['$http', '$q', 'ntechComponentService', '$scope', '$timeout'];
        return MortgageLoanApplicationDualCreditCheckNewController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanApplicationDualCreditCheckNewComponentNs.MortgageLoanApplicationDualCreditCheckNewController = MortgageLoanApplicationDualCreditCheckNewController;
    var MortgageLoanApplicationDualCreditCheckNewComponent = /** @class */ (function () {
        function MortgageLoanApplicationDualCreditCheckNewComponent() {
            this.acceptTabMainLoanTemplate = "<h2 class=\"text-center\">Mortgage loan</h2>\n                <div class=\"row pt-1\">\n                    <div class=\"col-xs-4\">\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Initial fee</label>\n                            <div class=\"col-xs-6\"><input type=\"text\" class=\"form-control\" custom-validate=\"$ctrl.isValidPositiveInt\" ng-model=\"$ctrl.m.acceptModel.mainInitialFeeAmount\" required ng-change=\"$ctrl.onAcceptModelChanged()\"></div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Notification fee</label>\n                            <div class=\"col-xs-6\"><input type=\"text\" class=\"form-control\" custom-validate=\"$ctrl.isValidPositiveDecimal\" ng-model=\"$ctrl.m.acceptModel.mainNotificationFeeAmount\" required ng-change=\"$ctrl.onAcceptModelChanged()\"></div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Valuation fee</label>\n                            <div class=\"col-xs-6\"><input type=\"text\" class=\"form-control\" custom-validate=\"$ctrl.isValidPositiveInt\" ng-model=\"$ctrl.m.acceptModel.mainValuationFeeAmount\" required ng-change=\"$ctrl.onAcceptModelChanged()\"></div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Deed fee</label>\n                            <div class=\"col-xs-6\"><input type=\"text\" class=\"form-control\" custom-validate=\"$ctrl.isValidPositiveInt\" ng-model=\"$ctrl.m.acceptModel.mainDeedFeeAmount\" required ng-change=\"$ctrl.onAcceptModelChanged()\"></div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Mortgage app. fee</label>\n                            <div class=\"col-xs-6\"><input type=\"text\" class=\"form-control\" custom-validate=\"$ctrl.isValidPositiveInt\" ng-model=\"$ctrl.m.acceptModel.mainMortgageApplicationFeeAmount\" required ng-change=\"$ctrl.onAcceptModelChanged()\"></div>\n                        </div>\n                    </div>\n                    <div class=\"col-xs-4\">\n                        <div class=\"form-group\" ng-show=\"$ctrl.m.acceptModel.applicationType !=='MoveExistingLoan'\">\n                            <label class=\"col-xs-6 control-label\">Purchase amount</label>\n                            <div class=\"col-xs-6\"><input type=\"text\" class=\"form-control\" custom-validate=\"$ctrl.isValidPositiveInt\" ng-model=\"$ctrl.m.acceptModel.mainPurchaseAmount\" ng-required=\"$ctrl.m.acceptModel.applicationType !=='MoveExistingLoan'\" ng-change=\"$ctrl.onAcceptModelChanged()\"></div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Payment to customer</label>\n                            <div class=\"col-xs-6\"><input type=\"text\" class=\"form-control\" custom-validate=\"$ctrl.isValidPositiveInt\" ng-model=\"$ctrl.m.acceptModel.mainDirectToCustomerAmount\" required ng-change=\"$ctrl.onAcceptModelChanged()\"></div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Total settlement amount</label>\n                            <div class=\"col-xs-6\"><p class=\"form-control-static\">{{$ctrl.m.decisionBasis.mainTotalSettlementAmount | number:'2'}}</p></div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Total amount</label>\n                            <div class=\"col-xs-6\"><p class=\"form-control-static\">{{$ctrl.calculateTotalAmount(true) | number:'2'}}</p></div>\n                        </div>\n                    </div>\n                    <div class=\"col-xs-4\">\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Margin interest rate</label>\n                            <div class=\"col-xs-6\"><input type=\"text\" class=\"form-control\" custom-validate=\"$ctrl.isValidPositiveDecimal\" ng-model=\"$ctrl.m.acceptModel.mainMarginInterestRatePercent\" required ng-change=\"$ctrl.onAcceptModelChanged()\"></div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Reference interest rate</label>\n                            <div class=\"col-xs-6\"><p class=\"form-control-static\">{{$ctrl.referenceInterestRatePercent(true) | number:'2'}}  %</p></div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Total interest rate</label>\n                            <div class=\"col-xs-6\"><p class=\"form-control-static\">{{$ctrl.totalInterestRatePercent(true) | number:'2'}}  %</p></div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Repayment time</label>\n                            <div class=\"col-xs-6\"><input type=\"text\" class=\"form-control\" custom-validate=\"$ctrl.isValidPositiveInt\" ng-model=\"$ctrl.m.acceptModel.mainRepaymentTimeInMonths\" required ng-change=\"$ctrl.onAcceptModelChanged()\"></div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Eff. interest rate</label>\n                            <div class=\"col-xs-6\"><p class=\"form-control-static\">{{$ctrl.m.acceptModelComputed.mainEffectiveInterstRatePercent | number:'2'}}  %</p></div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Monthly amount</label>\n                            <div class=\"col-xs-6\"><p class=\"form-control-static\">{{$ctrl.m.acceptModelComputed.mainMonthlyAmount | number:'2'}} </p></div>\n                        </div>\n                    </div>\n                </div>";
            this.acceptTabChildLoanTemplate = "<h2 class=\"text-center\">Loan with collateral</h2>\n                <div class=\"row pt-1\">\n                    <div class=\"col-xs-4\">\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Initial fee</label>\n                            <div class=\"col-xs-6\"><input type=\"text\" class=\"form-control\" custom-validate=\"$ctrl.isValidPositiveInt\" ng-model=\"$ctrl.m.acceptModel.childInitialFeeAmount\" required ng-change=\"$ctrl.onAcceptModelChanged()\"></div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Notification fee</label>\n                            <div class=\"col-xs-6\"><input type=\"text\" class=\"form-control\" custom-validate=\"$ctrl.isValidPositiveDecimal\" ng-model=\"$ctrl.m.acceptModel.childNotificationFeeAmount\" required ng-change=\"$ctrl.onAcceptModelChanged()\"></div>\n                        </div>\n                    </div>\n\n                    <div class=\"col-xs-4\">\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Payment to customer</label>\n                            <div class=\"col-xs-6\"><input type=\"text\" class=\"form-control\" custom-validate=\"$ctrl.isValidPositiveInt\" ng-model=\"$ctrl.m.acceptModel.childDirectToCustomerAmount\" required ng-change=\"$ctrl.onAcceptModelChanged()\"></div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Total settlement amount</label>\n                            <div class=\"col-xs-6\"><p class=\"form-control-static\">{{$ctrl.m.decisionBasis.childTotalSettlementAmount | number:'2'}}</p></div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Total amount</label>\n                            <div class=\"col-xs-6\"><p class=\"form-control-static\">{{$ctrl.calculateTotalAmount(false) | number:'2'}}</p></div>\n                        </div>\n                    </div>\n\n                    <div class=\"col-xs-4\">\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Margin interest rate</label>\n                            <div class=\"col-xs-6\"><input type=\"text\" class=\"form-control\" custom-validate=\"$ctrl.isValidPositiveDecimal\" ng-model=\"$ctrl.m.acceptModel.childMarginInterestRatePercent\" required ng-change=\"$ctrl.onAcceptModelChanged()\"></div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Reference interest rate</label>\n                            <div class=\"col-xs-6\"><p class=\"form-control-static\">{{$ctrl.referenceInterestRatePercent(false) | number:'2'}}  %</p></div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Total interest rate</label>\n                            <div class=\"col-xs-6\"><p class=\"form-control-static\">{{$ctrl.totalInterestRatePercent(false) | number:'2'}}  %</p></div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Repayment time</label>\n                            <div class=\"col-xs-6\"><input type=\"text\" class=\"form-control\" custom-validate=\"$ctrl.isValidPositiveInt\" ng-model=\"$ctrl.m.acceptModel.childRepaymentTimeInMonths\" required ng-change=\"$ctrl.onAcceptModelChanged()\"></div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Eff. interest rate</label>\n                            <div class=\"col-xs-6\"><p class=\"form-control-static\">{{$ctrl.m.acceptModelComputed.childEffectiveInterstRatePercent | number:'2'}}  %</p></div>\n                        </div>\n                        <div class=\"form-group\">\n                            <label class=\"col-xs-6 control-label\">Monthly amount</label>\n                            <div class=\"col-xs-6\"><p class=\"form-control-static\">{{$ctrl.m.acceptModelComputed.childMonthlyAmount | number:'2'}} </p></div>\n                        </div>\n                    </div>\n                </div>";
            this.acceptTabTemplate = "<form class=\"form-horizontal decision-form\" name=\"acceptform\" bootstrap-validation=\"'parent'\" novalidate ng-show=\"$ctrl.m.mode=='acceptNewLoan'\">\n                <div class=\"form-group\">\n                    <label class=\"col-xs-4 control-label\">Type</label>\n                    <div class=\"col-xs-4\">\n                        <select class=\"form-control\" ng-model=\"$ctrl.m.acceptModel.applicationType\" ng-change=\"$ctrl.onAcceptModelChanged()\">\n                            <option ng-if=\"!$ctrl.m.isFinal\" value=\"LoanPromise\">Loan prospect</option>\n                            <option value=\"NewLoan\">New loan</option>\n                            <option value=\"MoveExistingLoan\">Move existing loan</option>\n                        </select>\n                    </div>\n                </div>\n\n                <div class=\"pt-1\">\n                    <hr class=\"hr-section dotted\" />\n                </div>\n                ".concat(this.acceptTabMainLoanTemplate, "\n\n                <div class=\"pt-1\">\n                    <hr class=\"hr-section dotted\" />\n                </div>\n                ").concat(this.acceptTabChildLoanTemplate, "\n\n                <div class=\"pt-1\">\n                    <hr class=\"hr-section dotted\" />\n                </div>\n\n                <div class=\"form-group\">\n                    <label class=\"col-xs-6 control-label\">Loan to value</label>\n                    <div class=\"col-xs-6\"><p class=\"form-control-static\">{{ ($ctrl.calculateLtv() * 100) | number:'2'}}%</p></div>\n                 </div>\n\n                <div class=\"text-center pt-3\">\n                    <button type=\"button\" class=\"n-main-btn n-green-btn\" ng-class=\"{ disabled : $ctrl.m.isPendingValidation || acceptform.$invalid }\" ng-click=\"$ctrl.acceptNewLoan($event)\">Accept</button>\n                </div>\n            </form>");
            this.rejectTabTemplate = "<form name=\"rejectform\" novalidate class=\"form-horizontal decision-form\" ng-show=\"$ctrl.m.mode=='reject'\">\n                <h4 class=\"text-center\">Rejection reasons</h4>\n                <div class=\"row\">\n                    <div class=\"col-sm-6 col-md-6\">\n                        <div class=\"form-group\" ng-repeat=\"b in $ctrl.m.rejectModel.rejectModelCheckboxesCol1\">\n                            <label class=\"col-md-8 control-label\">{{b.displayName}}</label>\n                            <div class=\"col-md-4\"><div class=\"checkbox\"><input type=\"checkbox\" ng-model=\"$ctrl.m.rejectModel.reasons[b.reason]\"></div></div>\n                        </div>\n                    </div>\n                    <div class=\"col-sm-6 col-md-6\">\n                        <div class=\"form-group\" ng-repeat=\"b in $ctrl.m.rejectModel.rejectModelCheckboxesCol2\">\n                            <label class=\"col-md-6 control-label\">{{b.displayName}}</label>\n                            <div class=\"col-md-4\"><div class=\"checkbox\"><input type=\"checkbox\" ng-model=\"$ctrl.m.rejectModel.reasons[b.reason]\"></div></div>\n                        </div>\n                    </div>\n                </div>\n                <div class=\"form-group\">\n                    <label class=\"col-md-4 control-label\">Other</label>\n                    <div class=\"col-md-6\"><input type=\"text\" class=\"form-control\" ng-model=\"$ctrl.m.rejectModel.otherReason\"></div>\n                </div>\n                <div class=\"text-center pt-3\">\n                    <button type=\"button\" class=\"n-main-btn n-red-btn\" ng-disabled=\"!$ctrl.anyRejectionReasonGiven()\" ng-click=\"$ctrl.reject($event)\">Reject</button>\n                </div>\n            </form>";
            this.decisionBasisTemplate = MortgageLoanApplicationDualCreditCheckSharedNs.getDecisionBasisHtmlTemplate(true);
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationDualCreditCheckNewController;
            this.template = "<div ng-if=\"$ctrl.m\">\n\n    <div class=\"pt-1 pb-2\">\n        <div class=\"pull-left\"><a class=\"n-back\" href=\"#\" ng-click=\"$ctrl.onBack($event)\"><span class=\"glyphicon glyphicon-arrow-left\"></span></a></div>\n        <h1 class=\"adjusted\">{{$ctrl.m.isFinal ? 'Final' : 'Initial'}} credit check</h1>\n    </div>\n\n    <div class=\"row\">\n        <div class=\"col-xs-10 col-sm-offset-1\">\n            <div class=\"row\">\n                <div class=\"col-sm-offset-4 col-xs-2\">\n                    <span type=\"button\" class=\"btn\" ng-class=\"{ disabled : $ctrl.m.isCalculating, 'decision-form-active-btn' : $ctrl.m.mode =='reject', 'decision-form-inactive-btn' : $ctrl.m.mode !='reject'}\" ng-click=\"$ctrl.setAcceptRejectMode('reject', $event)\">\n                        Reject\n                    </span>\n                </div>\n                <div class=\"col-xs-2\">\n                    <span type=\"button\" class=\"btn\" ng-class=\"{ disabled : $ctrl.m.isCalculating, 'decision-form-active-btn' : $ctrl.m.mode =='acceptNewLoan', 'decision-form-inactive-btn' : $ctrl.m.mode !='acceptNewLoan' }\" ng-click=\"$ctrl.setAcceptRejectMode('acceptNewLoan', $event)\">\n                        New Loan\n                    </span>\n                </div>\n            </div>\n\n            ".concat(this.rejectTabTemplate, "\n\n            ").concat(this.acceptTabTemplate, "\n        </div>\n    </div>\n\n    ").concat(this.decisionBasisTemplate, "\n</div>");
        }
        return MortgageLoanApplicationDualCreditCheckNewComponent;
    }());
    MortgageLoanApplicationDualCreditCheckNewComponentNs.MortgageLoanApplicationDualCreditCheckNewComponent = MortgageLoanApplicationDualCreditCheckNewComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageLoanApplicationDualCreditCheckNewComponentNs.Model = Model;
    var RejectModel = /** @class */ (function () {
        function RejectModel() {
        }
        return RejectModel;
    }());
    MortgageLoanApplicationDualCreditCheckNewComponentNs.RejectModel = RejectModel;
    var AcceptModel = /** @class */ (function () {
        function AcceptModel() {
        }
        return AcceptModel;
    }());
    MortgageLoanApplicationDualCreditCheckNewComponentNs.AcceptModel = AcceptModel;
    var RejectionCheckboxModel = /** @class */ (function () {
        function RejectionCheckboxModel(reason, displayName) {
            this.reason = reason;
            this.displayName = displayName;
        }
        return RejectionCheckboxModel;
    }());
    MortgageLoanApplicationDualCreditCheckNewComponentNs.RejectionCheckboxModel = RejectionCheckboxModel;
})(MortgageLoanApplicationDualCreditCheckNewComponentNs || (MortgageLoanApplicationDualCreditCheckNewComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanApplicationDualCreditCheckNew', new MortgageLoanApplicationDualCreditCheckNewComponentNs.MortgageLoanApplicationDualCreditCheckNewComponent());
