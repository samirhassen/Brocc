import { Component, Input, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { NumberDictionary } from 'src/app/common.types';
import { FixedRateService, RateServerModel } from '../../services/fixed-rate-service';

@Component({
    selector: 'rate-editor',
    templateUrl: './rate-editor.component.html',
    styleUrls: ['./rate-editor.component.scss'],
})
export class RateEditorComponent {
    constructor(
        private fb: UntypedFormBuilder,
        private validationService: NTechValidationService,
        private fixedRateService: FixedRateService
    ) {}

    @Input()
    public initialData: RateEditorComponentInitialData;

    public m: Model;

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let addForm = new FormsHelper(
            this.fb.group({
                years: ['', [Validators.required]],
                rate: ['', [Validators.required, this.validationService.getDecimalValidator()]],
            })
        );

        let m: Model = {
            addRateYearOptions: [],
            addForm: addForm,
            editedRates: [],
            editForm: new FormsHelper(this.fb.group({})),
        };
        for (let i = 1; i <= 40; i++) {
            m.addRateYearOptions.push(i);
        }

        if (this.initialData.currentRates.length > 0) {
            let isRemoveAllowed = false;
            for (let currentRate of this.initialData.currentRates) {
                this.addEditedMonth(currentRate.MonthCount, m, isRemoveAllowed, currentRate.RatePercent);
                isRemoveAllowed = true;
            }
        } else {
            this.addEditedMonth(this.fixedRateService.getFallbackMonthCount(), m, false, null);
        }

        for (let years of m.addRateYearOptions) {
            if (!this.isEditedRate(years * 12, m)) {
                m.addForm.setValue('years', years.toString());
                break;
            }
        }

        this.m = m;
    }

    addEditedMonth(monthCount: number, m: Model, isRemoveAllowed: boolean, initialRate: number | null) {
        if (m.editedRates.find((x) => x.monthCount === monthCount)) {
            return;
        }

        if (m.editedRates.find((x) => x.monthCount === monthCount)) {
            return;
        }
        let formControlName = `rate${monthCount}`;
        m.editForm.addControlIfNotExists(
            formControlName,
            initialRate === null ? '' : this.validationService.formatDecimalForEdit(initialRate),
            [Validators.required, this.validationService.getPositiveDecimalValidator()]
        );

        let newRate = {
            formControlName,
            monthCount,
            isRemoveAllowed,
        };
        let placeBeforeIndex = m.editedRates.findIndex((x) => x.monthCount > monthCount);
        if (placeBeforeIndex < 0) {
            //Place last [..., <newGoesHere>]
            m.editedRates.push(newRate);
        } else if (placeBeforeIndex === 0) {
            //Place first [<newGoesHere>, ...] which is not allowed
            throw new Error('Guard code failed. Managed to attempt replacing the lowest entry');
        } else {
            //Place in the middle [..., <newGoesHere>, ...]
            m.editedRates = [
                ...m.editedRates.slice(0, placeBeforeIndex),
                newRate,
                ...m.editedRates.slice(placeBeforeIndex),
            ];
        }
    }

    public parseMonthCount(nrOfMonths: number) {
        return this.fixedRateService.parseMonthCountShared(nrOfMonths);
    }

    public isEditedRate(monthCount: number, m: Model) {
        if (!m?.editedRates) {
            return false;
        }
        return !!m.editedRates.find((x) => x.monthCount === monthCount);
    }

    public addRate(evt?: Event) {
        evt?.preventDefault();

        let f = this.m.addForm;
        let years = parseInt(f.getValue('years'));
        let newRate = this.validationService.parseDecimalOrNull(f.getValue('rate'), true);

        this.addEditedMonth(years * 12, this.m, true, newRate);
        let maxYears = Math.max(...this.m.editedRates.map((x) => x.monthCount)) / 12;
        this.m.addForm.setValue('years', (maxYears + 1).toString());
    }

    public removeRate(monthCount: number, evt?: Event) {
        evt?.preventDefault();

        let index = this.m.editedRates.findIndex((x) => x.monthCount === monthCount);
        let removedItem = this.m.editedRates[index];
        this.m.editedRates.splice(index, 1);
        this.m.editForm.form.removeControl(removedItem.formControlName);
    }

    public initiateChange(evt?: Event) {
        evt?.preventDefault();

        let rateByMonthCount: NumberDictionary<number> = {};
        for (let r of this.m.editedRates) {
            rateByMonthCount[r.monthCount] = this.validationService.parseDecimalOrNull(
                this.m.editForm.getValue(r.formControlName),
                true
            );
        }
        this.initialData.onInitiateChange(rateByMonthCount);
    }
}

interface Model {
    addRateYearOptions: number[];
    addForm: FormsHelper;
    editForm: FormsHelper;
    editedRates: { monthCount: number; formControlName: string; isRemoveAllowed: boolean }[];
}

export interface RateEditorComponentInitialData {
    currentRates: RateServerModel[];
    onInitiateChange: (rateByMonthCount: NumberDictionary<number>) => void;
}
