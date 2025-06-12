namespace ApplicationDataSourceHelper {
    export const MissingItemReplacementValue: string = 'e0d32aa3-6d36-4f07-9b66-2cc43087483c'

    export class ApplicationDataSourceService implements IApplicationDataSourceService {
        constructor(
            public applicationNr: string,
            private apiClient: NTechPreCreditApi.ApiClient,
            private $q: ng.IQService,
            private afterSave: (changes: NTechPreCreditApi.SetApplicationEditItemDataResponse[]) => void,
            private afterDataLoaded: (data: NTechPreCreditApi.FetchApplicationDataSourceItemsResponse) => void) {
        }
        public items: { dataSourceName: string, itemName: string, forceReadonly: boolean, isNavigationEditOrViewPossible: boolean }[] = []

        getIncludedItems() {
            return this.items
        }

        addDataSourceItem(dataSourceName: string, itemName: string, forceReadonly: boolean, isNavigationEditOrViewPossible: boolean) {
            this.items.push({ dataSourceName: dataSourceName, itemName: itemName, forceReadonly: forceReadonly, isNavigationEditOrViewPossible: isNavigationEditOrViewPossible })
        }

        addDataSourceItems(dataSourceName: string, itemNames: string[], forceReadonly: boolean, isNavigationEditOrViewPossible: boolean) {
            if (!itemNames) {
                return
            }
            for (let n of itemNames) {
                this.addDataSourceItem(dataSourceName, n, forceReadonly, isNavigationEditOrViewPossible)
            }
        }

        addComplexApplicationListItem(itemName: string, forceReadonly?: boolean) {
            this.addComplexApplicationListItems([itemName], forceReadonly)
        }

        addComplexApplicationListItems(itemNames: string[], forceReadonly?: boolean) {
            this.addDataSourceItems('ComplexApplicationList', itemNames, forceReadonly === true, true)
        }

        loadItems(): ng.IPromise<NTechPreCreditApi.FetchApplicationDataSourceItemsResponse> {
            let requests: NTechPreCreditApi.IStringDictionary<NTechPreCreditApi.FetchApplicationDataSourceRequestItem> = {}
            for (let i of this.items) {
                let r = requests[i.dataSourceName]
                if (!r) {
                    r = {
                        DataSourceName: i.dataSourceName,
                        ErrorIfMissing: false,
                        IncludeEditorModel: true,
                        IncludeIsChanged: true,
                        ReplaceIfMissing: true,
                        MissingItemReplacementValue: MissingItemReplacementValue,
                        Names: []
                    }
                    requests[i.dataSourceName] = r
                }
                r.Names.push(i.itemName)
            }
            let requestsArray: NTechPreCreditApi.FetchApplicationDataSourceRequestItem[] = []
            for (let k of Object.keys(requests)) {
                requestsArray.push(requests[k])
            }

            let p = this.apiClient.fetchApplicationDataSourceItems(this.applicationNr, requestsArray)
            if (!this.afterDataLoaded) {
                return p
            } else {
                return p.then(x => {
                    this.afterDataLoaded(x)
                    return x
                })
            }
        }

        saveItems(edits: { dataSourceName: string, itemName: string, fromValue: string, toValue: string }[]): ng.IPromise<NTechPreCreditApi.SetApplicationEditItemDataResponse[]> {
            let perDataSourceEdits: NTechPreCreditApi.IStringDictionary<{ dataSourceName: string, itemName: string, fromValue: string, toValue: string }[]> = {}
            for (let edit of edits) {
                if (!perDataSourceEdits[edit.dataSourceName]) {
                    perDataSourceEdits[edit.dataSourceName] = []
                }
                perDataSourceEdits[edit.dataSourceName].push(edit)
            }

            let promises: ng.IPromise<NTechPreCreditApi.SetApplicationEditItemDataResponse>[] = []

            for (let dataSourceName of Object.keys(perDataSourceEdits)) {
                for (let editItem of perDataSourceEdits[dataSourceName]) {
                    let isDelete = !editItem.toValue //Could possibly delete false boolean? make more rigourous?
                    promises.push(this.apiClient.setApplicationEditItemData(
                        this.applicationNr,
                        dataSourceName,
                        editItem.itemName,
                        isDelete ? null : editItem.toValue,
                        isDelete))
                }
            }
            let result = this.$q.all(promises)
            if (this.afterSave) {
                return result.then(x => {
                    this.afterSave(x)
                    return x
                })
            } else {
                return result
            }
        }
    }

    export interface IApplicationDataSourceService {
        getIncludedItems(): { dataSourceName: string, itemName: string, forceReadonly: boolean, isNavigationEditOrViewPossible: boolean }[]
        loadItems(): ng.IPromise<NTechPreCreditApi.FetchApplicationDataSourceItemsResponse>
        saveItems(edits: { dataSourceName: string, itemName: string, fromValue: string, toValue: string }[]): ng.IPromise<NTechPreCreditApi.SetApplicationEditItemDataResponse[]>
    }
}