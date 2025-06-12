namespace MortgageLoanApplicationDualCreditCheckNewComponentNs {
    export class MortgageLoanApplicationDualCreditCheckNewController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model
        currentReferenceInterestRate: { value: number }

        static $inject = ['$http', '$q', 'ntechComponentService', '$scope', '$timeout']
        constructor(
            private $http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private $scope: IScope) {
            super(ntechComponentService, $http, $q);

            this.ntechComponentService.subscribeToReloadRequired(() => {
                this.reload()
            })
        }

        componentName(): string {
            return 'mortgageLoanApplicationDualCreditCheckNew'
        }

        onBack(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            NavigationTargetHelper.handleBack(
                NavigationTargetHelper.createCodeTarget(NavigationTargetHelper.NavigationTargetCode.MortgageLoanApplication),
                this.apiClient,
                this.$q, {
                applicationNr: this.initialData.applicationNr
            })
        }

        setAcceptRejectMode(mode: string, evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (this.m) {
                this.m.mode = mode
            }
        }


        calculateLtv(): number | null {
            if (!this.initialData || !this.m || !this.m.ltvBasis)
                return null;

            let valuationAmount = this.calculateValuationAmount();
            let l = this.calculateLoanAmount();
            let ltv = 0;
            if (valuationAmount > 0.00001)
                ltv = l / valuationAmount;

            return ltv;
        }

        calculateValuationAmount(): number | null {
            if (!this.initialData || !this.m || !this.m.ltvBasis)
                return null;

            let valuationAmounts = [];
            this.m.ltvBasis.valuationAmount.forEach((element) => this.getArrWithElement(valuationAmounts, element));
            this.m.ltvBasis.statValuationAmount.forEach((element) => this.getArrWithElement(valuationAmounts, element));
            this.m.ltvBasis.priceAmount.forEach((element) => this.getArrWithElement(valuationAmounts, element));

            let sumValuationAmounts = valuationAmounts.reduce(function (a, b) {
                return a + b["value"];
            }, 0)

            return sumValuationAmounts;
        }


        getArrWithElement(arr: { key: number, value: number }[], element: { key: number, value: number }): { key: number, value: number }[] {
            let exists = arr.some(function (o) { return o["key"] === element.key; })
            if (!exists)
                arr.push({ key: element.key, value: element.value });

            return arr;
        }

        calculateMainTotalInitialFeesAmount(): number | null {
            let m = this.m
            if (!m)
                return null

            let totalFeesAmount = 0;
            for (let feeTypeName of ['Initial', 'Valuation', 'Deed', 'MortgageApplication']) {
                totalFeesAmount += this.parseDecimalOrNull(m.acceptModel[`main${feeTypeName}FeeAmount`]);
            }
            return totalFeesAmount;
        }

        calculateChildTotalInitialFeesAmount(): number | null {
            let m = this.m;
            if (!m) {
                return null;
            }
            return this.parseDecimalOrNull(m.acceptModel.childInitialFeeAmount);
        }

        calculateMainLoanAmount(): number | null {
            let m = this.m
            if (!m) {
                return null
            }
            return m.decisionBasis.mainTotalSettlementAmount
                + this.parseDecimalOrNull(m.acceptModel.mainPurchaseAmount)
                + this.parseDecimalOrNull(m.acceptModel.mainDirectToCustomerAmount);
        }


        calculateSecurityElsewhereAmount(): number | null {
            let m = this.m;
            if (!m)
                return null;

            if (m.ltvBasis.securityElsewhereAmount.length === 0)
                return 0;

            let sumSecurityElsewhereAmounts = m.ltvBasis.securityElsewhereAmount
                .reduce((a, b) => a + b, 0);

            return this.parseDecimalOrNull(sumSecurityElsewhereAmounts);
        }

        calculateHousingCompanyLoans(): number | null {
            let m = this.m;
            if (!m)
                return null;

            if (m.ltvBasis.housingCompanyLoans.length === 0)
                return 0;

            let sumHousingCompanyLoans = m.ltvBasis.housingCompanyLoans
                .reduce((a, b) => a + b, 0);

            return this.parseDecimalOrNull(sumHousingCompanyLoans);
        }

        calculateNewLoansAmount(): number | null {
            let m = this.m;
            if (!m)
                return null;

            let mainTotalAmount = (this.calculateMainLoanAmount()
                + this.calculateMainTotalInitialFeesAmount());
            let childTotalAmount = (this.calculateChildLoanAmount()
                + this.calculateChildTotalInitialFeesAmount());

            return (mainTotalAmount
                + childTotalAmount);
        }

        calculateExistingLoansAmount(): number | null {
            let m = this.m;
            if (!m)
                return null;

            let sumCustomerCreditCapitalBalance = m.ltvBasis.customerCredits.reduce(function (a, b) {
                return a + b["CapitalBalance"];
            }, 0)

            return sumCustomerCreditCapitalBalance;
        }

        calculateChildLoanAmount(): number | null {
            let m = this.m;
            if (!m) {
                return null;
            }

            return m.decisionBasis.childTotalSettlementAmount
                + this.parseDecimalOrNull(m.acceptModel.childDirectToCustomerAmount);
        }

        calculateTotalAmount(isMain: boolean): number {
            if (isMain) {
                return this.calculateMainLoanAmount() + this.calculateMainTotalInitialFeesAmount()
            } else {
                return this.calculateChildLoanAmount() + this.calculateChildTotalInitialFeesAmount()
            }
        }
        calculateLoanAmount(): number {
            let m = this.m;
            if (!m)
                return null;

            let collateralLoansAmount = (this.calculateSecurityElsewhereAmount()
                + this.calculateHousingCompanyLoans());
            let l = (this.calculateNewLoansAmount()
                + this.calculateExistingLoansAmount()
                + collateralLoansAmount);

            return this.parseDecimalOrNull(l);
        }

        getRejectionReasons(): {
            rejectionReasons: string[],
            rejectionReasonDisplayNames: string[]
        } {
            if (!this.m || !this.m.rejectModel) {
                return null
            }

            let reasons: string[] = []
            let displayNames: string[] = []
            for (let key of Object.keys(this.m.rejectModel.reasons)) {
                if (this.m.rejectModel.reasons[key] === true) {
                    reasons.push(key)
                    displayNames.push(this.initialData.rejectionReasonToDisplayNameMapping[key])
                }
            }
            if (!this.isNullOrWhitespace(this.m.rejectModel.otherReason)) {
                let otherReason = 'other: ' + this.m.rejectModel.otherReason
                reasons.push(otherReason)
                displayNames.push(otherReason)
            }

            return { rejectionReasons: reasons, rejectionReasonDisplayNames: displayNames }
        }

        anyRejectionReasonGiven() {
            var reasons = this.getRejectionReasons()
            return reasons && reasons.rejectionReasons && reasons.rejectionReasons.length > 0
        }

        buyNewSatReport(applicantNr) {
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
            }).then(function successCallback(response: any) {
                console.log("Success!");
            }, function errorCallback(response) {
                //$scope.satUi['applicant' + applicantNr] = { isLoading: false }
                toastr.error(response.statusText)
            })
        }

        private createAcceptModel(d: NTechPreCreditApi.ItemBasedDecisionModel, isFinal: boolean): AcceptModel {
            if (d) {
                return (d.UniqueItems as any) as AcceptModel
            } else {
                return {
                    applicationType: isFinal ? '' : 'LoanPromise'
                }
            }
        }

        private createRejectModel(initialReasons: string[], isFinal: boolean): RejectModel {
            let r: RejectModel = {
                otherReason: '',
                reasons: {},
                rejectModelCheckboxesCol1: [],
                rejectModelCheckboxesCol2: [],
                initialReasons: initialReasons
            }

            for (let reasonName of Object.keys(this.initialData.rejectionReasonToDisplayNameMapping)) {
                let displayName = this.initialData.rejectionReasonToDisplayNameMapping[reasonName]
                if (r.rejectModelCheckboxesCol1.length > r.rejectModelCheckboxesCol2.length) {
                    r.rejectModelCheckboxesCol2.push(new RejectionCheckboxModel(reasonName, displayName))
                } else {
                    r.rejectModelCheckboxesCol1.push(new RejectionCheckboxModel(reasonName, displayName))
                }
            }

            if (r.initialReasons) {
                for (let reasonName of r.initialReasons) {
                    r.reasons[reasonName] = true
                }
            }

            return r
        }

        withAi(a: (ai: NTechPreCreditApi.ApplicationInfoModel) => void) {
            this.apiClient.fetchApplicationInfo(this.initialData.applicationNr).then(ai => {
                a(ai)
            })
        }

        reject(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            this.withAi(ai => {
                let reasonsData = this.getRejectionReasons()
                this.apiClient.createItemBasedCreditDecision({
                    ApplicationNr: this.initialData.applicationNr,
                    IsAccepted: false,
                    SetAsCurrent: true,
                    DecisionType: this.m.decisionType,
                    RepeatingItems: {
                        rejectionReason: reasonsData.rejectionReasons,
                        rejectionReasonText: reasonsData.rejectionReasonDisplayNames
                    },
                    UniqueItems: {
                        decisionType: this.m.decisionType
                    },
                    RejectionReasonsItemName: 'rejectionReason'
                }).then(x => {
                    this.apiClient.setMortgageApplicationWorkflowStatus(
                        ai.ApplicationNr,
                        this.initialData.scoringWorkflowStepName, WorkflowHelper.RejectedName, `${this.m.decisionType} credit check rejected`).then(y => {
                            this.onBack(null)
                        })
                })
            })
        }

        private calculatePaymentPlan(a: AcceptModel, isMain: boolean): ng.IPromise<NTechPreCreditApi.CalculatedPaymentPlan> {
            let loanAmount = isMain ? this.calculateMainLoanAmount() : this.calculateChildLoanAmount()
            let initialFeeAmount = isMain ? this.calculateMainTotalInitialFeesAmount() : this.calculateChildTotalInitialFeesAmount()
            if (loanAmount === null || loanAmount < 0.0001) {
                let p = this.$q.defer<NTechPreCreditApi.CalculatedPaymentPlan>()
                p.resolve({
                    AnnuityAmount: 0,
                    InitialCapitalDebtAmount: 0,
                    TotalPaidAmount: 0,
                    FlatAmortizationAmount: 0,
                    EffectiveInterestRatePercent: 0,
                    Payments: null
                })
                return p.promise
            } else {
                return this.apiClient.calculatePaymentPlan({
                    LoanAmount: loanAmount,
                    RepaymentTimeInMonths: this.parseDecimalOrNull(isMain ? a.mainRepaymentTimeInMonths : a.childRepaymentTimeInMonths),
                    IsFlatAmortization: false,
                    CapitalizedInitialFeeAmount: initialFeeAmount,
                    MonthlyFeeAmount: this.parseDecimalOrNull(isMain ? a.mainNotificationFeeAmount : a.childNotificationFeeAmount),
                    TotalInterestRatePercent: this.currentReferenceInterestRate.value + this.parseDecimalOrNull(isMain ? a.mainMarginInterestRatePercent : a.childMarginInterestRatePercent),
                    IncludePayments: false,
                    MonthCountCapEvenIfNotFullyPaid: null
                })
            }
        }

        acceptNewLoan(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }

            this.withAi(ai => {
                let a: AcceptModel = JSON.parse(JSON.stringify(this.m.acceptModel)) //clone
                a.mainMarginInterestRatePercent = this.formatNumberForStorage(this.parseDecimalOrNull(a.mainMarginInterestRatePercent))
                a.childMarginInterestRatePercent = this.formatNumberForStorage(this.parseDecimalOrNull(a.childMarginInterestRatePercent))
                let uItems: NTechPreCreditApi.IStringDictionary<string> = (a as any) as NTechPreCreditApi.IStringDictionary<string>

                this.normalizeDecimalIfExists('mainNotificationFeeAmount', uItems)
                this.normalizeDecimalIfExists('childNotificationFeeAmount', uItems)

                uItems['decisionType'] = this.m.decisionType
                this.calculatePaymentPlan(a, true).then(mainPaymentPlan => {
                    uItems['mainHasLoan'] = (mainPaymentPlan.TotalPaidAmount > 0.0001) ? 'true' : 'false'
                    uItems['mainReferenceInterestRatePercent'] = this.formatNumberForStorage(this.referenceInterestRatePercent(true))
                    uItems['mainLoanAmount'] = this.formatNumberForStorage(this.calculateMainLoanAmount())
                    uItems['mainTotalInitialFeeAmount'] = this.formatNumberForStorage(this.calculateMainTotalInitialFeesAmount())
                    uItems['mainTotalSettlementAmount'] = this.formatNumberForStorage(this.m.decisionBasis.mainTotalSettlementAmount)
                    uItems['mainAnnuityAmount'] = this.formatNumberForStorage(mainPaymentPlan.AnnuityAmount)
                    uItems['mainTotalPaidAmount'] = this.formatNumberForStorage(mainPaymentPlan.TotalPaidAmount)
                    uItems['mainEffectiveInterestRatePercent'] = this.formatNumberForStorage(mainPaymentPlan.EffectiveInterestRatePercent)

                    this.calculatePaymentPlan(a, false).then(childPaymentPlan => {
                        uItems['childHasLoan'] = (childPaymentPlan.TotalPaidAmount > 0.0001) ? 'true' : 'false'
                        uItems['childReferenceInterestRatePercent'] = this.formatNumberForStorage(this.referenceInterestRatePercent(true))
                        uItems['childLoanAmount'] = this.formatNumberForStorage(this.calculateChildLoanAmount())
                        uItems['childTotalInitialFeeAmount'] = this.formatNumberForStorage(this.parseDecimalOrNull(this.m.acceptModel.childInitialFeeAmount))
                        uItems['childTotalSettlementAmount'] = this.formatNumberForStorage(this.m.decisionBasis.childTotalSettlementAmount)
                        uItems['childAnnuityAmount'] = this.formatNumberForStorage(childPaymentPlan.AnnuityAmount)
                        uItems['childTotalPaidAmount'] = this.formatNumberForStorage(childPaymentPlan.TotalPaidAmount)
                        uItems['childEffectiveInterestRatePercent'] = this.formatNumberForStorage(childPaymentPlan.EffectiveInterestRatePercent)
                        uItems['internalLoanToValue'] = this.formatNumberForStorage(this.calculateLtv())

                        this.apiClient.createItemBasedCreditDecision({
                            ApplicationNr: this.initialData.applicationNr,
                            IsAccepted: true,
                            SetAsCurrent: true,
                            DecisionType: this.m.decisionType,
                            UniqueItems: uItems,
                            RepeatingItems: null
                        }).then(x => {
                            let isLoanPromise = a.applicationType === 'LoanPromise'
                            if (this.m.isFinal && isLoanPromise) {
                                throw new Error("Loan promise is not allowed for the final step")
                            }
                            this.apiClient.setMortgageApplicationWorkflowStatus(
                                ai.ApplicationNr,
                                this.initialData.scoringWorkflowStepName,
                                WorkflowHelper.AcceptedName,
                                isLoanPromise ? 'Loan promise created' : `${this.m.decisionType} credit check accepted`,
                                null,
                                isLoanPromise ? 'AddToApplicationList:LoanPromise' : 'RemoveFromApplicationList:LoanPromise').then(y => {
                                    this.onBack(null)
                                })
                        })
                    })
                })
            })
        }

        private normalizeDecimalIfExists(name: string, d: NTechPreCreditApi.IStringDictionary<string>) {
            if (d && d[name]) {
                d[name] = this.formatNumberForStorage(this.parseDecimalOrNull(d[name]))
            }
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }
            this.reload()
        }

        private fetchCreditDecisionToStartFrom(cwf: MortgageLoanApplicationDualCreditCheckComponentNs.CustomWorkFlowStepDataModel): ng.IPromise<NTechPreCreditApi.ItemBasedDecisionModel> {
            let d = this.$q.defer<NTechPreCreditApi.ItemBasedDecisionModel>()
            this.apiClient.fetchItemBasedCreditDecision({ ApplicationNr: this.initialData.applicationNr, OnlyDecisionType: cwf.DecisionType, MaxCount: 1 }).then(x => {
                if (x.Decisions && x.Decisions.length > 0) {
                    //There is a stored decision of the current type, always use that in this case
                    d.resolve(x.Decisions[0])
                } else if (!cwf.CopyFromDecisionType) {
                    //There is nothing to copy from so start from scratch
                    d.resolve(null)
                } else {
                    this.apiClient.fetchItemBasedCreditDecision({ ApplicationNr: this.initialData.applicationNr, OnlyDecisionType: cwf.CopyFromDecisionType, MaxCount: 1 }).then(y => {
                        if (y.Decisions && y.Decisions.length > 0) {
                            //Start from a decision from an earlier step
                            d.resolve(y.Decisions[0])
                        } else {
                            //No decision exists in the earlier step so start from scratch
                            d.resolve(null)
                        }
                    })
                }
            })
            return d.promise
        }

        private reload() {
            let cwf = this.getCustomWorkflowData()
            let isFinal = cwf.IsFinal === 'yes'
            let setModel = () => {
                this.fetchCreditDecisionToStartFrom(cwf).then(d => {
                    let showAcceptedTab = true
                    let reject: RejectModel = this.createRejectModel([], isFinal)
                    let accept: AcceptModel = this.createAcceptModel(null, isFinal)

                    if (d) {
                        if (d.IsAccepted) {
                            showAcceptedTab = true
                            accept = this.createAcceptModel(d, isFinal)
                        } else {
                            showAcceptedTab = false
                            reject = this.createRejectModel(d.RepeatingItems['rejectionReason'], isFinal)
                        }
                    }
                    this.withAi(ai => this.init(showAcceptedTab, reject, accept, ai))
                })
            }
            if (this.currentReferenceInterestRate) {
                setModel()
            } else {
                this.apiClient.fetchCurrentReferenceInterestRate().then(x => {
                    this.currentReferenceInterestRate = {
                        value: x
                    }
                    setModel()
                })
            }
        }

        private getCustomWorkflowData(): MortgageLoanApplicationDualCreditCheckComponentNs.CustomWorkFlowStepDataModel {
            let scoringStepModel = new WorkflowHelper.WorkflowStepModel(this.initialData.workflowModel, this.initialData.scoringWorkflowStepName)
            return scoringStepModel.getCustomStepData<MortgageLoanApplicationDualCreditCheckComponentNs.CustomWorkFlowStepDataModel>()
        }

        private init(showAcceptedTab: boolean,
            reject: RejectModel, accept: AcceptModel,
            ai: NTechPreCreditApi.ApplicationInfoModel) {
            let cwf = this.getCustomWorkflowData()
            let isFinal = cwf.IsFinal === 'yes'

            let backTarget = NavigationTargetHelper.createCodeTarget(isFinal ? NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreditCheckNewFinal : NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreditCheckNewInitial)

            MortgageLoanApplicationDualCreditCheckSharedNs.getLtvBasisAndLoanListNrs(this, ai.ApplicationNr, this.apiClient).then(d => {
                let hasCoApplicant = ai.NrOfApplicants > 1
                let isEditAllowed = ai.IsActive && !ai.IsFinalDecisionMade && !ai.HasLockedAgreement
                let reportsModel = [{ applicationNrAndApplicantNr: { applicationNr: ai.ApplicationNr, applicantNr: 1 }, customerId: null, creditReportProviderName: ai.CreditReportProviderName, listProviders: ai.ListCreditReportProviders }]
                if (hasCoApplicant) {
                    reportsModel.push({ applicationNrAndApplicantNr: { applicationNr: ai.ApplicationNr, applicantNr: 2 }, customerId: null, creditReportProviderName: ai.CreditReportProviderName, listProviders: ai.ListCreditReportProviders })
                }
                MortgageLoanApplicationDualCreditCheckSharedNs.getApplicantDataByApplicantNr(ai.ApplicationNr, hasCoApplicant, this.apiClient).then(applicantDataByApplicantNr => {
                    MortgageLoanApplicationDualCreditCheckSharedNs.getCustomerCreditHistoryByApplicationNr(ai.ApplicationNr, this.apiClient).then(customerCreditHistory => {
                        let m: Model = {
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
                            b: MortgageLoanApplicationDualCreditCheckSharedNs.createDecisionBasisModel(false, this, this.apiClient, this.$q, ai, hasCoApplicant, isEditAllowed, backTarget, d.mortgageLoanNrs, d.otherLoanNrs, isFinal, applicantDataByApplicantNr),
                            customerCreditReports: reportsModel
                        }
                        this.m = m
                        this.registerTestFunctions()
                        if (showAcceptedTab) {
                            this.updateAcceptModelComputed()
                        }
                    })
                })
            })
        }

        onAcceptModelChanged() {
            if (!this.m || !this.m.acceptModel) {
                return
            }
            let a = this.m.acceptModel
            let mainPurchaseAmount = this.parseDecimalOrNull(a.mainPurchaseAmount)
            if (a.applicationType === 'MoveExistingLoan' && mainPurchaseAmount && Math.abs(mainPurchaseAmount) > 0.00001) {
                a.mainPurchaseAmount = ''
            }

            this.updateAcceptModelComputed()
        }

        referenceInterestRatePercent(isMain: boolean): number {
            if (!this.currentReferenceInterestRate) {
                return null
            }
            return this.currentReferenceInterestRate.value
        }

        totalInterestRatePercent(isMain: boolean): number {
            if (!this.m || !this.m.acceptModel) {
                return null
            }
            let refR = this.referenceInterestRatePercent(isMain)
            if (refR === null) {
                return null
            }
            let r: number
            if (isMain) {
                r = this.parseDecimalOrNull(this.m.acceptModel.mainMarginInterestRatePercent)
            } else {
                r = this.parseDecimalOrNull(this.m.acceptModel.childMarginInterestRatePercent)
            }
            if (r === null) {
                return null
            }
            return r + refR
        }

        private updateAcceptModelComputed() {
            if (!this.m) {
                return
            }

            let n = (s: string) => { let v = this.parseDecimalOrNull(s); return v === null ? 0 : v }

            let a = this.m.acceptModel

            let mainEffectiveInterstRatePercent: number = null
            let childEffectiveInterstRatePercent: number = null
            let mainMonthlyAmount: number = null
            let childMonthlyAmount: number = null

            let ps = []
            let mainLoanAmount = this.calculateMainLoanAmount()
            if (mainLoanAmount && mainLoanAmount > 0) {
                let r = {
                    LoanAmount: mainLoanAmount,
                    RepaymentTimeInMonths: n(a.mainRepaymentTimeInMonths),
                    MonthlyFeeAmount: n(a.mainNotificationFeeAmount),
                    CapitalizedInitialFeeAmount: this.calculateMainTotalInitialFeesAmount(),
                    TotalInterestRatePercent: this.totalInterestRatePercent(true),
                    IsFlatAmortization: false
                }
                if (r.TotalInterestRatePercent > 0 && r.RepaymentTimeInMonths > 0) {
                    ps.push(this.apiClient.calculatePaymentPlan(r).then(x => {
                        mainEffectiveInterstRatePercent = x.EffectiveInterestRatePercent
                        mainMonthlyAmount = x.AnnuityAmount + r.MonthlyFeeAmount
                    }))
                }
            }

            let childLoanAmount = this.calculateChildLoanAmount()
            if (childLoanAmount && childLoanAmount > 0) {
                let r = {
                    LoanAmount: childLoanAmount,
                    RepaymentTimeInMonths: n(a.childRepaymentTimeInMonths),
                    MonthlyFeeAmount: n(a.childNotificationFeeAmount),
                    CapitalizedInitialFeeAmount: this.calculateChildTotalInitialFeesAmount(),
                    TotalInterestRatePercent: this.totalInterestRatePercent(false),
                    IsFlatAmortization: false
                }
                if (r.TotalInterestRatePercent > 0 && r.RepaymentTimeInMonths > 0) {
                    ps.push(this.apiClient.calculatePaymentPlan(r).then(x => {
                        childEffectiveInterstRatePercent = x.EffectiveInterestRatePercent
                        childMonthlyAmount = x.AnnuityAmount + r.MonthlyFeeAmount
                    }))
                }
            }

            this.$q.all(ps).then(_ => {
                this.m.acceptModelComputed = {
                    mainEffectiveInterstRatePercent: mainEffectiveInterstRatePercent,
                    mainMonthlyAmount: mainMonthlyAmount,
                    childEffectiveInterstRatePercent: childEffectiveInterstRatePercent,
                    childMonthlyAmount: childMonthlyAmount,
                }
            })
        }

        private registerTestFunctions() {
            if (!this.initialData || !this.initialData.isTest) {
                return
            }
            let t = this.initialData.testFunctions
            let scopeName = t.generateUniqueScopeName()
            t.addFunctionCall(scopeName, 'Fill offer', () => {
                let isCurrentlyNewLoan = this.m.acceptModel && this.m.acceptModel.applicationType === 'NewLoan'
                this.m.acceptModel = {
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
                }
                this.onAcceptModelChanged()
            })
            t.addFunctionCall(scopeName, 'Add other loans', () => {
                if (this.m.b.additionalCurrentOtherLoans.length > 0 || this.m.b.currentMortgageLoans.length > 0) {
                    return
                }

                let ps: ng.IPromise<NTechPreCreditApi.SetApplicationEditItemDataResponse>[] = []
                let add = (name: string, value: string) => {
                    ps.push(this.apiClient.setApplicationEditItemData(this.initialData.applicationNr, 'ComplexApplicationList', name, value, false))
                }

                for (let isMortgage of [true, false]) {
                    let listName = isMortgage ? 'CurrentMortgageLoans' : 'CurrentOtherLoans'

                    add(`${listName}#${isMortgage ? 1 : 3}#u#exists`, 'true')
                    add(`${listName}#${isMortgage ? 1 : 3}#u#bankName`, `${listName} settled loan bank`)
                    add(`${listName}#${isMortgage ? 1 : 3}#u#loanTotalAmount`, isMortgage ? '23000' : '24000')
                    add(`${listName}#${isMortgage ? 1 : 3}#u#loanMonthlyAmount`, isMortgage ? '146' : '147')
                    add(`${listName}#${isMortgage ? 1 : 3}#u#loanShouldBeSettled`, 'true')
                    add(`${listName}#${isMortgage ? 1 : 3}#u#loanApplicant1IsParty`, 'true')
                    add(`${listName}#${isMortgage ? 1 : 3}#u#loanApplicant2IsParty`, 'false')

                    add(`${listName}#${isMortgage ? 2 : 4}#u#exists`, 'true')
                    add(`${listName}#${isMortgage ? 2 : 4}#u#bankName`, `${listName} not settled loan bank`)
                    add(`${listName}#${isMortgage ? 2 : 4}#u#loanTotalAmount`, isMortgage ? '21000' : '22000')
                    add(`${listName}#${isMortgage ? 2 : 4}#u#loanMonthlyAmount`, isMortgage ? '144' : '145')
                    add(`${listName}#${isMortgage ? 2 : 4}#u#loanShouldBeSettled`, 'false')
                    add(`${listName}#${isMortgage ? 2 : 4}#u#loanApplicant1IsParty`, 'true')
                    add(`${listName}#${isMortgage ? 2 : 4}#u#loanApplicant2IsParty`, 'false')
                }

                this.$q.all(ps).then(x => {
                    this.signalReloadRequired()
                })
            })
            //Prototype service ONLY
            //SelfContainedModules => PSD2Prototype
            t.addFunctionCall(scopeName, 'Get PSD2 File', () => {
                window.open("https://psd2-prototype.naktergaltech.se");
            })
        }
    }

    export class MortgageLoanApplicationDualCreditCheckNewComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationDualCreditCheckNewController;
            this.template = `<div ng-if="$ctrl.m">

    <div class="pt-1 pb-2">
        <div class="pull-left"><a class="n-back" href="#" ng-click="$ctrl.onBack($event)"><span class="glyphicon glyphicon-arrow-left"></span></a></div>
        <h1 class="adjusted">{{$ctrl.m.isFinal ? 'Final' : 'Initial'}} credit check</h1>
    </div>

    <div class="row">
        <div class="col-xs-10 col-sm-offset-1">
            <div class="row">
                <div class="col-sm-offset-4 col-xs-2">
                    <span type="button" class="btn" ng-class="{ disabled : $ctrl.m.isCalculating, 'decision-form-active-btn' : $ctrl.m.mode =='reject', 'decision-form-inactive-btn' : $ctrl.m.mode !='reject'}" ng-click="$ctrl.setAcceptRejectMode('reject', $event)">
                        Reject
                    </span>
                </div>
                <div class="col-xs-2">
                    <span type="button" class="btn" ng-class="{ disabled : $ctrl.m.isCalculating, 'decision-form-active-btn' : $ctrl.m.mode =='acceptNewLoan', 'decision-form-inactive-btn' : $ctrl.m.mode !='acceptNewLoan' }" ng-click="$ctrl.setAcceptRejectMode('acceptNewLoan', $event)">
                        New Loan
                    </span>
                </div>
            </div>

            ${this.rejectTabTemplate}

            ${this.acceptTabTemplate}
        </div>
    </div>

    ${this.decisionBasisTemplate}
</div>`;
        }

        private acceptTabMainLoanTemplate = `<h2 class="text-center">Mortgage loan</h2>
                <div class="row pt-1">
                    <div class="col-xs-4">
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Initial fee</label>
                            <div class="col-xs-6"><input type="text" class="form-control" custom-validate="$ctrl.isValidPositiveInt" ng-model="$ctrl.m.acceptModel.mainInitialFeeAmount" required ng-change="$ctrl.onAcceptModelChanged()"></div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Notification fee</label>
                            <div class="col-xs-6"><input type="text" class="form-control" custom-validate="$ctrl.isValidPositiveDecimal" ng-model="$ctrl.m.acceptModel.mainNotificationFeeAmount" required ng-change="$ctrl.onAcceptModelChanged()"></div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Valuation fee</label>
                            <div class="col-xs-6"><input type="text" class="form-control" custom-validate="$ctrl.isValidPositiveInt" ng-model="$ctrl.m.acceptModel.mainValuationFeeAmount" required ng-change="$ctrl.onAcceptModelChanged()"></div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Deed fee</label>
                            <div class="col-xs-6"><input type="text" class="form-control" custom-validate="$ctrl.isValidPositiveInt" ng-model="$ctrl.m.acceptModel.mainDeedFeeAmount" required ng-change="$ctrl.onAcceptModelChanged()"></div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Mortgage app. fee</label>
                            <div class="col-xs-6"><input type="text" class="form-control" custom-validate="$ctrl.isValidPositiveInt" ng-model="$ctrl.m.acceptModel.mainMortgageApplicationFeeAmount" required ng-change="$ctrl.onAcceptModelChanged()"></div>
                        </div>
                    </div>
                    <div class="col-xs-4">
                        <div class="form-group" ng-show="$ctrl.m.acceptModel.applicationType !=='MoveExistingLoan'">
                            <label class="col-xs-6 control-label">Purchase amount</label>
                            <div class="col-xs-6"><input type="text" class="form-control" custom-validate="$ctrl.isValidPositiveInt" ng-model="$ctrl.m.acceptModel.mainPurchaseAmount" ng-required="$ctrl.m.acceptModel.applicationType !=='MoveExistingLoan'" ng-change="$ctrl.onAcceptModelChanged()"></div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Payment to customer</label>
                            <div class="col-xs-6"><input type="text" class="form-control" custom-validate="$ctrl.isValidPositiveInt" ng-model="$ctrl.m.acceptModel.mainDirectToCustomerAmount" required ng-change="$ctrl.onAcceptModelChanged()"></div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Total settlement amount</label>
                            <div class="col-xs-6"><p class="form-control-static">{{$ctrl.m.decisionBasis.mainTotalSettlementAmount | number:'2'}}</p></div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Total amount</label>
                            <div class="col-xs-6"><p class="form-control-static">{{$ctrl.calculateTotalAmount(true) | number:'2'}}</p></div>
                        </div>
                    </div>
                    <div class="col-xs-4">
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Margin interest rate</label>
                            <div class="col-xs-6"><input type="text" class="form-control" custom-validate="$ctrl.isValidPositiveDecimal" ng-model="$ctrl.m.acceptModel.mainMarginInterestRatePercent" required ng-change="$ctrl.onAcceptModelChanged()"></div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Reference interest rate</label>
                            <div class="col-xs-6"><p class="form-control-static">{{$ctrl.referenceInterestRatePercent(true) | number:'2'}}  %</p></div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Total interest rate</label>
                            <div class="col-xs-6"><p class="form-control-static">{{$ctrl.totalInterestRatePercent(true) | number:'2'}}  %</p></div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Repayment time</label>
                            <div class="col-xs-6"><input type="text" class="form-control" custom-validate="$ctrl.isValidPositiveInt" ng-model="$ctrl.m.acceptModel.mainRepaymentTimeInMonths" required ng-change="$ctrl.onAcceptModelChanged()"></div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Eff. interest rate</label>
                            <div class="col-xs-6"><p class="form-control-static">{{$ctrl.m.acceptModelComputed.mainEffectiveInterstRatePercent | number:'2'}}  %</p></div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Monthly amount</label>
                            <div class="col-xs-6"><p class="form-control-static">{{$ctrl.m.acceptModelComputed.mainMonthlyAmount | number:'2'}} </p></div>
                        </div>
                    </div>
                </div>`

        private acceptTabChildLoanTemplate = `<h2 class="text-center">Loan with collateral</h2>
                <div class="row pt-1">
                    <div class="col-xs-4">
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Initial fee</label>
                            <div class="col-xs-6"><input type="text" class="form-control" custom-validate="$ctrl.isValidPositiveInt" ng-model="$ctrl.m.acceptModel.childInitialFeeAmount" required ng-change="$ctrl.onAcceptModelChanged()"></div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Notification fee</label>
                            <div class="col-xs-6"><input type="text" class="form-control" custom-validate="$ctrl.isValidPositiveDecimal" ng-model="$ctrl.m.acceptModel.childNotificationFeeAmount" required ng-change="$ctrl.onAcceptModelChanged()"></div>
                        </div>
                    </div>

                    <div class="col-xs-4">
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Payment to customer</label>
                            <div class="col-xs-6"><input type="text" class="form-control" custom-validate="$ctrl.isValidPositiveInt" ng-model="$ctrl.m.acceptModel.childDirectToCustomerAmount" required ng-change="$ctrl.onAcceptModelChanged()"></div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Total settlement amount</label>
                            <div class="col-xs-6"><p class="form-control-static">{{$ctrl.m.decisionBasis.childTotalSettlementAmount | number:'2'}}</p></div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Total amount</label>
                            <div class="col-xs-6"><p class="form-control-static">{{$ctrl.calculateTotalAmount(false) | number:'2'}}</p></div>
                        </div>
                    </div>

                    <div class="col-xs-4">
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Margin interest rate</label>
                            <div class="col-xs-6"><input type="text" class="form-control" custom-validate="$ctrl.isValidPositiveDecimal" ng-model="$ctrl.m.acceptModel.childMarginInterestRatePercent" required ng-change="$ctrl.onAcceptModelChanged()"></div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Reference interest rate</label>
                            <div class="col-xs-6"><p class="form-control-static">{{$ctrl.referenceInterestRatePercent(false) | number:'2'}}  %</p></div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Total interest rate</label>
                            <div class="col-xs-6"><p class="form-control-static">{{$ctrl.totalInterestRatePercent(false) | number:'2'}}  %</p></div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Repayment time</label>
                            <div class="col-xs-6"><input type="text" class="form-control" custom-validate="$ctrl.isValidPositiveInt" ng-model="$ctrl.m.acceptModel.childRepaymentTimeInMonths" required ng-change="$ctrl.onAcceptModelChanged()"></div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Eff. interest rate</label>
                            <div class="col-xs-6"><p class="form-control-static">{{$ctrl.m.acceptModelComputed.childEffectiveInterstRatePercent | number:'2'}}  %</p></div>
                        </div>
                        <div class="form-group">
                            <label class="col-xs-6 control-label">Monthly amount</label>
                            <div class="col-xs-6"><p class="form-control-static">{{$ctrl.m.acceptModelComputed.childMonthlyAmount | number:'2'}} </p></div>
                        </div>
                    </div>
                </div>`

        private acceptTabTemplate = `<form class="form-horizontal decision-form" name="acceptform" bootstrap-validation="'parent'" novalidate ng-show="$ctrl.m.mode=='acceptNewLoan'">
                <div class="form-group">
                    <label class="col-xs-4 control-label">Type</label>
                    <div class="col-xs-4">
                        <select class="form-control" ng-model="$ctrl.m.acceptModel.applicationType" ng-change="$ctrl.onAcceptModelChanged()">
                            <option ng-if="!$ctrl.m.isFinal" value="LoanPromise">Loan prospect</option>
                            <option value="NewLoan">New loan</option>
                            <option value="MoveExistingLoan">Move existing loan</option>
                        </select>
                    </div>
                </div>

                <div class="pt-1">
                    <hr class="hr-section dotted" />
                </div>
                ${this.acceptTabMainLoanTemplate}

                <div class="pt-1">
                    <hr class="hr-section dotted" />
                </div>
                ${this.acceptTabChildLoanTemplate}

                <div class="pt-1">
                    <hr class="hr-section dotted" />
                </div>

                <div class="form-group">
                    <label class="col-xs-6 control-label">Loan to value</label>
                    <div class="col-xs-6"><p class="form-control-static">{{ ($ctrl.calculateLtv() * 100) | number:'2'}}%</p></div>
                 </div>

                <div class="text-center pt-3">
                    <button type="button" class="n-main-btn n-green-btn" ng-class="{ disabled : $ctrl.m.isPendingValidation || acceptform.$invalid }" ng-click="$ctrl.acceptNewLoan($event)">Accept</button>
                </div>
            </form>`

        private rejectTabTemplate = `<form name="rejectform" novalidate class="form-horizontal decision-form" ng-show="$ctrl.m.mode=='reject'">
                <h4 class="text-center">Rejection reasons</h4>
                <div class="row">
                    <div class="col-sm-6 col-md-6">
                        <div class="form-group" ng-repeat="b in $ctrl.m.rejectModel.rejectModelCheckboxesCol1">
                            <label class="col-md-8 control-label">{{b.displayName}}</label>
                            <div class="col-md-4"><div class="checkbox"><input type="checkbox" ng-model="$ctrl.m.rejectModel.reasons[b.reason]"></div></div>
                        </div>
                    </div>
                    <div class="col-sm-6 col-md-6">
                        <div class="form-group" ng-repeat="b in $ctrl.m.rejectModel.rejectModelCheckboxesCol2">
                            <label class="col-md-6 control-label">{{b.displayName}}</label>
                            <div class="col-md-4"><div class="checkbox"><input type="checkbox" ng-model="$ctrl.m.rejectModel.reasons[b.reason]"></div></div>
                        </div>
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-4 control-label">Other</label>
                    <div class="col-md-6"><input type="text" class="form-control" ng-model="$ctrl.m.rejectModel.otherReason"></div>
                </div>
                <div class="text-center pt-3">
                    <button type="button" class="n-main-btn n-red-btn" ng-disabled="!$ctrl.anyRejectionReasonGiven()" ng-click="$ctrl.reject($event)">Reject</button>
                </div>
            </form>`

        private decisionBasisTemplate = MortgageLoanApplicationDualCreditCheckSharedNs.getDecisionBasisHtmlTemplate(true)
    }

    export interface LocalInitialData {
        applicationNr: string
        scoringWorkflowStepName: string
        rejectionReasonToDisplayNameMapping: NTechPreCreditApi.IStringDictionary<string>
        rejectionRuleToReasonNameMapping: NTechPreCreditApi.IStringDictionary<string>
        creditUrlPattern: string
        workflowModel: WorkflowHelper.WorkflowServerModel
    }

    export interface InitialData extends LocalInitialData, ComponentHostNs.ComponentHostInitialData {
    }

    export class Model {
        mode: string
        isCalculating?: boolean
        isPendingValidation?: boolean
        rejectModel: RejectModel
        acceptModel: AcceptModel
        hasCoApplicant: boolean
        isFinal: boolean
        decisionType: string
        isEditAllowed: boolean
        decisionBasis: {
            mainTotalSettlementAmount: number
            childTotalSettlementAmount: number
        }
        ltvBasis: {
            customerCredits: { CreditNr: string, CapitalBalance: number}[],
            valuationAmount: { key: number, value: number }[],
            statValuationAmount: { key: number, value: number }[],
            priceAmount: { key: number, value: number }[],
            securityElsewhereAmount: number[],
            housingCompanyLoans: number[]
        }
        acceptModelComputed: {
            mainEffectiveInterstRatePercent: number
            childEffectiveInterstRatePercent: number
            mainMonthlyAmount: number
            childMonthlyAmount: number
        }
        backTarget: NavigationTargetHelper.CodeOrUrl
        b: MortgageLoanApplicationDualCreditCheckSharedNs.DecisionBasisModel
        customerCreditReports: ListAndBuyCreditReportsForCustomerComponentNs.IInitialData[]
    }

    export interface IScope extends ng.IScope {
    }

    export class RejectModel {
        rejectModelCheckboxesCol1: RejectionCheckboxModel[]
        rejectModelCheckboxesCol2: RejectionCheckboxModel[]
        reasons: NTechPreCreditApi.IStringDictionary<boolean>
        otherReason: string
        initialReasons: string[]
    }

    export class AcceptModel {
        applicationType: string
        mainMarginInterestRatePercent?: string
        mainRepaymentTimeInMonths?: string
        mainInitialFeeAmount?: string
        mainValuationFeeAmount?: string
        mainDeedFeeAmount?: string
        mainMortgageApplicationFeeAmount?: string
        mainNotificationFeeAmount?: string
        mainPurchaseAmount?: string
        mainDirectToCustomerAmount?: string
        childDirectToCustomerAmount?: string
        childMarginInterestRatePercent?: string
        childRepaymentTimeInMonths?: string
        childInitialFeeAmount?: string
        childNotificationFeeAmount?: string
    }

    export class RejectionCheckboxModel {
        constructor(public reason: string, public displayName: string) {
        }
    }
}
angular.module('ntech.components').component('mortgageLoanApplicationDualCreditCheckNew', new MortgageLoanApplicationDualCreditCheckNewComponentNs.MortgageLoanApplicationDualCreditCheckNewComponent())