namespace EditCustomerContactInfoValueComponentNs {

    export class EditCustomerContactInfoValueController extends NTechComponents.NTechComponentControllerBase {
        static $inject = ['$http', '$q', 'ntechComponentService', '$translate']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private $translate: any) {
            super(ntechComponentService, $http, $q);     
        }

        initialData: InitialData
        m: Model

        componentName(): string {
            return 'editCustomerContactInfoValue'
        }

        getLabelText() {
            return this?.initialData?.labelText ?? 'Edit contact information'
        }
        
        onChanges() {
            this.m = null
            if (!this.initialData) {
                return
            }
            this.apiClient.fetchCustomerContactInfoEditValueData(this.initialData.customerId, this.initialData.itemName).then(result => {
                this.m = {
                    editValue: result.currentValue ? result.currentValue.Value : null,
                    templateName: result.templateName,
                    currentValue: result.currentValue,
                    historicalValues: result.historicalValues
                }
            })
        }

        saveChange(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.initialData || !this.m) {
                return
            }
            this.apiClient.changeCustomerContactInfoValue(this.initialData.customerId, this.initialData.itemName, this.m.editValue, false).then(result => {
                this.initialData.onClose(null);
            })
        }

        getInputType() {
            if (!this.m) {
                return 'text'
            }
            let t = this.m.templateName
            if (t === 'Email') {
                return 'email'
            } else if (t === 'Phonenr') {
                return 'phonenr'
            } else if (t === 'String') {
                return 'text'
            } else if (t === 'Date') {
                return 'date'
            }
            return 'text'
        }
    }

    export class EditCustomerContactInfoValueComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;
        public transclude: boolean;

        constructor() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = EditCustomerContactInfoValueController;
            this.templateUrl = 'edit-customer-contact-info-value.html';
        }
    }

    export class InitialData {
        customerId: number
        itemName: string
        onClose: (evt: Event) => void
        labelText?: string
        hideHeader?: boolean
    }

    export class Model {
        editValue: string
        templateName: string
        currentValue: NTechCustomerApi.ICustomerPropertyModelExtended
        historicalValues: NTechCustomerApi.ICustomerPropertyModelExtended[]
    }
}

angular.module('ntech.components').component('editCustomerContactInfoValue', new EditCustomerContactInfoValueComponentNs.EditCustomerContactInfoValueComponent())