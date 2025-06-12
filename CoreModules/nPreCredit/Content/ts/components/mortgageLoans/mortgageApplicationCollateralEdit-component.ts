namespace MortgageApplicationCollateralEditComponentNs {
    export const ListName: string = 'ApplicationObject'
    export const FieldNames: string[] = ['propertyType', 'addressCity', 'addressStreet', 'addressZipCode',
        'use', 'plot', 'constructionYear', 'valuationAmount', 'valuationDate', 'valuationSource',
        'statValuationAmount', 'statValuationDate', 'priceAmount', 'priceAmountDate', 'securityElsewhereAmount', 'url', 'additionalInformation']

    export const CompactFieldNames: string[] = ['propertyType', 'addressCity',
        'valuationAmount', 'statValuationAmount', 'priceAmount', 'securityElsewhereAmount', 'url']

    export const ReloadCollateralEventName = 'MortgageApplicationCollateralEditComponentNs_ReloadCollateralEventName'

    export function getDataSourceItemName(nr: string, itemName: string, repeatableCode: ComplexApplicationListHelper.RepeatableCode) {
        return ComplexApplicationListHelper.getDataSourceItemName(ListName, nr, itemName, repeatableCode)
    }

    export function reloadCollateralEstateData(applicationNr: string, apiClient: NTechPreCreditApi.ApiClient, $q: ng.IQService): ng.IPromise<{
        propertyType: string,
        estateDeeds: EstateDeedItemModel[]
    }> {
        let d = new ApplicationDataSourceHelper.ApplicationDataSourceService(applicationNr, apiClient, $q, changes => { }, data => { })

        d.addComplexApplicationListItems([
            getDataSourceItemName(this.initialData.listNr, 'propertyType', ComplexApplicationListHelper.RepeatableCode.No),
            getDataSourceItemName(this.initialData.listNr, 'estateDeeds', ComplexApplicationListHelper.RepeatableCode.Yes)])
        return d.loadItems().then(x => {
            let currentPropertyType: string = null
            let estateDeeds: EstateDeedItemModel[] = []
            for (let r of x.Results) {
                for (let i of r.Items) {
                    let n = ComplexApplicationListHelper.parseCompoundItemName(i.Name)
                    if (n.itemName === 'propertyType' && i.Value !== ApplicationDataSourceHelper.MissingItemReplacementValue) {
                        currentPropertyType = i.Value
                    } else if (n.itemName === 'estateDeeds' && i.Value != ApplicationDataSourceHelper.MissingItemReplacementValue) {
                        let models: string[] = JSON.parse(i.Value)
                        for (let m of models) {
                            let model: EstateDeedItemModel = JSON.parse(m)
                            estateDeeds.push(model)
                        }
                    }
                }
            }
            return {
                propertyType: currentPropertyType,
                estateDeeds: estateDeeds
            }
        })
    }

    export class MortgageApplicationCollateralEditController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);

            let reloadChildren = () => {
                let isReadonly = this.getIsReadonly(this.m.c.ai)
                this.reloadCollateralChildren(this.m.c.ai, isReadonly).then(d => {
                    this.m.currentPropertyType = d.currentPropertyType
                    this.m.estateData = d.estateData
                    this.m.housingCompanyData = d.housingCompanyData
                    this.m.estateItems = d.estateItems
                })
            }
            ntechComponentService.subscribeToNTechEvents(x => {
                if (x.eventName === ComplexListWithCustomerComponentNs.AfterEditEventName) {
                    let d = x.customData as ComplexListWithCustomerComponentNs.LocalInitialData
                    if (!d || !this.m || d.correlationId !== this.m.c.correlationId) {
                        return
                    }
                    reloadChildren()
                } else if (x.eventName == ReloadCollateralEventName && this.initialData && x.eventData == this.initialData.applicationNr) {
                    reloadChildren()
                }
            })
        }

        componentName(): string {
            return 'mortgageApplicationCollateralEdit'
        }
        private getIsReadonly(ai: NTechPreCreditApi.ApplicationInfoModel): boolean {
            return !ai.IsActive || ai.IsFinalDecisionMade || ai.HasLockedAgreement
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            let i = this.initialData
            let backContext: NavigationTargetHelper.NavigationContext = { listNr: i.listNr, applicationNr: i.applicationNr }
            this.apiClient.fetchApplicationInfo(i.applicationNr).then(x => {
                let isReadonly = this.getIsReadonly(x)
                let ci: ComplexListWithCustomerComponentNs.LocalInitialData = {
                    ai: x,
                    listName: ListName,
                    fieldNames: FieldNames,
                    listNr: parseInt(i.listNr),
                    correlationId: NTechComponents.generateUniqueId(6),
                    isReadonly: isReadonly
                }

                this.reloadCollateralChildren(x, isReadonly).then(d => {
                    this.m = {
                        c: ({ ...i, ...ci } as any) as ComplexListWithCustomerComponentNs.InitialData,
                        currentPropertyType: d.currentPropertyType,
                        estateData: d.estateData,
                        housingCompanyData: d.housingCompanyData,
                        estateItems: d.estateItems,
                        isReadonly: isReadonly,
                        headerData: {
                            host: this.initialData,
                            backTarget: NavigationTargetHelper.createCodeOrUrlFromInitialData(this.initialData, backContext, NavigationTargetHelper.NavigationTargetCode.MortgageLoanApplication),
                            backContext: backContext
                        }
                    }
                    this.m.c.backTarget = NavigationTargetHelper.createCodeTarget(NavigationTargetHelper.NavigationTargetCode.MortgageLoanEditCollateral, backContext).targetCode
                })
            })
        }

        reloadCollateralChildren(ai: NTechPreCreditApi.ApplicationInfoModel, isReadonly: boolean): ng.IPromise<{
            estateData: ApplicationEditorComponentNs.InitialData
            estateItems: EstateItemsService
            housingCompanyData: ApplicationEditorComponentNs.InitialData
            currentPropertyType: string
        }> {
            let d = new ApplicationDataSourceHelper.ApplicationDataSourceService(this.initialData.applicationNr, this.apiClient, this.$q, changes => { }, data => { })
            d.addComplexApplicationListItems([
                getDataSourceItemName(this.initialData.listNr, 'propertyType', ComplexApplicationListHelper.RepeatableCode.No),
                getDataSourceItemName(this.initialData.listNr, 'estateDeeds', ComplexApplicationListHelper.RepeatableCode.Yes)])
            return reloadCollateralEstateData(this.initialData.applicationNr, this.apiClient, this.$q).then(x => {
                if (!x.estateDeeds) {
                    x.estateDeeds = []
                }

                let estateItemsService = new EstateItemsService(ai.IsActive && !ai.IsFinalDecisionMade && !ai.HasLockedAgreement, this.apiClient, this.$q, ai.ApplicationNr, getDataSourceItemName(this.initialData.listNr, 'estateDeeds', ComplexApplicationListHelper.RepeatableCode.Yes), this.ntechComponentService)
                for (let estateDeed of x.estateDeeds) {
                    let virtualDataSource = new VirtualEstateItemsDataSourceService(this.$q, estateDeed, this.formatNumberForEdit, this.parseDecimalOrNull, estateItemsService)
                    let editorInitialData = ApplicationEditorComponentNs.createInitialDataVirtual(virtualDataSource, ai.ApplicationNr, ai.ApplicationType, NavigationTargetHelper.createCodeTarget(NavigationTargetHelper.NavigationTargetCode.MortgageLoanEditCollateral), {
                        isInPlaceEditAllowed: !isReadonly,
                    })
                    estateItemsService.rows.push({ uid: estateDeed.uid, d: editorInitialData, viewDetailsUrl: null })
                }

                let create = (a: (x: ApplicationDataSourceHelper.ApplicationDataSourceService) => void) => {
                    return ApplicationEditorComponentNs.createInitialData(ai.ApplicationNr, ai.ApplicationType, NavigationTargetHelper.createCodeTarget(NavigationTargetHelper.NavigationTargetCode.MortgageLoanEditCollateral), this.apiClient, this.$q, a, {
                        isInPlaceEditAllowed: !isReadonly,
                    })
                }

                return {
                    housingCompanyData: create(x => {
                        x.addComplexApplicationListItems(
                            [getDataSourceItemName(this.initialData.listNr, 'housingCompanyName', ComplexApplicationListHelper.RepeatableCode.No),
                            getDataSourceItemName(this.initialData.listNr, 'housingApartmentNumber', ComplexApplicationListHelper.RepeatableCode.No),
                            getDataSourceItemName(this.initialData.listNr, 'housingCompanyShareCount', ComplexApplicationListHelper.RepeatableCode.No),
                            getDataSourceItemName(this.initialData.listNr, 'housingSuperCertDate', ComplexApplicationListHelper.RepeatableCode.No),
                            getDataSourceItemName(this.initialData.listNr, 'housingCompanyLoans', ComplexApplicationListHelper.RepeatableCode.No)
                            ])
                    }),
                    estateData: create(x => {
                        x.addComplexApplicationListItems([
                            getDataSourceItemName(this.initialData.listNr, 'estatePropertyId', ComplexApplicationListHelper.RepeatableCode.No),
                            getDataSourceItemName(this.initialData.listNr, 'estateRegisterUnit', ComplexApplicationListHelper.RepeatableCode.No)])
                    }),
                    estateItems: estateItemsService,
                    currentPropertyType: x.propertyType
                }
            })
        }
    }

    export class Model {
        isReadonly: boolean
        c: ComplexListWithCustomerComponentNs.InitialData
        estateData: ApplicationEditorComponentNs.InitialData
        estateItems: EstateItemsService
        housingCompanyData: ApplicationEditorComponentNs.InitialData
        currentPropertyType: string
        headerData: PageHeaderComponentNs.InitialData
    }

    export interface LocalInitialData {
        applicationNr: string
        listNr: string
    }

    export interface InitialData extends LocalInitialData, ComponentHostNs.ComponentHostInitialData {
    }

    export class MortgageApplicationCollateralEditComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationCollateralEditController;
            this.template = `<div ng-if="$ctrl.m">
    <page-header initial-data="$ctrl.m.headerData" title-text="'Edit collateral'"></page-header>

    <complex-list-with-customer initial-data="$ctrl.m.c" ></complex-list-with-customer>

    <div class="row pt-2" ng-if="$ctrl.m.currentPropertyType === 'estate'">
        <div class="col-sm-5">
            <div class="editblock">
                <application-editor initial-data="$ctrl.m.estateData"></application-editor>
                <div>
                    <h3>Estate mortgage deeds</h3>
                    <hr class="hr-section" />
                    <button class="n-direct-btn n-green-btn" ng-click="$ctrl.m.estateItems.addRow($event)" ng-if="$ctrl.m.estateItems.isEditAllowed">Add</button>
                </div>
                <hr class="hr-section dotted" />

                <div class="row" ng-repeat="c in $ctrl.m.estateItems.rows">
                    <div class="col-sm-8">
                        <div>
                            <application-editor initial-data="c.d"></application-editor>
                        </div>
                    </div>
                    <div class="col-sm-4 text-right">
                        <button ng-if="$ctrl.m.estateItems.isEditAllowed" class="n-icon-btn n-red-btn" ng-click="$ctrl.m.estateItems.deleteRow(c.uid, $event)"><span class="glyphicon glyphicon-minus"></span></button>
                    </div>
                    <div class="clearfix"></div>
                    <hr class="hr-section dotted">
                </div>
            </div>
        </div>
    </div>

    <div class="row pt-2" ng-if="$ctrl.m.currentPropertyType === 'housingCompany'">
        <div class="col-sm-5"><div class="editblock"><application-editor initial-data="$ctrl.m.housingCompanyData"></application-editor></div></div>
    </div>
</div>`
        }
    }

    export class EstateItemsService {
        constructor(
            public isEditAllowed: boolean,
            private apiClient: NTechPreCreditApi.ApiClient,
            private $q: ng.IQService,
            private applicationNr: string,
            private estateDeedsDataSourceItemName: string,
            private ntechComponentService: NTechComponents.NTechComponentService
        ) {
        }

        public rows: { uid: string, d: ApplicationEditorComponentNs.InitialData, viewDetailsUrl: string }[] = []

        addRow(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            let newItem: EstateDeedItemModel = {
                uid: NTechComponents.generateUniqueId(10),
                deedAmount: null,
                deedNr: null
            }
            this.changeValue(currentValues => {
                currentValues.push(newItem)
                return currentValues
            })
        }

        deleteRow(uid: string, evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.changeValue(currentValues => {
                let newValues: EstateDeedItemModel[] = []
                for (let v of currentValues) {
                    if (v.uid !== uid) {
                        newValues.push(v)
                    }
                }
                return newValues
            })
        }

        public changeValue(transform: (estateDeeds: EstateDeedItemModel[]) => EstateDeedItemModel[]): ng.IPromise<NTechPreCreditApi.SetApplicationEditItemDataResponse> {
            return reloadCollateralEstateData(this.applicationNr, this.apiClient, this.$q).then(x => {
                if (!x.estateDeeds) {
                    x.estateDeeds = []
                }
                let newValues = transform(x.estateDeeds)
                let values: string[] = []
                for (let v of newValues) {
                    values.push(JSON.stringify(v))
                }
                let value = JSON.stringify(values)
                return this.apiClient.setApplicationEditItemData(this.applicationNr, 'ComplexApplicationList', this.estateDeedsDataSourceItemName, value, false).then(x => {
                    this.ntechComponentService.emitNTechEvent(ReloadCollateralEventName, this.applicationNr)
                    return x
                })
            })
        }
    }

    export class VirtualEstateItemsDataSourceService implements ApplicationDataSourceHelper.IApplicationDataSourceService {
        constructor(private $q: angular.IQService, private estateDeed: EstateDeedItemModel, private formatNrForEdit: (v: number) => string, private parseNrForStorage: (v: string) => number, private estateItemsService: EstateItemsService) {
        }

        getIncludedItems(): { dataSourceName: string; itemName: string; forceReadonly: boolean, isNavigationEditOrViewPossible: boolean }[] {
            return [{
                dataSourceName: 'VirtualEstateItem',
                itemName: 'deedAmount',
                forceReadonly: false,
                isNavigationEditOrViewPossible: false
            }, {
                dataSourceName: 'VirtualEstateItem',
                itemName: 'deedNr',
                forceReadonly: false,
                isNavigationEditOrViewPossible: false
            }]
        }

        loadItems(): angular.IPromise<NTechPreCreditApi.FetchApplicationDataSourceItemsResponse> {
            let r: NTechPreCreditApi.FetchApplicationDataSourceItemsResponse = {
                Results: [{
                    ChangedNames: [],
                    DataSourceName: 'VirtualEstateItem',
                    MissingNames: [],
                    Items: [
                        {
                            Name: 'deedAmount',
                            Value: this.formatNrForEdit(this.estateDeed.deedAmount),
                            EditorModel: {
                                DataSourceName: 'VirtualEstateItem',
                                DataType: 'positiveDecimal',
                                EditorType: 'text',
                                ItemName: 'deedAmount',
                                LabelText: 'Deed amount',
                                DropdownRawDisplayTexts: null,
                                DropdownRawOptions: null,
                                IsRemovable: false,
                                IsRequired: true
                            }
                        },
                        {
                            Name: 'deedNr',
                            Value: this.estateDeed.deedNr,
                            EditorModel: {
                                DataSourceName: 'VirtualEstateItem',
                                DataType: 'string',
                                EditorType: 'text',
                                ItemName: 'deedNr',
                                LabelText: 'Deed nr',
                                DropdownRawDisplayTexts: null,
                                DropdownRawOptions: null,
                                IsRemovable: false,
                                IsRequired: true
                            }
                        }
                    ]
                }]
            }

            let p = this.$q.defer<NTechPreCreditApi.FetchApplicationDataSourceItemsResponse>()
            p.resolve(r)
            return p.promise
        }

        saveItems(edits: { dataSourceName: string; itemName: string; fromValue: string; toValue: string }[]): angular.IPromise<NTechPreCreditApi.SetApplicationEditItemDataResponse[]> {
            return this.estateItemsService.changeValue(currentValues => {
                let newValues: EstateDeedItemModel[] = []
                for (let v of currentValues) {
                    if (v.uid === this.estateDeed.uid && edits) {
                        for (let e of edits) {
                            if (e.itemName === 'deedAmount') {
                                v.deedAmount = this.parseNrForStorage(e.toValue)
                            } else if (e.itemName === 'deedNr') {
                                v.deedNr = e.toValue
                            }
                        }
                    }
                    newValues.push(v)
                }
                return newValues
            }).then(x => [x])
        }
    }

    export interface EstateDeedItemModel {
        uid: string
        deedNr: string
        deedAmount: number
    }
}

angular.module('ntech.components').component('mortgageApplicationCollateralEdit', new MortgageApplicationCollateralEditComponentNs.MortgageApplicationCollateralEditComponent())