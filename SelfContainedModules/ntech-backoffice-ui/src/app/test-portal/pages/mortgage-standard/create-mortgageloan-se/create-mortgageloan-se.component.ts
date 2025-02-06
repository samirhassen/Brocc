import { Component, OnInit } from '@angular/core';
import { UntypedFormBuilder } from '@angular/forms';
import { JsonEditorOptions } from 'ang-jsoneditor';
import { ToastrService } from 'ngx-toastr';
import { ConfigService } from 'src/app/common-services/config.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { Dictionary } from 'src/app/common.types';
import { CustomCostFormPrefix, MortgageLoanSeLoanBuilder, UiLoanModel } from './mortgageloan-se.loanbuilder';
import { MortgageLoanSeService } from './mortgageloan-se.service';
import { PaymentOrderService, PaymentOrderUiItem } from 'src/app/common-services/payment-order-service';
import { AgreementNrFeatureName } from 'src/app/common-services/credit-features';

const LoanDefaultFormValues: Dictionary<string> = {
    'loanAmount': '1000000',
    'maxLoanAmount': '',
    'marginInterestRatePercent': '0.5',
    'rebindingMonthCount': '3',
    'isUsingAlternateAmortizationRule': 'false',
    'amortizationException': 'none'
};

@Component({
    selector: 'app-create-mortgageloan-se',
    templateUrl: './create-mortgageloan-se.component.html',
    styles: [
    ]
})
export class CreateMortgageloanSeComponent implements OnInit {
    constructor(private apiService: MortgageLoanSeService, private formBuilder: UntypedFormBuilder,
        private validationService: NTechValidationService, private toastr: ToastrService,
        private config: ConfigService, private paymentOrderService: PaymentOrderService) { }

    public m: Model

    private LoanHistoryLocalStorageKeyName: string = 'NTech-MortgageLoansStandardSEHistoryV1';

    ngOnInit(): void {
        this.reload();
    }

    private async reload() {
        let m: Model = {
            isAgreementNrAllowed: this.config.isFeatureEnabled(AgreementNrFeatureName),
            currentFixedRates: (await this.apiService.testPortal().fetchMortgageLoanFixedInterstRates())?.CurrentRates.map(x => ({
                MonthCount: x.MonthCount.toString(),
                RatePercent: x.RatePercent
            })),
            form: null,
            loansToBeAdded: [],
            historyItems: this.getHistoryItems(),
            possibleExceptions: [{
                key: 'none',
                text: 'Nej'
            }, {
                key: 'twoYearsZero',
                text: '2 år, amorteringsfritt'
            }],
            customPaymentOrderItems: (await this.paymentOrderService.getPaymentOrderUiItems()).filter(x => x.orderItem.isBuiltin === false)
        };

        let fields: {[key: string]: any} = {
            'nrOfApplicants': ['2', []],
            'applicant1CivicRegNr': ['', [this.validationService.getCivicRegNrValidator()]],
            'applicant2CivicRegNr': ['', [this.validationService.getCivicRegNrValidator()]],
            'nrOfConsentingParties': ['0', []],
            'consentingParty1CivicRegNr': ['', [this.validationService.getCivicRegNrValidator()]],
            'collateralType': ['newBrf', []],
            'reuseCollateralCreditNr': ['', []],
            'newCollateralLtvPercent': ['55', []],
            'newCollateralLtiFraction': ['5', []],
            'newCollateralAmortizationRuleCode': ['r201723', []],
            'loanAmount': ['', [this.validationService.getPositiveIntegerValidator()]],
            'maxLoanAmount': ['', [this.validationService.getPositiveIntegerValidator()]],
            'marginInterestRatePercent': ['', [this.validationService.getDecimalValidator()]],
            'rebindingMonthCount': ['', []],
            'isUsingAlternateAmortizationRule': ['', []],
            'amortizationException': ['', []],
            'objectValuationAgeInMonths': ['0', []]
        }

        for(let item of m.customPaymentOrderItems) {
            fields[CustomCostFormPrefix + item.uniqueId] = ['0', [this.validationService.getDecimalValidator()]]
        }

        if(m.isAgreementNrAllowed) {
            fields['mortgageLoanAgreementNr'] = ['', []]
        }

        m.form = new FormsHelper(this.formBuilder.group(fields));

        this.resetLoanForm(m);

        this.m = m;
    }

    public async addLoan(evt?: Event) {
        evt?.preventDefault();

        let newLoan: Dictionary<string> = {};
        Object.keys(LoanDefaultFormValues).forEach(nameToCopy => newLoan[nameToCopy] = this.m.form.getValue(nameToCopy));
        for(let cost of this.m.customPaymentOrderItems) {
            let name = CustomCostFormPrefix + cost.uniqueId;
            newLoan[name] = this.m.form.getValue(name);
        }

        this.m.loansToBeAdded.push({
            raw: newLoan,
            parsed: MortgageLoanSeLoanBuilder.parseUiLoan(newLoan, null, this.m.currentFixedRates, this.m.customPaymentOrderItems)
        });
        this.resetLoanForm(this.m);
    }

    private createBuilder(): MortgageLoanSeLoanBuilder {
        return new MortgageLoanSeLoanBuilder(this.apiService, this.m.currentFixedRates, this.config,
            x => this.m.form.getValue(x), this.m.loansToBeAdded.map(x => x.raw), this.m.customPaymentOrderItems);
    }

    public async createInLedger(editRequestBeforeSending: boolean, evt?: Event) {
        evt?.preventDefault();

        try {
            let builder = this.createBuilder();

            let loanRequest = await builder.createLoanRequestBasedOnForm();

            if(editRequestBeforeSending) {
                this.m.requestEditForm = new FormsHelper(this.formBuilder.group({
                    'requestModel': [loanRequest, []]
                }));
            } else {
                this.createInLedgerFromRequest(loanRequest);
            }
        } catch(e) {
            this.toastr.error(e.toString());
        }
    }

    public async createInLedgerFromRequest(loanRequest: any, evt ?: Event) {
        evt?.preventDefault();

        try {
            let builder = this.createBuilder();
            let result = await builder.createLoanBasedOnFormRequest(loanRequest);

            this.addToHistory({
                createdDate: new Date(),
                creditNrs: result.creditNrs
            });

            this.toastr.info('Loans added');

            this.reload();
        } catch(e: any) {
            let errorMessages : string[] = e?.error?.Error;
            if(errorMessages) {
                this.toastr.error(errorMessages.join(', '));
            } else {
                this.toastr.error('Create failed');
            }
        }
    }

    public removeLoan(i: number, evt?: Event) {
        evt?.preventDefault();
        this.m.loansToBeAdded.splice(i, 1);
    }

    public clearHistoryItems() {
        localStorage.removeItem(this.LoanHistoryLocalStorageKeyName);
        this.m.historyItems = [];
    }

    public getUrlToCredit(item: LoanHistoryItem) {
        return this.config
                .getServiceRegistry()
                .createUrl('nCredit', 'Ui/Credit', [['creditNr', item.creditNrs[0]]])
    }

    public getExceptionText(key: string) {
        return this.m?.possibleExceptions?.find(x => x.key === key)?.text ?? key;
    }

    private resetLoanForm(m: Model) {
        Object.keys(LoanDefaultFormValues).forEach(name => m.form.setValue(name, LoanDefaultFormValues[name]));
    }

    private addToHistory(item: LoanHistoryItem) {
        let items = this.getHistoryItems();
        items.push(item);
        localStorage.setItem(this.LoanHistoryLocalStorageKeyName, JSON.stringify(items));
        this.m.historyItems = items;
    }

    private getHistoryItems(): LoanHistoryItem[] {
        let itemsJson = localStorage.getItem(this.LoanHistoryLocalStorageKeyName);
        if (!itemsJson) {
            return [];
        }

        let items = JSON.parse(itemsJson) as LoanHistoryItem[];
        return items.sort((a: LoanHistoryItem, b: LoanHistoryItem) => {
            return <any>new Date(b.createdDate) - <any>new Date(a.createdDate);
        });
    }

    getJsonEditorOptions() {
        let options = new JsonEditorOptions();
        options.mode = 'tree';
        options.modes = ['text', 'tree'];
        return options;
    }
}

interface Model {
    isAgreementNrAllowed: boolean
    historyItems: LoanHistoryItem[]
    form: FormsHelper
    currentFixedRates?: {
        MonthCount: string;
        RatePercent: number;
    }[]
    loansToBeAdded: {
        raw: Dictionary<string>
        parsed: UiLoanModel
    }[]
    possibleExceptions: {
        key: string
        text: string
    }[]
    requestEditForm ?: FormsHelper
    customPaymentOrderItems: PaymentOrderUiItem[]
}

interface LoanHistoryItem {
    creditNrs: string[];
    createdDate: Date;
}
