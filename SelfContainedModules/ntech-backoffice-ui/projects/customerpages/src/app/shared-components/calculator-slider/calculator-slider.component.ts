import { LabelType, Options } from 'ngx-slider-v2';
import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';

@Component({
    selector: 'calculator-slider',
    templateUrl: './calculator-slider.component.html',
})
export class CalculatorSliderComponent implements OnInit {
    constructor() {}

    @Input()
    public initialData: CalculatorSliderInitialData;

    public m: Model;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let i = this.initialData;
        this.m = {
            sliderOptions: {
                floor: i.minValue,
                ceil: i.maxValue,
                step: i.stepSize,
                showSelectionBar: true,
                translate: (value: number, label: LabelType): string => {
                    return ''; //Hide the label above the pointer
                },
            },
            formControlName: i.formControlName,
            form: i.form,
        };
    }

    tickSlider(isUp: boolean) {
        let v = parseInt(this.m.form.getValue(this.m.formControlName));
        v = v + (isUp ? 1 : -1) * (this.initialData.tickButtonStepSize ?? this.initialData.stepSize);
        this.m.form.setValue(this.m.formControlName, v.toString());
    }
}

export class CalculatorSliderInitialData {
    minValue: number;
    maxValue: number;
    stepSize: number;
    //Causes the button to tick up more than just whatever the step is
    tickButtonStepSize ?: number;
    formControlName: string;
    form: FormsHelper;
}

class Model {
    formControlName: string;
    sliderOptions: Options;
    form: FormsHelper;
}
