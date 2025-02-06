import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { FormsHelper, NewFormGroupControl } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import {
    getAmortizationRuleCodeDisplayName,
    MortgageLoanSeAmortizationBasisLoan,
    MortgageLoanSeAmortizationExceptionModel,
    MortgageLoanSeCurrentLoanModel,
} from '../../ml-amortization-se.service';
import { BehaviorSubject } from 'rxjs';

@Component({
    selector: 'ml-se-loans-amortization',
    templateUrl: './ml-se-loans-amortization.component.html',
    styleUrls: ['./ml-se-loans-amortization.component.css'],
})
export class MlSeLoansAmortizationComponent implements OnInit {
    constructor(private formBuilder: UntypedFormBuilder, private validationService: NTechValidationService) {}

    async ngOnInit() {}

    ngOnChanges(_: SimpleChanges) {
        const i = this.initialData;

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
                ruleCode: x.ruleCode,
                isUsingAlternateRule: x.isUsingAlternateRule,
                amortizationException: x.loan.amortizationException,
                interestBindMonthCount: x.interestBindMonthCount,
            })),
            isKeepExistingRuleCodeChosen: i.isKeepExistingRuleCodeChosen,
            keepExistingRuleCode: i.keepExistingRuleCode,
            isKeepExistingRuleCodeAllowed: i.isKeepExistingRuleCodeAllowed,
        };

        this.chooseBasis();
    }

    isSettledLoan(x: CurrentLoanModel) {
        return x.currentCapitalBalanceAmount <= Number.EPSILON;
    }

    @Input()
    public initialData: MlSeLoanAmortizationComponentInitialData;

    public m: Model;

    public async chooseRevaluationBasis(isKeepExistingRuleCode?: boolean, evt?: Event) {
        evt?.preventDefault();

        this.m.isKeepExistingRuleCodeChosen = isKeepExistingRuleCode;

        await this.ChangeActualFixedMonthlyPayment();
    }

    public chooseBasis(evt?: Event) {
        evt?.preventDefault();
        let f = new FormsHelper(this.formBuilder.group({}));

        for (let loan of this.m.loans) {
            let groupControls: NewFormGroupControl[] = [
                {
                    controlName: 'actualFixedMonthlyPayment',
                    initialValue: loan.actualFixedMonthlyPayment,
                    validators: [Validators.required, this.validationService.getDecimalValidator()],
                },
            ];

            f.addGroupIfNotExists(loan.creditNr, groupControls);
        }

        this.m.edit = {
            form: f,
        };
    }

    public GetMinimumTotalAmortizationAmount(useFormValues: boolean) {
        if (useFormValues) {
            let f = this.m.edit.form;

            const amounts: number[] = this.m.loans.map((x) =>
                Number(f.getFormGroupValue(x.creditNr, 'actualFixedMonthlyPayment'))
            );
            const sum = amounts.reduce((partialSum, a) => partialSum + a, 0);
            return sum;
        }

        const amounts = this.m.loans.map((x) => x.actualFixedMonthlyPayment);
        const sum = amounts.reduce((partialSum, a) => partialSum + a, 0);

        return sum;
    }

    public hasError(loan: CurrentLoanModel, controlName: string, isCheckBox?: boolean): boolean {
        let f = this.m?.edit?.form;
        return (
            f &&
            f.form.get(loan.creditNr).dirty &&
            (f.hasError(controlName, loan.creditNr) || f.hasNamedValidationError(`${loan.creditNr}_${controlName}`))
        );
    }

    public showAmortizationInput(isUsingAlternateRule: boolean): boolean {
        if (!this.m) {
            return false;
        }

        if (isUsingAlternateRule) {
            return false;
        }

        return this.m.keepExistingRuleCode ? this.m.isKeepExistingRuleCodeChosen : !this.m.isKeepExistingRuleCodeChosen;
    }

    public async ChangeActualFixedMonthlyPayment() {
        let f = this.m.edit.form;
        const arr: MortgageLoanSeAmortizationBasisLoan[] = [];

        this.m.loans.forEach((x) => {
            const y: MortgageLoanSeAmortizationBasisLoan = {
                creditNr: x.creditNr,
                currentCapitalBalanceAmount: x.currentCapitalBalanceAmount,
                maxCapitalBalanceAmount: null, //tmp
                ruleCode: x.ruleCode,
                isUsingAlternateRule: x.isUsingAlternateRule,
                monthlyAmortizationAmount: Number(f.getFormGroupValue(x.creditNr, 'actualFixedMonthlyPayment')),
                interestBindMonthCount: x.interestBindMonthCount,
            };

            arr.push(y);
        });

        this.initialData.newRevalueModel.next({
            isValid: this.m.loans.filter((x) => this.hasError(x, 'actualFixedMonthlyPayment') === true).length < 1,
            keepExistingRuleCode: this.m.isKeepExistingRuleCodeChosen,
            editedLoans: arr,
        });
    }
}

interface Model {
    creditNr: string;
    loans: CurrentLoanModel[];
    edit?: {
        form: FormsHelper;
    };
    isKeepExistingRuleCodeChosen: boolean;
    keepExistingRuleCode: boolean;
    isKeepExistingRuleCodeAllowed: boolean;
}

interface CurrentLoanModel {
    creditNr: string;
    currentCapitalBalanceAmount: number;
    ruleText: string;
    ruleCode: string;
    isUsingAlternateRule: boolean;
    actualFixedMonthlyPayment: number;
    amortizationException: MortgageLoanSeAmortizationExceptionModel;
    interestBindMonthCount: number;
}

export interface MlSeLoanAmortizationComponentInitialData {
    creditNr: string;
    loans: MlSeLoanAmortizationComponentLoanModel[];
    keepExistingRuleCode: boolean;
    isKeepExistingRuleCodeChosen: boolean;
    isKeepExistingRuleCodeAllowed: boolean;
    newRevalueModel: BehaviorSubject<RevalueModel>;
}

export interface MlSeLoanAmortizationComponentLoanModel {
    loan: MortgageLoanSeCurrentLoanModel;
    actualFixedMonthlyPayment: number;
    ruleCode: string;
    isUsingAlternateRule: boolean;
    interestBindMonthCount: number;
}

export class RevalueModel {
    isValid: boolean;
    keepExistingRuleCode: boolean;
    editedLoans: MortgageLoanSeAmortizationBasisLoan[];
}
