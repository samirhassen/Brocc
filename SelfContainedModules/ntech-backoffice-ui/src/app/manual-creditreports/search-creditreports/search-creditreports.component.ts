import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, UntypedFormControl, Validators } from '@angular/forms';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';

@Component({
    selector: 'app-search-creditreports',
    templateUrl: './search-creditreports.component.html',
    styles: [],
})
export class SearchCreditreportsComponent implements OnInit {
    constructor(private formBuilder: UntypedFormBuilder, private validationService: NTechValidationService) {}

    public m: Model = null;

    @Input()
    public initialData: SearchInitialData;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let entityForm = this.formBuilder.group({
            entityType: [this.initialData.allowPerson ? 'person' : this.initialData.allowCompany ? 'company' : '', []],
        });
        this.m = {
            allowPerson: this.initialData.allowPerson,
            allowCompany: this.initialData.allowCompany,
            entityForm: new FormsHelper(entityForm),
        };
        let updateEntityFormOnChange = () => {
            let entityType: string = entityForm.value.entityType;
            if (entityType === 'person' && !entityForm.contains('civicRegNr')) {
                if (entityForm.contains('orgnr')) {
                    entityForm.removeControl('orgnr');
                }
                entityForm.addControl(
                    'civicRegNr',
                    new UntypedFormControl('', [Validators.required, this.validationService.getCivicRegNrValidator()])
                );
            } else if (entityType === 'company' && !entityForm.contains('orgnr')) {
                if (entityForm.contains('civicRegNr')) {
                    entityForm.removeControl('civicRegNr');
                }
                entityForm.addControl(
                    'orgnr',
                    new UntypedFormControl('', [Validators.required, this.validationService.getOrgNrValidator()])
                );
            }
        };
        updateEntityFormOnChange();
        this.m.entityForm.form.valueChanges.subscribe((x) => {
            updateEntityFormOnChange();
        });
    }

    search(evt?: Event) {
        evt?.preventDefault();
        let isCompany = this.m.entityForm.getValue('entityType') === 'company';
        let civicRegNrOrOrgnr = this.m.entityForm.getValue(isCompany ? 'orgnr' : 'civicRegNr');
        this.initialData.onSearch({ isCompany: isCompany, civicRegNrOrOrgnr: civicRegNrOrOrgnr });
    }
}

class Model {
    entityForm: FormsHelper;
    allowCompany: boolean;
    allowPerson: boolean;
}

export class SearchInitialData {
    allowCompany: boolean;
    allowPerson: boolean;
    onSearch: (data: { isCompany: boolean; civicRegNrOrOrgnr: string }) => void;
}
