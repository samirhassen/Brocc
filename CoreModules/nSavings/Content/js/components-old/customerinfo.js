class InitialCustomerInfo {
}
class ContactInfo {
}
class CustomerItem {
}
class CustomerInfoController {
    constructor() {
    }
    toggleContactInfo(evt) {
        if (evt) {
            evt.preventDefault();
        }
        if (!this.contactInfo) {
            this.fetchcustomeritems(this.customer.customerId, ['firstName', 'lastName', 'addressStreet', 'addressZipcode', 'addressCity', 'addressCountry', 'phone', 'email'], (items) => {
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
                };
            }, (msg) => {
                toastr.warning(msg);
            });
        }
        else {
            this.contactInfo.isOpen = !this.contactInfo.isOpen;
        }
    }
    unlockCivicRegNr(evt) {
        if (evt) {
            evt.preventDefault();
        }
        this.fetchcustomeritems(this.customer.customerId, ['civicRegNr'], (items) => {
            this.civicRegNr = this.getArrayItemValue(items, 'civicRegNr');
        }, (msg) => {
            toastr.warning(msg);
        });
    }
    formatmissing(i) {
        if (!i) {
            return '-';
        }
        else {
            return i;
        }
    }
    getArrayItemValue(items, name) {
        for (let i of items) {
            if (i.name == name) {
                return i.value;
            }
        }
        return null;
    }
}
class CustomerInfoComponent {
    constructor() {
        this.bindings = {
            customer: '<',
            fetchcustomeritems: '<'
        };
        this.controller = CustomerInfoController;
        this.templateUrl = 'customerinfo.html'; //In Shared/Component_CustomerInfo.cshtml
    }
}
angular.module('ntech.components').component('customerinfo', new CustomerInfoComponent());
