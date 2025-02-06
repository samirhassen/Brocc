import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { Router } from '@angular/router';
import { WorkflowHelper } from 'src/app/shared-application-components/services/workflow-helper';
import { StandardCreditApplicationModel } from '../../services/standard-credit-application-model';
import { UnsecuredLoanApplicationApiService } from '../../services/unsecured-loan-application-api.service';
import { WorkflowStepInitialData } from '../workflow-step';

@Component({
    selector: 'credit-check-standard',
    templateUrl: './credit-check-standard.component.html',
    styles: [],
})
export class CreditCheckStandardComponent implements OnInit {
    constructor(private router: Router, private apiService: UnsecuredLoanApplicationApiService) {}

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
        return !a.IsActive && !a.IsFinalDecisionMade && (a.IsCancelled || a.IsRejected) && !this.hasAgreement();
    }

    isNewCreditCheckPossible() {
        let application = this.initialData.application;
        let a = application.applicationInfo;
        let hasCustomerOfferDecision =
            application.hasCurrentCreditDecision() &&
            application.getUniqueCurrentCreditDecisionItem('customerDecisionCode') !== 'initial';

        let isActiveAndOk = a.IsActive && !this.hasAgreement() && !hasCustomerOfferDecision;
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
            //Inactive application reactivated when new credit check is done
            this.apiService.reactivateCancelledApplication(this.initialData.application.applicationNr).then((x) => {
                this.router.navigate([
                    '/unsecured-loan-application/new-credit-check',
                    this.initialData.application.applicationNr,
                ]);
            });
            return;
        }

        this.router.navigate([
            '/unsecured-loan-application/new-credit-check',
            this.initialData.application.applicationNr,
        ]);
    }

    viewCreditCheckDetails(evt?: Event) {
        evt?.preventDefault();

        this.router.navigate([
            '/unsecured-loan-application/view-credit-check',
            this.initialData.application.applicationNr,
        ]);
    }
}

class Model {
    application: StandardCreditApplicationModel;
}
