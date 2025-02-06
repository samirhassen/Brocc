import { Component, Input, SimpleChanges } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { StandardCreditApplicationModel } from '../../services/standard-credit-application-model';
import { UnsecuredLoanApplicationApiService } from '../../services/unsecured-loan-application-api.service';
import { WorkflowStepComponent, WorkflowStepInitialData } from '../workflow-step';

@Component({
    selector: 'fraud-standard',
    templateUrl: './fraud-standard.component.html',
    styles: [],
})
export class FraudStandardComponent implements WorkflowStepComponent {
    constructor(
        private apiService: UnsecuredLoanApplicationApiService,
        private eventService: NtechEventService,
        private toastr: ToastrService
    ) {}

    public m: Model;

    @Input()
    public initialData: WorkflowStepInitialData;

    ngOnInit() {}

    ngOnChanges(changes: SimpleChanges): void {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let applicaton = this.initialData.application;
        let ai = applicaton.applicationInfo;

        let isActiveAndCurrentStep =
            ai.IsActive &&
            this.initialData.workflow.step.areAllStepBeforeThisAccepted() &&
            this.initialData.workflow.step.isStatusInitial();

        let controls: FraudControlModel[] = [];
        controls.push({
            controlKey: 'SameAddressCheck',
            controlName: 'Any with same address',
            approvedByHandler: false,
            isInitial: true,
            matches: null,
            hasBeenRun: false,
        });
        controls.push({
            controlKey: 'SameEmailCheck',
            controlName: 'Any with same email',
            approvedByHandler: false,
            isInitial: true,
            matches: null,
            hasBeenRun: false,
        });
        controls.push({
            controlKey: 'SameAccountNrCheck',
            controlName: 'Any with same bank account',
            approvedByHandler: false,
            isInitial: true,
            matches: null,
            hasBeenRun: false,
        });

        this.apiService.getFraudControlResults(this.initialData.application.applicationNr).then((res) => {
            if (res !== null) {
                res.FraudControls.forEach((ctrl) => {
                    let control = controls.find((x) => x.controlKey === ctrl.CheckName);
                    control.matches = ctrl.Values;
                    control.approvedByHandler = ctrl.IsApproved;
                    control.hasBeenRun = true;
                });
            }

            let hasConfirmedBankAccounts =
                applicaton
                    .getComplexApplicationList('Application', false)
                    .getRow(1, false)
                    .getUniqueItem('confirmedBankAccountsCode') === 'Approved';

            this.m = {
                application: this.initialData.application,
                hasCustomerConfirmedBankAccounts: hasConfirmedBankAccounts,
                isActiveAndCurrentStep: isActiveAndCurrentStep,
                isStepStatusRevertible: this.initialData.workflow.step.isRevertable(),
                isStepStatusAccepted: this.initialData.workflow.step.isStatusAccepted(),
                controls: controls,
            };
        });
    }

    runFraudControls() {
        this.apiService.runFraudControls(this.initialData.application.applicationNr).then((res) => {
            res.FraudControls.forEach((ctrl) => {
                let control = this.m.controls.find((x) => x.controlKey === ctrl.CheckName);
                control.matches = ctrl.Values;
                control.approvedByHandler = ctrl.IsApproved;
                control.hasBeenRun = true;
            });
        });
    }

    allFraudControlsApproved() {
        return this.m?.controls?.every((x) => x.approvedByHandler);
    }

    getIconClass(isAccepted: boolean, isRejected?: boolean) {
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

    getNullMatchesText(controlKey: string, hasBeenRun: boolean) {
        if (!hasBeenRun) return 'Not run';

        if (controlKey === 'SameAccountNrCheck') {
            let bankAccountNr = this.initialData.application
                .getComplexApplicationList('Application', false)
                .getRow(1, false)
                .getUniqueItem('paidToCustomerBankAccountNr');

            return bankAccountNr ? 'No hit' : 'Missing';
        }

        return 'No hit';
    }

    toggleFraudControlApproval(controlKey: string) {
        let control = this.m.controls.find((ctrl) => ctrl.controlKey === controlKey);

        if (!control.approvedByHandler) {
            this.apiService
                .setFraudControlItemApproved(this.initialData.application.applicationNr, controlKey)
                .then((x) => {
                    control.approvedByHandler = true;
                });
        } else {
            this.apiService
                .setFraudControlItemInitial(this.initialData.application.applicationNr, controlKey)
                .then((x) => {
                    control.approvedByHandler = false;
                });
        }
    }

    approveFraudStep(evt?: Event) {
        evt?.preventDefault();

        this.apiService.approveFraudStep(this.initialData.application.applicationNr, true).then(
            (onSuccess) => {
                this.eventService.signalReloadApplication(this.initialData.application.applicationNr);
            },
            (onError) => {
                this.toastr.error('Could not approve fraud step, ensure all fraud controls are approved. ');
            }
        );
    }

    revertFraudStep(evt?: Event) {
        evt?.preventDefault();

        this.apiService.approveFraudStep(this.initialData.application.applicationNr, false).then((x) => {
            this.eventService.signalReloadApplication(this.initialData.application.applicationNr);
        });
    }

    toggleBankAccountConfirmationStatusCode(evt?: Event) {
        evt?.preventDefault();

        this.apiService
            .toggleBankAccountConfirmationCodeStatus(this.initialData.application.applicationNr)
            .then((_) => {
                this.m.hasCustomerConfirmedBankAccounts = !this.m.hasCustomerConfirmedBankAccounts;
                this.eventService.signalReloadApplication(this.initialData.application.applicationNr);
            })
            .catch((_) => this.toastr.error('Could not change bank account status, something went wrong.'));
    }
}

class Model {
    application: StandardCreditApplicationModel;
    isActiveAndCurrentStep: boolean;
    isStepStatusRevertible: boolean;
    hasCustomerConfirmedBankAccounts: boolean;
    controls: FraudControlModel[];
    isStepStatusAccepted: boolean;
}

class FraudControlModel {
    controlKey: string;
    controlName: string;
    approvedByHandler: boolean;
    isInitial: boolean;
    matches: string[];
    hasBeenRun: boolean = false;
}
