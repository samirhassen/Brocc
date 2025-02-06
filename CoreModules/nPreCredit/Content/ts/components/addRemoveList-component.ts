namespace AddRemoveListComponentNs {
    export function createNewRow(applicationNr: string, listName: string, nr: number, apiClient: NTechPreCreditApi.ApiClient): ng.IPromise<number> {
        let itemName = ComplexApplicationListHelper.getDataSourceItemName(listName, nr.toString(), 'exists', ComplexApplicationListHelper.RepeatableCode.No)
        return apiClient.setApplicationEditItemData(applicationNr, 'ComplexApplicationList', itemName, 'true', false).then(x => {
            return nr
        })
    }

    export class AddRemoveListController extends NTechComponents.NTechComponentControllerBase {
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
            return 'addRemoveList'
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }
            let i = this.initialData
            let ai = this.initialData.ai
            ComplexApplicationListHelper.getNrs(ai.ApplicationNr, i.listName, this.apiClient).then(rowNrs => {
                this.m = {
                    isEditAllowed: i.isEditAllowed,
                    headerText: i.headerText,
                    rows: NTechLinq.select(rowNrs, rowNr => {
                        return {
                            d: this.createItemInitialData(rowNr),
                            nr: rowNr,
                            viewDetailsUrl: this.initialData.getViewDetailsUrl ? this.initialData.getViewDetailsUrl(rowNr) : null
                        }
                    })
                }
            })
        }

        addRow(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            let currentMax = 0
            for (let c of this.m.rows) {
                currentMax = Math.max(c.nr, currentMax)
            }
            createNewRow(this.initialData.ai.ApplicationNr, this.initialData.listName, currentMax + 1, this.apiClient).then(newRowNr => {
                this.m.rows.push({
                    d: this.createItemInitialData(newRowNr),
                    nr: newRowNr,
                    viewDetailsUrl: this.initialData.getViewDetailsUrl ? this.initialData.getViewDetailsUrl(newRowNr) : null
                })
                this.emitEvent(newRowNr, false, true, false)
            })
        }

        deleteRow(nr: number, evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            ComplexApplicationListHelper.deleteRow(this.initialData.ai.ApplicationNr, this.initialData.listName, nr, this.apiClient).then(x => {
                let i = NTechLinq.firstIndexOf(this.m.rows, x => x.nr === nr)
                if (i >= 0) {
                    this.m.rows.splice(i, 1)
                    this.emitEvent(nr, false, false, true)
                }
            })
        }

        private createItemInitialData(rowNr: number): ApplicationEditorComponentNs.InitialData {
            let i = this.initialData
            let ai = i.ai

            return ApplicationEditorComponentNs.createInitialData(ai.ApplicationNr, ai.ApplicationType, NavigationTargetHelper.createTargetFromComponentHostToHere(i.host), this.apiClient, this.$q, x => {
                for (let itemName of i.itemNames) {
                    x.addComplexApplicationListItem(`${i.listName}#${rowNr}#u#${itemName}`)
                }
            }, {
                isInPlaceEditAllowed: true,
                afterInPlaceEditsCommited: () => { this.emitEvent(rowNr, true, false, false) },
                labelSize: this.initialData.applicationEditorLabelSize,
                enableChangeTracking: this.initialData.applicationEditorEnableChangeTracking
            })
        }

        emitEvent(nr: number, isEdit: boolean, isAdd: boolean, isRemove: boolean) {
            this.ntechComponentService.emitNTechCustomDataEvent<ChangeEvent>(ChangeEventName, {
                nr: nr,
                isEdit: isEdit,
                isAdd: isAdd,
                isRemove: isRemove,
                eventCorrelationId: this.initialData.eventCorrelationId
            })
        }
    }

    export class Model {
        isEditAllowed: boolean
        headerText: string
        rows: { d: ApplicationEditorComponentNs.InitialData, nr: number, viewDetailsUrl: string }[]
    }

    export interface InitialData {
        host: ComponentHostNs.ComponentHostInitialData
        ai: NTechPreCreditApi.ApplicationInfoModel
        headerText: string
        listName: string
        itemNames: string[]
        isEditAllowed: boolean
        getViewDetailsUrl?: (rowNr: number) => string
        eventCorrelationId?: string
        applicationEditorLabelSize?: number
        applicationEditorEnableChangeTracking?: boolean
    }

    export interface ChangeEvent {
        nr: number
        isEdit: boolean
        isAdd: boolean
        isRemove: boolean
        eventCorrelationId: string
    }

    export const ChangeEventName = 'addRemoveListChangeEvent'

    export class AddRemoveListComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = AddRemoveListController;
            this.template = `<div>

<div>
    <h3>{{$ctrl.m.headerText}}</h3>
    <hr class="hr-section"/>
    <button class="n-direct-btn n-green-btn" ng-click="$ctrl.addRow($event)" ng-if="$ctrl.m.isEditAllowed">Add</button>
</div>

<hr class="hr-section dotted" />

<div ng-repeat="c in $ctrl.m.rows">
    <div class="row" >
        <div ng-class="{ 'col-sm-8': c.viewDetailsUrl, 'col-sm-10': !c.viewDetailsUrl }">
            <application-editor initial-data="c.d"></application-editor>
        </div>
        <div class="col-sm-2 text-right">
            <a ng-if="c.viewDetailsUrl" class="n-anchor" ng-href="{{c.viewDetailsUrl}}">View details</a>
        </div>
        <div class="col-sm-2 text-right">
            <button ng-if="$ctrl.m.isEditAllowed" class="n-icon-btn n-red-btn" ng-click="$ctrl.deleteRow(c.nr, $event)"><span class="glyphicon glyphicon-minus"></span></button>
        </div>
    </div>
    <hr ng-if="$ctrl.m.rows && $ctrl.m.rows.length > 0" class="hr-section dotted">
</div>

</div>`
        }
    }
}

angular.module('ntech.components').component('addRemoveList', new AddRemoveListComponentNs.AddRemoveListComponent())