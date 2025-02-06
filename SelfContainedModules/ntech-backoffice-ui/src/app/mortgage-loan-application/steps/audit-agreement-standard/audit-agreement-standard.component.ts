import { Component, Input } from '@angular/core';
import { WorkflowStepInitialData } from '../workflow-step';

@Component({
    selector: 'audit-agreement-standard',
    templateUrl: './audit-agreement-standard.component.html',
    styles: [],
})
export class AuditAgreementStandardMLComponent {
    constructor() {}
    @Input()
    public initialData: WorkflowStepInitialData;
}
