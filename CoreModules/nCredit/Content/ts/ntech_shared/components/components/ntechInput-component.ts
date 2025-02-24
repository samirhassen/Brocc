namespace NTechInputComponentNs {

    export class NTechInputController extends NTechComponents.NTechComponentControllerBase {
        label: string;
        required: boolean;
        model: any;
        t: string;
        labelClasses: string
        inputClasses: string
        groupStyle: string
        alias: string
        customIsValid: (value: any) => boolean

        formName: string;
        editInputName: string;
        inputtype: string;
        rows: number;

        static $inject = ['$http', '$q', '$scope', '$timeout', '$filter', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            private $scope: IScopeWithForm,
            private $timeout: ng.ITimeoutService,
            private $filter: ng.IFilterService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);

            this.formName = 'f' + NTechComponents.generateUniqueId(6)
            this.editInputName = 'i' + NTechComponents.generateUniqueId(6)
            this.inputtype = 'input'
            this.rows = 1

            ntechComponentService.subscribeToNTechEvents(evt => {
                if (evt.eventName == 'FocusControlByAlias' && this.alias && evt.eventData == this.alias) {
                    this.$timeout(() => {
                        document.getElementsByName(this.editInputName)[0].focus()
                    })
                }
            })
        }

        componentName(): string {
            return 'ntechInput'
        }

        onChanges() {

        }

        getForm(): ng.IFormController {
            if (this.formName && this.$scope[this.formName]) {
                return this.$scope[this.formName]
            }
            return null
        }

        onEdit() {
            let f = this.getForm()
            if (f) {
                f.$setValidity(this.editInputName + 'IsValid', this.isValid(this.model), f);
            }
        }

        public onPaste = (evt: any) => { //evt is and event with originalEvent being a clipboardEvent
            if (!(this.t === 'positivedecimal' || this.t === 'positiveint' || this.t === 'moneyint')) {
                return
            }
            if (evt && evt.originalEvent && evt.originalEvent.clipboardData) {
                let et: ClipboardEvent = evt.originalEvent
                let text = et.clipboardData.getData('text/plain')
                let trimmedText = text.replace(/[a-z]|\s/gi, '')
                if (trimmedText !== text) {
                    evt.preventDefault()
                    this.model = trimmedText
                }
            }
        }

        isValid(v: any) {
            let isValidIfOptional = () => {
                if (this.t == 'positiveint' || this.t == 'moneyint') {
                    return this.isValidPositiveInt(v)
                } else if (this.t == 'email') {
                    return this.isValidEmail(v)
                } else if (this.t == 'phonenr') {
                    return this.isValidPhoneNr(v)
                } else if (this.t == 'date') {
                    return this.isValidDate(v)
                } else if (this.t == 'positivedecimal') {
                    return this.isValidPositiveDecimal(v)
                } else if (this.t == 'custom') {
                    return this.customIsValid(v)
                } else {
                    return true //type = text or missing
                }
            }
            return isValidIfOptional() && (!this.required || v)
        }

        isFormInvalid() {
            let f = this.getForm()
            if (!f) {
                return true
            }
            return (f.$invalid && !f.$pristine)
        }
    }

    export class NTechInputComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;
        public transclude: boolean;

        constructor() {
            this.transclude = true;
            this.bindings = {
                required: '<',
                label: '<',
                model: '=',
                t: '<',
                labelClasses: '<',
                inputClasses: '<',
                groupStyle: '<',
                alias: '<',
                customIsValid: '<',
                placeholderText: '<',
                inputtype: '<',
                rows: '<'
            };
            this.controller = NTechInputController;

            this.template = `<div class="form-group ntechinput" ng-form name="{{$ctrl.formName}}" ng-class="{ 'has-error' : $ctrl.isFormInvalid() }" style="{{$ctrl.groupStyle}}">
        <label class="control-label {{$ctrl.labelClasses}}" ng-if="$ctrl.label">
            {{$ctrl.label}}
        </label>
        <div class="{{$ctrl.inputClasses}}">
            <div ng-show="!$ctrl.inputtype">
                <input type="text" class="form-control" autocomplete="off" placeholder="{{$ctrl.placeholderText}}" name="{{$ctrl.editInputName}}" ng-paste="$ctrl.onPaste($event)" ng-change="{{$ctrl.onEdit()}}" ng-required="$ctrl.required" ng-model="$ctrl.model" />
            </div>
            <div ng-show="$ctrl.inputtype=='textarea'">
                <textarea class="form-control" rows="{{$ctrl.rows}}" placeholder="{{$ctrl.placeholderText}}" name="{{$ctrl.editInputName}}" ng-paste="$ctrl.onPaste($event)" ng-change="{{$ctrl.onEdit()}}" ng-required="$ctrl.required" ng-model="$ctrl.model" />
            </div>
        </div>
        <ng-transclude></ng-transclude>
                </div>`
        }
    }

    interface IScopeWithForm extends ng.IScope {
        [index: string]: any
    }
}

angular.module('ntech.components').component('ntechInput', new NTechInputComponentNs.NTechInputComponent())