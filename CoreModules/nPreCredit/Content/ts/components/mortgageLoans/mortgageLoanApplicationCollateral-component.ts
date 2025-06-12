namespace MortgageLoanApplicationCollateralComponentNs {
    export function createNewCollateral(applicationNr: string, nr: number, apiClient: NTechPreCreditApi.ApiClient): ng.IPromise<void> {
        let itemName = MortgageApplicationCollateralEditComponentNs.getDataSourceItemName(nr.toString(), 'exists', ComplexApplicationListHelper.RepeatableCode.No)
        return apiClient.setApplicationEditItemData(applicationNr, 'ComplexApplicationList', itemName, 'true', false).then(x => {
        })
    }

    export function getAdditionalCollateralNrs(applicationNr: string, apiClient: NTechPreCreditApi.ApiClient): ng.IPromise<number[]> {
        return ComplexApplicationListHelper.getNrs(applicationNr, MortgageApplicationCollateralEditComponentNs.ListName, apiClient).then(x => NTechLinq.where(x, y => y !== 1))
    }

    export class MortgageLoanApplicationCollateralController extends NTechComponents.NTechComponentControllerBase {
        initialData: MortgageLoanApplicationDynamicComponentNs.StepInitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'mortgageLoanApplicationCollateral'
        }

        onChanges() {
            this.m = null

            if (!this.initialData || !this.initialData.applicationInfo) {
                return
            }

            let i = this.initialData
            let ai = i.applicationInfo
            let wf = this.initialData.workflowModel;
            let isEditAllowed = ai.IsActive && !ai.IsFinalDecisionMade && !ai.HasLockedAgreement

            getAdditionalCollateralNrs(ai.ApplicationNr, this.apiClient).then(x => {
                this.m = {
                    isEditAllowed: ai.IsActive && !ai.IsFinalDecisionMade && !ai.HasLockedAgreement,
                    isToggleAcceptedAllowed: wf.areAllStepBeforeThisAccepted(ai) && wf.areAllStepsAfterInitial(ai) && isEditAllowed,
                    isStepAccepted: wf.isStatusAccepted(ai),
                    objectCollateralData: {
                        applicationNr: ai.ApplicationNr,
                        allowDelete: isEditAllowed,
                        allowViewDetails: true,
                        onlyMainCollateral: true,
                        onlyOtherCollaterals: false,
                        viewDetailsUrlTargetCode: NavigationTargetHelper.NavigationTargetCode.MortgageLoanApplication
                    },
                    otherCollateralData: {
                        applicationNr: ai.ApplicationNr,
                        allowDelete: isEditAllowed,
                        allowViewDetails: true,
                        onlyMainCollateral: false,
                        onlyOtherCollaterals: true,
                        viewDetailsUrlTargetCode: NavigationTargetHelper.NavigationTargetCode.MortgageLoanApplication
                    }
                }
            })
        }

        addCollateral(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            let currentMax = 1
            let ai = this.initialData.applicationInfo
            ComplexApplicationListHelper.getNrs(ai.ApplicationNr, MortgageApplicationCollateralEditComponentNs.ListName, this.apiClient).then(nrs => {
                let currentMax = Math.max(...nrs, 1)
                createNewCollateral(this.initialData.applicationInfo.ApplicationNr, currentMax + 1, this.apiClient).then(x => {
                    this.signalReloadRequired()
                })
            })
        }

        toggleStepAccepted(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }

            let i = this.initialData
            let ai = i.applicationInfo
            let wf = this.initialData.workflowModel
            this.initialData.apiClient.setMortgageApplicationWorkflowStatus(ai.ApplicationNr, wf.stepName, wf.isStatusAccepted(ai) ? 'Initial' : 'Accepted').then(() => {
                this.signalReloadRequired()
            })
        }
    }

    export class Model {
        isEditAllowed: boolean
        isStepAccepted: boolean
        isToggleAcceptedAllowed: boolean
        objectCollateralData: MortgageLoanDualCollateralCompactComponentNs.InitialData
        otherCollateralData: MortgageLoanDualCollateralCompactComponentNs.InitialData
    }

    export class MortgageLoanApplicationCollateralComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationCollateralController;
            this.template = `<div ng-if="$ctrl.m">

    <div class="row">
        <div class="col-sm-12">
            <mortgage-loan-dual-collateral-compact initial-data="$ctrl.m.objectCollateralData"></mortgage-loan-dual-collateral-compact>
        </div>
    </div>

    <div>
        <h3>Other properties</h3>
        <hr class="hr-section" />
        <button class="n-direct-btn n-green-btn" ng-click="$ctrl.addCollateral($event)"
            ng-if="$ctrl.m.isEditAllowed">Add</button>
    </div>
    <hr class="hr-section dotted" />

    <div class="row">
        <div class="col-sm-12">
            <mortgage-loan-dual-collateral-compact initial-data="$ctrl.m.otherCollateralData"></mortgage-loan-dual-collateral-compact>
        </div>
    </div>

    <div class="pt-3" ng-show="$ctrl.m.isToggleAcceptedAllowed">
        <label class="pr-2">Collateral {{$ctrl.m.isStepAccepted ? 'done' : 'not done'}}</label>
        <label class="n-toggle">
            <input type="checkbox" ng-checked="$ctrl.m.isStepAccepted" ng-click="$ctrl.toggleStepAccepted($event)" />
            <span class="n-slider"></span>
        </label>
    </div>

</div>`
        }
    }
}

angular.module('ntech.components').component('mortgageLoanApplicationCollateral', new MortgageLoanApplicationCollateralComponentNs.MortgageLoanApplicationCollateralComponent())