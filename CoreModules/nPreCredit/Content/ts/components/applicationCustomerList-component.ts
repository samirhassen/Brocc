namespace ApplicationCustomerListComponentNs {
    export class ApplicationCustomerListController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData

        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', '$timeout']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private $timeout: ng.ITimeoutService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'applicationCustomerList'
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            this.initialData.editorService.fetchCustomerIds().then(customerIds => {
                this.m = {
                    selectNr: {},
                    customers: [],
                    multipleEditorServiceCustomers: []
                }
                for (let customerId of customerIds) {
                    this.addCustomerToModel(customerId)
                }
            }).then(x => {
                if (this.initialData.multipleEditorService) {
                    this.initialData.multipleEditorService.editorService.fetchCustomerIds().then(customerIds => {
                        for (let customerId of customerIds) {
                            this.addCustomerToMultipleEditorModel(customerId);
                        }
                    });
                }
            });
        }

        selectNr(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            let nr = this.m.selectNr.nr
            this.apiClient.fetchCustomerIdByCivicRegNr(nr).then(customerId => {
                this.apiClient.fetchCustomerItemsDict(customerId, ['firstName', 'lastName', 'email', 'phone', 'addressStreet', 'addressZipcode', 'addressCity', 'addressCountry']).then(customer => {
                    this.m.selectNr = null
                    this.m.editData = {
                        customerId: customerId,
                        nr: nr,
                        firstName: customer['firstName'],
                        lastName: customer['lastName'],
                        email: customer['email'],
                        phone: customer['phone'],
                        addressStreet: customer['addressStreet'], 
                        addressZipcode: customer['addressZipcode'],
                        addressCity: customer['addressCity'],
                        addressCountry: customer['addressCountry']
                    }
                })
            })
        }

        private addCustomerToModel(customerId: number) {
            this.m.customers.push({
                componentData: {
                    customerId: customerId,
                    onkycscreendone: null,
                    customerIdCompoundItemName: null,
                    applicantNr: null,
                    applicationNr: this.initialData.applicationInfo.ApplicationNr,
                    backTarget: this.initialData.backTarget
                },
                isRemoved: false
            })
        }

        private addCustomerToMultipleEditorModel(customerId: number) {
            this.m.multipleEditorServiceCustomers.push({
                componentData: {
                    customerId: customerId,
                    onkycscreendone: null,
                    customerIdCompoundItemName: null,
                    applicantNr: null,
                    applicationNr: this.initialData.applicationInfo.ApplicationNr,
                    backTarget: this.initialData.backTarget
                },
                isRemoved: false
            })
        }


        addCustomer(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            if (this.initialData.multipleEditorService && this.initialData.multipleEditorService.includeCompanyRoles) 
                this.addCustomerForMultipleEditorService();  

            else {
                let d = this.m.editData;
                this.initialData.editorService.addCustomer(d.customerId, d.nr, d.firstName, d.lastName, d.email, d.phone, d.addressStreet, d.addressZipcode, d.addressCity, d.addressCountry).then(wasAdded => {
                    if (wasAdded) {
                        this.addCustomerToModel(d.customerId)
                    }
                    this.m.selectNr = {}
                    this.m.editData = null
                })
            }
        }

        addCustomerForMultipleEditorService() {
            let d = this.m.editData;
            if (!d.isBeneficialOwner && !d.isAuthorizedSignatory)
                toastr.error('Customer must be at least one of Beneficial Owner and Authorized Signatory.');

            if (d.isBeneficialOwner) {
                this.initialData.multipleEditorService.editorService.addCustomer(d.customerId, d.nr, d.firstName, d.lastName, d.email, d.phone, d.addressStreet, d.addressZipcode, d.addressCity, d.addressCountry).then(wasAdded => {
                    if (wasAdded) {
                        this.addCustomerToMultipleEditorModel(d.customerId)
                    }
                    this.m.selectNr = {}
                    this.m.editData = null
                })
            }

            if (d.isAuthorizedSignatory) {
                this.initialData.editorService.addCustomer(d.customerId, d.nr, d.firstName, d.lastName, d.email, d.phone, d.addressStreet, d.addressZipcode, d.addressCity, d.addressCountry).then(wasAdded => {
                    if (wasAdded) {
                        this.addCustomerToModel(d.customerId)
                    }
                    this.m.selectNr = {}
                    this.m.editData = null
                })
            }
        }

        removeCustomer(model: CustomerModel, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.initialData.editorService.removeCustomer(model.componentData.customerId).then(wasRemoved => {
                if (wasRemoved) {
                    model.isRemoved = true
                } else {
                    toastr.error('Customer could not be removed')
                }
            })
        }

        removeMultipleEditorServiceCustomer(model: CustomerModel, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            this.initialData.multipleEditorService.editorService.removeCustomer(model.componentData.customerId).then(wasRemoved => {
                    if (wasRemoved) {
                        model.isRemoved = true
                    } else {
                        toastr.error('Customer could not be removed')
                    }
                })
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

        isToggleCompanyLoanDocumentCheckStatusAllowed() {
            if (!this.initialData) {
                return false
            }

            let ai = this.initialData.applicationInfo

            return ai.IsActive;
        }
    }

    export class ApplicationCustomerListComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ApplicationCustomerListController;
            this.templateUrl = 'application-customer-list.html';
        }
    }

    export class Model {
        selectNr?: SelectNrModel
        editData?: EditDataModel
        customers: CustomerModel[]
        multipleEditorServiceCustomers: CustomerModel[]
    }

    export class CustomerModel {
        componentData: ApplicationCustomerInfoComponentNs.InitialData
        isRemoved: boolean
    }

    export class SelectNrModel {
        nr?: string
    }

    export class EditDataModel {
        customerId: number
        nr: string
        firstName?: string
        lastName?: string
        email?: string
        phone?: string
        addressStreet?: string
        addressZipcode?: string
        addressCity?: string
        addressCountry?: string
        isBeneficialOwner?: boolean
        isAuthorizedSignatory?: boolean
    }

    export class InitialData {
        applicationInfo: NTechPreCreditApi.ApplicationInfoModel
        header?: string
        isEditable: boolean
        editorService: IEditorService
        multipleEditorService?: IMultipleEditorService
        backUrl: string
        backTarget: string
    }

    export interface IEditorService {
        removeCustomer(customerId: number): ng.IPromise<boolean>
        addCustomer(customerId: number, civicRegNr: string, firstName: string, lastName: string, email: string, phone: string, addressStreet: string, addressZipcode: string, addressCity: string, addressCountry: string): angular.IPromise<boolean>
        fetchCustomerIds(): ng.IPromise<number[]>
    }

    export interface IMultipleEditorService {
        editorService: IEditorService
        header?: string
        includeCompanyRoles?: boolean
    }

    export class CustomerApplicationListEditorService implements IEditorService {
        constructor(private applicationNr: string, private listName: string, private apiClient: NTechPreCreditApi.ApiClient) {
        }

        removeCustomer(customerId: number): angular.IPromise<boolean> {
            return this.apiClient.removeCustomerFromApplicationList(this.applicationNr, this.listName, customerId).then(x => x.WasRemoved)
        }

        addCustomer(customerId: number, civicRegNr: string, firstName: string, lastName: string, email: string, phone: string, addressStreet: string, addressZipcode: string, addressCity: string, addressCountry: string): angular.IPromise<boolean> {
            return this.apiClient.addCustomerToApplicationList(
                this.applicationNr, this.listName,
                customerId, civicRegNr, firstName, lastName, email, phone, addressStreet, addressZipcode, addressCity, addressCountry).then(x => x.WasAdded)
        }

        fetchCustomerIds(): ng.IPromise<number[]> {
            return this.apiClient.fetchCustomerApplicationListMembers(this.applicationNr, this.listName).then(x => x.CustomerIds)
        }
    }

    export class ComplexApplicationListEditorService implements IEditorService {
        private dataSourceName: string = 'ComplexApplicationList'
        private dataSourceItemName: string
        constructor(private applicationNr: string, listName: string, nr: number, private apiClient: NTechPreCreditApi.ApiClient) {
            this.dataSourceItemName = `${listName}#${nr}#r#customerIds`
        }

        private changeCustomerIds(newCustomerId: number, isRemove: boolean): angular.IPromise<boolean> {
            let wasInList: boolean = false
            return this.fetchCustomerIds().then(currentCustomerIds => {
                let newCustomerIds: string[] = []
                for (let c of currentCustomerIds) {
                    if (c === newCustomerId) {
                        wasInList = true
                        if (!isRemove) {
                            newCustomerIds.push(c.toString())
                        }
                    } else {
                        newCustomerIds.push(c.toString())
                    }
                }

                if (!isRemove && !wasInList) {
                    newCustomerIds.push(newCustomerId.toString())
                }

                let wasChanged: boolean = (isRemove && wasInList) || (!isRemove && !wasInList)

                if (wasChanged) {
                    return this.apiClient.setApplicationEditItemData(this.applicationNr, this.dataSourceName, this.dataSourceItemName, JSON.stringify(newCustomerIds), false).then(x => true)
                } else {
                    return false
                }
            })
        }

        removeCustomer(customerId: number): angular.IPromise<boolean> {
            return this.changeCustomerIds(customerId, true)
        }

        addCustomer(customerId: number, civicRegNr: string, firstName: string, lastName: string, email: string, phone: string): angular.IPromise<boolean> {
            let p: NTechPreCreditApi.IStringDictionary<string> = {
            }
            let add = (n: string, v: string) => {
                if (v) {
                    p[n] = v
                }
            }
            add('firstName', firstName)
            add('lastName', lastName)
            add('email', email)
            add('phone', phone)

            return this.apiClient.createOrUpdatePersonCustomerSimple(civicRegNr, p, customerId).then(_ => {
                return this.changeCustomerIds(customerId, false)
            })
        }

        fetchCustomerIds(): ng.IPromise<number[]> {
            let r: NTechPreCreditApi.FetchApplicationDataSourceRequestItem = {
                DataSourceName: this.dataSourceName,
                ErrorIfMissing: false,
                IncludeEditorModel: false,
                IncludeIsChanged: false,
                MissingItemReplacementValue: ApplicationDataSourceHelper.MissingItemReplacementValue,
                Names: [this.dataSourceItemName],
                ReplaceIfMissing: true
            }
            return this.apiClient.fetchApplicationDataSourceItems(this.applicationNr, [r]).then(x => {
                let rv: number[] = []
                let i = x.Results[0].Items
                if (i.length > 0) {
                    let value = i[0].Value
                    if (value === ApplicationDataSourceHelper.MissingItemReplacementValue) {
                        return rv
                    }
                    for (let v of JSON.parse(i[0].Value)) {
                        rv.push(parseInt(v))
                    }
                }
                return rv
            })
        }
    }
}

angular.module('ntech.components').component('applicationCustomerList', new ApplicationCustomerListComponentNs.ApplicationCustomerListComponent())