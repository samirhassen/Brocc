import { Component, Input, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { EditblockFormFieldModel } from 'src/app/shared-application-components/components/editblock-form-field/editblock-form-field.component';
import { StandardMortgageLoanApplicationModel } from '../../services/mortgage-loan-application-model';
import {
    GetValuationResult,
    PropertyValuation,
    UcBvValuationProcess,
    ValuationOverrides,
} from './ucbv-valuation-process';

@Component({
    selector: 'ucbv-valuation-buyer',
    templateUrl: './ucbv-valuation-buyer.component.html',
    styles: [],
})
export class UcbvValuationBuyerComponent {
    constructor(private fb: UntypedFormBuilder) {}

    @Input()
    public initialData: UcbvValuationBuyerComponentInitialData;

    public m: Model;

    ngOnChanges(changes: SimpleChanges) {
        if (this.m?.subs) {
            for (let sub of this.m.subs) {
                sub.unsubscribe();
            }
        }
        this.m = null;

        if (!this.initialData) {
            return;
        }

        this.resetForm(this.initialData.initialResult, null, null);
    }

    private resetForm(valuationResult: GetValuationResult, subs: Subscription[], overrides: ValuationOverrides) {
        if (subs) {
            for (let sub of this.m.subs) {
                sub.unsubscribe();
            }
        }
        let f = new FormsHelper(this.fb.group({}));

        let m: Model = {
            valuationProcess: this.initialData.valuationProcess,
            lastResult: valuationResult,
            form: f,
            subs: [],
            editFields: [],
            overrides: overrides,
            headerText: null,
        };

        if (valuationResult.resultCode === 'requireObjectType') {
            m.headerText = 'Is it an apartment?';
            m.editFields.push({
                getForm: () => f,
                formControlName: 'objectType',
                labelText: 'Is apartment',
                inEditMode: () => true,
                getOriginalValue: () => null,
                getValidators: () => [Validators.required],
                labelColCount: 3,
                dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption([
                    { Code: 'true', DisplayName: 'Yes' },
                    { Code: 'false', DisplayName: 'No' },
                ]),
            });
        } else if (valuationResult.resultCode === 'requireAddress') {
            m.headerText = 'What is the adress?';

            let adr = this.initialData.valuationProcess.getApplicationAddress();

            m.editFields.push({
                getForm: () => f,
                formControlName: 'street',
                labelText: 'Street',
                inEditMode: () => true,
                getOriginalValue: () => adr.street,
                getValidators: () => [],
                labelColCount: 3,
            });
            m.editFields.push({
                getForm: () => f,
                formControlName: 'zipCode',
                labelText: 'Zip code',
                inEditMode: () => true,
                getOriginalValue: () => adr.zipCode,
                getValidators: () => [Validators.required],
                labelColCount: 3,
            });
            m.editFields.push({
                getForm: () => f,
                formControlName: 'city',
                labelText: 'City',
                inEditMode: () => true,
                getOriginalValue: () => adr.city,
                getValidators: () => [Validators.required],
                labelColCount: 3,
            });
            m.editFields.push({
                getForm: () => f,
                formControlName: 'municipality',
                labelText: 'Municipality',
                inEditMode: () => true,
                getOriginalValue: () => adr.municipality,
                getValidators: () => [],
                labelColCount: 3,
            });
        } else if (valuationResult.resultCode === 'requireObjectChoice') {
            m.headerText = 'Pick object';
            m.objectChoice = {
                objects: valuationResult.requireObjectChoice.objects,
            };
        } else if (valuationResult.resultCode === 'requireApartmentNrChoice') {
            m.headerText = `Pick apartment (Förening: ${valuationResult.requireApartmentNrChoice.foreningName})`;
            m.apartmentChoice = {
                seTaxOfficeApartmentNrs: valuationResult.requireApartmentNrChoice.seTaxOfficeApartmentNrs,
            };
        }

        if (m.editFields.length > 0) {
            m.subs = EditblockFormFieldModel.setupForm(m.editFields, f);
        }

        this.m = m;
    }

    public continue(evt?: Event) {
        evt.preventDefault();

        let currentOverrides = this.m.overrides ?? {};
        let f = this.m.form;
        if (this.m.lastResult.resultCode === 'requireObjectType') {
            currentOverrides.isApartment = f.getValue('objectType') === 'true';
        } else if (this.m.lastResult.resultCode === 'requireAddress') {
            currentOverrides.address = {
                street: f.getValue('street'),
                zipCode: f.getValue('zipCode'),
                municipality: f.getValue('municipality'),
                city: f.getValue('city'),
            };
        }
        this.reRunValuation(currentOverrides);
    }

    pickApartmentNr(nr: string, evt?: Event) {
        evt?.preventDefault();

        let currentOverrides = this.m.overrides ?? {};

        currentOverrides.seTaxOfficeApartmentNr = nr;

        this.reRunValuation(currentOverrides);
    }

    public pickAddress(objectId: string, evt?: Event) {
        evt?.preventDefault();

        let currentOverrides = this.m.overrides ?? {};
        currentOverrides.objectId = objectId;

        this.reRunValuation(currentOverrides);
    }

    private reRunValuation(currentOverrides: ValuationOverrides) {
        this.initialData.valuationProcess.getValuation(currentOverrides).then((x) => {
            if (x.resultCode === 'success') {
                this.initialData.onValuationAccepted(x.valuation);
            } else {
                this.resetForm(x, this.m.subs, currentOverrides);
            }
        });
    }
}

class Model {
    form: FormsHelper;
    subs: Subscription[];
    valuationProcess: UcBvValuationProcess;
    lastResult: GetValuationResult;
    overrides?: ValuationOverrides;
    editFields: EditblockFormFieldModel[];
    headerText: string;
    objectChoice?: {
        objects: { Id: string; Name: string }[];
    };
    apartmentChoice?: {
        seTaxOfficeApartmentNrs: string[];
    };
}

export class UcbvValuationBuyerComponentInitialData {
    valuationProcess: UcBvValuationProcess;
    initialResult: GetValuationResult;
    application: StandardMortgageLoanApplicationModel;
    onValuationAccepted: (valuation: PropertyValuation) => void;
}
