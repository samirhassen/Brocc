import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { CalculatorSliderInitialData } from '../../../../shared-components/calculator-slider/calculator-slider.component';
import { MortgageLoanWebappSettings } from '../loan-calculator.component';

@Component({
    selector: 'calculator-tab-move',
    templateUrl: './calculator-tab-move.component.html',
    styles: [],
})
export class CalculatorTabMoveComponent implements OnInit {
    constructor(private fb: UntypedFormBuilder) {}

    public m: Model;

    @Input()
    public settings: MortgageLoanWebappSettings;

    @Output()
    public moveDataChanged = new EventEmitter<CalculatorMoveData>();

    private subs: Subscription[] = [];

    ngOnInit(): void {
        if (!this.settings) {
            return;
        }

        let set = this.settings;
        let initialMoveData: CalculatorMoveData = {
            isValid: true,
            objectValueAmount: this.getStartValue(set.MinEstimatedValue, set.MaxEstimatedValue, 0.7),
            currentDebtAmount: this.getStartValue(
                set.MinCurrentMortgageLoanAmount,
                set.MaxCurrentMortgageLoanAmount,
                0.3
            ),
            paidToCustomerAmount: 0,
        };

        let f = this.fb.group({
            objectValueAmount: [initialMoveData.objectValueAmount, [Validators.required]],
            currentDebtAmount: [initialMoveData.currentDebtAmount, [Validators.required]],
            paidToCustomerAmount: [initialMoveData.paidToCustomerAmount, [Validators.required]],
            isPayoutRequested: [false, []], //This is just used to hide/show the paidToCustomerAmount slider, it has no semanti meaning in the model
        });

        let form = new FormsHelper(f);

        this.m = {
            form: form,
            currentDebtAmountSliderData: {
                minValue: set.MinCurrentMortgageLoanAmount,
                maxValue: set.MaxCurrentMortgageLoanAmount,
                stepSize: 10000,
                tickButtonStepSize: 1000,
                formControlName: 'currentDebtAmount',
                form: form,
            },
            objectValueAmountSliderData: {
                minValue: set.MinEstimatedValue,
                maxValue: set.MaxEstimatedValue,
                stepSize: 10000,
                tickButtonStepSize: 1000,
                formControlName: 'objectValueAmount',
                form: form,
            },
            paidToCustomerAmountSliderData: {
                minValue: set.MinAdditionalLoanAmount,
                maxValue: set.MaxAdditionalLoanAmount,
                stepSize: 1000,
                tickButtonStepSize: 1000,
                formControlName: 'paidToCustomerAmount',
                form: form,
            },
        };

        this.moveDataChanged.emit(initialMoveData);

        this.subs.push(
            f.valueChanges.subscribe((_) => {
                let f = this.m.form;
                let isChecked = f.getValue('isPayoutRequested');

                if (f.invalid()) {
                    this.moveDataChanged.emit({ isValid: false });
                } else {
                    this.moveDataChanged.emit({
                        isValid: true,
                        paidToCustomerAmount: isChecked ? parseInt(f.getValue('paidToCustomerAmount')) : 0,
                        objectValueAmount: parseInt(f.getValue('objectValueAmount')),
                        currentDebtAmount: parseInt(f.getValue('currentDebtAmount')),
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
    objectValueAmountSliderData: CalculatorSliderInitialData;
    currentDebtAmountSliderData: CalculatorSliderInitialData;
    paidToCustomerAmountSliderData: CalculatorSliderInitialData;
}

export class CalculatorMoveData {
    isValid: boolean;
    objectValueAmount?: number;
    currentDebtAmount?: number;
    paidToCustomerAmount?: number;
}
