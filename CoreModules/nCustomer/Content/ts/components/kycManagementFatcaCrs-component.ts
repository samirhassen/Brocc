namespace KycManagementFatcaCrsComponentNs {
    export class KycManagementFatcaCrsController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'kycManagementFatcaCrs'
        }

        onChanges() {
            this.m = null
            if (!this.initialData) {
                return
            }
            this.refresh()
        }

        onBack(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, this.apiClient, this.$q)
        }

        refresh() {
            this.apiClient.fetchCustomerItemsDict(this.initialData.customerId, ['includeInFatcaExport', 'taxcountries', 'citizencountries']).then(c => {
                this.apiClient.kycManagementFetchLatestCustomerQuestionsSet(this.initialData.customerId).then(q => {
                    this.m = {
                        includeInFatcaExport: this.getValue(c, 'includeInFatcaExport', 'unknown'),
                        latestCustomerQuestionsSet: this.filterQuestions(q),
                        customerInfoInitialData: {
                            customerId: this.initialData.customerId,
                            backUrl: this.initialData.backUrl
                        },
                        fatcaEditModel: null,
                        isTinUnlocked: false,
                        tin: null,
                        taxCountries: this.parseCountriesFromString(this.getValue(c, 'taxcountries', '[]'), true),
                        taxCountriesHistoryItems: null,
                        taxCountriesEdit: null,
                        citizenCountries: this.parseCountriesFromString(this.getValue(c, 'citizencountries', '[]'), false),
                        citizenCountriesHistoryItems: null,
                        citizenCountriesEdit: null,
                        customerRelations: null
                    }
                    this.apiClient.fetchCustomerRelations(this.initialData.customerId).then(relationResult => { //Load async to make the ui feel a bit snappier
                        if (this.m) {
                            this.m.customerRelations = relationResult.CustomerRelations
                        }
                    })
                })
            })
        }

        private getRelationName(r: NTechCustomerApi.CustomerRelationModel) {
            if (!r) {
                return ''
            }
            let typeTag = r.RelationType
            if (r.RelationType === 'Credit_UnsecuredLoan') {
                typeTag = 'Unsecured loan'
            } else if (r.RelationType === 'Credit_MortgageLoan') {
                typeTag = 'Mortgage loan'
            } else if (r.RelationType === 'SavingsAccount_StandardAccount') {
                typeTag = 'Savings account'
            }

            return `${typeTag} ${r.RelationId}`
        }

        private parseCountriesFromString(c: string, isTaxCountries: boolean): string[] {
            if (!c) {
                return []
            }
            if (isTaxCountries) {
                let cs: TaxCountryModel[] = JSON.parse(c)
                return _.pluck(cs, 'countryIsoCode')
            } else {
                return JSON.parse(c)
            }
        }

        loadTin(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.fetchCustomerPropertiesWithGroupedEditHistory(this.initialData.customerId, ['tin']).then(c => {
                this.m.isTinUnlocked = true
                this.m.tin = this.getValue(c.CurrentValues, 'tin', '')
            })
        }

        editFatca(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.fetchCustomerPropertiesWithGroupedEditHistory(this.initialData.customerId, ['tin', 'includeInFatcaExport']).then(c => {
                this.m.fatcaEditModel = {
                    includeInFatcaExport: this.getValue(c.CurrentValues, 'includeInFatcaExport', 'unknown'),
                    tin: this.getValue(c.CurrentValues, 'tin', ''),
                    historyItems: c.HistoryItems
                }
            })
        }

        getFatcaDisplayValue(v: string) {
            if (v === 'true') {
                return 'Yes'
            } else if (v === 'false') {
                return 'No'
            } else {
                return ''
            }
        }

        cancelFatcaEdit(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.m.fatcaEditModel = null
        }

        saveFatcaEdit(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            let items: NTechCustomerApi.CustomerPropertyModel[] = null
            if (this.m.fatcaEditModel.includeInFatcaExport === 'true' || this.m.fatcaEditModel.includeInFatcaExport === 'false') {
                items = [{ Name: 'includeInFatcaExport', Value: this.m.fatcaEditModel.includeInFatcaExport, Group: 'fatca', IsSensitive: false, CustomerId: this.initialData.customerId }]
                if (this.m.fatcaEditModel.includeInFatcaExport === 'true') {
                    items.push({ Name: 'tin', Value: this.m.fatcaEditModel.tin, Group: 'fatca', IsSensitive: true, CustomerId: this.initialData.customerId })
                }
            }

            if (items == null) {
                return
            }
            this.apiClient.updateCustomer(items, true).then(x => {
                this.refresh()
            })
        }

        getCountryName(countryIsoCode: string) {
            return this.initialData.allCountryCodesAndNames[countryIsoCode]
        }

        editTaxCountries(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.fetchCustomerPropertiesWithGroupedEditHistory(this.initialData.customerId, ['taxcountries']).then(x => {
                this.m.taxCountriesHistoryItems = x.HistoryItems
                this.m.taxCountriesEdit = {
                    allCountryCodesAndNames: this.initialData.allCountryCodesAndNames,
                    countryIsoCodes: this.parseCountriesFromString(this.getValue(x.CurrentValues, 'taxcountries', '[]'), true),
                    labelText: 'Tax recidency countries',
                    onSaveEdit: newTaxCountries => {
                        let tcs: TaxCountryModel[] = []
                        for (let t of newTaxCountries) {
                            tcs.push({ countryIsoCode: t })
                        }
                        let customerProperties: NTechCustomerApi.CustomerPropertyModel[] = [{
                            CustomerId: this.initialData.customerId,
                            Group: 'taxResidency',
                            IsSensitive: false,
                            Name: 'taxcountries',
                            Value: JSON.stringify(tcs)
                        }]
                        this.apiClient.updateCustomer(customerProperties, true).then(x => {
                            this.m.taxCountriesEdit = null
                            this.m.taxCountriesHistoryItems = null
                            this.refresh()
                        })
                    },
                    onCancelEdit: () => {
                        this.m.taxCountriesEdit = null
                    },
                    historyItems: CountryListPropertyComponentNs.createEditHistoryItems(this.m.taxCountriesHistoryItems, x =>
                        this.parseCountriesFromString(this.getValue(x.Values, 'taxcountries', '[]'), true)
                    )
                }
            })
        }

        editCitizenCountries(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.fetchCustomerPropertiesWithGroupedEditHistory(this.initialData.customerId, ['citizencountries']).then(x => {
                this.m.citizenCountriesHistoryItems = x.HistoryItems
                this.m.citizenCountriesEdit = {
                    allCountryCodesAndNames: this.initialData.allCountryCodesAndNames,
                    countryIsoCodes: this.parseCountriesFromString(this.getValue(x.CurrentValues, 'citizencountries', '[]'), false),
                    labelText: 'Citizen countries',
                    onSaveEdit: newCitizenCountries => {
                        let customerProperties: NTechCustomerApi.CustomerPropertyModel[] = [{
                            CustomerId: this.initialData.customerId,
                            Group: 'taxResidency',
                            IsSensitive: false,
                            Name: 'citizencountries',
                            Value: JSON.stringify(newCitizenCountries)
                        }]
                        this.apiClient.updateCustomer(customerProperties, true).then(x => {
                            this.m.citizenCountriesEdit = null
                            this.m.citizenCountriesHistoryItems = null
                            this.refresh()
                        })
                    },
                    onCancelEdit: () => {
                        this.m.citizenCountriesEdit = null
                    },
                    historyItems: CountryListPropertyComponentNs.createEditHistoryItems(this.m.citizenCountriesHistoryItems, x =>
                        this.parseCountriesFromString(this.getValue(x.Values, 'citizencountries', '[]'), false)
                    )
                }
            })
        }

        private getValue(items: { [index: string]: string }, n: string, defaultValue: string) {
            if (Object.keys(items).indexOf(n) < 0) {
                return defaultValue
            } else {
                var v = items[n]
                if (v === null) {
                    return defaultValue
                } else {
                    return v
                }
            }
        }

        private filterQuestions(q: NTechCustomerApi.CustomerQuestionsSet): NTechCustomerApi.CustomerQuestionsSet {
            if (!q || !q.Items || q.Items.length == 0) {
                return null
            }
            let items = _.filter(q.Items, x => x.QuestionCode !== 'ispep' && x.QuestionCode !== 'pep')
            if (items.length == 0) {
                return null
            }
            return {
                AnswerDate: q.AnswerDate,
                CustomerId: q.CustomerId,
                Source: q.Source,
                Items: items
            }
        }
    }

    class TaxCountryModel {
        countryIsoCode: string
        taxNumber?: string //Legacy model
    }

    export class KycManagementFatcaCrsComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = KycManagementFatcaCrsController;
            this.templateUrl = 'kyc-management-fatca-crs.html';
        }
    }

    export class InitialData {
        backUrl: string
        customerId: number
        allCountryCodesAndNames: { [index: string]: string }
    }

    export class Model {
        includeInFatcaExport: string
        isTinUnlocked: boolean
        tin: string
        fatcaEditModel: FatcaEditModel
        latestCustomerQuestionsSet: NTechCustomerApi.CustomerQuestionsSet
        customerInfoInitialData: CustomerInfoComponentNs.InitialData
        taxCountries: string[]
        taxCountriesEdit: CountryListPropertyComponentNs.InitialData
        taxCountriesHistoryItems: NTechCustomerApi.HistoryItem[]
        citizenCountries: string[]
        citizenCountriesEdit: CountryListPropertyComponentNs.InitialData
        citizenCountriesHistoryItems: NTechCustomerApi.HistoryItem[]
        customerRelations?: NTechCustomerApi.CustomerRelationModel[]
    }

    export class FatcaEditModel {
        includeInFatcaExport: string
        tin: string
        historyItems: NTechCustomerApi.HistoryItem[]
    }
}

angular.module('ntech.components').component('kycManagementFatcaCrs', new KycManagementFatcaCrsComponentNs.KycManagementFatcaCrsComponent())