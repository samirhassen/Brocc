import { FormsHelper } from "src/app/common-services/ntech-forms-helper";
import { SettingFormDataFieldModel } from "src/app/settings/services/settings-model";

export interface FieldTypeHandler {
    formatStoredValueForDisplay(storedValue: string, field: SettingFormDataFieldModel): string;
    parseFormValueForStorage(formValue: string): string;
    addToForm(
        field: SettingFormDataFieldModel,
        form: FormsHelper,
        editFields: EditViewModel[],
        storedValue: string
    ): void;
}

export class EditViewModel {
    constructor(public labelText: string, public controlType: string, public formControlName: string) {}
    public dropdownOptions: { Code: string; DisplayName: string }[];
    public nrOfRows?: number;
}