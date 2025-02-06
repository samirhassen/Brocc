import { Component, Input } from '@angular/core';
import { WorkflowStepInitialData } from '../workflow-step';

@Component({
    selector: 'final-credit-check-standard',
    templateUrl: './final-credit-check-standard.component.html',
    styles: [],
})
export class FinalCreditCheckStandardMLComponent {
    constructor() {}
    @Input()
    public initialData: WorkflowStepInitialData;
}
