import { Component, Input } from '@angular/core';
import { WorkflowStepInitialData } from '../workflow-step';

@Component({
    selector: 'agreement-standard',
    templateUrl: './agreement-standard.component.html',
    styles: [],
})
export class AgreementStandardMLComponent {
    constructor() {}
    @Input()
    public initialData: WorkflowStepInitialData;
}
