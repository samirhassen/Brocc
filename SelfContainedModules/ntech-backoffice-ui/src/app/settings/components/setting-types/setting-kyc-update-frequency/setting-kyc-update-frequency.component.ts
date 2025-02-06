import { Component, Input, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, UntypedFormGroup, Validators } from '@angular/forms';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { distinct, StringDictionary } from 'src/app/common.types';
import { SettingsApiService } from 'src/app/settings/services/settings-api.service';
import { SettingModel } from 'src/app/settings/services/settings-model';

const defaultName = '__default__';

@Component({
    selector: 'setting-kyc-update-frequency',
    templateUrl: './setting-kyc-update-frequency.component.html',
    styleUrls: ['./setting-kyc-update-frequency.component.scss']
})
export class SettingKycUpdateFrequencyComponent {
    constructor(private formBuilder: UntypedFormBuilder, private validationService: NTechValidationService,
        private apiService: SettingsApiService) { }

    @Input()
    public initialData: SettingKycUpdateFrequencyComponentInitialData;

    public m: Model;

    async ngOnChanges(_: SimpleChanges) {
        this.m = null;

        let i = this.initialData;

        if (!i || i.setting?.Type !== 'KycUpdateFrequency')  {
            return;
        }

        await this.reload(i.setting);
    }

    private async reload(setting: SettingModel) {
        let currentValues = (await this.apiService.apiService.shared.getCurrentSettingValues(setting.Code)).SettingValues;
        let defaultMonthCount = parseInt(currentValues[defaultName]);
        let customKeys = Object.keys(currentValues).filter(x => x !== defaultName);

        this.m = {
            setting: setting,
            current:  {
                defaultMonthCount: defaultMonthCount,
                customValues: customKeys.map(x => ({
                    riskClass: x,
                    monthCount: parseInt(currentValues[x])
                }))
            }
        };
    }
    
    edit(evt?: Event) {
        evt?.preventDefault();
        let f = new FormsHelper(this.formBuilder.group({
            'defaultMonthCount': [this.m.current.defaultMonthCount.toString(), [Validators.required, 
                this.validationService.getPositiveIntegerValidator()]],
        }));

        let nextIndex = 0
        let customRows: {
            group: UntypedFormGroup
            groupName: string
        }[] = [];
        for(let currentValue of this.m.current.customValues) {
            let customRow = this.addRowToForm(nextIndex++, f,currentValue.riskClass, currentValue.monthCount);
            customRows.push(customRow);
        }
        f.setFormValidator(_ => {
            if(!this.m?.edit || f.invalid()) {
                return true;
            }
            let allRiskClasses = this.m.edit.customRows.map(x => x.group.get('riskClass').value);
            return allRiskClasses.length == distinct(allRiskClasses).length;
        }, 'duplicateRiskClass');
        this.m.edit = {
            form: f,
            nextIndex: nextIndex,
            customRows: customRows
        };
    }

    hasDuplicateRiskClass() {
        return !!this.m.edit?.form?.form?.errors?.duplicateRiskClass;
    }

    async commit(evt?: Event) {
        evt?.preventDefault();

        let f = this.m.edit.form;

        let defaultMonthCount = parseInt(f.getValue('defaultMonthCount'));
        let newValues : StringDictionary = {};
        newValues[defaultName] = defaultMonthCount.toString();
        for(let row of this.m.edit.customRows) {
            let monthCount = parseInt(f.getFormGroupValue(row.groupName, 'monthCount'));
            let riskClass = f.getFormGroupValue(row.groupName, 'riskClass');
            newValues[riskClass] = monthCount.toString();
        }

        await this.apiService.saveSettingValues(this.m.setting.Code, newValues);
        await this.reload(this.m.setting);
    }

    addRow(evt?: Event) {
        evt?.preventDefault();
        let e = this.m.edit;

        let customRow = this.addRowToForm(e.nextIndex++, e.form, null, null);
        e.customRows.push(customRow);
        this.m.edit.form.form.markAsUntouched();
    }

    cancel(evt?: Event) {
        evt?.preventDefault();
        this.m.edit = null;
    }

    removeRow(groupName: string, evt ?: Event) {
        evt?.preventDefault();
        let index = this.m.edit.customRows.findIndex(x => x.groupName == groupName);
        this.m.edit.customRows.splice(index, 1);
        this.m.edit.form.form.removeControl(groupName);
    }

    private addRowToForm(index: number, f: FormsHelper, initialRiskClass: string, initialMonthCount: number) {
        let groupName = `g${index}`;
        let group = f.addGroupIfNotExists(groupName, [{
            controlName: 'riskClass',
            initialValue: initialRiskClass == null ? '' : initialRiskClass,
            validators: [Validators.required, this.validationService.getValidator('riskClassDefaultName', x => {
                return x !== defaultName;
            })],
        }, {
            controlName: 'monthCount',
            initialValue: initialMonthCount == null ? '' : initialMonthCount.toString(),
            validators: [Validators.required, this.validationService.getPositiveIntegerValidator()],
        }]);        

        return { group, groupName };
    }
}

interface Model {
    setting: SettingModel
    current: {
        defaultMonthCount: number,
        customValues: {
            riskClass: string,
            monthCount: number
        }[]
    }
    edit?: {
        nextIndex: number
        customRows: {
            groupName: string
            group: UntypedFormGroup
        }[]
        form: FormsHelper
    }
}

export interface SettingKycUpdateFrequencyComponentInitialData {
    setting: SettingModel
}