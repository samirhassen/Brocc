namespace MortgageLoanApplicationDualSettlementComponentNs {
    export class MortgageLoanApplicationDualSettlementController extends NTechComponents.NTechComponentControllerBase {
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
            return 'mortgageLoanApplicationDualSettlement'
        }

        onChanges() {
            this.reload()
        }

        private reload() {
            this.m = null

            if (!this.initialData) {
                return
            }

            let ai = this.initialData.applicationInfo
            let areAllStepBeforeThisAccepted = this.initialData.workflowModel.areAllStepBeforeThisAccepted(ai)

            if (!areAllStepBeforeThisAccepted) {
                this.m = {
                    isHandleAllowed: areAllStepBeforeThisAccepted && ai.HasLockedAgreement
                }
            } else {
                this.apiClient.fetchItemBasedCreditDecision({
                    ApplicationNr: ai.ApplicationNr,
                    MustBeCurrent: true,
                    MustBeAccepted: true,
                    MaxCount: 1
                }).then(decisions => {
                    let decision = decisions.Decisions[0]
                    this.m = {
                        decision: {
                            applicationType: decision.UniqueItems['applicationType']
                        },
                        isHandleAllowed: areAllStepBeforeThisAccepted && ai.HasLockedAgreement
                    }
                })
            }
        }

        handle(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.getUserModuleUrl(
                'nPreCredit',
                'Ui/MortgageLoan/Handle-Settlement',
                {
                    applicationNr: this.initialData.applicationInfo.ApplicationNr,
                    backUrl: this.initialData.urlToHereFromOtherModule
                }
            ).then(x => {
                document.location.href = x.UrlExternal
            })
        }
    }

    export class Model {
        isHandleAllowed: boolean
        decision?: {
            applicationType: string
        }
    }

    export class MortgageLoanApplicationDualSettlementComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationDualSettlementController;
            this.template = `<div class="container" ng-if="$ctrl.m">

        <div ng-if="$ctrl.m.decision">
            <div>
                <div class="row">
                    <div class="col-xs-6">
                        <div class="form-horizontal">
                            <div class="form-group">
                                <label class="col-xs-6 control-label">Type</label>
                                <div class="col-xs-6 form-control-static">{{$ctrl.m.decision.applicationType}}</div>
                            </div>
                        </div>
                    </div>
                    <div class="col-xs-6">

                    </div>
                </div>
            </div>
        </div>

        <div class="pt-3 text-center" ng-if="$ctrl.m.isHandleAllowed">
            <a class="n-main-btn n-blue-btn" ng-click="$ctrl.handle($event)">
                Handle payments and settlement <span class="glyphicon glyphicon-arrow-right"></span>
            </a>
        </div>
</div>`
        }
    }
}

angular.module('ntech.components').component('mortgageLoanApplicationDualSettlement', new MortgageLoanApplicationDualSettlementComponentNs.MortgageLoanApplicationDualSettlementComponent())