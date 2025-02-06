namespace LegacyCustomerCardComponentNs {
    export class LegacyCustomerCardController extends NTechComponents.NTechComponentControllerBase {
        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);

            this.apiClient = new NTechCustomerApi.ApiClient(msg => toastr.error(msg), $http, $q)
        }
        initialData: InitialData
        apiClient: NTechCustomerApi.ApiClient

        m: ICustomerCardScope
        prettyPrinter: CustomerCardPrettyPrinterNs.CustomerCardPrettyPrinter

        componentName(): string {
            return 'legacyCustomerCard'
        }

        onBack(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, this.apiClient, this.$q)
        }

        onChanges() {
            this.m = null
            this.prettyPrinter = null
            if (!this.initialData) {
                return
            }

            this.apiClient.fetchLegacyCustomerCardUiData(this.initialData.customerId, this.initialData.backUrl).then(r => {
                this.prettyPrinter = new CustomerCardPrettyPrinterNs.CustomerCardPrettyPrinter()

                this.m = {
                    app: {
                        backUrl: this.initialData.backUrl,
                        customerCard: {
                            customerId: this.initialData.customerId,
                            items: r.customerCardItems
                        },
                        customerId: this.initialData.customerId
                    },
                    editMode: false,
                    latestSavedData: null
                }

                this.m.app.customerCard.items.forEach((item, index) => {
                    item.FriendlyName = this.prettyPrinter.getFriendlyName(item.Group, item.Name)
                    if (!item.Locked) {
                        item.FriendlyValue = this.prettyPrinter.getFriendlyValue(item.Group, item.Name, item.Value)
                    }
                });

                this.updateLatestSaved();
            })
        }

        itemByName(name: string) {
            if (!this.m) {
                return null
            }
            for (let i of this.m.app.customerCard.items) {
                if (i.Name === name) {
                    return i
                }
            }
            return null
        }

        private formatPhoneNr(nr: any) {
            if (!nr) {
                return nr
            }
            var p = ntech.libphonenumber.parsePhoneNr(nr, ntechClientCountry)
            if (p.isValid) {
                return p.validNumber.standardDialingNumber
            } else {
                return nr
            }
        }

        private restoreLatestSaved() {
            this.m.app = JSON.parse(JSON.stringify(this.m.latestSavedData));
        }

        private shouldBeSaved(item: CustomerCardNs.ICustomerPropertyEditModel) {
            if (item.Locked || item.Group === 'civicRegNr' || item.Group === 'pep' || item.Group === 'taxResidency' || item.Group === 'amlCft') {
                return false
            }
            if (item.Name == 'includeInFatcaExport' || item.Name == 'tin' || item.Name == 'taxcountries') {
                return false
            }

            return true;
        }

        private findSavedItem(name: string): CustomerCardNs.ICustomerPropertyEditModel {
            var foundItem = null
            angular.forEach(this.m.latestSavedData.customerCard.items, (newItem) => {
                if (newItem.Name === name) {
                    foundItem = newItem
                }
            })
            return foundItem
        }

        updateLatestSaved() {
            this.m.latestSavedData = JSON.parse(JSON.stringify(this.m.app));
        }

        formatValue(item: ICustomerPropertyEditModel) {
            if (!item) {
                return item
            } else if (item.Name === 'phone') {
                return this.formatPhoneNr(item.Value)
            } else {
                return item.Value
            }
        }

        currentLanguage() {
            return "sv";
        }

        toggleEditMode() {
            if (this.m.editMode) {
                this.restoreLatestSaved();
            }
            this.m.editMode = !this.m.editMode;
        }

        save() {
            let itemsToSave: CustomerCardNs.ICustomerPropertyEditModel[] = angular.copy(this.m.app.customerCard.items).filter(this.shouldBeSaved);

            var isInvalid = false
            var invalidItems = ""
            angular.forEach(itemsToSave, (newItem) => {
                if (newItem.Locked === false) {
                    if (ntech.forms.isNullOrWhitespace(newItem.Value)) {
                        invalidItems = invalidItems + " " + newItem.Name
                        isInvalid = true
                    } else if (newItem.UiType == "Date" && !this.isValidDate(newItem.Value)) {
                        invalidItems = invalidItems + " " + newItem.Name
                        isInvalid = true
                    } else if (newItem.UiType == "Email" && newItem.Value.indexOf('@') < 0) {
                        invalidItems = invalidItems + " " + newItem.Name
                        isInvalid = true
                    } else if (newItem.UiType == "Boolean" && newItem.Value !== true && newItem.Value !== false && newItem.Value !== "true" && newItem.Value !== "false") {
                        invalidItems = invalidItems + " " + newItem.Name
                        isInvalid = true
                    }
                }
            })

            if (isInvalid) {
                toastr.warning("Cannot save because the following are invalid: " + invalidItems)
                return
            }

            //Filter out items that have not changed
            itemsToSave = itemsToSave.filter((newItem) => {
                var oldItem = this.findSavedItem(newItem.Name)
                if (oldItem && angular.toJson(oldItem.Value) !== angular.toJson(newItem.Value)) {
                    return true
                }
                return false
            })

            if (itemsToSave.length == 0) {
                toastr.warning("Nothing has been changed")
                return
            }

            for (let i of itemsToSave) {
                i.CustomerId = this.m.app.customerId
            }

            this.apiClient.updateCustomer(itemsToSave, true).then(result => {
                this.onChanges()
            })
        }

        unlock(sensitiveItem: CustomerCardNs.ICustomerPropertyEditModel, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.unlockSensitiveItemByName(sensitiveItem.CustomerId, sensitiveItem.Name).then(value => {
                sensitiveItem.Locked = false;
                sensitiveItem.Value = value;
            })
        }

        removeIsFlaggedForRemoval(item: CustomerCardPrettyPrinterNs.ITaxCountryItem) {
            return !(item.isFlaggedForRemoval === true)
        }
    }

    export class LegacyCustomerCardComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;
        public transclude: boolean;

        constructor() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = LegacyCustomerCardController;
            this.templateUrl = 'legacy-customer-card.html';
        }
    }

    export class InitialData {
        backUrl: string
        customerId: number
    }

    export interface ICustomerCardScope {
        editMode: boolean
        app: IAppModel
        latestSavedData: IAppModel
    }

    export interface ICustomerPropertyEditModel {
        Name: string
        Group: string
        CustomerId: number
        Value: any
        IsSensitive: boolean
        IsReadonly: boolean
        Locked: boolean
        UiType?: string
        FriendlyName?: string
        FriendlyValue?: string
    }

    export interface IAppModel {
        backUrl: string
        customerCard: IAppCustomerCardModel
        customerId: number
    }
    export interface IAppCustomerCardModel {
        items: ICustomerPropertyEditModel[]
        customerId: number
    }
}

angular.module('ntech.components').component('legacyCustomerCard', new LegacyCustomerCardComponentNs.LegacyCustomerCardComponent())