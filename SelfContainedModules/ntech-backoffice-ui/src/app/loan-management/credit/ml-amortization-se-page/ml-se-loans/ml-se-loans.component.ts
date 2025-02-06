import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { FormsHelper, NewFormGroupControl } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { Dictionary } from 'src/app/common.types';
import {
    getAmortizationRuleCodeDisplayName,
    MortageLoanSeSetExceptionsRequest,
    MortgageLoanSeAmortizationExceptionModel,
} from '../../ml-amortization-se.service';
import { MlSeLoansComponentLoanModel } from '../ml-amortization-se-page.component';

function getAllExceptionReasons(currentReasons: string[]) {
    const defaultReasons: string[] = ['Nyproduktion', 'Lantbruksenhet', 'Sjukdom', 'Arbetslöshet', 'Dödsfall'];

    var reasonsSet = new Set<string>();
    var reasons: string[] = [];

    let addReasons = (rs: string[]) => {
        for (let r of rs) {
            if (!reasonsSet.has(r)) {
                reasons.push(r);
            }
            reasonsSet.add(r);
        }
    };

    addReasons(defaultReasons);
    addReasons(currentReasons ?? []);

    return reasons;
}

@Component({
    selector: 'ml-se-loans',
    templateUrl: './ml-se-loans.component.html',
    styleUrls: ['./ml-se-loans.component.css'],
})
export class MlSeLoansComponent implements OnInit {
    constructor(
        private formBuilder: UntypedFormBuilder,
        private eventService: NtechEventService,
        private validationService: NTechValidationService
    ) {}

    async ngOnInit() {}

    ngOnChanges(_: SimpleChanges) {
        this.m = null;

        let i = this.initialData;

        if (!i) {
            return;
        }

        this.m = {
            creditNr: i.creditNr,
            loans: i.loans.map((x) => ({
                creditNr: x.loan.creditNr,
                currentCapitalBalanceAmount: x.loan.currentCapitalBalanceAmount,
                actualFixedMonthlyPayment: x.actualFixedMonthlyPayment,
                ruleText: getAmortizationRuleCodeDisplayName(x.ruleCode, x.isUsingAlternateRule),
                amortizationException: x.loan.amortizationException,
            })).filter(x => x.currentCapitalBalanceAmount > Number.EPSILON),
            isEditAllowed: !!i.onCommitEdit,
        };
    }

    @Input()
    public initialData: MlSeLoansComponentInitialData;

    public m: Model;

    public beginEditExceptions(evt?: Event) {
        evt?.preventDefault();
        let f = new FormsHelper(this.formBuilder.group({}));

        let reasonsPerGroup: Dictionary<{ controlName: string; reasonName: string }[]> = {};

        for (let loan of this.m.loans) {
            let groupControls: NewFormGroupControl[] = [
                {
                    controlName: 'hasException',
                    initialValue: !!loan.amortizationException,
                    validators: [Validators.required],
                },
                {
                    controlName: 'untilDate',
                    initialValue: !loan.amortizationException?.untilDate
                        ? ''
                        : this.validationService.formatDateForEdit(
                              this.validationService.parseDatetimeOffset(loan.amortizationException?.untilDate)
                          ),
                    validators: [],
                },
                {
                    controlName: 'amortizationAmount',
                    initialValue: this.validationService.formatDecimalForEdit(
                        loan.amortizationException?.amortizationAmount
                    ),
                    validators: [],
                },
                {
                    controlName: 'isOtherActive',
                    initialValue: false,
                    validators: [],
                },
                {
                    controlName: 'otherText',
                    initialValue: '',
                    validators: [],
                },
            ];

            reasonsPerGroup[loan.creditNr] = getAllExceptionReasons(loan.amortizationException?.reasons).map(
                (reasonName) => ({ controlName: this.getReasonControlName(reasonName), reasonName: reasonName })
            );

            for (let reason of reasonsPerGroup[loan.creditNr]) {
                groupControls.push({
                    controlName: reason.controlName,
                    initialValue: (loan.amortizationException?.reasons ?? []).indexOf(reason.reasonName) >= 0,
                    validators: [],
                });
            }

            f.addGroupIfNotExists(loan.creditNr, groupControls);
        }

        f.form.validator = (_) => {
            let result: ValidationErrors | null = null;
            let validateControl = (loan: CurrentLoanModel, formControlName: string, validators: ValidatorFn[]) => {
                let control = f.form.get(loan.creditNr).get(formControlName);
                for (let validate of validators) {
                    let errors = validate(control);
                    if (errors && !result) {
                        result = {};
                    }
                    if (errors) {
                        result[`${loan.creditNr}_${formControlName}`] = true;
                    }
                }
            };
            for (let loan of this.m.loans) {
                if (f.getFormGroupValue(loan.creditNr, 'hasException')) {
                    validateControl(loan, 'untilDate', [
                        Validators.required,
                        this.validationService.getDateOnlyValidator(),
                    ]);
                    validateControl(loan, 'amortizationAmount', [
                        Validators.required,
                        this.validationService.getDecimalValidator(),
                    ]);
                    let isAnyReasonSelected = this.getSelectedReasons(loan).size > 0;
                    for (let reason of getAllExceptionReasons(loan.amortizationException?.reasons)) {
                        let controlName = this.getReasonControlName(reason);
                        validateControl(loan, controlName, isAnyReasonSelected ? [] : [Validators.requiredTrue]);
                    }
                    validateControl(loan, 'isOtherActive', isAnyReasonSelected ? [] : [Validators.requiredTrue]);
                    validateControl(
                        loan,
                        'otherText',
                        f.getFormGroupValue(loan.creditNr, 'isOtherActive') ? [Validators.required] : []
                    );
                }
            }
            return result;
        };

        this.m.edit = {
            form: f,
            reasonsPerGroup: reasonsPerGroup,
        };
    }

    private getReasonControlName(reasonName: string) {
        return `is_selected_${reasonName}`;
    }

    public hasError(loan: CurrentLoanModel, controlName: string, isCheckBox?: boolean): boolean {
        let f = this.m?.edit?.form;
        return (
            f &&
            f.form.get(loan.creditNr).dirty &&
            (f.hasError(controlName, loan.creditNr) || f.hasNamedValidationError(`${loan.creditNr}_${controlName}`))
        );
    }

    public async commitEditExceptions(evt?: Event) {
        evt?.preventDefault();

        let f = this.m.edit.form;

        let request: MortageLoanSeSetExceptionsRequest = {
            credits: [],
        };

        for (let loan of this.m.loans) {
            let hasException: boolean = f.getFormGroupValue(loan.creditNr, 'hasException');
            if (hasException) {
                let untilDate = this.validationService.parseDateOnlyOrNull(
                    f.getFormGroupValue(loan.creditNr, 'untilDate')
                );
                let amortizationAmount = this.validationService.parseDecimalOrNull(
                    f.getFormGroupValue(loan.creditNr, 'amortizationAmount'),
                    false
                );
                let selectedReasons = this.getSelectedReasons(loan);

                request.credits.push({
                    creditNr: loan.creditNr,
                    hasException: true,
                    exception: {
                        untilDate: untilDate,
                        amortizationAmount: amortizationAmount,
                        reasons: Array.from(selectedReasons),
                    },
                });
            } else {
                request.credits.push({
                    creditNr: loan.creditNr,
                    hasException: false,
                    exception: null,
                });
            }
        }

        await this.initialData.onCommitEdit(request);
        this.eventService.signalReloadCreditComments(this.m.creditNr);
    }

    private getSelectedReasons(loan: CurrentLoanModel): Set<string> {
        let f = this.m.edit.form;
        let allReasons = getAllExceptionReasons(loan.amortizationException?.reasons);
        let selectedReasons: Set<string> = new Set<string>();
        for (let reason of allReasons) {
            let isSelected = f.getFormGroupValue(loan.creditNr, this.getReasonControlName(reason));
            if (isSelected) {
                selectedReasons.add(reason);
            }
            if (f.getFormGroupValue(loan.creditNr, 'isOtherActive')) {
                let otherText = this.validationService.normalizeString(f.getFormGroupValue(loan.creditNr, 'otherText'));
                if (otherText) {
                    selectedReasons.add(f.getFormGroupValue(loan.creditNr, 'otherText'));
                }
            }
        }
        return selectedReasons;
    }

    public cancelEditExceptions(evt?: Event) {
        evt?.preventDefault();
        this.m.edit = null;
    }

    public removeException(creditNr: string, evt?: Event) {
        evt?.preventDefault();
        this.m.edit.form.setValue2(creditNr, 'hasException', false);
    }

    public addException(loan: CurrentLoanModel, evt?: Event) {
        evt?.preventDefault();

        this.m.edit.form.setValue2(loan.creditNr, 'hasException', true);
    }
}

interface Model {
    creditNr: string;
    isEditAllowed: boolean;
    loans: CurrentLoanModel[];
    edit?: {
        form: FormsHelper;
        reasonsPerGroup: Dictionary<{ controlName: string; reasonName: string }[]>;
    };
}

export interface CurrentLoanModel {
    creditNr: string;
    currentCapitalBalanceAmount: number;
    ruleText: string;
    actualFixedMonthlyPayment: number;
    amortizationException: MortgageLoanSeAmortizationExceptionModel;
}

export interface MlSeLoansComponentInitialData {
    creditNr: string;
    loans: MlSeLoansComponentLoanModel[];
    onCommitEdit?: (request: MortageLoanSeSetExceptionsRequest) => Promise<void>;
}
