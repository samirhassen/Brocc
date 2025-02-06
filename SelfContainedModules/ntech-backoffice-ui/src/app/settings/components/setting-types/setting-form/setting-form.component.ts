import { Component, Input, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder } from '@angular/forms';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { Dictionary } from 'src/app/common.types';
import { SettingsApiService } from '../../../services/settings-api.service';
import { SettingModel, SettingTypeCode } from '../../../services/settings-model';
import { FieldService } from './field-service';
import { EditViewModel } from './form-types';

@Component({
    selector: 'setting-form',
    templateUrl: './setting-form.component.html',
    styles: [
        `
            .word-wrap {
                word-wrap: break-word;
            }
        `,
    ],
})
export class SettingFormComponent {
    constructor(
        validationService: NTechValidationService,
        private fb: UntypedFormBuilder,
        private apiService: SettingsApiService
    ) {
        this.fieldService = new FieldService(validationService);
    }

    private fieldService: FieldService;

    @Input()
    public initialData: SettingFormComponentInitialData;

    public m: Model;

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (this?.initialData?.formSetting?.Type !== SettingTypeCode.Form) {
            return;
        }

        this.reload();
    }

    private reload() {
        let s = this.initialData.formSetting;

        this.apiService.apiService.shared.getCurrentSettingValues(s.Code).then((x) => {
            let m: Model = {
                isEditing: false,
                viewFields: [],
                rawValues: x.SettingValues,
                showHiddenFields: false,
            };
            for (let field of s.FormData.Fields) {
                let fieldHandler = this.fieldService.getFieldHandler(field.Type);
                let valueText = fieldHandler.formatStoredValueForDisplay(m.rawValues[field.Name], field);
                let v = new SettingViewModel(field.DisplayName, valueText);
                m.viewFields.push(v);
            }
            this.m = m;
        });
    }

    beginEdit(evt?: Event) {
        evt?.preventDefault();
        this.m.isEditing = true;
        let f = new FormsHelper(this.fb.group({}));

        let s = this.initialData.formSetting;
        let editFields: EditViewModel[] = [];

        for (let field of s.FormData.Fields) {
            let fieldHandler = this.fieldService.getFieldHandler(field.Type);
            fieldHandler.addToForm(field, f, editFields, this.m.rawValues[field.Name]);
        }

        this.m.edit = {
            form: f,
            editFields: editFields,
        };
    }

    rollbackEdit(evt?: Event) {
        evt?.preventDefault();
        this.m.isEditing = false;
        this.m.showHiddenFields = false;
    }

    async commitEdit(evt?: Event) {
        evt?.preventDefault();

        let newValues: Dictionary<string> = {};
        let s = this.initialData.formSetting;

        let form = this.m.edit.form;

        for (let field of s.FormData.Fields) {
            let fieldHandler = this.fieldService.getFieldHandler(field.Type);
            newValues[field.Name] = fieldHandler.parseFormValueForStorage(form.getValue(field.Name));
        }

        let result = await this.apiService.saveSettingValuesWithValidation(s.Code, newValues);
        if(result.IsSaved) {
            this.reload();
        } else {
            this.m.edit.validationErrors = result.ValidationErrors;
        }
    }

    hasHiddenFields(): boolean {
        return this.m?.edit?.editFields?.filter((x) => x.controlType === 'hiddenText').length > 0;
    }
}

class Model {
    isEditing: boolean;
    viewFields: SettingViewModel[];
    rawValues: Dictionary<string>;
    edit?: {
        form: FormsHelper;
        editFields: EditViewModel[];
        validationErrors ?: string[]
    };
    showHiddenFields: boolean;
}

export class SettingFormComponentInitialData {
    formSetting: SettingModel;
}

class SettingViewModel {
    constructor(public labelText: string, public valueText: string) {}
}
