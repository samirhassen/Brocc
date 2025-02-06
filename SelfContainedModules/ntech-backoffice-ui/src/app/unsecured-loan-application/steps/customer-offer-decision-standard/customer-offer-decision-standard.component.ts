import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder } from '@angular/forms';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { StandardCreditApplicationModel } from '../../services/standard-credit-application-model';
import { UnsecuredLoanApplicationApiService } from '../../services/unsecured-loan-application-api.service';
import { WorkflowStepInitialData } from '../workflow-step';

@Component({
    selector: 'customer-offer-decision-standard',
    templateUrl: './customer-offer-decision-standard.component.html',
    styles: [],
})
export class CustomerOfferDecisionStandardComponent implements OnInit {
    constructor(
        private fb: UntypedFormBuilder,
        private apiService: UnsecuredLoanApplicationApiService,
        private eventService: NtechEventService
    ) {}

    @Input()
    public initialData: WorkflowStepInitialData;

    public m: Model;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let application = this.initialData.application;
        let ai = application.applicationInfo;

        let customerDecisionCode = application.getUniqueCurrentCreditDecisionItem('customerDecisionCode');
        this.m = {
            application: application,
            currentCustomerDecisionCode: customerDecisionCode,
            hasAcceptedOffer: customerDecisionCode === 'accepted',
            hasRejectedOffer: customerDecisionCode === 'rejected',
            isPossibleToApprove:
                customerDecisionCode === 'accepted' &&
                ai.IsActive &&
                this.initialData.workflow.step.areAllStepBeforeThisAccepted() &&
                this.initialData.workflow.step.isStatusInitial(),
            isPossibleToRevert: this.initialData.workflow.step.isRevertable(),
            customerDecisionForm: new FormsHelper(
                this.fb.group({
                    customerDecisionCode: ['', []],
                })
            ),
            isEditDecisionAllowed:
                ai.IsActive &&
                this.initialData.workflow.step.isStatusInitial() &&
                application.hasCurrentCreditDecision(),
        };

        this.m.customerDecisionForm.form.valueChanges.subscribe((_) => {
            if (!this.m?.isEditDecisionAllowed === true) {
                return;
            }
            let newValue = this.m?.customerDecisionForm?.getValue('customerDecisionCode');
            if (newValue != this.m.currentCustomerDecisionCode) {
                this.apiService.setCustomerCreditDecisionCode(application.applicationNr, newValue).then((x) => {
                    this.eventService.signalReloadApplication(application.applicationNr);
                });
            }
        });
    }

    iconClassFromStatus() {
        return this.getIconClass(this.m?.hasAcceptedOffer === true, this.m?.hasRejectedOffer === true);
    }

    private getIconClass(isAccepted: boolean, isRejected: boolean) {
        let isOther = !isAccepted && !isRejected;
        return {
            'glyphicon-ok': isAccepted,
            'glyphicon-remove': isRejected,
            'glyphicon-minus': isOther,
            'glyphicon': true,
            'text-success': isAccepted,
            'text-danger': isRejected,
        };
    }

    approve(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.initialData.application.applicationNr;
        this.apiService.setIsApprovedCustomerOfferDecisionStep(applicationNr, true).then((x) => {
            this.eventService.signalReloadApplication(applicationNr);
        });
    }

    revert(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.initialData.application.applicationNr;
        this.apiService.setIsApprovedCustomerOfferDecisionStep(applicationNr, false).then((x) => {
            this.eventService.signalReloadApplication(applicationNr);
        });
    }
}

class Model {
    application: StandardCreditApplicationModel;
    currentCustomerDecisionCode: string;
    hasAcceptedOffer?: boolean;
    hasRejectedOffer?: boolean;
    isPossibleToApprove: boolean;
    isPossibleToRevert: boolean;
    customerDecisionForm: FormsHelper;
    isEditDecisionAllowed: boolean;
}
