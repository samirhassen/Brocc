import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { CalculatorSliderInitialData } from '../../../../shared-components/calculator-slider/calculator-slider.component';
import { Subscription } from 'rxjs';
import { MortgageLoanWebappSettings } from '../loan-calculator.component';

@Component({
    selector: 'calculator-tab-purchase',
    templateUrl: './calculator-tab-purchase.component.html',
    styles: [],
})
export class CalculatorTabPurchaseComponent implements OnInit {
    constructor(private fb: UntypedFormBuilder) {}

    public m: Model;

    @Input()
    public settings: MortgageLoanWebappSettings;

    @Output()
    public purchaseDataChanged = new EventEmitter<CalculatorPurchaseData>();

    private subs: Subscription[] = [];

    ngOnInit(): void {
        let set = this.settings;
        let initialPurchaseData: CalculatorPurchaseData = {
            isValid: true,
            objectPriceAmount: this.getStartValue(set.MinEstimatedValue, set.MaxEstimatedValue, 0.7),
            ownSavingsAmount: this.getStartValue(set.MinCashAmount, set.MaxCashAmount, 0.3),
        };

        let f = this.fb.group({
            objectPriceAmount: [initialPurchaseData.objectPriceAmount, [Validators.required]],
            ownSavingsAmount: [initialPurchaseData.ownSavingsAmount, [Validators.required]],
        });

        let form = new FormsHelper(f);
        this.m = {
            form: form,
            objectPriceSliderData: {
                minValue: set.MinEstimatedValue,
                maxValue: set.MaxEstimatedValue,
                stepSize: 10000,
                tickButtonStepSize: 1000,
                formControlName: 'objectPriceAmount',
                form: form,
            },
            ownSavingsSliderData: {
                minValue: set.MinCashAmount,
                maxValue: set.MaxCashAmount,
                stepSize: 1000,
                tickButtonStepSize: 1000,
                formControlName: 'ownSavingsAmount',
                form: form,
            },
        };

        this.purchaseDataChanged.emit(initialPurchaseData);

        this.subs.push(
            f.valueChanges.subscribe((_) => {
                let f = this.m.form;
                if (f.invalid()) {
                    this.purchaseDataChanged.emit({ isValid: false });
                } else {
                    this.purchaseDataChanged.emit({
                        isValid: true,
                        objectPriceAmount: parseInt(f.getValue('objectPriceAmount')),
                        ownSavingsAmount: parseInt(f.getValue('ownSavingsAmount')),
                    });
                }
            })
        );
    }

    private getStartValue(min: number, max: number, multi: number) {
        return min + (max - min) * multi;
    }

    ngOnDestroy() {
        if (this.subs) {
            for (let sub of this.subs) {
                sub.unsubscribe();
                this.subs = [];
            }
        }
    }
}

class Model {
    form: FormsHelper;
    objectPriceSliderData: CalculatorSliderInitialData;
    ownSavingsSliderData: CalculatorSliderInitialData;
}

export class CalculatorPurchaseData {
    isValid: boolean;
    objectPriceAmount?: number;
    ownSavingsAmount?: number;
}
