namespace MortgageLoanApplicationDualCreditCheckComponentNs {
    export class MortgageApplicationRawController extends NTechComponents.NTechComponentControllerBase {
        initialData: MortgageLoanApplicationDynamicComponentNs.StepInitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'mortgageLoanApplicationDualCreditCheck'
        }

        onChanges() {
            this.m = null
            if (!this.initialData || !this.initialData.applicationInfo) {
                return
            }

            let ai = this.initialData.applicationInfo
            let wfc = this.initialData.workflowModel.getCustomStepData<CustomWorkFlowStepDataModel>()

            let setup = (d: NTechPreCreditApi.ItemBasedDecisionModel, rr: NTechPreCreditApi.IStringDictionary<string>, doesDecisionHistoryAllowNewCreditCheck: boolean, applicationtypeName: string) => {
                this.m = {
                    decision: d,
                    rejectionReasonToDisplayNameMapping: rr,
                    customWorkflowStepData: wfc,
                    doesDecisionHistoryAllowNewCreditCheck: doesDecisionHistoryAllowNewCreditCheck,
                    applicationtypeName: applicationtypeName
                }
            }

            let r: NTechPreCreditApi.FetchApplicationDataSourceRequestItem = {
                DataSourceName: 'CreditApplicationItem',
                ErrorIfMissing: false,
                IncludeEditorModel: true,
                IncludeIsChanged: false,
                MissingItemReplacementValue: ApplicationDataSourceHelper.MissingItemReplacementValue,
                Names: ['application.mortgageLoanApplicationType'],
                ReplaceIfMissing: true
            }

            this.apiClient.fetchItemBasedCreditDecision({ ApplicationNr: ai.ApplicationNr, OnlyDecisionType: wfc.DecisionType, MaxCount: 1, MustBeCurrent: false, IncludeRejectionReasonToDisplayNameMapping: true }).then(x => {
                this.apiClient.fetchApplicationDataSourceItems(this.initialData.applicationInfo.ApplicationNr, [r]).then(y => {
                    let applicationtypename = '';
                    for (var i = 0; i < y.Results[0].Items[0].EditorModel.DropdownRawDisplayTexts.length; i++) {
                        if (x.Decisions.length > 0 && y.Results[0].Items[0].EditorModel.DropdownRawOptions[i] == x.Decisions[0].UniqueItems.applicationType)
                            applicationtypename = y.Results[0].Items[0].EditorModel.DropdownRawDisplayTexts[i]
                    }

                    let isFinal = wfc.IsFinal === 'yes'
                    let doesDecisionHistoryAllowNewCreditCheck: boolean
                    if (x.Decisions && x.Decisions.length > 0) {
                        let d = x.Decisions[0]
                        doesDecisionHistoryAllowNewCreditCheck = !d.ExistsLaterDecisionOfDifferentType //Later decisons must be remove first to repeat previous
                            && (!isFinal || d.ExistsEarlierDecisionOfDifferentType) //Final has the additional demand of requiring at least one earlier
                    } else {
                        doesDecisionHistoryAllowNewCreditCheck = !isFinal || (isFinal && x.ExistsEarlierDecisionOfDifferentType === true)
                    }

                    if (!this.initialData.workflowModel.isStatusInitial(ai)) {
                        if (x.Decisions && x.Decisions.length > 0) {
                            setup(x.Decisions[0], x.RejectionReasonToDisplayNameMapping, doesDecisionHistoryAllowNewCreditCheck, applicationtypename)
                        } else {
                            setup(null, x.RejectionReasonToDisplayNameMapping, doesDecisionHistoryAllowNewCreditCheck, applicationtypename)
                        }
                    } else {
                        setup(null, x.RejectionReasonToDisplayNameMapping, doesDecisionHistoryAllowNewCreditCheck, applicationtypename)
                    }
                })
            })
        }

        isNewCreditCheckPossibleButNeedsReactivate(): boolean {
            if (!this.initialData || !this.initialData.applicationInfo) {
                return false
            }
            let a = this.initialData.applicationInfo
            return !a.IsActive && !a.IsFinalDecisionMade && (a.IsCancelled || a.IsRejected)
                && !a.HasLockedAgreement
                && this.m.doesDecisionHistoryAllowNewCreditCheck
        }

        isNewCreditCheckPossible(): boolean {
            if (!this.initialData || !this.initialData.applicationInfo) {
                return false
            }
            let appInfo = this.initialData.applicationInfo

            let isActiveAndOk = appInfo.IsActive
                && !appInfo.HasLockedAgreement
                && this.m.doesDecisionHistoryAllowNewCreditCheck
                && this.initialData.workflowModel.areAllStepBeforeThisAccepted(appInfo)
            return isActiveAndOk || this.isNewCreditCheckPossibleButNeedsReactivate()
        }

        isViewCreditCheckDetailsPossible(): boolean {
            if (!this.initialData) {
                return false
            }
            return !this.initialData.workflowModel.isStatusInitial(this.initialData.applicationInfo)
        }

        a(name: string) {
            if (!this.m || !this.m.decision || !this.m.decision.IsAccepted) {
                return null
            }
            return this.m.decision.UniqueItems[name]
        }

        an(name: string) {
            return this.parseDecimalOrNull(this.a(name))
        }

        newCreditCheck(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            if (!this.isNewCreditCheckPossible()) {
                return
            }

            let doCheck = () => {
                let url = this.getLocalModuleUrl('Ui/MortgageLoan/NewCreditCheck', [
                    ['applicationNr', this.initialData.applicationInfo.ApplicationNr],
                    ['scoringWorkflowStepName', this.initialData.workflowModel.currentStep.Name]
                ])
                location.href = url
            }

            if (this.isNewCreditCheckPossibleButNeedsReactivate()) {
                this.apiClient.reactivateCancelledApplication(this.initialData.applicationInfo.ApplicationNr).then(() => {
                    doCheck()
                })
            } else {
                doCheck()
            }
        }

        viewCreditCheckDetails(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            if (!this.isViewCreditCheckDetailsPossible()) {
                return
            }

            let url = this.getLocalModuleUrl('Ui/MortgageLoan/ViewCreditCheckDetails', [
                ['applicationNr', this.initialData.applicationInfo.ApplicationNr],
                ['scoringWorkflowStepName', this.initialData.workflowModel.currentStep.Name]
            ])

            location.href = url
        }

        getRejectionReasonDisplayName(reasonName: string) {
            let r = ''
            if (this.m && this.m.rejectionReasonToDisplayNameMapping) {
                r = this.m.rejectionReasonToDisplayNameMapping[reasonName]
            }
            if (!r) {
                r = reasonName
            }
            return r
        }
    }

    export class Model {
        decision: NTechPreCreditApi.ItemBasedDecisionModel
        rejectionReasonToDisplayNameMapping: NTechPreCreditApi.IStringDictionary<string>
        customWorkflowStepData: CustomWorkFlowStepDataModel
        doesDecisionHistoryAllowNewCreditCheck: boolean
        applicationtypeName: string
    }

    export interface CustomWorkFlowStepDataModel {
        DecisionType: string
        CopyFromDecisionType: string
        IsFinal: string //"yes" or "no". Avoiding "true" and "false" since boolean strings an javascript dont mix well
        NewCreditCheckComponentName: string
        ViewCreditCheckComponentName: string
    }

    export class MortgageLoanApplicationDualInitialCreditCheckComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationRawController;
            this.template = `<div ng-if="$ctrl.m">
        <div class="text-right" ng-if="$ctrl.isViewCreditCheckDetailsPossible()">
            <a class="n-anchor" ng-click="$ctrl.viewCreditCheckDetails($event)">View details</a>
        </div>
        <div ng-if="$ctrl.m.decision">
            <div ng-if="!$ctrl.m.decision.IsAccepted" class="form-horizontal pb-3">
                <div class="form-group">
                    <label class="col-xs-3 control-label">Reasons</label>
                    <div class="col-xs-9 form-control-static">
                        <span ng-repeat="r in $ctrl.m.decision.RepeatingItems['rejectionReason']"><span ng-hide="$first">, </span>{{$ctrl.getRejectionReasonDisplayName(r)}}</span>
                    </div>
                </div>
            </div>
            <div ng-if="$ctrl.m.decision.IsAccepted" class="pb-3">
                <h3>{{$ctrl.m.applicationtypeName}}</h3>
                <h3>Mortgage loan</h3>
                <div class="row pb-2">
                    <div class="col-xs-6">
                        <div class="form-horizontal">
                            <div class="form-group">
                                <label class="col-xs-6 control-label">Loan amount</label>
                                <div class="col-xs-6 form-control-static">{{$ctrl.an('mainLoanAmount') | currency}}</div>
                            </div>
                            <div class="form-group">
                                <label class="col-xs-6 control-label">Repayment time</label>
                                <div class="col-xs-6 form-control-static">{{$ctrl.an('mainRepaymentTimeInMonths') }} months</div>
                            </div>
                            <div class="form-group">
                                <label class="col-xs-6 control-label">Interest rate</label>
                                <div class="col-xs-6 form-control-static">
                                    <span>{{$ctrl.an('mainMarginInterestRatePercent') | number}} %</span>
                                    <span ng-if="$ctrl.an('mainReferenceInterestRatePercent')">&nbsp;({{($ctrl.an('mainReferenceInterestRatePercent') > 0 ? '+' : '') + ($ctrl.an('mainReferenceInterestRatePercent') | number:2)}} %)</span>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="col-xs-6">
                        <div class="form-horizontal">
                            <div class="form-group">
                                <label class="col-xs-6 control-label">Initial fee</label>
                                <div class="col-xs-6 form-control-static">{{$ctrl.an('mainTotalInitialFeeAmount') | currency}}</div>
                            </div>
                            <div class="form-group">
                                <label class="col-xs-6 control-label">Monthly fee</label>
                                <div class="col-xs-6 form-control-static">{{$ctrl.an('mainNotificationFeeAmount') | currency}}</div>
                            </div>
                            <div class="form-group">
                                <label class="col-xs-6 control-label">Eff. interest rate</label>
                                <div class="col-xs-6 form-control-static">{{$ctrl.an('mainEffectiveInterestRatePercent') | number:2}} %</div>
                            </div>
                            <div class="form-group">
                                <label class="col-xs-6 control-label">Monthly amount</label>
                                <div class="col-xs-6 form-control-static">{{($ctrl.an('mainAnnuityAmount') + $ctrl.an('mainNotificationFeeAmount')) | currency}}</div>
                            </div>
                        </div>
                    </div>
                </div>
                <hr class="hr-section dotted"/>
                <h3>Loan with collateral</h3>
                <div class="row">
                    <div class="col-xs-6">
                        <div class="form-horizontal">
                            <div class="form-group">
                                <label class="col-xs-6 control-label">Loan amount</label>
                                <div class="col-xs-6 form-control-static">{{$ctrl.an('childLoanAmount') | currency}}</div>
                            </div>
                            <div class="form-group">
                                <label class="col-xs-6 control-label">Repayment time</label>
                                <div class="col-xs-6 form-control-static">{{$ctrl.an('childRepaymentTimeInMonths') }} months</div>
                            </div>
                            <div class="form-group">
                                <label class="col-xs-6 control-label">Interest rate</label>
                                <div class="col-xs-6 form-control-static">
                                    <span>{{$ctrl.an('childMarginInterestRatePercent') | number}} %</span>
                                    <span ng-if="$ctrl.an('childReferenceInterestRatePercent')">&nbsp;({{($ctrl.an('childReferenceInterestRatePercent') > 0 ? '+' : '') + ($ctrl.an('childReferenceInterestRatePercent') | number:2)}} %)</span>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="col-xs-6">
                        <div class="form-horizontal">
                            <div class="form-group">
                                <label class="col-xs-6 control-label">Initial fee</label>
                                <div class="col-xs-6 form-control-static">{{$ctrl.an('childTotalInitialFeeAmount') | currency}}</div>
                            </div>
                            <div class="form-group">
                                <label class="col-xs-6 control-label">Monthly fee</label>
                                <div class="col-xs-6 form-control-static">{{$ctrl.an('childNotificationFeeAmount') | currency}}</div>
                            </div>
                            <div class="form-group">
                                <label class="col-xs-6 control-label">Eff. interest rate</label>
                                <div class="col-xs-6 form-control-static">{{$ctrl.an('childEffectiveInterestRatePercent') | number:2}} %</div>
                            </div>
                            <div class="form-group">
                                <label class="col-xs-6 control-label">Monthly amount</label>
                                <div class="col-xs-6 form-control-static">{{($ctrl.an('childAnnuityAmount') + $ctrl.an('childNotificationFeeAmount')) | currency}}</div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div class="pt-3" ng-if="$ctrl.isNewCreditCheckPossible()">
            <a class="n-main-btn n-blue-btn" ng-click="$ctrl.newCreditCheck($event)">
                New credit check <span class="glyphicon glyphicon-arrow-right"></span>
            </a>
        </div></div>`
        }
    }
}

angular.module('ntech.components').component('mortgageLoanApplicationDualCreditCheck', new MortgageLoanApplicationDualCreditCheckComponentNs.MortgageLoanApplicationDualInitialCreditCheckComponent())