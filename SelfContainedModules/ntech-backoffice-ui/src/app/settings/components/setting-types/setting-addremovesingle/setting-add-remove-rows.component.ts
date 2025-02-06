import { Component, Input, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, UntypedFormGroup, Validators } from '@angular/forms';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { distinct, StringDictionary } from 'src/app/common.types';
import { SettingsApiService } from 'src/app/settings/services/settings-api.service';
import { SettingModel } from 'src/app/settings/services/settings-model';

@Component({
    selector: 'setting-add-remove-rows',
    templateUrl: './setting-add-remove-rows.component.html',
    styleUrls: ['./setting-add-remove-rows.component.scss'],
})
export class SettingAddRemoveSingleComponent {
    constructor(private formBuilder: UntypedFormBuilder, private apiService: SettingsApiService) {}

    @Input()
    public initialData: SettingAddRemoveSingleFormComponentInitialData;

    public m: Model;

    async ngOnChanges(_: SimpleChanges) {
        this.m = null;

        const i = this.initialData;

        if (!i || i.setting?.Type !== 'AddRemoveRows') {
            return;
        }

        await this.reload(i.setting);
    }

    private async reload(setting: SettingModel) {
        const currentValues = (await this.apiService.apiService.shared.getCurrentSettingValues(setting.Code))
            .SettingValues;

        this.m = {
            setting: setting,
            current: {
                customValues: JSON.parse(currentValues['listOfNames']).map((x: string) => ({
                    rowInput: x,
                })),
            },
        };
    }

    edit(evt?: Event) {
        evt?.preventDefault();
        const f = new FormsHelper(this.formBuilder.group({}));

        let nextIndex = 0;
        const rows: {
            group: UntypedFormGroup;
            groupName: string;
        }[] = [];

        if (this.m.current.customValues.length == 0) {
            const row = this.addRowToForm(nextIndex++, f, '');
            rows.push(row);
        }

        for (const currentValue of this.m.current.customValues) {
            const row = this.addRowToForm(nextIndex++, f, currentValue.rowInput);
            rows.push(row);
        }
        f.setFormValidator((_) => {
            if (!this.m?.edit || f.invalid()) {
                return true;
            }
            const allInputRows = this.m.edit.customRows.map((x) => x.group.get('rowInput').value);
            return allInputRows.length == distinct(allInputRows).length;
        });
        this.m.edit = {
            form: f,
            nextIndex: nextIndex,
            customRows: rows,
        };
    }

    async commit(evt?: Event) {
        evt?.preventDefault();

        const f = this.m.edit.form;

        const values: string[] = [];
        for (const row of this.m.edit.customRows) {
            const rowInput = f.getFormGroupValue(row.groupName, 'rowInput');
            values.push(rowInput.toString());
        }

        const newValues: StringDictionary = { listOfNames: JSON.stringify(values) };

        await this.apiService.saveSettingValues(this.m.setting.Code, newValues);
        await this.reload(this.m.setting);
    }

    addRow(evt?: Event) {
        evt?.preventDefault();
        const e = this.m.edit;

        const customRow = this.addRowToForm(e.nextIndex++, e.form, null);
        e.customRows.push(customRow);
        this.m.edit.form.form.markAsUntouched();
    }

    cancel(evt?: Event) {
        evt?.preventDefault();
        this.m.edit = null;
    }

    removeRow(groupName: string, evt?: Event) {
        evt?.preventDefault();
        const index = this.m.edit.customRows.findIndex((x) => x.groupName == groupName);
        this.m.edit.customRows.splice(index, 1);
        this.m.edit.form.form.removeControl(groupName);
    }

    private addRowToForm(index: number, f: FormsHelper, initialInputRow: string) {
        const groupName = `g${index}`;
        const group = f.addGroupIfNotExists(groupName, [
            {
                controlName: 'rowInput',
                initialValue: initialInputRow == null ? '' : initialInputRow,
                validators: [Validators.required],
            },
        ]);

        return { group, groupName };
    }
}

interface Model {
    setting: SettingModel;
    current: {
        customValues: {
            rowInput: string;
        }[];
    };
    edit?: {
        nextIndex: number;
        customRows: {
            groupName: string;
            group: UntypedFormGroup;
        }[];
        form: FormsHelper;
    };
}

export interface SettingAddRemoveSingleFormComponentInitialData {
    setting: SettingModel;
}
