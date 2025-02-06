namespace ComplexApplicationListHelper {
    export enum RepeatableCode {
        Yes = 'r',
        No = 'u',
        All = '*'
    }

    export const DataSourceName = 'ComplexApplicationList'

    export function getDataSourceItemName(listName: string, nr: string, itemName: string, repeatableCode: RepeatableCode) {
        return `${listName}#${nr}#${repeatableCode}#${itemName}`
    }

    export function setValue(applicationNr: string, itemName: string, value: string, apiClient: NTechPreCreditApi.ApiClient): ng.IPromise<void> {
        return apiClient.setApplicationEditItemData(applicationNr, DataSourceName, itemName, value, false).then(x => {
        })
    }

    export function deleteRow(applicationNr: string, listName: string, nr: number, apiClient: NTechPreCreditApi.ApiClient): ng.IPromise<void> {
        return apiClient.setApplicationEditItemData(applicationNr, DataSourceName, getDataSourceItemName(listName, nr.toString(), '*', RepeatableCode.All), null, true).then(x => {
        })
    }

    export function parseCompoundItemName(n: string): { listName: string, itemName: string, nr: string, repeatable: string } {
        let r = /([\w]+)#([\d\*]+)#([ur\*])#([\w\*]+)/
        let m = r.exec(n)
        return {
            listName: m[1],
            itemName: m[4],
            nr: m[2],
            repeatable: m[3]
        }
    }

    export function getNrs(applicationNr: string, listName: string, apiClient: NTechPreCreditApi.ApiClient): ng.IPromise<number[]> {
        return apiClient.fetchApplicationDataSourceItems(applicationNr, [{
            DataSourceName: 'ComplexApplicationList',
            ErrorIfMissing: false,
            IncludeEditorModel: false,
            IncludeIsChanged: false,
            MissingItemReplacementValue: ApplicationDataSourceHelper.MissingItemReplacementValue,
            ReplaceIfMissing: true,
            Names: [getDataSourceItemName(listName, '*', '*', RepeatableCode.All)]
        }]).then(x => {
            let nrs = []
            let ds: NTechPreCreditApi.INumberDictionary<boolean> = {}
            for (let i of x.Results[0].Items) {
                let nr = parseCompoundItemName(i.Name).nr
                if (nr && nr != '*') {
                    let nrP = parseInt(nr)
                    if (ds[nrP] !== true) {
                        nrs.push(nrP)
                        ds[nrP] = true
                    }
                }
            }
            return nrs
        })
    }

    export function getAllCustomerIds(applicationNr: string, listNames: string[], apiClient: NTechPreCreditApi.ApiClient, startingData?: NTechPreCreditApi.INumberDictionary<string[]>): ng.IPromise<NTechPreCreditApi.INumberDictionary<string[]>> {
        let names: string[] = []
        for (let n of listNames) {
            names.push(getDataSourceItemName(n, '*', 'customerIds', RepeatableCode.Yes))
        }

        let listNamesByCustomerId: NTechPreCreditApi.INumberDictionary<string[]>
        if (startingData) {
            listNamesByCustomerId = startingData
        }
        if (!listNamesByCustomerId) {
            listNamesByCustomerId = {}
        }
        let add = (customerId: number, roleName: string) => {
            if (!listNamesByCustomerId[customerId]) {
                listNamesByCustomerId[customerId] = []
            }
            listNamesByCustomerId[customerId].push(roleName)
        }
        return apiClient.fetchApplicationDataSourceItems(applicationNr, [{
            DataSourceName: 'ComplexApplicationList',
            ErrorIfMissing: false,
            IncludeEditorModel: false,
            IncludeIsChanged: false,
            MissingItemReplacementValue: ApplicationDataSourceHelper.MissingItemReplacementValue,
            ReplaceIfMissing: false,
            Names: names
        }]).then(x => {
            for (let i of x.Results[0].Items) {
                if (i.Value) {
                    let localValues: string[] = JSON.parse(i.Value)
                    let itemNameParsed = parseCompoundItemName(i.Name)
                    for (let s of localValues) {
                        add(parseInt(s), itemNameParsed.listName)
                    }
                }
            }
            let customerIds = Object.keys(listNamesByCustomerId)
            for (let k of customerIds) {
                listNamesByCustomerId[k] = NTechLinq.distinct(listNamesByCustomerId[k])
            }
            return listNamesByCustomerId
        })
    }

    export function fetch(applicationNr: string, listName: string, apiClient: NTechPreCreditApi.ApiClient, uniqueItemNames: string[], repeatingNames?: string[]): ng.IPromise<ComplexApplicationListData> {
        return ComplexApplicationListHelper.getNrs(applicationNr, listName, apiClient).then(nrs => {
            let names = []
            if (uniqueItemNames) {
                for (let itemName of uniqueItemNames) {
                    for (let nr of nrs) {
                        names.push(getDataSourceItemName(listName, nr.toString(), itemName, RepeatableCode.No))
                    }
                }
            }
            if (repeatingNames) {
                for (let itemName of repeatingNames) {
                    for (let nr of nrs) {
                        names.push(getDataSourceItemName(listName, nr.toString(), itemName, RepeatableCode.Yes))
                    }
                }
            }
            return apiClient.fetchApplicationDataSourceItems(applicationNr, [{
                DataSourceName: 'ComplexApplicationList',
                ErrorIfMissing: false,
                IncludeEditorModel: true,
                IncludeIsChanged: false,
                MissingItemReplacementValue: ApplicationDataSourceHelper.MissingItemReplacementValue,
                ReplaceIfMissing: true,
                Names: names
            }]).then(x => {
                let r = x.Results[0]
                let d = new ComplexApplicationListData(listName)
                for (let i of r.Items) {
                    let n = parseCompoundItemName(i.Name)
                    let nr = parseInt(n.nr)
                    d.ensureNr(nr)
                    d.setEditorModel(n.itemName, i.EditorModel)
                    if (i.Value !== ApplicationDataSourceHelper.MissingItemReplacementValue) {
                        if (n.repeatable === RepeatableCode.Yes) {
                            d.setRepeatableItem(nr, n.itemName, JSON.parse(i.Value))
                        } else {
                            d.setUniqueItem(nr, n.itemName, i.Value)
                        }
                    }
                }
                return d
            })
        })
    }

    export class ComplexApplicationListData {
        constructor(public listName: string) {
        }

        private uniqueItems: NTechPreCreditApi.INumberDictionary<NTechPreCreditApi.IStringDictionary<string>>
        private repeatableItems: NTechPreCreditApi.INumberDictionary<NTechPreCreditApi.IStringDictionary<string[]>>
        private editorModelByName: NTechPreCreditApi.IStringDictionary<NTechPreCreditApi.FetchApplicationEditItemDataResponseEditModel>
        private nrs: number[]

        getNrs(): number[] {
            if (!this.nrs) {
                return []
            }

            let clonedArray = this.nrs.slice()
            clonedArray.sort()
            return clonedArray
        }

        getEditorModel(name: string) {
            if (!this.editorModelByName || !this.editorModelByName[name]) {
                return null
            }
            return this.editorModelByName[name]
        }

        public ensureNr(nr: number) {
            if (!this.nrs) {
                this.nrs = []
            }
            if (this.nrs.indexOf(nr) < 0) {
                this.nrs.push(nr)
            }
        }

        setEditorModel(name: string, editorModel: NTechPreCreditApi.FetchApplicationEditItemDataResponseEditModel) {
            if (!editorModel) {
                return
            }
            if (!this.editorModelByName) {
                this.editorModelByName = {}
            }
            this.editorModelByName[name] = editorModel
        }

        setRepeatableItem(nr: number, name: string, value: string[]) {
            if (!this.repeatableItems) {
                this.repeatableItems = {}
            }
            if (!this.repeatableItems[nr]) {
                this.repeatableItems[nr] = {}
            }
            this.ensureNr(nr)

            this.repeatableItems[nr][name] = value
        }

        setUniqueItem(nr: number, name: string, value: string) {
            if (!this.uniqueItems) {
                this.uniqueItems = {}
            }
            if (!this.uniqueItems[nr]) {
                this.uniqueItems[nr] = {}
            }
            this.ensureNr(nr)
            this.uniqueItems[nr][name] = value
        }

        getRepeatableItems(nr: number): NTechPreCreditApi.IStringDictionary<string[]> {
            if (!this.repeatableItems || !this.repeatableItems[nr]) {
                return {}
            }
            return this.repeatableItems[nr]
        }

        getUniqueItems(nr: number): NTechPreCreditApi.IStringDictionary<string> {
            if (!this.uniqueItems || !this.uniqueItems[nr]) {
                return {}
            }
            return this.uniqueItems[nr]
        }

        getOptionalUniqueValue(nr: number, name: string): string {
            if (!this.uniqueItems || !this.uniqueItems[nr]) {
                return null
            }
            return this.uniqueItems[nr][name]
        }

        getOptionalRepeatingValue(nr: number, name: string): string[] {
            if (!this.repeatableItems || !this.repeatableItems[nr]) {
                return null
            }
            return this.repeatableItems[nr][name]
        }
    }
}