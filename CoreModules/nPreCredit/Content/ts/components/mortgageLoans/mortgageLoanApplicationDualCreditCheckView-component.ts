namespace MortgageLoanApplicationDualCreditCheckViewComponentNs {
    export class MortgageLoanApplicationDualInitialCreditCheckViewController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model
        currentReferenceInterestRate: { value: number }

        static $inject = ['$http', '$q', 'ntechComponentService', '$scope', '$timeout']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private $scope: IScope) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'mortgageLoanApplicationDualCreditCheckView'
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

        private getCustomWorkflowData(): MortgageLoanApplicationDualCreditCheckComponentNs.CustomWorkFlowStepDataModel {
            let scoringStepModel = new WorkflowHelper.WorkflowStepModel(this.initialData.workflowModel, this.initialData.scoringWorkflowStepName)
            return scoringStepModel.getCustomStepData<MortgageLoanApplicationDualCreditCheckComponentNs.CustomWorkFlowStepDataModel>()
        }

        private getRejectionReasonDisplayName(rejectionReasonToDisplayNameMapping: NTechPreCreditApi.IStringDictionary<string>, reasonName: string) {
            let r = ''
            if (rejectionReasonToDisplayNameMapping) {
                r = rejectionReasonToDisplayNameMapping[reasonName]
            }
            if (!r) {
                r = reasonName
            }
            return r
        }

        onChanges() {
            this.m = null
            if (!this.initialData) {
                return
            }
            let scoringStepModel = new WorkflowHelper.WorkflowStepModel(this.initialData.workflowModel, this.initialData.scoringWorkflowStepName)
            let cwf = scoringStepModel.getCustomStepData<MortgageLoanApplicationDualCreditCheckComponentNs.CustomWorkFlowStepDataModel>()

            this.apiClient.fetchItemBasedCreditDecision({ ApplicationNr: this.initialData.applicationNr, OnlyDecisionType: cwf.DecisionType, MaxCount: 1, MustBeCurrent: false, IncludeRejectionReasonToDisplayNameMapping: true }).then(x => {
                this.apiClient.fetchApplicationInfo(this.initialData.applicationNr).then(ai => {
                    MortgageLoanApplicationDualCreditCheckSharedNs.getLtvBasisAndLoanListNrs(this, ai.ApplicationNr, this.apiClient).then(d => {
                            let cwf = this.getCustomWorkflowData()
                            let isFinal = cwf.IsFinal === 'yes'
                            let backTarget = NavigationTargetHelper.createCodeTarget(isFinal ? NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreditCheckViewFinal : NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreditCheckViewInitial)
                            let decision = x.Decisions && x.Decisions.length > 0 ? x.Decisions[0] : null

                            let hasMainLoan = !decision.IsAccepted ? false : decision.UniqueItems['mainHasLoan'] === 'true'
                            let hasChildLoan = !decision.IsAccepted ? false : decision.UniqueItems['childHasLoan'] === 'true'
                            let rejectionReasons = ''
                            if (!decision.IsAccepted) {
                                for (let rejectionReason of decision.RepeatingItems['rejectionReason']) {
                                    if (rejectionReasons.length > 0) {
                                        rejectionReasons += ', '
                                    }
                                    rejectionReasons += this.getRejectionReasonDisplayName(x.RejectionReasonToDisplayNameMapping, rejectionReason)
                                }
                            }

                            let calcMonthlyAmount = (isMain: boolean) => {
                                return this.formatNumberForStorage(this.parseDecimalOrNull(decision.UniqueItems[`${isMain ? 'main' : 'child'}AnnuityAmount`]) + this.parseDecimalOrNull(decision.UniqueItems[`${isMain ? 'main' : 'child'}NotificationFeeAmount`]))
                            }

                            MortgageLoanApplicationDualCreditCheckSharedNs.getApplicantDataByApplicantNr(ai.ApplicationNr, ai.NrOfApplicants > 1, this.apiClient).then(applicantDataByApplicantNr => {
                                this.m = {
                                    isFinal: isFinal,
                                    headerClass: { 'text-success': decision.IsAccepted, 'text-danger': !decision.IsAccepted },
                                    iconClass: { 'glyphicon-ok': decision.IsAccepted, 'glyphicon-remove': !decision.IsAccepted, 'text-success': decision.IsAccepted, 'text-danger': !decision.IsAccepted },
                                    decision: decision,
                                    b: MortgageLoanApplicationDualCreditCheckSharedNs.createDecisionBasisModel(true, this, this.apiClient, this.$q, ai, ai.NrOfApplicants > 1, false, backTarget, d.mortgageLoanNrs, d.otherLoanNrs, isFinal, applicantDataByApplicantNr),
                                    rejectedCommon: !decision.IsAccepted ? new TwoColumnInformationBlockComponentNs.InitialData()
                                        .item(true, rejectionReasons, null, 'Reasons') : null,
                                    acceptedCommon: (hasMainLoan || hasChildLoan) ? new TwoColumnInformationBlockComponentNs.InitialData()
                                        .item(true, decision.UniqueItems['applicationType'], null, 'Type') : null,
                                    acceptedMainLoan: hasMainLoan ? {
                                        d1: new TwoColumnInformationBlockComponentNs.InitialData()
                                            .item(true, decision.UniqueItems['mainInitialFeeAmount'], null, 'Initial fee', 'currency')
                                            .item(true, decision.UniqueItems['mainNotificationFeeAmount'], null, 'Notification fee', 'currency')
                                            .item(true, decision.UniqueItems['mainValuationFeeAmount'], null, 'Valuation fee', 'currency')
                                            .item(true, decision.UniqueItems['mainDeedFeeAmount'], null, 'Deed fee', 'currency')
                                            .item(true, decision.UniqueItems['mainMortgageApplicationFeeAmount'], null, 'Mortgage app. fee', 'currency')
                                            .item(false, decision.UniqueItems['mainPurchaseAmount'], null, 'Purchase amount', 'currency')
                                            .item(false, decision.UniqueItems['mainDirectToCustomerAmount'], null, 'Payment to customer', 'currency')
                                            .item(false, decision.UniqueItems['mainTotalSettlementAmount'], null, 'Total settlement amount', 'currency')
                                            .item(false, decision.UniqueItems['mainLoanAmount'], null, 'Total amount', 'currency'),
                                        d2: new TwoColumnInformationBlockComponentNs.InitialData()
                                            .item(true, decision.UniqueItems['mainMarginInterestRatePercent'], null, 'Margin interest rate', 'percent', 3)
                                            .item(true, decision.UniqueItems['mainReferenceInterestRatePercent'], null, 'Reference interest rate', 'percent', 3)
                                            .item(true, this.formatNumberForStorage(this.parseDecimalOrNull(decision.UniqueItems['mainMarginInterestRatePercent']) + this.parseDecimalOrNull(decision.UniqueItems['mainReferenceInterestRatePercent'])), null, 'Total interest rate', 'percent', 3)
                                            .item(true, decision.UniqueItems['mainRepaymentTimeInMonths'], null, 'Repayment time', 'number', 3)
                                            .item(true, decision.UniqueItems['mainEffectiveInterestRatePercent'], null, 'Eff. interest rate', 'percent', 3)
                                            .item(true, calcMonthlyAmount(true), null, 'Monthly amount', 'currency', 3)
                                    } : null,
                                    acceptedChildLoan: hasChildLoan ? {
                                        d1: new TwoColumnInformationBlockComponentNs.InitialData()
                                            .item(true, decision.UniqueItems['childInitialFeeAmount'], null, 'Initial fee', 'currency')
                                            .item(true, decision.UniqueItems['childNotificationFeeAmount'], null, 'Notification fee', 'currency')
                                            .item(false, decision.UniqueItems['childDirectToCustomerAmount'], null, 'Payment to customer', 'currency')
                                            .item(false, decision.UniqueItems['childTotalSettlementAmount'], null, 'Total settlement amount', 'currency')
                                            .item(false, decision.UniqueItems['childLoanAmount'], null, 'Total amount', 'currency'),
                                        d2: new TwoColumnInformationBlockComponentNs.InitialData()
                                            .item(true, decision.UniqueItems['childMarginInterestRatePercent'], null, 'Margin interest rate', 'percent', 3)
                                            .item(true, decision.UniqueItems['childReferenceInterestRatePercent'], null, 'Reference interest rate', 'percent', 3)
                                            .item(true, this.formatNumberForStorage(this.parseDecimalOrNull(decision.UniqueItems['childMarginInterestRatePercent']) + this.parseDecimalOrNull(decision.UniqueItems['childReferenceInterestRatePercent'])), null, 'Total interest rate', 'percent', 3)
                                            .item(true, decision.UniqueItems['childRepaymentTimeInMonths'], null, 'Repayment time', 'number', 3)
                                            .item(true, decision.UniqueItems['childEffectiveInterestRatePercent'], null, 'Eff. interest rate', 'percent', 3)
                                            .item(true, calcMonthlyAmount(false), null, 'Monthly amount', 'currency', 3)
                                    } : null
                                }
                            })                        
                    })
                })
            })
        }
    }

    export class MortgageLoanApplicationDualInitialCreditCheckViewComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationDualInitialCreditCheckViewController;
            this.template = `<div ng-if="$ctrl.m">

        <div class="pt-1 pb-2">
            <div class="pull-left">
                <a class="n-back" href="#" ng-click="$ctrl.onBack($event)">
                    <span class="glyphicon glyphicon-arrow-left"></span>
                </a>
            </div>
            <h1 class="adjusted" ng-class="$ctrl.m.headerClass">{{$ctrl.m.isFinal ? 'Final' : 'Initial'}} credit decision <span ng-class="$ctrl.m.iconClass" style="font-size:20px; margin-left: 5px;" class="glyphicon"></span></h1>
        </div>

        <div class="row pb-3 pt-3" ng-if="$ctrl.m.rejectedCommon">
            <two-column-information-block class="col-sm-8 col-sm-offset-3" initial-data="$ctrl.m.rejectedCommon"></two-column-information-block>
        </div>

        <div class="row" ng-if="$ctrl.m.acceptedCommon">
            <two-column-information-block class="col-sm-8" initial-data="$ctrl.m.acceptedCommon"></two-column-information-block>
        </div>
        <div ng-if="$ctrl.m.acceptedCommon">
            <hr class="hr-section dotted">
        </div>

        <div ng-if="$ctrl.m.acceptedMainLoan">
            <h3 class="text-center">Mortgage loan</h3>
        </div>
        <div class="row" ng-if="$ctrl.m.acceptedMainLoan">
            <two-column-information-block class="col-sm-8" initial-data="$ctrl.m.acceptedMainLoan.d1"></two-column-information-block>
            <two-column-information-block class="col-sm-4" initial-data="$ctrl.m.acceptedMainLoan.d2"></two-column-information-block>
        </div>
        <div ng-if="$ctrl.m.acceptedMainLoan">
            <hr class="hr-section dotted">
        </div>

        <div ng-if="$ctrl.m.acceptedChildLoan">
            <h3 class="text-center">Other loan</h3>
        </div>
        <div class="row" ng-if="$ctrl.m.acceptedChildLoan">
            <two-column-information-block class="col-sm-8" initial-data="$ctrl.m.acceptedChildLoan.dt"></two-column-information-block>
        </div>
        <div class="row" ng-if="$ctrl.m.acceptedChildLoan">
            <two-column-information-block class="col-sm-8" initial-data="$ctrl.m.acceptedChildLoan.d1"></two-column-information-block>
            <two-column-information-block class="col-sm-4" initial-data="$ctrl.m.acceptedChildLoan.d2"></two-column-information-block>
        </div>
        <div ng-if="$ctrl.m.acceptedChildLoan">
            <hr class="hr-section dotted">
        </div>

        <div class="row pb-3" ng-if="$ctrl.m.acceptedMainLoan || $ctrl.m.acceptedChildLoan">

        </div>
        ${this.decisionBasisTemplate}
</div>`;
        }

        private decisionBasisTemplate = MortgageLoanApplicationDualCreditCheckSharedNs.getDecisionBasisHtmlTemplate(false)
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
        isFinal: boolean
        decision: NTechPreCreditApi.ItemBasedDecisionModel
        b: MortgageLoanApplicationDualCreditCheckSharedNs.DecisionBasisModel
        rejectedCommon: TwoColumnInformationBlockComponentNs.InitialData
        acceptedCommon: TwoColumnInformationBlockComponentNs.InitialData
        acceptedMainLoan: {
            d1: TwoColumnInformationBlockComponentNs.InitialData,
            d2: TwoColumnInformationBlockComponentNs.InitialData
        }
        acceptedChildLoan: {
            d1: TwoColumnInformationBlockComponentNs.InitialData,
            d2: TwoColumnInformationBlockComponentNs.InitialData
        }
        headerClass: NTechPreCreditApi.IStringDictionary<boolean>
        iconClass: NTechPreCreditApi.IStringDictionary<boolean>
    }

    export interface IScope extends ng.IScope {
    }
}

angular.module('ntech.components').component('mortgageLoanApplicationDualCreditCheckView', new MortgageLoanApplicationDualCreditCheckViewComponentNs.MortgageLoanApplicationDualInitialCreditCheckViewComponent())