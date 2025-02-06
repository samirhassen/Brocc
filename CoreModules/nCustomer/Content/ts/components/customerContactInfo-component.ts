namespace CustomerContactInfoComponentNs {
    export class CustomerContactInfoController extends NTechComponents.NTechComponentControllerBase {
        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        initialData: InitialData

        m: Model

        componentName(): string {
            return 'customerContactInfo'
        }

        onBack(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, this.apiClient, this.$q)
        }

        private formatDate(d: Date): string {
            if (d) {
                return moment(d).format('YYYY-MM-DD')
            } else {
                return null
            }
        }

        onChanges() {
            let includeSensitive = false
            if (this.m && this.m.showSensitive) {
                includeSensitive = true
            }
            this.m = null
            this.apiClient.fetchCustomerContactInfo(this.initialData.customerId, includeSensitive, true).then(result => {
                let items: ItemViewModel[] = []

                let includeSensitive = result.includeSensitive
                let isSensitiveItem = (name: string) => result.sensitiveItems.indexOf(name) >= 0

                let addItem = (name: string, displayLabelText: string, value: string, isEditable: boolean) => {
                    let i = ItemViewModel.createEditableItemWithoutValue(name, displayLabelText)
                    if (includeSensitive || !isSensitiveItem(name)) {
                        i.value = value
                        i.hasValue = true
                    }
                    i.isEditable = isEditable
                    items.push(i)
                }

                let isCompany = result.isCompany === 'true'

                let addSeparator = () => items.push(ItemViewModel.createSeparator())

                if (isCompany) {
                    addItem('companyName', 'Company name', result.companyName, true)
                    addItem('orgnr', 'Orgnr', result.orgnr, false)
                } else {
                    addItem('firstName', 'First name', result.firstName, true)
                    addItem('lastName', 'Last name', result.lastName, true)
                    addItem('birthDate', 'Birthdate', this.formatDate(result.birthDate), true)
                    addItem('civicRegNr', 'Civic reg nr', result.civicRegNr, false)
                }

                addSeparator()
                addItem('addressStreet', 'Street', result.addressStreet, true)
                addItem('addressZipcode', 'Zipcode', result.addressZipcode, true)
                addItem('addressCity', 'City', result.addressCity, true)
                addItem('addressCountry', 'Country', result.addressCountry, true)
                addSeparator()
                addItem('email', 'Email', result.email, true)
                addItem('phone', 'Phone', result.phone, true)

                this.m = {
                    items: items,
                    showSensitive: includeSensitive,
                    editCustomerContactInfoValueInitialData: null
                }
                if (initialData && initialData.testFunctions) {
                    let td: ComponentHostNs.TestFunctionsModel = initialData.testFunctions
                    td.addFunctionCall(td.generateUniqueScopeName(), 'Wipe name, address and contact info', () => {
                        this.apiClient.wipeCustomerContactInfo([this.initialData.customerId]).then(x => {
                            document.location.reload()
                        })
                    })
                }
            })
        }

        showSensitive(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (this.m && !this.m.showSensitive) {
                this.m.showSensitive = true
                this.onChanges()
            }
        }

        editItem(i: ItemViewModel, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!(i && i.isEditable)) {
                return
            }
            let closeEdit = (evt: Event) => {
                if (evt) {
                    evt.preventDefault()
                }
                this.onChanges()
            }
            this.m.editCustomerContactInfoValueInitialData = {
                onClose: closeEdit,
                customerId: this.initialData.customerId,
                itemName: i.name
            }
        }

        getDisplayLabelText(i: ItemViewModel) {
            //TODO: Get from translation and remove from view model
            return i.displayLabelText
        }
    }

    export class CustomerContactInfoComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;
        public transclude: boolean;

        constructor() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = CustomerContactInfoController;
            this.templateUrl = 'customer-contact-info.html';
        }
    }

    export class ItemViewModel {
        name: string
        displayLabelText: string
        hasValue: boolean
        value: string
        isEditable: boolean
        isSeparator: boolean

        static createEditableItemWithoutValue(name: string, displayLabelText: string): ItemViewModel {
            let i: ItemViewModel = {
                name: name,
                displayLabelText: displayLabelText,
                isEditable: true,
                isSeparator: false,
                hasValue: false,
                value: null
            }
            return i
        }

        static createSeparator(): ItemViewModel {
            let i: ItemViewModel = {
                isSeparator: true,
                name: null, displayLabelText: null, hasValue: null, isEditable: null, value: null
            }
            return i
        }
    }

    export class InitialData {
        customerId: number
        backUrl: string
        testFunctions?: ComponentHostNs.TestFunctionsModel
    }

    export class Model {
        showSensitive: boolean
        items: ItemViewModel[]
        editCustomerContactInfoValueInitialData: EditCustomerContactInfoValueComponentNs.InitialData
    }
}

angular.module('ntech.components').component('customerContactInfo', new CustomerContactInfoComponentNs.CustomerContactInfoComponent())