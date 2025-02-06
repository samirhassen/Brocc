class InitialCustomerInfo {
    public firstName: string
    public companyName: string
    public birthDate: string
    public customerId: number
    public isCompany: string
    public customerCardUrl: string
    public pepKycCustomerUrl: string
    public customerFatcaCrsUrl: string
}

class ContactInfo {
    public isOpen: boolean
    public firstName: string
    public lastName: string
    public companyName: string
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
    public fetchcustomeritems: (customerId: number, names: Array<string>, onsuccess: (items: NTechCreditApi.IStringStringDictionary) => void, onerror: (msg: string) => void) => void;
    public civicRegNrOrOrgnr: string;
    public amlRiskClass: string;
    public contactInfo: ContactInfo
    public initialData: CustomerInfoComponentNs.InitialData
    public isReady: boolean

    constructor() {

    }

    $onChanges(changesObj: any) {
        if (this.isReady) {
            return
        }

        if (!this.initialData && this.customer) {
            //Old invocation style
            this.isReady = true
            return
        } else if (!this.initialData) {
            return
        }

        //New invocation style
        let i = this.initialData

        i.apiClient.fetchCustomerCardItems(i.customerId, ['isCompany', 'firstName', 'birthDate', 'companyName', ]).then(x => {
            this.fetchcustomeritems = (cid, names, onSuccess) => {
                i.apiClient.fetchCustomerCardItems(cid, names).then(y => onSuccess(y))
            };
            this.customer = {
                firstName: x['firstName'],
                birthDate: x['birthDate'],
                companyName: x['companyName'],
                isCompany: x['isCompany'],
                customerCardUrl: null,
                customerFatcaCrsUrl: null,
                customerId: i.customerId,
                pepKycCustomerUrl: null
            }
            this.isReady = true
        })

    }

    isCompany(): boolean {
        return this.customer.isCompany === 'true'
    }

    toggleContactInfo(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }
        if (!this.contactInfo) {
            this.fetchcustomeritems(this.customer.customerId, ['firstName', 'lastName', 'addressStreet', 'addressZipcode', 'addressCity', 'addressCountry', 'phone', 'email', 'companyName'], (items) => {
                this.contactInfo = {
                    isOpen: true,
                    firstName: this.isCompany() ? null : this.getArrayItemValue(items, 'firstName'),
                    lastName: this.isCompany() ? null :this.getArrayItemValue(items, 'lastName'),
                    companyName: this.isCompany() ? this.getArrayItemValue(items, 'companyName') : null,
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

    unlockCivicRegNrOrOrgnr(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }
        let idName = this.isCompany() ? 'orgnr' : 'civicRegNr'
        this.fetchcustomeritems(this.customer.customerId, [idName], (items) => {
            this.civicRegNrOrOrgnr = this.getArrayItemValue(items, idName)
        }, (msg) => {
            toastr.warning(msg)
        })
    }

    unlockAmlRiskClass(evt: Event) {
        if (evt) {
            evt.preventDefault();
        }
        let propertyName = "amlRiskClass";
        this.fetchcustomeritems(this.customer.customerId, [propertyName], (items) => {
            let riskClass = this.getArrayItemValue(items, propertyName);
            if (riskClass) {
                this.amlRiskClass = riskClass;
            } else {
                this.amlRiskClass = "-";
            }
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

    private getArrayItemValue(items: NTechCreditApi.IStringStringDictionary, name: string): string {
        return items[name]
    }
}

class CustomerInfoComponent implements ng.IComponentOptions {
    public bindings: any;
    public controller: any;
    public templateUrl: string;
    public transclude: boolean

    constructor() {
        this.bindings = {
            customer: '<',
            fetchcustomeritems: '<',
            initialData: '<',
        };
        this.controller = CustomerInfoController;
        this.templateUrl = 'customerinfo.html'; //In Shared/Component_CustomerInfo.cshtml
        this.transclude = true;
    }
}

angular.module('ntech.components').component('customerinfo', new CustomerInfoComponent())

namespace CustomerInfoComponentNs {
    export class InitialData {
        customerId: number
        apiClient: NTechCreditApi.ApiClient
    }
}