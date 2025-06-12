namespace ApplicationEditorComponentNs {
    export class ApplicationEditorController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData;
        formName: string;
        m: Model

        static $inject = ['$http', '$q', '$scope', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            private $scope: ng.IScope,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);

            this.formName = 'f' + NTechComponents.generateUniqueId(6)
            this.$scope['formContainer'] = {}
        }

        componentName(): string {
            return 'applicationEditor'
        }

        onBack(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            NavigationTargetHelper.handleBack(
                this.initialData.navigationOptionToHere,
                this.apiClient,
                this.$q,
                { applicationNr: this.initialData.applicationNr })
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            let d = this.initialData
            d.loadItems().then(x => {
                let mode = d.isInPlaceEditAllowed ? Mode.DirectEdit : Mode.ViewOnly
                let m: Model = {
                    items: [],
                    models: {},
                    mode: mode,
                    isDirectEditOnlyMode: mode === Mode.DirectEdit,
                    isEditing: false,
                    directEditModels: null,
                    labelSize: this.initialData.labelSize,
                    enableChangeTracking: this.initialData.enableChangeTracking
                }
                for (let ds of x.Results) {
                    m.models[ds.DataSourceName] = {}
                }
                for (let i of d.getIncludedItems()) {
                    if (i.dataSourceName) {
                        let dsResult = NTechLinq.first(x.Results, y => y.DataSourceName === i.dataSourceName)
                        let dsItem = NTechLinq.first(dsResult.Items, x => x.Name === i.itemName)
                        m.models[i.dataSourceName][i.itemName] = ApplicationItemEditorComponentNs.createDataModelUsingDataSourceResult(d.applicationNr, d.applicationType, (m.mode === Mode.DirectEdit) && !i.forceReadonly, d.navigationOptionToHere, dsResult)
                        m.items.push({ itemType: ItemType.DataSourceItem, dataSourceName: i.dataSourceName, itemName: i.itemName, editModel: dsItem.EditorModel, forceReadonly: i.forceReadonly }) //todo, buttons, static values and so on
                    } else {
                        //TODO: Buttons, separators and such
                    }
                }
                this.m = m
            })
        }

        form = () => {
            if (!this.$scope) {
                return null
            }
            let c = this.$scope['formContainer']
            if (!c) {
                return null
            }
            return c[this.formName] as ng.IFormController
        }

        isFormInvalid() {
            return !this.form() || this.form().$invalid
        }

        cancelDirectEdit() {
            if (!this.m) {
                return
            }
            this.m.isEditing = false
            this.m.directEditModels = null
        }

        commitDirectEdit() {
            if (!this.m) {
                return
            }

            let originalValues = this.createEditValues()
            let newValues = this.m.directEditModels
            let edits: { dataSourceName: string, itemName: string, fromValue: string, toValue: string }[] = []

            for (let dataSourceName of Object.keys(originalValues)) {
                for (let itemName of Object.keys(originalValues[dataSourceName])) {
                    let originalValue = originalValues[dataSourceName][itemName]
                    let newValue = newValues[dataSourceName][itemName]
                    if (originalValue !== newValue) {
                        edits.push({ dataSourceName: dataSourceName, itemName: itemName, fromValue: originalValue, toValue: newValue })
                    }
                }
            }

            if (edits.length == 0) {
                this.m.isEditing = false
                this.m.directEditModels = null
            } else {
                this.initialData.saveItems(edits).then(x => {
                    this.m.isEditing = null
                    this.m.directEditModels = null
                    for (let e of edits) {
                        let itemModel = this.m.models[e.dataSourceName][e.itemName]
                        itemModel.isEditedByGroupedName[e.itemName] = true
                        itemModel.valueByGroupedName[e.itemName] = e.toValue
                    }
                })
            }
        }

        beginDirectEdit() {
            if (!this.m) {
                return
            }

            this.m.directEditModels = this.createEditValues()
            this.m.isEditing = true
        }

        private createEditValues(): NTechPreCreditApi.IStringDictionary<NTechPreCreditApi.IStringDictionary<any>> {
            let ms: NTechPreCreditApi.IStringDictionary<NTechPreCreditApi.IStringDictionary<any>> = {}
            for (let dataSourceName of Object.keys(this.m.models)) {
                ms[dataSourceName] = {}
                for (let itemName of Object.keys(this.m.models[dataSourceName])) {
                    let item = this.m.models[dataSourceName][itemName]
                    let value = item.valueByGroupedName[itemName]
                    if (value === ApplicationDataSourceHelper.MissingItemReplacementValue) {
                        value = null
                    }
                    ms[dataSourceName][itemName] = value
                }
            }
            return ms
        }
    }

    export class ApplicationEditorComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;
        public transclude: boolean;

        constructor() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = ApplicationEditorController;
            this.template = `<div ng-if="$ctrl.m" class="form-horizontal">
        <form novalidate name="{{$ctrl.formName}}" ng-if="$ctrl.m.mode === 'NavigationEdit' || $ctrl.m.mode === 'ViewOnly'">
            <application-item-editor ng-repeat="i in $ctrl.m.items" name="i.itemName" data="$ctrl.m.models[i.dataSourceName][i.itemName]" label-size="$ctrl.m.labelSize" enable-change-tracking="$ctrl.m.enableChangeTracking"></application-item-editor>
        </form>

        <form name="formContainer.{{$ctrl.formName}}" ng-if="$ctrl.m.mode === 'DirectEdit'">
            <application-item-editor ng-repeat="i in $ctrl.m.items" name="i.itemName" direct-edit-form="$ctrl.form" direct-edit="!i.forceReadonly && $ctrl.m.isEditing" direct-edit-model="$ctrl.m.directEditModels[i.dataSourceName]" data="$ctrl.m.models[i.dataSourceName][i.itemName]" label-size="$ctrl.m.labelSize" enable-change-tracking="$ctrl.m.enableChangeTracking"></application-item-editor>

            <div class="text-right pt-1">
                <button class="n-icon-btn n-white-btn" style="margin-right:5px;" ng-click="$ctrl.cancelDirectEdit()" ng-if="$ctrl.m.isEditing"><span class="glyphicon glyphicon-remove"></span></button>
                <button class="n-icon-btn n-green-btn" ng-click="$ctrl.commitDirectEdit()" ng-disabled="$ctrl.isFormInvalid()" ng-if="$ctrl.m.isEditing"><span class="glyphicon glyphicon-ok"></span></button>
                <button class="n-icon-btn n-blue-btn" ng-click="$ctrl.beginDirectEdit()" ng-if="!$ctrl.m.isEditing"><span class="glyphicon glyphicon-pencil"></span></button>
            </div>
        </form>
</div>`;
        }
    }
    export class Model {
        mode: Mode
        isDirectEditOnlyMode: boolean
        models: NTechPreCreditApi.IStringDictionary<NTechPreCreditApi.IStringDictionary<ApplicationItemEditorComponentNs.DataModel>>
        items: {
            itemType: ItemType,
            dataSourceName: string,
            itemName: string,
            editModel: NTechPreCreditApi.FetchApplicationEditItemDataResponseEditModel,
            forceReadonly: boolean
        }[]
        isEditing: boolean
        directEditModels: NTechPreCreditApi.IStringDictionary<NTechPreCreditApi.IStringDictionary<any>>
        labelSize: number
        enableChangeTracking: boolean
    }

    export enum ItemType {
        DataSourceItem = "DataSourceItem "
    }

    export enum Mode {
        ViewOnly = "ViewOnly",
        DirectEdit = "DirectEdit"
    }

    export interface CreateFormOptions {
        isHorizontal?: boolean
        isInPlaceEditAllowed?: boolean
        labelIsTranslationKey?: boolean
        afterInPlaceEditsCommited?: (changes: NTechPreCreditApi.SetApplicationEditItemDataResponse[]) => void
        afterDataLoaded?: (data: NTechPreCreditApi.FetchApplicationDataSourceItemsResponse) => void
        labelSize?: number
        enableChangeTracking?: boolean
    }

    export function createInitialDataVirtual(dataSourceService: ApplicationDataSourceHelper.IApplicationDataSourceService, applicationNr: string, applicationType: string, navigationOptionToHere: NavigationTargetHelper.CodeOrUrl, opts?: CreateFormOptions): InitialData {
        let fields = new InitialData(dataSourceService, !!(opts && opts.isHorizontal),
            !!(opts && opts.isInPlaceEditAllowed),
            !!(opts && opts.labelIsTranslationKey),
            applicationNr,
            applicationType,
            navigationOptionToHere,
            opts ? opts.labelSize : null,
            opts ? opts.enableChangeTracking : null)
        return fields
    }

    export function createInitialData2(dataSourceService: ApplicationDataSourceHelper.ApplicationDataSourceService, applicationType: string, navigationOptionToHere: NavigationTargetHelper.CodeOrUrl, setupFields: (f: ApplicationDataSourceHelper.ApplicationDataSourceService) => void, opts?: CreateFormOptions): InitialData {
        let fields = new InitialData(dataSourceService, !!(opts && opts.isHorizontal),
            !!(opts && opts.isInPlaceEditAllowed),
            !!(opts && opts.labelIsTranslationKey),
            dataSourceService.applicationNr,
            applicationType,
            navigationOptionToHere,
            opts ? opts.labelSize : null,
            opts ? opts.enableChangeTracking : null)
        setupFields(dataSourceService)
        return fields
    }

    export function createInitialData(applicationNr: string, applicationType: string, navigationOptionToHere: NavigationTargetHelper.CodeOrUrl, apiClient: NTechPreCreditApi.ApiClient, $q: ng.IQService, setupFields: (f: ApplicationDataSourceHelper.ApplicationDataSourceService) => void, opts?: CreateFormOptions): InitialData {
        let d = new ApplicationDataSourceHelper.ApplicationDataSourceService(applicationNr, apiClient, $q, opts ? opts.afterInPlaceEditsCommited : null,
            opts ? opts.afterDataLoaded : null)
        let fields = new InitialData(d, !!(opts && opts.isHorizontal),
            !!(opts && opts.isInPlaceEditAllowed),
            !!(opts && opts.labelIsTranslationKey),
            applicationNr,
            applicationType,
            navigationOptionToHere,
            opts ? opts.labelSize : null,
            opts ? opts.enableChangeTracking : null)
        setupFields(d)
        return fields
    }

    export class InitialData {
        constructor(
            private dataSourceService: ApplicationDataSourceHelper.IApplicationDataSourceService,
            public isHorizontal: boolean,
            public isInPlaceEditAllowed: boolean,
            public labelIsTranslationKey: boolean,
            public applicationNr: string,
            public applicationType: string,
            public navigationOptionToHere: NavigationTargetHelper.CodeOrUrl,
            public labelSize: number,
            public enableChangeTracking: boolean) {
        }

        public getIncludedItems(): { dataSourceName: string, itemName: string, forceReadonly: boolean }[] {
            return this.dataSourceService.getIncludedItems()
        }

        loadItems(): ng.IPromise<NTechPreCreditApi.FetchApplicationDataSourceItemsResponse> {
            return this.dataSourceService.loadItems()
        }

        saveItems(edits: { dataSourceName: string, itemName: string, fromValue: string, toValue: string }[]): ng.IPromise<NTechPreCreditApi.SetApplicationEditItemDataResponse[]> {
            return this.dataSourceService.saveItems(edits)
        }
    }

    export enum ItemDataType {
        Text = "Text",
        Enum = "Enum",
        Number = "Number",
        Currency = "Currency",
        Percent = "Percent",
        Url = "Url"
    }

    export interface IApplicationEditorDataItem {
        name: string
        label: string
        dataType: ItemDataType
        initialValue: any
        wasInitiallyEdited: boolean
        editUrl: string
    }
}

angular.module('ntech.components').component('applicationEditor', new ApplicationEditorComponentNs.ApplicationEditorComponent())