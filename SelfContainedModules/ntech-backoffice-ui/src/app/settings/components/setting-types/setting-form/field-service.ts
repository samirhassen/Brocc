import { Validators } from '@angular/forms';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { SettingsFormFieldTypeCode } from '../../../services/settings-model';
import { EditViewModel, FieldTypeHandler } from './form-types';

export class FieldService {
    constructor(private validationService: NTechValidationService) {}

    public getFieldHandler = (fieldType: string): FieldTypeHandler => {
        if (fieldType === SettingsFormFieldTypeCode.InterestRate) {
            return {
                formatStoredValueForDisplay: (valueText, _) => {
                    let parsedValue = this.validationService.parsePositiveDecimalOrNull(valueText);
                    return this.validationService.formatDecimalForDisplay(parsedValue, 4) + ' %';
                },
                addToForm: (field, form, editFields, storedValue) => {
                    let parsedValue = this.validationService.parsePositiveDecimalOrNull(storedValue);
                    let editValue = this.validationService.formatPositiveDecimalForEdit(parsedValue);
                    form.addControlIfNotExists(field.Name, editValue, [
                        Validators.required,
                        this.validationService.getPositiveDecimalValidator(),
                    ]);
                    editFields.push(new EditViewModel(field.DisplayName, 'inputText', field.Name));
                },
                parseFormValueForStorage: (formValue) =>
                    this.validationService.parsePositiveDecimalOrNull(formValue)?.toString(),
            };
        } else if (fieldType === SettingsFormFieldTypeCode.Enum) {
            return {
                formatStoredValueForDisplay: (valueText, field) =>
                    field.EnumOptions.find((x) => x.Code == valueText)?.DisplayName,
                addToForm: (field, form, editFields, storedValue) => {
                    form.addControlIfNotExists(field.Name, storedValue, [Validators.required]);
                    let editModel = new EditViewModel(field.DisplayName, 'dropdown', field.Name);
                    editModel.dropdownOptions = field.EnumOptions;
                    editFields.push(editModel);
                },
                parseFormValueForStorage: (formValue) => formValue,
            };
        } else if (fieldType === SettingsFormFieldTypeCode.PositiveInteger) {
            return {
                formatStoredValueForDisplay: (valueText, _) => {
                    let parsedValue = this.validationService.parseIntegerOrNull(valueText);
                    return this.validationService.formatIntegerForDisplay(parsedValue);
                },
                addToForm: (field, form, editFields, storedValue) => {
                    let parsedValue = this.validationService.parseIntegerOrNull(storedValue);
                    let editValue = this.validationService.formatIntegerForEdit(parsedValue);
                    form.addControlIfNotExists(field.Name, editValue, [
                        Validators.required,
                        this.validationService.getPositiveIntegerValidator(),
                    ]);
                    editFields.push(new EditViewModel(field.DisplayName, 'inputText', field.Name));
                },
                parseFormValueForStorage: (formValue) => {
                    let parsedValue = this.validationService.parseIntegerOrNull(formValue);
                    return parsedValue.toString();
                },
            };
        } else if (fieldType === SettingsFormFieldTypeCode.Url) {
            return {
                formatStoredValueForDisplay: (valueText, _) => valueText,
                addToForm: (field, form, editFields, storedValue) => {
                    form.addControlIfNotExists(field.Name, storedValue, [
                        Validators.required,
                        this.validationService.getUrlValidator(),
                    ]);
                    editFields.push(new EditViewModel(field.DisplayName, 'inputText', field.Name));
                },
                parseFormValueForStorage: (formValue) => formValue,
            };
        } else if (fieldType === SettingsFormFieldTypeCode.HiddenText) {
            return {
                formatStoredValueForDisplay: (_) => '**********',
                addToForm: (field, form, editFields, storedValue) => {
                    form.addControlIfNotExists(field.Name, storedValue, [Validators.required]);
                    editFields.push(new EditViewModel(field.DisplayName, 'hiddenText', field.Name));
                },
                parseFormValueForStorage: (formValue) => formValue,
            };
        } else if (fieldType === SettingsFormFieldTypeCode.TextArea) {
            return {
                formatStoredValueForDisplay: (valueText, _) => valueText,
                addToForm: (field, form, editFields, storedValue) => {
                    form.addControlIfNotExists(field.Name, storedValue, [Validators.required]);
                    let vm = new EditViewModel(field.DisplayName, 'textarea', field.Name);
                    vm.nrOfRows = field.NrOfRows;
                    editFields.push(vm);
                },
                parseFormValueForStorage: (formValue) => formValue,
            };
        } else if (fieldType === SettingsFormFieldTypeCode.Text) {
            return {
                formatStoredValueForDisplay: (valueText, _) => valueText,
                addToForm: (field, form, editFields, storedValue) => {
                    form.addControlIfNotExists(field.Name, storedValue, [Validators.required]);
                    editFields.push(new EditViewModel(field.DisplayName, 'inputText', field.Name));
                },
                parseFormValueForStorage: (formValue) => formValue,
            };
        } else {
            throw new Error('Field type not implemented: ' + fieldType);
        }
    }
}