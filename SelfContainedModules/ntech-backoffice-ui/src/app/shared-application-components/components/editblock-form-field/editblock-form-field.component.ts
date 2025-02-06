import { Component, Input, OnInit } from '@angular/core';
import { AsyncValidatorFn, UntypedFormControl, ValidatorFn } from '@angular/forms';
import { Subscription } from 'rxjs';
import { ConfigService } from 'src/app/common-services/config.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { getBankAccountTypeDropdownOptions } from 'src/app/common.types';

@Component({
    selector: 'editblock-form-field',
    templateUrl: './editblock-form-field.component.html',
    styles: [],
})
export class EditblockFormFieldComponent implements OnInit {
    constructor() {}

    @Input()
    public model: EditblockFormFieldModel;

    ngOnInit(): void {}

    getDisplayValue() {
        if (this.model.getCustomDisplayValue) {
            return this.model.getCustomDisplayValue();
        } else if (this.model.dropdownOptions) {
            return EditblockFormFieldModel.getDropdownDisplayNameFromCode(
                this.model.getOriginalValue(),
                this.model.dropdownOptions
            );
        } else {
            return this.model.getOriginalValue();
        }
    }

    isDropdown() {
        return !!this.model.dropdownOptions;
    }

    isRegularInput() {
        return !this.model.dropdownOptions;
    }

    formControl() {
        //This is used since binding [formControlName]="model.formControlName" doesnt work for no apparent reason while [formControl]="formControl()" does.
        return this.model.getForm().getFormControl(null, this.model.formControlName) as UntypedFormControl;
    }

    isReadonly() {
        if (!this.model.isReadonly) {
            return false;
        } else {
            return this.model.isReadonly();
        }
    }

    showEditControls() {
        return this.model && !this.isReadonly() && this.model.inEditMode();
    }

    hasError() {
        let hasError = this.model?.getForm()?.hasError(this.model.formControlName);
        if (!hasError && this.model && this.model.formErrorCodesCausingHasError) {
            let f = this.model.getForm().form;
            for (let code of this.model.formErrorCodesCausingHasError) {
                if (f.hasError(code)) {
                    return true;
                }
            }
        }
        return hasError;
    }

    getLabelSizeClass() {
        let size = this.model.labelColCount ?? 6;
        return `col-xs-${size}`;
    }

    getValueSizeClass() {
        let size = this.model.labelColCount ? 12 - this.model.labelColCount : 6;
        return `col-xs-${size}`;
    }
}

export class EditblockFormFieldModel {
    getForm: () => FormsHelper;
    formControlName: string;
    labelText: string;
    inEditMode: () => boolean;
    getOriginalValue: () => string;
    getCustomDisplayValue?: () => string;
    placeholder?: string;
    getValidators: () => ValidatorFn[];
    getAsyncValidators?: () => AsyncValidatorFn[];
    conditional?: {
        shouldExist: () => boolean;
    };
    dropdownOptions?: { Code: string; DisplayName: string }[];
    isReadonly?: () => boolean; //Value exists and is shown in view mode but you cant edit it
    formErrorCodesCausingHasError?: string[]; //To let form level validators like date pairs and accountnr + type to flag all the fields as erroring
    labelColCount?: number;

    static setupForm(fields: EditblockFormFieldModel[], form: FormsHelper): Subscription[] {
        let subscriptions: Subscription[] = [];
        //form is assumed to not already contain these fields so it's not a synch but the form can contain other fields
        if (!fields) {
            return subscriptions;
        }
        for (let field of fields.filter((x) => !x.conditional)) {
            form.addControlIfNotExists(
                field.formControlName,
                field.getOriginalValue(),
                field.getValidators(),
                field?.getAsyncValidators ? field.getAsyncValidators() : null
            );
        }
        let synchConditionalFields = () => {
            for (let field of fields.filter((x) => x.conditional)) {
                form.ensureControlOnConditionOnly(
                    field.conditional.shouldExist(),
                    field.formControlName,
                    field.getOriginalValue,
                    field.getValidators,
                    field?.getAsyncValidators
                );
            }
        };
        subscriptions.push(
            form.form.valueChanges.subscribe((_) => {
                synchConditionalFields();
            })
        );
        synchConditionalFields();
        return subscriptions;
    }

    static includeEmptyDropdownOption(options: { Code: string; DisplayName: string }[]) {
        return [{ Code: '', DisplayName: '' }].concat(options);
    }

    static getDropdownDisplayNameFromCode(code: string, options: { Code: string; DisplayName: string }[]) {
        if (!options) {
            return code;
        }
        for (let option of options) {
            if (option.Code === code) {
                return option.DisplayName;
            }
        }
        return code;
    }

    static getBankAccountTypeDropdownOptions(config: ConfigService) {
        return getBankAccountTypeDropdownOptions(config.getClient().BaseCountry, true, 'en');
    }
}
