namespace MortgageLoanDualCollateralCompactComponentNs {
    export class MortgageLoanDualCollateralCompactController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'mortgageLoanDualCollateralCompact'
        }

        onChanges() {
            this.reload()
        }

        private reload() {
            this.m = null

            if (!this.initialData) {
                return
            }

            this.m = {
                infoBlocks: []
            }

            ComplexApplicationListHelper.fetch(this.initialData.applicationNr, MortgageApplicationCollateralEditComponentNs.ListName, this.apiClient, MortgageApplicationCollateralEditComponentNs.CompactFieldNames).then(x => {
                let shownNrs = NTechLinq.where(x.getNrs(), nr => {
                    if (this.initialData.onlyMainCollateral) {
                        return nr === 1
                    } else if (this.initialData.onlyOtherCollaterals) {
                        return nr > 1
                    } else {
                        return true
                    }
                })
                for (let nr of shownNrs) {
                    let id = new TwoColumnInformationBlockComponentNs.InitialData()
                    let uniqueItems = x.getUniqueItems(nr)
                    for (let fieldName of MortgageApplicationCollateralEditComponentNs.CompactFieldNames) {
                        let m = x.getEditorModel(fieldName)
                        let value = uniqueItems[fieldName]
                        if (m) {
                            id.applicationItem(true, value, m, 3)
                        } else {
                            id.item(true, uniqueItems[fieldName], null, fieldName, null, 3)
                        }
                    }
                    this.m.infoBlocks.push({
                        data: id,
                        viewDetailsUrl: this.initialData.allowViewDetails ? this.getLocalModuleUrl('/Ui/MortgageLoan/Edit-Collateral', [
                            ['applicationNr', this.initialData.applicationNr],
                            ['listNr', nr.toString()],
                            ['backTarget', this.initialData.viewDetailsUrlTargetCode]]) : null,
                        allowDelete: this.initialData.allowDelete && nr > 1,
                        nr: nr
                    })
                }
            })
        }

        getInfoBlockWidthClass(infoBlock: { data: TwoColumnInformationBlockComponentNs.InitialData, viewDetailsUrl: string, allowDelete: boolean }) {
            let width = 6
            if (!infoBlock.viewDetailsUrl) {
                width += 2
            }
            if (!infoBlock.allowDelete) {
                width += 4
            }
            return `col-sm-${width}`
        }

        deleteCollateral(nr: number, evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (nr <= 1) {
                return
            }
            ComplexApplicationListHelper.deleteRow(this.initialData.applicationNr, MortgageApplicationCollateralEditComponentNs.ListName, nr, this.apiClient).then(x => {
                this.signalReloadRequired()
            })
        }
    }

    export class Model {
        infoBlocks: { data: TwoColumnInformationBlockComponentNs.InitialData, viewDetailsUrl: string, nr: number, allowDelete: boolean }[]
    }

    export interface InitialData {
        applicationNr: string
        onlyMainCollateral: boolean
        onlyOtherCollaterals: boolean
        allowViewDetails: boolean
        allowDelete: boolean
        viewDetailsUrlTargetCode: string
    }

    export class MortgageLoanDualCollateralCompactComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanDualCollateralCompactController;
            this.template = `<div ng-if="$ctrl.m">
    <div class="row {{$index > 0 ? 'pt-3' : '0'}}" ng-repeat="i in $ctrl.m.infoBlocks track by $index">
        <div class="{{$ctrl.getInfoBlockWidthClass(i)}}">
            <two-column-information-block initial-data="i.data">
            </two-column-information-block>
        </div>
        <div class="col-sm-2 text-right" ng-if="i.viewDetailsUrl">
            <a class="n-anchor" ng-href="{{i.viewDetailsUrl}}">View details</a>
        </div>
        <div class="col-sm-4 text-right" ng-if="i.allowDelete">
            <button class="n-icon-btn n-red-btn" ng-click="$ctrl.deleteCollateral(i.nr, $event)"><span class="glyphicon glyphicon-minus"></span></button>
        </div>
    </div>
</div>`
        }
    }
}

angular.module('ntech.components').component('mortgageLoanDualCollateralCompact', new MortgageLoanDualCollateralCompactComponentNs.MortgageLoanDualCollateralCompactComponent())