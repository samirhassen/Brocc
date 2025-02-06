import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { CalculatorSliderInitialData } from '../../../../shared-components/calculator-slider/calculator-slider.component';
import { MortgageLoanWebappSettings } from '../loan-calculator.component';

@Component({
    selector: 'calculator-tab-additionalloan',
    templateUrl: './calculator-tab-additionalloan.component.html',
    styles: [],
})
export class CalculatorTabAdditionalloanComponent implements OnInit {
    constructor(private fb: UntypedFormBuilder) {}

    public m: Model;

    @Input()
    public settings: MortgageLoanWebappSettings;

    @Output()
    public additionalLoanDataChanged = new EventEmitter<CalculatorAdditionalLoanData>();

    private subs: Subscription[] = [];

    ngOnInit(): void {
        let set = this.settings;
        let initialAdditionalLoanData: CalculatorAdditionalLoanData = {
            isValid: true,
            objectValueAmount: this.getStartValue(set.MinEstimatedValue, set.MaxEstimatedValue, 0.7),
            currentDebtAmount: this.getStartValue(
                set.MinCurrentMortgageLoanAmount,
                set.MaxCurrentMortgageLoanAmount,
                0.3
            ),
            paidToCustomerAmount: this.getStartValue(set.MinAdditionalLoanAmount, set.MaxAdditionalLoanAmount, 0.3),
        };

        let f = this.fb.group({
            objectValueAmount: [initialAdditionalLoanData.objectValueAmount, [Validators.required]],
            currentDebtAmount: [initialAdditionalLoanData.currentDebtAmount, [Validators.required]],
            paidToCustomerAmount: [initialAdditionalLoanData.paidToCustomerAmount, [Validators.required]],
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

        this.additionalLoanDataChanged.emit(initialAdditionalLoanData);

        this.subs.push(
            f.valueChanges.subscribe((_) => {
                let f = this.m.form;
                if (f.invalid()) {
                    this.additionalLoanDataChanged.emit({ isValid: false });
                } else {
                    this.additionalLoanDataChanged.emit({
                        isValid: true,
                        paidToCustomerAmount: parseInt(f.getValue('paidToCustomerAmount')),
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
    paidToCustomerAmountSliderData: CalculatorSliderInitialData;
    currentDebtAmountSliderData: CalculatorSliderInitialData;
    objectValueAmountSliderData: CalculatorSliderInitialData;
}

export class CalculatorAdditionalLoanData {
    isValid: boolean;
    paidToCustomerAmount?: number;
    currentDebtAmount?: number;
    objectValueAmount?: number;
}
