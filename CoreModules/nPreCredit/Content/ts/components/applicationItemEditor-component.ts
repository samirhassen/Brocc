namespace ApplicationItemEditorComponentNs {
    export function getItemDisplayValueShared(v: string,
        e: NTechPreCreditApi.FetchApplicationEditItemDataResponseEditModel,
        parseDecimalOrNull: (n: any) => number) {
        if (v === '-' || v === ApplicationDataSourceHelper.MissingItemReplacementValue || !v) {
            return null
        }

        if (e.DataSourceName === 'BankAccountTypeAndNr') {
            let i = v.indexOf('#')
            if (i >= 0) {
                let accountType = v.substr(0, i)
                let accountNr = v.substr(i + 1)

                if (accountType === 'IBANFi') {
                    accountType = 'IBAN'
                } else if (accountType === 'BankAccountSe') {
                    accountType = 'regular account'
                } else if (accountType === 'BankGiroSe') {
                    accountType = 'bankgiro account'
                } else if (accountType == 'PlusGiroSe') {
                    accountType = 'plugiro account'
                }
                return `${accountNr} (${accountType})`
            } else {
                return v
            }
        } else {
            let dt = e.DataType
            if (dt == 'positiveInt' || dt == 'dueDay') {
                return Math.round(parseDecimalOrNull(v))
            } else if (dt == 'positiveDecimal') {
                return parseDecimalOrNull(v)
            } else if (e.EditorType === 'dropdownRaw') {
                return ApplicationDataEditorComponentNs.getDropdownDisplayValue(v, e)
            } else if (e.DataType == "url") {
                if (v.length > 30) {
                    return v.substr(0, 30)
                } else {
                    return v
                }
            } else if (e.DataType == 'ibanfi') {
                if (v.length === 18) {
                    return `${v.substr(0, 4)} ${v.substr(4, 4)} ${v.substr(8, 4)} ${v.substr(12, 4)} ${v.substr(16, 2)}`
                } else {
                    return v
                }
            } else if (e.DataType == 'iban') {
                return v
            } else if (e.DataType == 'localDateAndTime') {
                return moment(v).format('YYYY-MM-DD HH:mm')
            } else {
                return v
            }
        }
    }

    export class ApplicationItemEditorController extends NTechComponents.NTechComponentControllerBaseTemplate {
        static $inject = ['ntechComponentService', '$q', '$http']
        constructor(ntechComponentService: NTechComponents.NTechComponentService,
            private $q: ng.IQService,
            private $http: ng.IHttpService) {
            super(ntechComponentService);
            this.fieldName = 'n' + NTechComponents.generateUniqueId(6)
            this.apiClient = new NTechPreCreditApi.ApiClient(e => toastr.error(e), this.$http, this.$q)
        }

        fieldName: string
        m: Model
        apiClient: NTechPreCreditApi.ApiClient

        //Bindings
        name: string
        directEdit: any
        data: DataModel
        directEditForm: () => ng.IFormController
        directEditModel: NTechPreCreditApi.IStringDictionary<any>
        labelSize: string
        enableChangeTracking: any

        componentName(): string {
            return 'applicationItemEditor'
        }

        onChanges() {
            if (!this.m) {
                let em = this.e()
                this.m = {
                    dropdownRawOptions: this.createDropdownRawOptions(),
                    b: em.EditorType === 'bankaccountnr'
                        ? new BankAccountEditor(
                            this.v(),
                            this.$q,
                            this.apiClient,
                            x => this.directEditModel[this.name] = x,
                            this.isReadOnly())
                        : null
                }
            } else {
                this.m.dropdownRawOptions = this.createDropdownRawOptions()
            }
        }

        e() {
            return this.data && this.data.modelByGroupedName ? this.data.modelByGroupedName[this.name] : null
        }

        v() {
            let v = this.data && this.data.valueByGroupedName ? this.data.valueByGroupedName[this.name] : null
            return v === ApplicationDataSourceHelper.MissingItemReplacementValue ? null : v
        }

        lbl() {
            let e = this.e()
            return e ? e.LabelText : null
        }

        isChangeTrackingEnabled(): boolean {
            return this.enableChangeTracking === 'true' || this.enableChangeTracking === true
        }

        isReadOnlyDataType(dt: string): boolean {
            return dt == 'localDateAndTime'
        }

        isReadOnly(): boolean {
            let e = this.e()
            return !this.data.isEditAllowed || (e && (e.IsReadonly || this.isReadOnlyDataType(e.DataType)))
        }

        isDirectEditAllowed(): boolean {
            return !this.isReadOnly() && (this.directEdit === 'true' || this.directEdit === true)
        }

        isRequired(): boolean {
            let e = this.e()
            return e ? e.IsRequired === true : false
        }

        getCreditApplicationItemDisplayValue() {
            return getItemDisplayValueShared(this.v(), this.e(), x => this.parseDecimalOrNull(x))
        }

        getLabelSize(): number {
            if (!this.labelSize) {
                return 6
            }
            let n = this.parseDecimalOrNull(this.labelSize)

            if (n && n > 0.5 && n < 12.4) {
                return Math.round(n)
            } else {
                return 6
            }
        }

        getLabelSizeClass(): string {
            return `col-xs-${this.getLabelSize().toFixed(0)}`
        }

        getInputSizeClass(): string {
            return `col-xs-${(12 - this.getLabelSize()).toFixed(0)}`
        }

        private createDropdownRawOptions(): [string, string][] {
            let em = this.e()
            if (!em || !em.DropdownRawOptions) {
                return null
            }
            let v = em.DropdownRawOptions
            let t = em.DropdownRawDisplayTexts
            if (!t) {
                t = []
            }
            let r = []
            for (let i = 0; i < v.length; i++) {
                r.push([v[i], t.length > i ? t[i] : v[i]])
            }
            return r
        }

        isNavigableUrl() {
            let e = this.e()
            if (e.DataType != 'url') {
                return false
            }
            let value = this.v()
            if (value) {
                return this.isValidURL(value)
            }

            return false
        }

        isCreditApplicationItemEdited() {
            return this.data.isEditedByGroupedName[this.name] === true
        }

        getEditApplicationItemUrl() {
            if (!this.data) {
                return null
            }
            let e = this.e()
            if (!e) {
                return null
            }
            let url = ''
            if (this.data.applicationType === 'mortgageLoan') {
                url += `/Ui/MortgageLoan/EditItem`
            } else if (this.data.applicationType === 'companyLoan') {
                url += `/Ui/CompanyLoan/Application/EditItem`
            } else {
                throw new Error('Not implemented for unsecuredLoans. Needs a controller host')
            }
            url += `?applicationNr=${this.data.applicationNr}&dataSourceName=${e.DataSourceName}&itemName=${encodeURIComponent(this.name)}&ro=${this.data.isEditAllowed ? 'False' : 'True'}`
            url = NavigationTargetHelper.AppendBackNavigationToUrl(url, this.data.navigationOptionToHere)

            return url
        }

        getDirectEditForm() {
            if (!this.directEditForm) {
                return null
            }
            return this.directEditForm()
        }

        getDirectEditErrorClasses() {
            let f = this.getDirectEditForm()
            if (!f) {
                return null
            }
            let e = this.e()
            if (e.EditorType === 'bankaccountnr') {
                let field = f[this.fieldName]
                return {
                    'has-error': !this.m.b.validBankAccountInfo && field.$viewValue && !field.$pending,
                    'has-success': this.m.b.validBankAccountInfo && !field.$pending
                }
            } else {
                let field = f[this.fieldName]
                return {
                    'has-error': field && field.$invalid,
                    'has-success': field && field.$dirty && field.$valid
                }
            }
        }

        getPlaceholderStandard = () => {
            let e = this.e()
            if (!e) {
                return ''
            }
            let et = e.EditorType
            let dt = e.DataType
            if (et == 'text') {
                if (dt == 'month') {
                    return 'YYYY-MM'
                } else if (dt == 'date') {
                    return 'YYYY-MM-DD'
                } else if (dt == 'url') {
                    return 'https://somewhere.example.org/test'
                } else if (dt == 'dueDay') {
                    return '1-28'
                }
            }
            return ''
        }

        isValidStandard = (value: string) => {
            let e = this.e()
            if (!e) {
                return false
            }
            let et = e.EditorType
            let dt = e.DataType
            if (et == 'text') {
                if (dt == 'positiveInt') {
                    return this.isValidPositiveInt(value)
                } else if (dt == 'positiveDecimal') {
                    return this.isValidPositiveDecimal(value)
                } else if (dt == 'month') {
                    return this.isValidMonth(value)
                } else if (dt == 'date') {
                    return this.isValidDate(value)
                } else if (dt == 'ibanfi') {
                    return this.isValidIBANFI(value)
                } else if (dt == 'iban') {
                    return this.isValidIBAN(value)
                } else if (dt == 'string') {
                    return true
                } else if (dt == 'url') {
                    return this.isValidURL(value)
                } else if (dt == 'dueDay') {
                    if (!this.isValidPositiveInt(value)) {
                        return false
                    }
                    let v = this.parseDecimalOrNull(value)
                    return v >= 1 && v <= 28
                }
            }

            return false
        }

        //Others are just a text input with custom validation and placeholder
        getEditorTemplate(): string {
            let e = this.e()
            if (!e) {
                return null
            }
            if (e.EditorType === 'dropdownRaw' && e.DataType === 'string') {
                return 'dropdown'
            } else if (e.EditorType === 'bankaccountnr') {
                return 'bankaccountnr'
            }
            return 'standard'
        }
    }

    export class ApplicationItemEditorComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                name: '<',
                data: '<',
                directEdit: '<',
                directEditForm: '<',
                directEditModel: '<',
                labelSize: '<',
                enableChangeTracking: '<'
            };
            this.controller = ApplicationItemEditorController;
            
            let labelTemplate = `<label ng-if="!$ctrl.getEditApplicationItemUrl()" class="{{$ctrl.getLabelSizeClass()}} control-label">{{$ctrl.lbl()}}</label>
                            <label ng-if="$ctrl.getEditApplicationItemUrl()" class="{{$ctrl.getLabelSizeClass()}} control-label"><a class="pull-right n-anchor-neutral" ng-href="{{$ctrl.getEditApplicationItemUrl()}}">{{$ctrl.lbl()}}</a></label>`

            let inPlaceEditValueTemplate = `
                            <div class="{{$ctrl.getInputSizeClass()}}" ng-class="$ctrl.getDirectEditErrorClasses()" ng-if="$ctrl.isDirectEditAllowed()">
                                    <input name="{{$ctrl.fieldName}}" ng-if="$ctrl.getEditorTemplate() === 'standard'" type="text" class="form-control" custom-validate="$ctrl.isValidStandard" ng-model="$ctrl.directEditModel[$ctrl.name]" placeholder="{{$ctrl.getPlaceholderStandard()}}" ng-required="$ctrl.isRequired()">

                                    <select name="{{$ctrl.fieldName}}" ng-if="$ctrl.getEditorTemplate() === 'dropdown'" class="form-control" ng-model="$ctrl.directEditModel[$ctrl.name]"  ng-required="$ctrl.isRequired()">
                                        <option value="" translate="valj" ng-hide="$ctrl.directEditModel[$ctrl.name]">None</option>
                                        <option value="{{p[0]}}" ng-repeat="p in $ctrl.m.dropdownRawOptions">{{p[1]}}</option>
                                    </select>

                                    <div ng-if="$ctrl.getEditorTemplate() ==='bankaccountnr'">
                                        <div class="pb-1">
                                            <label style="padding-top:7px;">Account type</label>
                                            <select name="{{$ctrl.fieldName + 'bankAccountNrType'}}" class="form-control" ng-model="$ctrl.m.b.bankAccountNrType" ng-change=" $ctrl.m.b.onTypeChanged()">
                                                <option ng-repeat="b in $ctrl.m.b.bankAccountNrTypes" value="{{b.code}}">{{b.text}}</option>
                                            </select>
                                        </div>
                                        <label>
                                            {{$ctrl.m.b.getAccountNrFieldLabel($ctrl.m.b.bankAccountNrType)}}
                                        </label>
                                        <input type="text"
                                               class="form-control"
                                               autocomplete="off"
                                               ng-model="$ctrl.m.b.bankAccountNr"
                                               name="{{$ctrl.fieldName}}"
                                               custom-validate-async="$ctrl.m.b.isValidBankAccount"
                                               ng-model-options="{ updateOn: 'default blur', debounce: {'default': 300, 'blur': 0} }"
                                               required placeholder="{{$ctrl.m.b.getAccountNrMask()}}">

                                        <div ng-if="$ctrl.m.b.validBankAccountInfo">
                                            <hr style="border-color: #fff;" />
                                            {{$ctrl.m.b.validBankAccountInfo.displayValue}}
                                            <hr style="border-color: #fff;" />
                                        </div>
                                        <div ng-if="!$ctrl.m.b.validBankAccountInfo && $ctrl.getDirectEditForm()[$ctrl.fieldName].$viewValue && !$ctrl.getDirectEditForm()[$ctrl.fieldName].$pending">
                                            Invalid
                                        </div>
                                        <div ng-if="$ctrl.getDirectEditForm()[$ctrl.fieldName].$pending">...</div>

                                    </div>
                                </div>`

            let readOnlyValueTemplate =  `<div class="{{$ctrl.getInputSizeClass()}}" ng-if="!$ctrl.isDirectEditAllowed()">
                                <p class="form-control-static" ng-if="$ctrl.isNavigableUrl()" style="border-bottom:solid 1px">
                                    <a ng-href="{{$ctrl.v()}}" target="_blank" class="n-anchor n-longer">
                                        {{$ctrl.getCreditApplicationItemDisplayValue()}}
                                        <span class="pull-right n-star" ng-show="$ctrl.isCreditApplicationItemEdited() && $ctrl.isChangeTrackingEnabled()">*</span>
                                    </a>
                                </p>
                                <p class="form-control-static" ng-if="!$ctrl.isNavigableUrl()" style="border-bottom:solid 1px">
                                    <span ng-if="$ctrl.getCreditApplicationItemDisplayValue()">{{$ctrl.getCreditApplicationItemDisplayValue()}}</span>
                                    <span ng-if="!$ctrl.getCreditApplicationItemDisplayValue()">&nbsp;</span> <!-- Prevent collapsing to no height -->
                                    <span class="pull-right n-star" ng-show="$ctrl.isCreditApplicationItemEdited() && $ctrl.isChangeTrackingEnabled()">*</span>
                                </p>
                            </div>`

            this.template = `<div class="form-group">

                            ${labelTemplate}

                            ${readOnlyValueTemplate}

                            ${inPlaceEditValueTemplate}
                        </div>`;
        }
    }

    export class Model {
        dropdownRawOptions: [string, string][]
        b: BankAccountEditor
    }

    export class DataModel {
        applicationNr: string
        applicationType: string
        isEditAllowed: boolean
        navigationOptionToHere: NavigationTargetHelper.CodeOrUrl
        valueByGroupedName: NTechPreCreditApi.IStringDictionary<string>
        isEditedByGroupedName?: NTechPreCreditApi.IStringDictionary<boolean>
        modelByGroupedName: NTechPreCreditApi.IStringDictionary<NTechPreCreditApi.FetchApplicationEditItemDataResponseEditModel>
    }

    export function createDataModelUsingDataSourceResult(applicationNr: string,
        applicationType: string,
        isEditAllowed: boolean,
        navigationOptionToHere: NavigationTargetHelper.CodeOrUrl,
        r: NTechPreCreditApi.FetchApplicationDataSourceItemsResponseItem): DataModel {
        let valueByGroupedName: NTechPreCreditApi.IStringDictionary<string> = {}
        let isEditedByGroupedName: NTechPreCreditApi.IStringDictionary<boolean> = {}
        let modelByGroupedName: NTechPreCreditApi.IStringDictionary<NTechPreCreditApi.FetchApplicationEditItemDataResponseEditModel> = {}

        for (let i of r.Items) {
            valueByGroupedName[i.Name] = i.Value
            modelByGroupedName[i.Name] = i.EditorModel
            isEditedByGroupedName[i.Name] = false
        }

        for (let e of r.ChangedNames) {
            isEditedByGroupedName[e] = true
        }

        return {
            applicationNr: applicationNr,
            applicationType: applicationType,
            isEditAllowed: isEditAllowed,
            modelByGroupedName: modelByGroupedName,
            navigationOptionToHere: navigationOptionToHere,
            valueByGroupedName: valueByGroupedName,
            isEditedByGroupedName: isEditedByGroupedName
        }
    }

    export class BankAccountEditor {
        constructor(initialValue: string,
            private $q: ng.IQService,
            private apiClient: NTechPreCreditApi.ApiClient,
            private updateValue: (newValue: string) => void,
            private isReadOnly: boolean) {
            this.bankAccountNrTypes = []
            if (ntechClientCountry === 'FI') {
                this.bankAccountNrTypes.push({ code: 'IBANFi', text: 'IBAN' })
            } else if (ntechClientCountry === 'SE') {
                this.bankAccountNrTypes.push({ code: 'BankAccountSe', text: 'Bank account nr' })
                this.bankAccountNrTypes.push({ code: 'BankGiroSe', text: 'Bankgiro nr' })
                this.bankAccountNrTypes.push({ code: 'PlusGiroSe', text: 'Plusgiro nr' })
            }
            if (initialValue) {
                let values = initialValue.split('#')
                this.bankAccountNrType = values[0]
                this.bankAccountNr = values[1]
                if (this.isReadOnly) {
                    //Populate validBankAccountInfo
                    this.isValidBankAccount(this.bankAccountNr)
                }
            }
        }

        public bankAccountNrType: string
        public bankAccountNr: string
        public bankAccountNrTypes: { code: string, text: string }[]
        public validBankAccountInfo: { displayValue: string }

        public getAccountNrFieldLabel(nrType: string): string {
            if (!this.bankAccountNrTypes) {
                return nrType
            }
            for (let t of this.bankAccountNrTypes) {
                if (t.code === nrType) {
                    return t.text
                }
            }
            return nrType
        }

        public getAccountNrMask(): string {
            return 'Account nr'
        }

        public isValidBankAccount = (input: string) => {
            var deferred = this.$q.defer<string>();

            this.apiClient.isValidAccountNr(input, this.bankAccountNrType).then(x => {
                if (x.isValid) {
                    deferred.resolve(input)
                    this.validBankAccountInfo = {
                        displayValue: x.displayValue
                    }
                    this.updateValue(`${this.bankAccountNrType}#${x.normalizedValue}`)
                } else {
                    deferred.reject(x.message)
                    this.validBankAccountInfo = null
                }
            })

            return deferred.promise;
        }

        public onTypeChanged = () => {
            this.bankAccountNr = ''
            this.validBankAccountInfo = null
        }
    }
}
angular.module('ntech.components').component('applicationItemEditor', new ApplicationItemEditorComponentNs.ApplicationItemEditorComponent())