namespace AddCustomerComponentNs {
    export class AddCustomerController extends NTechComponents.NTechComponentControllerBase {
        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        initialData: InitialData

        m: Model

        componentName(): string {
            return 'addCustomer'
        }

        private getArrayItemValue(items: Array<NTechCustomerApi.CustomerItem>, name: string): string {
            for (let i of items) {
                if (i.name == name) {
                    return i.value
                }
            }
            return null
        }

        onBack(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, this.apiClient, this.$q)
        }

        isValidCivicRegNr(value) {
            if (ntech.forms.isNullOrWhitespace(value))
                return true;
            if (ntechClientCountry == 'SE') {
                return ntech.se.isValidCivicNr(value)
            } else if (ntechClientCountry == 'FI') {
                return ntech.fi.isValidCivicNr(value)
            } else {
                //So they can at least get the data in
                return true
            }
        }

        saveNewCustomer(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.m.FirstName)
                return

            let proppertyitems: NTechCustomerApi.PropertyModel[] = null

            proppertyitems = [{
                Value: this.m.FirstName, Name: "firstName", ForceUpdate: true
            }]
            proppertyitems.push({
                Value: this.m.City, Name: "addressCity", ForceUpdate: true
            })
            proppertyitems.push({
                Value: this.m.Country, Name: "addressCountry", ForceUpdate: true
            })
            proppertyitems.push({
                Value: this.m.Email, Name: "email", ForceUpdate: true
            })
            proppertyitems.push({
                Value: this.m.LastName, Name: "lastName", ForceUpdate: true
            })
            proppertyitems.push({
                Value: this.m.Phone, Name: "phone", ForceUpdate: true
            })
            proppertyitems.push({
                Value: this.m.Street, Name: "addressStreet", ForceUpdate: true
            })
            proppertyitems.push({
                Value: this.m.ZipCode, Name: "addressZipcode", ForceUpdate: true
            })

            this.apiClient.CreateOrUpdateCustomer(this.m.CivicNr, "CreateCustomerManually", proppertyitems).then(x => {
                this.m = this.createModel('view', y => {
                    y.CustomerInfoInitialData = {
                        backUrl: this.initialData.urlToHere,
                        customerId: x.CustomerId
                    }
                })
            })
        }

        resetSearch() { this.m.SearchCivicNr = "" }

        private createModel(mode: string, modify?: (m: Model) => void): Model {
            let m = {
                Mode: mode,
                BackUrl: this.initialData ? this.initialData.backUrl : null
            }
            if (modify) {
                modify(m)
            }

            return m
        }

        searchCivicNr(CivicNr: string) {
            this.apiClient.getCustomerIdsByCivicRegNrs(CivicNr).then(items => {
                let customerid = items.CustomerId
                this.apiClient.fetchCustomerItems(items.CustomerId, ['firstName', 'lastName', 'addressStreet', 'addressZipcode', 'addressCity', 'addressCountry', 'phone', 'email', 'civicRegNr']).then(items => {
                    if (this.getArrayItemValue(items, 'civicRegNr') == null)//Case New Customer
                    {
                        this.apiClient.parseCivicRegNr(CivicNr).then(birthDataItems => {
                            this.m = this.createModel('new', x => {
                                x.SearchCivicNr = CivicNr
                                x.CivicNr = CivicNr
                                x.BirthDate = moment(birthDataItems.BirthDate).format('YYYY-MM-DD')
                            })
                        }
                        )
                    }
                    else {
                        this.m = this.createModel('view', x => {
                            x.CustomerInfoInitialData = {
                                backUrl: this.initialData.urlToHere,
                                customerId: customerid
                            }, x.SearchCivicNr = CivicNr, x.CivicNr = CivicNr
                        })
                    }
                })
            })
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            this.m = this.createModel('search')
        }
    }

    export class AddCustomerComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;
        public transclude: boolean;

        constructor() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = AddCustomerController;
            this.templateUrl = 'add-customer.html';
        }
    }

    export interface InitialData {
        urlToHere: string;
        backUrl: string
    }

    export class Model {
        Mode: string
        CivicNr?: string
        FirstName?: string
        LastName?: string
        Street?: string
        ZipCode?: string
        City?: string
        Country?: string
        Email?: string
        Phone?: string
        CustomerInfoInitialData?: CustomerInfoComponentNs.InitialData
        BackUrl: string
        BirthDate?: string
        SearchCivicNr?: string
    }
}

angular.module('ntech.components').component('addCustomer', new AddCustomerComponentNs.AddCustomerComponent())