import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { WorkflowStepHelper } from 'src/app/shared-application-components/services/workflow-helper';

@Component({
    selector: 'step-status-block',
    templateUrl: './step-status-block.component.html',
    styles: [],
})
export class StepStatusBlockComponent implements OnChanges {
    @Input()
    public initialData: StepStatusBlockInitialData;

    public isExpanded: boolean;

    constructor() {}

    ngOnChanges(changes: SimpleChanges) {
        if (this.initialData) {
            this.isExpanded = this.initialData.isInitiallyExpanded;
        } else {
            this.isExpanded = false;
        }
    }

    headerClassFromStatus() {
        let step = this.initialData?.step;
        return this.getHeaderClass(step?.isStatusAccepted(), step?.isStatusRejected(), this.initialData.isActive);
    }

    getHeaderClass(isAccepted: boolean, isRejected: boolean, isActive: boolean) {
        let isOther = !isAccepted && !isRejected;
        return { 'text-success': isAccepted, 'text-danger': isRejected, 'text-ntech-inactive': isOther && !isActive };
    }

    public isAccepted() {
        return this.initialData?.step?.isStatusAccepted();
    }

    public isRejected() {
        return this.initialData?.step?.isStatusRejected();
    }

    toggleExpanded(evt?: Event) {
        if (evt) {
            evt.preventDefault();
        }
        this.isExpanded = !this.isExpanded;
    }
}

export class StepStatusBlockInitialData {
    step: WorkflowStepHelper;
    isInitiallyExpanded: boolean;
    isActive: boolean;
}
