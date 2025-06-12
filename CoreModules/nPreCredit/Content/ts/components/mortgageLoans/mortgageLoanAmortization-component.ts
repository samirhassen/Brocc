namespace MortgageLoanAmortizationComponentNs {

    export class MortgageLoanAmortizationController extends NTechComponents.NTechComponentControllerBase {
        initialData: MortgageLoanApplicationDynamicComponentNs.StepInitialData
        m: Model
        u: string
        editDialogId: string

        static $inject = ['$http', '$q', '$scope', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            private $scope : ng.IScope,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);

            this.u = NTechComponents.generateUniqueId(6)
            this.editDialogId = modalDialogService.generateDialogId()
        }

        componentName(): string {
            return 'mortgageLoanAmortization'
        }

        private showOverview(model: NTechPreCreditApi.MortgageLoanAmortizationBasisModel, isNewAllowed: boolean, allowSetAndEdit: boolean) {
            this.m = {
                nm: null,
                om: {
                    basis: model,
                    loanFractionPercent: 100 * (model.AmortizationBasisLoanAmount / model.AmortizationBasisObjectValue),
                    loanIncomeRatio: model.CurrentCombinedTotalLoanAmount / model.CurrentCombinedYearlyIncomeAmount,
                    isNewAllowed: isNewAllowed,
                    isSetAndEditAllowed: allowSetAndEdit,
                    showLoanIncomeRatioDetails: false,
                    onNew: isNewAllowed ? ((evt => {
                        if (evt) {
                            evt.preventDefault()
                        }
                        this.reload(false)
                    })) : (evt => { })
                },
                showFuturePossibleMessage: false
            }
        }

        onChanges() {
            this.reload(true)
        }

        reload(isOverview: boolean) {  
            this.m = null

            if (!this.initialData) {
                return
            }

            let ai = this.initialData.applicationInfo
            let areAllStepsBeforeThisCompleted = this.initialData.workflowModel.areAllStepBeforeThisAccepted(ai)
            let isNewAllowed = ai.IsActive && areAllStepsBeforeThisCompleted
            
            if (!areAllStepsBeforeThisCompleted) {
                this.m = {
                    om: null,
                    nm: null,
                    showFuturePossibleMessage: true
                }
            } else if (isOverview) {
                this.apiClient.fetchMortgageLoanAmortizationBasis(this.initialData.applicationInfo.ApplicationNr).then(x => {
                    this.showOverview(x, isNewAllowed, false)
                })
            } else {
                this.apiClient.fetchApplicationDocuments(this.initialData.applicationInfo.ApplicationNr, ['MortgageLoanCustomerAmortizationPlan']).then(documents => {
                    this.apiClient.fetchMortageLoanApplicationInitialCreditCheckStatus(this.initialData.applicationInfo.ApplicationNr, null).then(initialCreditCheckStatus => {
                        this.m = {
                            nm: {
                                mortgageLoanCustomerAmortizationPlanDownloadUrl: documents && documents.length == 1 ? NTechPreCreditApi.ApplicationDocument.GetDownloadUrl(documents[0]) : null,
                                initialOfferLoanAmount: initialCreditCheckStatus.AcceptedDecision.Offer.LoanAmount,
                                initialOfferMonthlyAmortizationAmount: initialCreditCheckStatus.AcceptedDecision.Offer.MonthlyAmortizationAmount,
                                c: {

                                },
                                p: null
                            },
                            om: null,
                            showFuturePossibleMessage: false
                        }
                    })
                })
            }
        }

        calculate(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            let c = this.m.nm.c
            this.apiClient.calculateMortgageLoanAmortizationSuggestionBasedOnStandardBankForm(this.initialData.applicationInfo.ApplicationNr, {
                AlternateRuleAmortizationAmount: this.parseDecimalOrNull(c.totalAmortizationAlternateAmount),
                AmortizationBasisDate: NTechDates.DateOnly.parseDateOrNull(c.amortizationBasisDate),
                AmortizationBasisLoanAmount: this.parseDecimalOrNull(c.amortizationBasisLoanAmount),
                AmortizationBasisObjectValue: this.parseDecimalOrNull(c.amortizationBasisObjectValue),
                RuleAlternateCurrentAmount: this.parseDecimalOrNull(c.ruleAlternateCurrentAmount),
                RuleNoneCurrentAmount: this.parseDecimalOrNull(c.ruleNoneCurrentAmount),
                RuleR201616CurrentAmount: this.parseDecimalOrNull(c.ruleR201616CurrentAmount),
                RuleR201723CurrentAmount: this.parseDecimalOrNull(c.ruleR201723CurrentAmount)
            }).then(x => {
                this.showOverview(x, false, true)
            })
        }

        setBasis(basis: NTechPreCreditApi.MortgageLoanAmortizationBasisModel, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.setMortgageLoanAmortizationBasis(this.initialData.applicationInfo.ApplicationNr, basis).then(() => {
                this.signalReloadRequired()
            })
        }

        editManually(basis: NTechPreCreditApi.MortgageLoanAmortizationBasisModel, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.m.om || !this.m.om.basis) {
                return
            }
            let b = this.m.om.basis
            let currentExceptions = b.AmortizationExceptionReasons || []
            let possibleExceptions = ['Nyproduktion', 'Lantbruksenhet', 'Sjukdom', 'Arbetsl\u00f6shet', 'D\u00f6dsfall']
            let exceptions: ExceptionEditModel[] = []
            for (let e of possibleExceptions) {
                exceptions.push({
                    name: e,
                    checked: currentExceptions.indexOf(e) >= 0
                })
            }

            this.m.om.e = {
                actualAmortizationAmount: this.formatNumberForEdit(b.ActualAmortizationAmount),
                amortizationExceptionReasons: exceptions,
                hasAmortizationFree: !!b.AmortizationFreeUntilDate,
                amortizationExceptionUntilDate: this.formatDateOnlyForEdit(b.AmortizationExceptionUntilDate),
                amortizationFreeUntilDate: this.formatDateOnlyForEdit(b.AmortizationFreeUntilDate),
                exceptionAmortizationAmount: this.formatNumberForEdit(b.ExceptionAmortizationAmount),
                hasException: !!b.AmortizationExceptionUntilDate
            }
            this.modalDialogService.openDialog(this.editDialogId)
        }

        cancelEdit(evt: Event) {
            this.m.om.e = null
            this.modalDialogService.closeDialog(this.editDialogId)
        }

        saveEdit(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.m.om || !this.m.om.e) {
                return
            }
            let b = angular.copy(this.m.om.basis)
            let e = this.m.om.e
            b.ActualAmortizationAmount = this.parseDecimalOrNull(e.actualAmortizationAmount)
            b.AmortizationFreeUntilDate = e.hasAmortizationFree ? NTechDates.DateOnly.parseDateOrNull(e.amortizationFreeUntilDate) : null
            if (e.hasException) {
                b.ExceptionAmortizationAmount = this.parseDecimalOrNull(e.exceptionAmortizationAmount)
                b.AmortizationExceptionUntilDate = NTechDates.DateOnly.parseDateOrNull(e.amortizationExceptionUntilDate)
                b.AmortizationExceptionReasons = []
                for (let r of e.amortizationExceptionReasons) {
                    if (r.checked) {
                        b.AmortizationExceptionReasons.push(r.name)
                    }
                }
            } else {
                b.ExceptionAmortizationAmount = null
                b.AmortizationExceptionUntilDate = null
                b.AmortizationExceptionReasons = []
            }
            this.m.om.basis = b
            this.m.om.e = null
            this.modalDialogService.closeDialog(this.editDialogId)
        }

        toggleArrayItem(item: string, items: string[], evt: Event) {
            if (evt) {
                evt.stopPropagation()
                evt.preventDefault()
            }
            let index = items.indexOf(item)
            if (index >= 0) {
                items.splice(index, 1)
            } else {
                items.push(item)
            }
        }

        isAtLeastOnceExceptionChecked() {
            if (!this.m.om || !this.m.om.e || !this.m.om.e.amortizationExceptionReasons) {
                return false
            }
            return this.m.om.e.amortizationExceptionReasons.some(x => x.checked)
        }
    }

    export class MortgageLoanAmortizationComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanAmortizationController;
            this.templateUrl = 'mortgage-loan-amortization.html';
        }
    }

    export class InitialData {
        mode: string
        applicationInfo: NTechPreCreditApi.ApplicationInfoModel
        onNew?: (evt: Event) => void
        afterSet?: (basis: NTechPreCreditApi.MortgageLoanAmortizationBasisModel) => void
    }


    export class Model {
        om: OverviewModel
        nm: NewModel
        showFuturePossibleMessage: boolean
    }


    export class OverviewModel {
        basis: NTechPreCreditApi.MortgageLoanAmortizationBasisModel
        loanFractionPercent: number
        loanIncomeRatio: number
        showLoanIncomeRatioDetails: boolean
        isNewAllowed: boolean
        isSetAndEditAllowed: boolean
        onNew: (evt: Event) => void
        e?: MortgageLoanAmortizationBasisEditModel
    }

    export class NewModel {
        mortgageLoanCustomerAmortizationPlanDownloadUrl: string
        initialOfferLoanAmount: number
        initialOfferMonthlyAmortizationAmount: number
        c: CalculateModel
        p: PreviewModel
    }

    export class CalculateModel {
        amortizationBasisDate?: string
        amortizationBasisObjectValue?: string
        amortizationBasisLoanAmount?: string
        ruleNoneCurrentAmount?: string
        ruleR201616CurrentAmount?: string
        ruleR201723CurrentAmount?: string
        ruleAlternateCurrentAmount?: string
        totalAmortizationAlternateAmount?: string
    }

    export class PreviewModel {
        m: NTechPreCreditApi.MortgageLoanAmortizationBasisModel
        loanFractionPercent: number
        debtMultiplier: number
    }

    export class MortgageLoanAmortizationBasisEditModel {        
        actualAmortizationAmount: string

        hasAmortizationFree: boolean
        amortizationFreeUntilDate: string
        
        hasException: boolean
        amortizationExceptionUntilDate: string
        exceptionAmortizationAmount: string
        amortizationExceptionReasons: ExceptionEditModel[]
    }

    export class ExceptionEditModel {
        name: string
        checked: boolean
    }
}

angular.module('ntech.components').component('mortgageLoanAmortization', new MortgageLoanAmortizationComponentNs.MortgageLoanAmortizationComponent())