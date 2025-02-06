import { Component, Input } from '@angular/core';
import { WorkflowStepInitialData } from '../workflow-step';

@Component({
    selector: 'payment-standard',
    templateUrl: './payment-standard.component.html',
    styles: [],
})
export class PaymentStandardMLComponent {
    constructor() {}
    @Input()
    public initialData: WorkflowStepInitialData;
}
