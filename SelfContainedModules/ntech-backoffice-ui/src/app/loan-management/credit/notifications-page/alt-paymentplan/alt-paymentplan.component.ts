import { Component, Input, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import * as moment from 'moment';
import { ToastrService } from 'ngx-toastr';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { NTechMath } from 'src/app/common-services/ntech.math';

@Component({
    selector: 'alt-paymentplan',
    templateUrl: './alt-paymentplan.component.html',
    styles: [],
})
export class AltPaymentplanComponent {
    constructor(
        private fb: UntypedFormBuilder,
        private apiService: NtechApiService,
        private eventService: NtechEventService,
        private toastrService: ToastrService,
        private validationService: NTechValidationService
    ) {}

    @Input()
    public initialData: AltPaymentplanInitialData;

    public m: Model;

    //arbitrary max amount, will be replaced with loan max amount requirements
    private maxAllowedAmount: number = 1000000000000000;

    async ngOnChanges(changes: SimpleChanges) {
        await this.reload();
    }

    async reload() {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let creditState = await getAlternatePaymentPlanState(this.apiService, this.initialData.creditNr);

        this.m = {
            state: creditState,
            inEditMode: false,
            pendingEdit: false,
            pendingCancel: false,
            paymentPlanForm: null,
            nrOfMonths: null,
            forceStartNextMonth: false,
            formPaymentPlanSum: 0,
            requiredPaymentPlanSum: 0,
            isValidationError: null,
        };
    }

    getPaymentPlanForm() {
        const paymentPlanForm = new FormsHelper(this.fb.group({}));
        const suggestedPaymentPlan = this.m.suggestedPlan.months;
        const paymentPlanFormGroupNames: string[] = [];

        paymentPlanForm.addControlIfNotExists('forceStartNextMonth', this.m.forceStartNextMonth, []);

        suggestedPaymentPlan.forEach((x, i) =>
            paymentPlanFormGroupNames.push(this.addPaymentPlanFormGroup(paymentPlanForm, i, suggestedPaymentPlan[i]))
        );

        return paymentPlanForm;
    }

    private addPaymentPlanFormGroup(
        form: FormsHelper,
        index: number,
        suggested: AlternatePaymentPlanSpecificationMonth
    ): string {
        let formGroupName = (index + 1).toString();
        form.addGroupIfNotExists(formGroupName, [
            {
                controlName: 'dueDate',
                initialValue: this.validationService.formatDateForEdit(moment(suggested.dueDate)),
                validators: [Validators.required, this.validationService.getDateOnlyValidator()],
            },
            {
                controlName: 'amount',
                initialValue: this.validationService.formatDecimalForEdit(suggested.monthAmount),
                validators: [Validators.required, this.validationService.getPositiveDecimalValidator()],
            },
        ]);

        return formGroupName;
    }

    async beginCreate(evt?: Event) {
        evt?.preventDefault();

        const suggestedPlan = await this.fetchSuggestedPaymentPlan(true);

        this.m.suggestedPlan = suggestedPlan;
        this.m.requiredPaymentPlanSum = suggestedPlan.requiredPaymentPlanSum;
        this.m.nrOfMonths = suggestedPlan.months.length;
        this.m.paymentPlanForm = this.getPaymentPlanForm();
        this.m.formPaymentPlanSum = this.getFormPaymentPlanSum();

        this.m.isValidationError = null;
        this.m.inEditMode = true;
    }

    beginEdit(evt?: Event) {
        evt?.preventDefault();

        this.m.pendingEdit = true;
    }

    cancelEdit(evt?: Event) {
        evt?.preventDefault();

        this.m.pendingEdit = false;
        this.m.pendingCancel = false;
        this.m.inEditMode = false;
    }

    async beginCancel(evt?: Event) {
        evt?.preventDefault();

        this.m.pendingCancel = true;
    }

    async commitCancel(evt?: Event) {
        evt?.preventDefault();

        await this.apiService.post<AlternatePaymentPlanState>('NTechHost', 'Api/Credit/AlternatePaymentPlan/Cancel', {
            creditNr: this.initialData.creditNr,
            isManualCancel: true
        });

        this.eventService.signalReloadCreditComments(this.initialData.creditNr);
        await this.reload();
    }

    async commitCreate(evt?: Event) {
        evt?.preventDefault();

        if (this.m.paymentPlanForm.invalid()) {
            this.toastrService.error(`Error: invalid form`);
            this.m.formPaymentPlanSum = null;
            return;
        }

        const paymentPlan = await this.getPaymentFormModel();
        const validate = await this.validatePaymentModel(paymentPlan);

        if (!validate.isValid) {
            this.toastrService.error(`Error: ${validate.errorMessage}`);
            return;
        }

        return await this.apiService
            .post('NTechHost', 'Api/Credit/AlternatePaymentPlan/Start', {
                creditNr: this.initialData.creditNr,
                nrOfPayments: this.m.nrOfMonths,
                paymentPlan,
                forceStartNextMonth: this.m.forceStartNextMonth,
            })
            .then((_) => {
                this.eventService.signalReloadCreditComments(this.initialData.creditNr);
                this.reload();
                return;
            })
            .catch((_) => {
                this.toastrService.error(`Error, contact support.`);
                return;
            });
    }

    async onFormInput(evt?: Event) {
        evt?.preventDefault();

        if (this.m.paymentPlanForm.invalid()) {
            this.m.isValidationError = null;
            this.m.formPaymentPlanSum = null;
            return;
        }

        const paymentPlan = await this.getPaymentFormModel();
        const validate = await this.validatePaymentModel(paymentPlan);
        this.m.formPaymentPlanSum = this.getFormPaymentPlanSum();

        if (!validate.isValid) {
            this.m.isValidationError = {
                isError: true,
                errorBlockMessage: validate.errorMessage ?? 'Error',
            };
        } else {
            this.m.isValidationError = null;
        }

        return;
    }

    async changeMonth(isAddMonth: boolean, evt?: Event) {
        evt?.preventDefault();

        if (this.m.nrOfMonths <= 2 || this.m.nrOfMonths >= 12) {
            this.toastrService.error('Error: payment plan needs to be between 2-12 months');
            return;
        }

        isAddMonth ? (this.m.nrOfMonths += 1) : (this.m.nrOfMonths -= 1);

        this.m.suggestedPlan = await this.fetchSuggestedPaymentPlan();
        this.m.paymentPlanForm = this.getPaymentPlanForm();
        this.m.isValidationError = null;
        this.m.formPaymentPlanSum = this.getFormPaymentPlanSum();
    }

    async changeForceStartNextMonth(evt?: Event) {
        evt?.preventDefault();

        this.m.forceStartNextMonth = this.m.paymentPlanForm.getValue('forceStartNextMonth') === 'true';
        this.m.suggestedPlan = await this.fetchSuggestedPaymentPlan();
        this.m.paymentPlanForm = this.getPaymentPlanForm();
        this.m.formPaymentPlanSum = this.getFormPaymentPlanSum();
        this.m.isValidationError = null;
    }

    cancelCreate(evt?: Event) {
        evt?.preventDefault();

        this.m.forceStartNextMonth = false;
        this.m.inEditMode = false;
    }

    private async fetchSuggestedPaymentPlan(isInit?: boolean): Promise<AlternatePaymentPlanSpecification> {
        const params = isInit
            ? { creditNr: this.initialData.creditNr }
            : {
                  creditNr: this.initialData.creditNr,
                  nrOfPayments: this.m.nrOfMonths,
                  forceStartNextMonth: this.m.forceStartNextMonth,
              };

        return await this.apiService.post('NTechHost', 'Api/Credit/AlternatePaymentPlan/Get-Suggested', params);
    }

    private async validatePaymentModel(
        paymentPlanToValidate: {
            dueDate: string;
            paymentPlanAmount: number;
        }[]
    ): Promise<{ isValid: boolean; errorMessage: string }> {
        if (paymentPlanToValidate.some((plan) => plan.paymentPlanAmount >= this.maxAllowedAmount)) {
            return {
                isValid: false,
                errorMessage: 'Payment plan amount too high',
            };
        }

        return await this.apiService.post<{ isValid: boolean; errorMessage: string }>(
            'NTechHost',
            'Api/Credit/AlternatePaymentPlan/Validate',
            {
                creditNr: this.initialData.creditNr,
                paymentPlan: paymentPlanToValidate,
            }
        );
    }

    private async getPaymentFormModel() {
        return Array.from({ length: this.m.nrOfMonths }, (_, i) => {
            const dueDate: string = this.m.paymentPlanForm.getFormGroupValue((i + 1).toString(), 'dueDate');
            const amount: string = this.m.paymentPlanForm.getFormGroupValue((i + 1).toString(), 'amount');
            const parsedAmount = this.validationService.parseDecimalOrNull(amount, false);
            return {
                dueDate: dueDate,
                paymentPlanAmount: parsedAmount,
            };
        });
    }

    private getFormPaymentPlanSum(): number {
        let sum = Array.from({ length: this.m.nrOfMonths }, (_, i) =>
                this.validationService.parseDecimalOrNull(this.m.paymentPlanForm.getFormGroupValue((i + 1).toString(), 'amount'), false))
            .reduce((partialSum, a) => partialSum + a, 0);

        if (sum > this.maxAllowedAmount) {
            return null;
        }

        return NTechMath.roundToPlaces(sum, 2);
    }
}

interface Model {
    inEditMode: boolean;
    paymentPlanForm: FormsHelper;
    state: AlternatePaymentPlanState;
    pendingEdit: boolean;
    pendingCancel: boolean;
    suggestedPlan?: AlternatePaymentPlanSpecification;
    nrOfMonths: number;
    forceStartNextMonth: boolean;
    formPaymentPlanSum: number;
    requiredPaymentPlanSum: number;
    isValidationError: {
        isError: boolean;
        errorBlockMessage?: string;
    };
}

export function getAlternatePaymentPlanState(apiService: NtechApiService, creditNr: string) {
    return apiService.post<AlternatePaymentPlanState>(
        'NTechHost',
        'Api/Credit/AlternatePaymentPlan/Credit-State',
        { creditNr: creditNr }
    );
}

export interface AlternatePaymentPlanState {
    creditNr: string;
    isNewPaymentPlanPossible: boolean;
    paymentPlanState: PaymentPlanState;
}

interface PaymentPlanState {
    alternatePaymentPlanMonths: AlternatePaymentPlanMonth[];
    paymentPlanPaidAmountsResult: PaymentPlanPaidAmountsModel[];
}

interface AlternatePaymentPlanMonth {
    dueDate: Date;
    monthAmount: number;
    totalAmount: number;
}

interface PaymentPlanPaidAmountsModel {
    dueDate: Date;
    paymentPlanAmount: number;
    paidAmount: number | null;
    latestPaymentDate: Date | null;
}

interface AlternatePaymentPlanSpecification {
    creditNr: string;
    months: AlternatePaymentPlanSpecificationMonth[];
    requiredPaymentPlanSum: number;
}

interface AlternatePaymentPlanSpecificationMonth {
    dueDate: Date;
    monthAmount: number;
}

export interface AltPaymentplanInitialData {
    creditNr: string;
}
