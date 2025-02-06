import { Component, OnInit, TemplateRef } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import {
    MortgageLoanSeAmortizationBasisModel,
    SwedishAmorteringsunderlag,
} from 'projects/ntech-components/src/public-api';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import {
    LoadAmortizationBasisSeResult,
    MlAmortizationSeService,
    MortgageLoanSeAmortizationBasisLoan,
    MortgageLoanSeCurrentLoanModel,
} from '../ml-amortization-se.service';
import { MlSeLoansComponentInitialData } from './ml-se-loans/ml-se-loans.component';
import { BehaviorSubject } from 'rxjs';
import {
    MlSeLoanAmortizationComponentInitialData,
    RevalueModel,
} from './ml-se-loans-amortization/ml-se-loans-amortization.component';

@Component({
    selector: 'ml-amortization-se-page',
    templateUrl: './ml-amortization-se-page.component.html',
    styleUrls: ['./ml-amortization-se-page.component.css'],
})
export class MlAmortizationPageSeComponent implements OnInit {
    constructor(
        private apiService: MlAmortizationSeService,
        private config: ConfigService,
        private eventService: NtechEventService,
        private formBuilder: UntypedFormBuilder,
        private modalService: BsModalService,
        private ntechApiService: NtechApiService,
        private route: ActivatedRoute,
        private validationService: NTechValidationService
    ) {}

    public m: Model;
    public modalRef: BsModalRef;

    async ngOnInit(): Promise<void> {
        this.route.paramMap.subscribe((params: ParamMap) => {
            this.reload(params.get('creditNr'));
        });
    }

    public showAmortizationBasis(popupTemplate: TemplateRef<any>, evt?: Event) {
        evt?.preventDefault();
        this.modalRef = this.modalService.show(popupTemplate, { class: 'modal-xl', ignoreBackdropClick: true });
    }

    private async reload(creditNr: string) {
        let basisResult = await this.apiService.loadAmortizationBasisSe(creditNr, true, true);
        let currentLoans = (await this.apiService.getCurrentLoans(creditNr)).loans;

        let m: Model = {
            creditNr: creditNr,
            basisResult: basisResult,
            currentLoans: [],
            loansData: null,
            initLoanAmortizationData: null,
        };

        for (let currentLoan of currentLoans) {
            let basisLoan = basisResult.amortizationBasis.loans.find((x) => x.creditNr === currentLoan.creditNr);
            m.currentLoans.push({
                loan: currentLoan,
                actualFixedMonthlyPayment: currentLoan.actualFixedMonthlyPayment,
                ruleCode: basisLoan.ruleCode,
                isUsingAlternateRule: basisLoan.isUsingAlternateRule,
                interestBindMonthCount: basisLoan.interestBindMonthCount,
            });
        }

        m.revalueForm = new FormsHelper(
            this.formBuilder.group({
                currentCombinedYearlyIncomeAmount: [
                    this.validationService.formatDecimalForEdit(
                        m.basisResult.amortizationBasis.currentCombinedYearlyIncomeAmount
                    ),
                    [Validators.required, this.validationService.getDecimalValidator()],
                ],
                otherMortageLoansAmount: [
                    this.validationService.formatDecimalForEdit(
                        m.basisResult.amortizationBasis.otherMortageLoansAmount
                    ),
                    [Validators.required, this.validationService.getDecimalValidator()],
                ],
                isNewValuationSelected: [false, [Validators.required]],
                newObjectValueDate: [
                    this.validationService.formatDateForEdit(this.config.getCurrentDateAndTime()),
                    [this.validationService.getDateOnlyValidator()],
                ],
                newObjectValue: ['', [this.validationService.getPositiveDecimalValidator()]],
            })
        );

        m.revalueForm.form.setValidators([
            (_) => {
                if (!m.revalueForm.getValue('isNewValuationSelected')) {
                    return null;
                }
                return m.revalueForm.getValue('newObjectValueDate') ? null : { missingNewObjectValueDate: true };
            },
            (_) => {
                if (!m.revalueForm.getValue('isNewValuationSelected')) {
                    return null;
                }
                return m.revalueForm.getValue('newObjectValue') ? null : { missingNewObjectValue: true };
            },
        ]);

        m.loansData = {
            creditNr: creditNr,
            loans: m.currentLoans,
            onCommitEdit: async (request) => {
                await this.apiService.setAmortizationExceptions(request);
                await this.reload(creditNr);
            },
        };

        this.m = m;
    }

    public async previewRevalue(evt?: Event) {
        evt?.preventDefault();

        let f = this.m.revalueForm;

        let isNewValuationSelected = f.getValue('isNewValuationSelected');

        let {
            mlStandardSeRevaluationCalculateResult,
            mlStandardSeRevaluationKeepExistingRuleCodeResult,
            isKeepExistingRuleCodeAllowed,
        } = await this.apiService.computeRevaluate({
            creditNr: this.m.creditNr,
            currentCombinedYearlyIncomeAmount: this.validationService.parseDecimalOrNull(
                f.getValue('currentCombinedYearlyIncomeAmount'),
                false
            ),
            otherMortageLoansAmount: this.validationService.parseDecimalOrNull(
                f.getValue('otherMortageLoansAmount'),
                false
            ),
            newValuationAmount: isNewValuationSelected
                ? this.validationService.parsePositiveDecimalOrNull(f.getValue('newObjectValue'))
                : null,
            newValuationDate: isNewValuationSelected
                ? this.validationService.parseDateOnly(f.getValue('newObjectValueDate'))
                : null,
        });

        this.m.revaluePreview = {
            newBasis: mlStandardSeRevaluationCalculateResult.newBasis,
            newBasisKeepExistingRuleCode: mlStandardSeRevaluationKeepExistingRuleCodeResult.newBasis,
            newUnderlag: mlStandardSeRevaluationCalculateResult.newAmorteringsunderlag,
            newUnderlagKeepExistingRuleCode: mlStandardSeRevaluationKeepExistingRuleCodeResult.newAmorteringsunderlag,
            currentLoans: mlStandardSeRevaluationCalculateResult.newBasis.loans.map((loan) =>
                this.getLoanAmortizationModel(loan)
            ),
            isKeepExistingRuleCodeAllowed: isKeepExistingRuleCodeAllowed,
            currentLoansKeepExistingRuleCode: isKeepExistingRuleCodeAllowed
                ? mlStandardSeRevaluationKeepExistingRuleCodeResult?.newBasis?.loans?.map((loan) =>
                      this.getLoanAmortizationModel(loan)
                  )
                : null,
            isKeepExistingRuleCodeChosen: true,
            editedNewBasisToCommit: mlStandardSeRevaluationCalculateResult.newBasis,
            newRevalueModel: new BehaviorSubject<RevalueModel>(null),
            isValid: true,
        };

        this.m.revaluePreview.newRevalueModel.subscribe((x) => this.setNewRevalueModel(x));

        this.m.initLoanAmortizationData = {
            initLoanData: this.getLoanAmortizationInitialData(false),
            initLoanDataKeepExistingRuleCode: this.getLoanAmortizationInitialData(true),
        };
    }

    private getLoanAmortizationModel(loan: MortgageLoanSeAmortizationBasisLoan): MlSeLoansComponentLoanModel {
        let currentLoan = this.m.currentLoans.find((x) => x.loan.creditNr === loan.creditNr).loan;
        return {
            loan: currentLoan,
            actualFixedMonthlyPayment: loan.monthlyAmortizationAmount,
            ruleCode: loan.ruleCode,
            isUsingAlternateRule: loan.isUsingAlternateRule,
            interestBindMonthCount: loan.interestBindMonthCount,
        };
    }

    private setNewRevalueModel(revalueModel: RevalueModel) {
        if (!revalueModel) return;

        if (revalueModel.keepExistingRuleCode) {
            this.m.initLoanAmortizationData.initLoanData = this.getLoanAmortizationInitialData(false, true);
        } else {
            this.m.initLoanAmortizationData.initLoanDataKeepExistingRuleCode =
                this.getLoanAmortizationInitialData(true);
        }

        this.m.revaluePreview.isKeepExistingRuleCodeChosen = revalueModel.keepExistingRuleCode;

        this.m.revaluePreview.editedNewBasisToCommit = revalueModel.keepExistingRuleCode
            ? this.m.revaluePreview.newBasis
            : this.m.revaluePreview.newBasisKeepExistingRuleCode;

        this.m.revaluePreview.editedNewBasisToCommit.loans = revalueModel.editedLoans;
        this.m.revaluePreview.isValid = revalueModel.isValid;
    }

    public async commitRevalue(evt?: Event) {
        evt?.preventDefault();

        await this.apiService.commitRevaluate(this.m.revaluePreview.editedNewBasisToCommit);
        await this.reload(this.m.creditNr);
        this.eventService.signalReloadCreditComments(this.m.creditNr);
    }

    getAmortizationBasisPdfUrl() {
        return this.m
            ? this.ntechApiService.getUiGatewayUrl('nCredit', 'Api/Credit/MortageLoanAmortizationBasisPdf', [
                  ['creditNr', this.m.creditNr],
              ])
            : '';
    }

    public getLoanAmortizationInitialData(
        keepExistingRuleCode: boolean,
        isChosen?: boolean
    ): MlSeLoanAmortizationComponentInitialData {
        if (!this.m) {
            return null;
        }

        return {
            newRevalueModel: this.m.revaluePreview.newRevalueModel,
            creditNr: this.m.creditNr,
            loans: keepExistingRuleCode
                ? this.m.revaluePreview.currentLoansKeepExistingRuleCode
                : this.m.revaluePreview.currentLoans,
            isKeepExistingRuleCodeChosen: isChosen ?? false,
            isKeepExistingRuleCodeAllowed: this.m.revaluePreview.isKeepExistingRuleCodeAllowed,
            keepExistingRuleCode: keepExistingRuleCode,
        };
    }
}

interface Model {
    creditNr: string;
    basisResult: LoadAmortizationBasisSeResult;
    currentLoans: MlSeLoansComponentLoanModel[];
    loansData: MlSeLoansComponentInitialData;
    initLoanAmortizationData: {
        initLoanData: MlSeLoanAmortizationComponentInitialData;
        initLoanDataKeepExistingRuleCode: MlSeLoanAmortizationComponentInitialData;
    };
    revalueForm?: FormsHelper;
    revaluePreview?: RevaluePreview;
}

interface RevaluePreview {
    newBasis: MortgageLoanSeAmortizationBasisModel;
    newBasisKeepExistingRuleCode: MortgageLoanSeAmortizationBasisModel;
    editedNewBasisToCommit: MortgageLoanSeAmortizationBasisModel;
    newUnderlag: SwedishAmorteringsunderlag;
    newUnderlagKeepExistingRuleCode: SwedishAmorteringsunderlag;
    currentLoans: MlSeLoansComponentLoanModel[];
    currentLoansKeepExistingRuleCode?: MlSeLoansComponentLoanModel[];
    isKeepExistingRuleCodeAllowed: boolean;
    isKeepExistingRuleCodeChosen: boolean;
    newRevalueModel: BehaviorSubject<RevalueModel>;
    isValid: boolean;
}

export interface MlSeLoansComponentLoanModel {
    loan: MortgageLoanSeCurrentLoanModel;
    actualFixedMonthlyPayment: number;
    ruleCode: string;
    isUsingAlternateRule: boolean;
    interestBindMonthCount: number;
}
