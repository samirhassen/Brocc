import { Component, Input, SimpleChanges } from '@angular/core';
import { Router } from '@angular/router';
import { WorkflowHelper } from 'src/app/shared-application-components/services/workflow-helper';
import { MortgageLoanApplicationApiService } from '../../services/mortgage-loan-application-api.service';
import { StandardMortgageLoanApplicationModel } from '../../services/mortgage-loan-application-model';
import { WorkflowStepInitialData } from '../workflow-step';

@Component({
    selector: 'initial-credit-check-standard',
    templateUrl: './initial-credit-check-standard.component.html',
    styles: [],
})
export class InitialCreditCheckStandardMLComponent {
    constructor(private router: Router, private apiService: MortgageLoanApplicationApiService) {}

    @Input()
    public initialData: WorkflowStepInitialData;

    public m: Model;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        this.m = {
            application: this.initialData.application,
        };
    }

    private hasAgreement() {
        return !WorkflowHelper.isStepInitial('Agreement', this.initialData.application.applicationInfo);
    }

    isNewCreditCheckPossibleButNeedsReactivate(): boolean {
        let a = this.initialData.application.applicationInfo;

        let wf = this.initialData.workflow;

        return (
            !a.IsActive &&
            !a.IsFinalDecisionMade &&
            (a.IsCancelled || a.IsRejected) &&
            !this.hasAgreement() &&
            wf.step.areAllStepsAfterInitial()
        );
    }

    isNewCreditCheckPossible() {
        let application = this.initialData.application;
        let a = application.applicationInfo;

        let wf = this.initialData.workflow;
        let isActiveAndOk = a.IsActive && !this.hasAgreement() && wf.step.areAllStepsAfterInitial();

        return isActiveAndOk || this.isNewCreditCheckPossibleButNeedsReactivate();
    }

    isViewCreditCheckDetailsPossible() {
        if (!this.initialData) {
            return false;
        }
        return !this.initialData.workflow.step.isStatusInitial();
    }

    newCreditCheck(evt?: Event) {
        evt?.preventDefault();

        if (!this.initialData.application.applicationInfo.IsActive) {
            this.apiService.reactivateCancelledApplication(this.initialData.application.applicationNr).then((x) => {
                this.router.navigate([
                    '/mortgage-loan-application/new-credit-check',
                    this.initialData.application.applicationNr,
                ]);
            });
            return;
        }

        this.router.navigate([
            '/mortgage-loan-application/new-credit-check',
            this.initialData.application.applicationNr,
        ]);
    }

    viewCreditCheckDetails(evt?: Event) {
        evt?.preventDefault();

        this.router.navigate([
            '/mortgage-loan-application/view-credit-check',
            this.initialData.application.applicationNr,
        ]);
    }
}

class Model {
    application: StandardMortgageLoanApplicationModel;
}
