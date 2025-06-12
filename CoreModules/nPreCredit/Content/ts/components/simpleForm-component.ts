namespace SimpleFormComponentNs {

    export class SimpleFormController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData;
        formName: string;

        static $inject = ['$http', '$q', '$scope', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            private $scope: ng.IScope,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);

            this.formName = 'f' + NTechComponents.generateUniqueId(6)
        }

        componentName(): string {
            return 'simpleForm'
        }
        
        onChanges() {
                
        }

        items(): SimpleFormItem[] {
            if (this.initialData) {
                return this.initialData.items
            }
            return []            
        }

        model(i: SimpleFormItem) {
            return this.initialData.modelBase[i.modelPropertyName]
        }

        onClick(i: SimpleFormItem, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (i.onClick) {
                i.onClick();
            }
        }

        form(): ng.IFormController {            
            return this.$scope[this.formName]
        }

        isFormInvalid() {
            return !this.form() || this.form().$invalid
        }
    }

    export class SimpleFormComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;
        public transclude: boolean;
        
        constructor() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = SimpleFormController;
            this.templateUrl = 'simple-form.html';
        }
    }

    export enum SimpleFormItemType {
        TextField = "input",
        Button = "button",
        TextView = "textview"
    }

    export enum ButtonType {
        NotAButton = "notAButton",
        Default = "defaultButton",
        Accept = "acceptButton"
    }

    export class SimpleFormItem {
        itemType: SimpleFormItemType
        labelText: string
        required: boolean
        modelPropertyName: string
        onClick: () => void
        buttonType : ButtonType
    }

    export interface ITextFieldOptions {
        model: string,
        labelText?: string,        
        required?: boolean,        
    }
    export function textField(opt: ITextFieldOptions): SimpleFormItem {
        return {
            itemType: SimpleFormItemType.TextField,
            labelText: opt.labelText,
            required: !!opt.required,
            modelPropertyName: opt.model,
            onClick: null,
            buttonType: ButtonType.NotAButton
        }
    }

    export interface ITexViewOptions {
        model: string,
        labelText?: string,
    }
    export function textView(opt: ITexViewOptions): SimpleFormItem {
        return {
            itemType: SimpleFormItemType.TextView,
            labelText: opt.labelText,
            required: false,
            modelPropertyName: opt.model,
            onClick: null,
            buttonType: ButtonType.NotAButton
        }
    }

    export interface IButtonOptions {
        buttonText: string
        onClick: () => void
        buttonType?: ButtonType
    }
    export function button(opt: IButtonOptions): SimpleFormItem {
        return {
            itemType: SimpleFormItemType.Button,
            labelText: opt.buttonText,
            onClick: opt.onClick,
            required: null,
            modelPropertyName: null,
            buttonType: opt.buttonType || ButtonType.Default
        }
    }

    export class InitialData {
        items: SimpleFormItem[]
        modelBase: any
    }
}

angular.module('ntech.components').component('simpleForm', new SimpleFormComponentNs.SimpleFormComponent())