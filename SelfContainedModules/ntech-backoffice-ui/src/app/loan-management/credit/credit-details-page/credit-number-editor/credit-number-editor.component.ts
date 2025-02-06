import { Component, Input, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';

@Component({
    selector: 'credit-number-editor',
    templateUrl: './credit-number-editor.component.html',
    styles: [],
})
export class CreditNumberEditorComponent {
    constructor(private fb: UntypedFormBuilder, private validationService: NTechValidationService) {}

    ngOnChanges(changes: SimpleChanges) {
        this.reload();
    }

    public m: Model;

    @Input()
    public initialData: CreditNumberEditorInitialData;

    private async reload() {
        this.m = null;

        if (!this.initialData) {
            return;
        }
        let currentValue = this.initialData.initialValue;

        let f = new FormsHelper(
            this.fb.group({
                editedValue: [
                    this.validationService.formatDecimalForEdit(currentValue),
                    [Validators.required, this.validationService.getPositiveDecimalValidator()],
                ],
            })
        );

        this.m = {
            form: f,
            isEditing: false,
            currentValue: currentValue,
            isMissingCurrentValue: currentValue === null,
            isReadOnly: this.initialData.isReadOnly,
        };
    }

    beginEdit(evt?: Event) {
        evt?.preventDefault();
        this.m.isEditing = true;
    }

    cancelEdit(evt?: Event) {
        evt?.preventDefault();
        this.m.isEditing = false;
        this.m.form.setValue('editedValue', this.validationService.formatDecimalForEdit(this.m.currentValue));
    }

    async commitEdit(evt?: Event) {
        evt?.preventDefault();

        let editedValue = this.validationService.parsePositiveDecimalOrNull(this.m.form.getValue('editedValue'));
        let { newValue } = await this.initialData.save(editedValue);

        this.m.currentValue = newValue;
        this.m.isEditing = false;
        this.m.isMissingCurrentValue = false;
        this.m.form.setValue('editedValue', this.validationService.formatDecimalForEdit(this.m.currentValue));
    }
}

class Model {
    currentValue: number;
    form: FormsHelper;
    isEditing: boolean;
    isMissingCurrentValue: boolean;
    isReadOnly: boolean;
}

export interface CreditNumberEditorInitialData {
    labelText: string;
    initialValue: number | null;
    save: (newValue: number) => Promise<{ newValue: number }>;
    isReadOnly: boolean;
}
