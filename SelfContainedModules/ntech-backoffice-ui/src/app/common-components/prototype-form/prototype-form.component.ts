import { Component, EventEmitter, Input, Output, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder } from '@angular/forms';
import { Subscription } from 'rxjs';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { Dictionary } from 'src/app/common.types';
import { EditblockFormFieldModel } from '../../shared-application-components/components/editblock-form-field/editblock-form-field.component';

@Component({
    selector: 'prototype-form',
    templateUrl: './prototype-form.component.html',
    styles: [],
})
export class PrototypeFormComponent {
    constructor(private fb: UntypedFormBuilder) {}

    @Input()
    public fields: Dictionary<string>;

    @Output()
    public go: EventEmitter<Dictionary<string>> = new EventEmitter();

    public m: Model;

    ngOnChanges(changes: SimpleChanges) {
        if (this.m?.subs) {
            for (let sub of this.m.subs) {
                sub.unsubscribe();
            }
        }

        this.m = null;

        if (!this.fields) {
            return;
        }

        let form = new FormsHelper(this.fb.group({}));

        let m: Model = {
            form: form,
            editFields: [],
            subs: [],
        };

        for (let fieldName of Object.keys(this.fields)) {
            m.editFields.push({
                getForm: () => form,
                formControlName: fieldName,
                labelText: fieldName,
                inEditMode: () => true,
                getOriginalValue: () => this.fields[fieldName],
                getValidators: () => [],
            });
        }

        m.subs = EditblockFormFieldModel.setupForm(m.editFields, m.form);

        this.m = m;
    }

    doGo(evt?: Event) {
        let newValue: Dictionary<string> = {};

        for (let field of this.m.editFields) {
            newValue[field.formControlName] = this.m.form.getValue(field.formControlName);
        }

        this.go.emit(newValue);
    }
}

class Model {
    form: FormsHelper;
    editFields: EditblockFormFieldModel[];
    subs: Subscription[];
}
