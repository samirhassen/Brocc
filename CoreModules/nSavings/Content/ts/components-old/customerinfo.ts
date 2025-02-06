class InitialCustomerInfo {
    public firstName: string
    public birthDate: string
    public customerId: number
    public customerCardUrl: string
    public customerFatcaCrsUrl: string
    public customerPepKycUrl: string;
    public customerKycQuestionsUrl: string; 
}
class ContactInfo {
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

class CustomerItem {
    public name: string
    public value: string
}

class CustomerInfoController  {
    public customer: InitialCustomerInfo;
    public fetchcustomeritems: (customerId: number, names: Array<string>, onsuccess: (items: Array<CustomerItem>) => void, onerror: (msg: string) => void) => void;
    public civicRegNr: string;
    public contactInfo: ContactInfo

    constructor() {

    }

    toggleContactInfo(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }
        if (!this.contactInfo) {
            this.fetchcustomeritems(this.customer.customerId, ['firstName', 'lastName', 'addressStreet', 'addressZipcode', 'addressCity', 'addressCountry', 'phone', 'email' ], (items) => {
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
                
            }, (msg) => {
                toastr.warning(msg)
            })
        } else {
            this.contactInfo.isOpen = !this.contactInfo.isOpen
        }
    }

    unlockCivicRegNr(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }
        this.fetchcustomeritems(this.customer.customerId, ['civicRegNr'], (items) => {
            this.civicRegNr = this.getArrayItemValue(items, 'civicRegNr')
        }, (msg) => {
            toastr.warning(msg)
        })
    }

    formatmissing(i: any) {
        if (!i) {
            return '-'
        } else {
            return i
        }
    }

    private getArrayItemValue(items: Array<CustomerItem>, name: string): string {
        for (let i of items) {
            if (i.name == name) {
                return i.value
            }
        }
        return null
    }
}

class CustomerInfoComponent implements ng.IComponentOptions {
    public bindings: any;
    public controller: any;
    public templateUrl: string;

    constructor() {
        this.bindings = {
            customer: '<',
            fetchcustomeritems: '<'
        };
        this.controller = CustomerInfoController;
        this.templateUrl = 'customerinfo.html'; //In Shared/Component_CustomerInfo.cshtml
    }
}

angular.module('ntech.components').component('customerinfo', new CustomerInfoComponent())