namespace ApplicationCustomerInfoComponentNs {
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
        public companyName: string
    }

    class PepKycInfo {
        public isOpen: boolean
        latestScreeningDate: NTechDates.DateOnly
    }

    class CustomerItem {
        public name: string
        public value: string
    }

    export class ApplicationCustomerInfoController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData

        public customer: NTechPreCreditApi.CustomerComponentInitialData;
        public civicRegNr: string;
        public contactInfo: ContactInfo
        public pepKycInfo: PepKycInfo

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'applicationCustomerInfo'
        }

        onChanges() {
            this.contactInfo = null;
            this.civicRegNr = null;
            this.customer = null;

            if (this.initialData == null) {
                return
            }
            let c = 0
            if (this.initialData.applicantNr) c += 1
            if (this.initialData.customerIdCompoundItemName) c += 1
            if (this.initialData.customerId) c += 1
            if (c !== 1) {
                throw new Error('Exactly one of applicantNr, customerIdCompoundItemName and customerId must be set')
            }

            if (this.initialData.applicantNr) {
                this.apiClient.fetchCustomerComponentInitialData(this.initialData.applicationNr, this.initialData.applicantNr, this.initialData.backTarget).then(result => {
                    this.customer = result;
                })
            } else if (this.initialData.customerIdCompoundItemName) {
                this.apiClient.fetchCustomerComponentInitialDataByItemCompoundName(this.initialData.applicationNr, this.initialData.customerIdCompoundItemName, this.initialData.birthDateCompoundItemName, this.initialData.backTarget).then(result => {
                    this.customer = result;
                })
            } else {
                this.apiClient.fetchCustomerComponentInitialDataByCustomerId(this.initialData.customerId, this.initialData.backTarget).then(result => {
                    this.customer = result;
                })
            }
        }

        isCompany() {
            if (!this.customer) {
                return null
            }
            return this.customer.isCompany
        }

        toggleContactInfo(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.contactInfo) {
                let itemNames = ['addressStreet', 'addressZipcode', 'addressCity', 'addressCountry', 'phone', 'email']
                if (this.isCompany()) {
                    itemNames.push('companyName')
                } else {
                    itemNames.push('firstName')
                    itemNames.push('lastName')
                }
                this.apiClient.fetchCustomerItems(this.customer.customerId, itemNames).then(items => {
                    this.contactInfo = {
                        isOpen: true,
                        firstName: this.getArrayItemValue(items, 'firstName'),
                        lastName: this.getArrayItemValue(items, 'lastName'),
                        addressStreet: this.getArrayItemValue(items, 'addressStreet'),
                        addressCity: this.getArrayItemValue(items, 'addressCity'),
                        addressZipcode: this.getArrayItemValue(items, 'addressZipcode'),
                        addressCountry: this.getArrayItemValue(items, 'addressCountry'),
                        phone: this.getArrayItemValue(items, 'phone'),
                        email: this.getArrayItemValue(items, 'email'),
                        companyName: this.getArrayItemValue(items, 'companyName')
                    }
                })
            } else {
                this.contactInfo.isOpen = !this.contactInfo.isOpen
            }
        }

        doKycScreen(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.pepKycInfo) {
                return
            }
            this.apiClient.kycScreenCustomer(this.customer.customerId, false).then(x => {
                if (!x.Success) {
                    toastr.warning('Screening failed: ' + x.FailureCode)
                } else if (x.Skipped) {
                    toastr.info('Customer has already been screened')
                } else {
                    if (this.initialData.onkycscreendone != null) {
                        if (!this.initialData.onkycscreendone(this.customer.customerId)) {
                            return
                        }
                    }
                    this.pepKycInfo = null
                    this.togglePepKycInfo(null)
                }
            })
        }

        togglePepKycInfo(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.pepKycInfo) {
                this.apiClient.fetchCustomerKycScreenStatus(this.customer.customerId).then(x => {
                    this.pepKycInfo = {
                        latestScreeningDate: x.LatestScreeningDate,
                        isOpen: true
                    }
                })
            } else {
                this.pepKycInfo.isOpen = !this.pepKycInfo.isOpen
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
            let nrName = this.isCompany() ? 'orgnr' : 'civicRegNr'
            this.apiClient.fetchCustomerItems(this.customer.customerId, [nrName]).then(items => {
                this.civicRegNr = this.getArrayItemValue(items, nrName)
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

    export class ApplicationCustomerInfoComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ApplicationCustomerInfoController;
            this.templateUrl = 'application-customerinfo.html';
        }
    }

    export class InitialData {
        applicationNr: string
        backTarget?: string
        showKycBlock?: boolean
        onkycscreendone?: (customerId: number) => boolean;
        applicantNr: number
        customerIdCompoundItemName: string //Examples: applicant1.customerId or application.companyCustomerId
        birthDateCompoundItemName?: string
        customerId?: number
        isArchived?: boolean 
    }
}

angular.module('ntech.components').component('applicationCustomerinfo', new ApplicationCustomerInfoComponentNs.ApplicationCustomerInfoComponent())