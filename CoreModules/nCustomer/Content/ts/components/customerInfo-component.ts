namespace CustomerInfoComponentNs {
    export class ContactInfo {
        public isOpen: boolean
        public firstName: string
        public lastName: string
        public addressStreet: string
        public addressZipcode: string
        public addressCity: string
        public addressCountry: string
        public phone: string
        public email: string
    }

    export class InitialCustomerData {
        public firstName: string
        public birthDate: string
        public customerId: number
        public customerCardUrl: string
    }

    export class CustomerInfoController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData

        public customer: InitialCustomerData;
        public civicRegNr: string;
        public contactInfo: ContactInfo

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {

            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'customerInfo'
        }

        onChanges() {
            this.contactInfo = null;
            this.civicRegNr = null;
            this.customer = null;

            if (this.initialData) {
                this.apiClient.fetchCustomerItems(this.initialData.customerId, ['firstName', 'birthDate']).then(items => {
                    this.customer = {
                        firstName: this.getArrayItemValue(items, 'firstName'),
                        birthDate: this.getArrayItemValue(items, 'birthDate'),
                        customerId: this.initialData.customerId,
                        customerCardUrl: '/Customer/CustomerCard?customerId=' + this.initialData.customerId + (this.initialData.backUrl ? ('&backUrl=' + encodeURIComponent(this.initialData.backUrl)) : '')
                    }
                })
            }
        }

        toggleContactInfo(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.contactInfo) {
                this.apiClient.fetchCustomerItems(this.customer.customerId, ['firstName', 'lastName', 'addressStreet', 'addressZipcode', 'addressCity', 'addressCountry', 'phone', 'email']).then(items => {
                    this.contactInfo = {
                        isOpen: true,
                        firstName: this.getArrayItemValue(items, 'firstName'),
                        lastName: this.getArrayItemValue(items, 'lastName'),
                        addressStreet: this.getArrayItemValue(items, 'addressStreet'),
                        addressCity: this.getArrayItemValue(items, 'addressCity'),
                        addressZipcode: this.getArrayItemValue(items, 'addressZipcode'),
                        addressCountry: this.getArrayItemValue(items, 'addressCountry'),
                        phone: this.getArrayItemValue(items, 'phone'),
                        email: this.getArrayItemValue(items, 'email')
                    }
                })
            } else {
                this.contactInfo.isOpen = !this.contactInfo.isOpen
            }
        }

        formatPhoneNr(nr: string) {
            if (ntech && ntech.libphonenumber && ntechClientCountry) {
                if (!nr) {
                    return nr
                }
                var p = ntech.libphonenumber.parsePhoneNr(nr, ntechClientCountry)
                if (p.isValid) {
                    return p.validNumber.standardDialingNumber
                } else {
                    return nr
                }
            } else {
                return nr
            }
        }

        unlockCivicRegNr(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.fetchCustomerItems(this.customer.customerId, ['civicRegNr']).then(items => {
                this.civicRegNr = this.getArrayItemValue(items, 'civicRegNr')
            })
        }

        formatmissing(i: any) {
            if (!i) {
                return '-'
            } else {
                return i
            }
        }

        private getArrayItemValue(items: Array<NTechCustomerApi.CustomerItem>, name: string): string {
            for (let i of items) {
                if (i.name == name) {
                    return i.value
                }
            }
            return null
        }
    }

    export class ApplicationCustomerInfoComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CustomerInfoController;
            this.templateUrl = 'customer-info.html';
        }
    }

    export class InitialData {
        customerId: number
        backUrl: string
    }
}

angular.module('ntech.components').component('customerInfo', new CustomerInfoComponentNs.ApplicationCustomerInfoComponent())