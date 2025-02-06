import { Component, Input, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder } from '@angular/forms';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { Dictionary } from 'src/app/common.types';
import { sharedQuillSettings } from 'src/app/secure-messages/shared.quill.settings';
import { SettingsApiService } from '../../../services/settings-api.service';
import { SettingModel, SettingTypeCode } from '../../../services/settings-model';

@Component({
    selector: 'setting-htmltemplate',
    templateUrl: './setting-htmltemplate.component.html',
    styles: [
        `
            .mr5px {
                margin-right: 5px;
            }
            .editor-background {
                background-color: #f9f9f9;
                min-height: 50px;
            }
        `,
    ],
})
export class SettingHtmltemplateComponent {
    constructor(private fb: UntypedFormBuilder, private apiService: SettingsApiService) {}

    @Input()
    public initialData: SettingHtmlTemplateComponentInitialData;

    public m: Model;
    private editorIdIncrement: number;

    public quillEditorOptions = sharedQuillSettings.editorOptions;
    public quillEditorFormats = sharedQuillSettings.editorFormats;

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (this?.initialData?.setting?.Type !== SettingTypeCode.HtmlTemplate) {
            return;
        }

        this.reload();
    }

    private reload() {
        let s = this.initialData.setting;

        this.apiService.apiService.shared.getCurrentSettingValues(s.Code).then((x) => {
            let value = x.SettingValues[s.Code];
            let separatedValues = value.split('[[[PAGE_BREAK]]]');

            let m: Model = {
                isEditing: false,
                viewFields: separatedValues,
            };
            this.m = m;
        });
    }

    beginEdit(evt?: Event) {
        evt?.preventDefault();
        this.m.isEditing = true;
        let f = new FormsHelper(this.fb.group({}));

        let editFields: string[] = [];
        this.editorIdIncrement = 0;

        for (let field of this.m.viewFields) {
            let controlName = this.getNextFormControlName();
            f.addControlIfNotExists(controlName, field, []);
            editFields.push(controlName);
        }

        this.m.edit = {
            form: f,
            editorNames: editFields,
        };
    }

    rollbackEdit(evt?: Event) {
        evt?.preventDefault();
        this.m.isEditing = false;
    }

    commitEdit(evt?: Event) {
        evt?.preventDefault();

        let s = this.initialData.setting;

        let editorValues: string[] = [];

        let form = this.m.edit.form;
        for (let editorControlName of this.m.edit.editorNames) {
            editorValues.push(form.getValue(editorControlName));
        }
        let newValue = editorValues.join('[[[PAGE_BREAK]]]');
        let newValues: Dictionary<string> = {};
        newValues[s.Code] = newValue;

        this.apiService.saveSettingValues(s.Code, newValues).then(() => {
            this.reload();
        });
    }

    deleteEditor(formControlName: string, evt?: Event) {
        evt?.preventDefault();

        if (this.m.edit.editorNames.length > 1) {
            this.m.edit.form.form.removeControl(formControlName);
            let indexToRemove = this.m.edit.editorNames.indexOf(formControlName);
            this.m.edit.editorNames.splice(indexToRemove, 1);
        } else console.log('Can not remove last editor!');
    }

    appendEditor(evt?: Event) {
        evt?.preventDefault();

        let newControlName = this.getNextFormControlName();
        this.m.edit.form.addControlIfNotExists(newControlName, '', []);
        this.m.edit.editorNames.push(newControlName);
    }

    private getNextFormControlName(): string {
        if (!this.editorIdIncrement) this.editorIdIncrement = 0;

        this.editorIdIncrement++;
        return 'quillFormControl' + this.editorIdIncrement;
    }
}

export class SettingHtmlTemplateComponentInitialData {
    setting: SettingModel;
}

class Model {
    isEditing: boolean;
    viewFields: string[];
    edit?: {
        form: FormsHelper;
        editorNames: string[];
    };
}
