import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, UntypedFormGroup, Validators } from '@angular/forms';
import { from, of } from 'rxjs';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { Dictionary, getBankAccountTypeDropdownOptions } from 'src/app/common.types';
import { CustomerPagesEventService } from '../../../common-services/customerpages-event.service';
import {
    BankAccountNrValidationRequest,
    BankAccountNrValidationResult,
    BankAccountsTaskModel,
    CustomerPagesApplicationsApiService,
} from '../../services/customer-pages-applications-api.service';
import { CustomerPagesConfigService } from '../../../common-services/customer-pages-config.service';
import { createCountrySpecificValidatorUsingCountryIsoCode } from 'src/app/country-specific/country-specific-validator';

const RefTypeReference = 'ref';
const RefTypeMessage = 'msg';

@Component({
    selector: 'customer-pages-bankaccounts',
    templateUrl: './customer-pages-bankaccounts.component.html',
    styles: [],
})
export class CustomerPagesBankaccountsComponent implements OnInit {
    constructor(
        private apiService: CustomerPagesApplicationsApiService,
        private fb: UntypedFormBuilder,
        private eventService: CustomerPagesEventService,
        private config: CustomerPagesConfigService
    ) {}

    @Input()
    public initialData: CustomerPagesBankAccountsInitialData;

    public m: Model;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let i = this.initialData;

        let bankAccountRequest: Dictionary<BankAccountNrValidationRequest> = {};
        if (i.task?.PaidToCustomer?.BankAccountNr) {
            bankAccountRequest['p'] = {
                bankAccountNr: i.task.PaidToCustomer.BankAccountNr,
                bankAccountNrType: i.task.PaidToCustomer.BankAccountNrType,
            };
        }
        if (i.task?.LoansToSettle) {
            for (let s of i.task.LoansToSettle.filter((x) => x.BankAccountNr)) {
                bankAccountRequest['L' + s.Nr] = {
                    bankAccountNr: s.BankAccountNr,
                    bankAccountNrType: s.BankAccountNrType,
                };
            }
        }

        this.apiService.validateBankAccountNrsBatch(bankAccountRequest, false).then((x) => {
            let m: Model = {
                view: {
                    paidToCustomer: null,
                    loansToSettle: null,
                },
                dummyViewForm: this.fb.group({}),
                isPossibleToEditPaidToCustomerBankAccount:
                    this.initialData.task.IsPossibleToEditPaidToCustomerBankAccount,
                isPossibleToEditLoansToSettleBankAccounts:
                    this.initialData.task.IsPossibleToEditLoansToSettleBankAccounts,
                isPossibleToConfirm: false,
                isBankAccountsConfirmed: this.initialData.task.IsAccepted,
            };

            let p = i.task.PaidToCustomer;
            m.view.paidToCustomer = {
                amount: p.Amount,
                account: new ViewAccount(p.BankAccountNr, p.BankAccountNrType, x.ValidatedAccountsByKey['p']),
            };

            let paidToCustomerAccountValid = x.ValidatedAccountsByKey['p']?.IsValid ?? false;

            m.view.loansToSettle = [];
            let settleAccountsValid = false;

            if (i.task?.LoansToSettle?.length > 0) {
                for (let s of i.task.LoansToSettle) {
                    let account = x.ValidatedAccountsByKey['L' + s.Nr];
                    m.view.loansToSettle.push({
                        nr: s.Nr,
                        loanType: s.LoanType,
                        currentDebtAmount: s.CurrentDebtAmount,
                        monthlyCostAmount: s.MonthlyCostAmount,
                        settlementPaymentReference: s.SettlementPaymentReference,
                        settlementPaymentMessage: s.SettlementPaymentMessage,
                        account: new ViewAccount(s.BankAccountNr, s.BankAccountNrType, account),
                    });
                    if (account?.IsValid) settleAccountsValid = true;
                }
            } else settleAccountsValid = true; // when no loans to settle

            m.isPossibleToConfirm =
                !this.initialData.task.IsAccepted && paidToCustomerAccountValid && settleAccountsValid;

            this.m = m;
        });
    }

    getOtherLoanTypeDisplayName(loanType: string) {
        if (!loanType) {
            return '';
        }
        let t = this.initialData.otherLoanTypes.find((x) => x.Code == loanType);
        if (t) {
            return t.DisplayName;
        } else {
            return loanType;
        }
    }

    beginEditNewLoan(evt?: Event) {
        let form = new FormsHelper(this.fb.group({}));

        let bankAccountValidator = FormsHelper.createValidatorAsync('bankAccountNr', (x) => {
            if (!x) {
                return of(true); //Handled by required
            }
            return from(
                this.apiService
                    .validateBankAccountNrsBatch({ b: { bankAccountNr: x, bankAccountNrType: '' } }, true)
                    .then((y) => {
                        let result = y.ValidatedAccountsByKey['b'];
                        return result?.IsValid === true;
                    })
            );
        });
        form.addControlIfNotExists(
            'bankAccountNr',
            this.m.view.paidToCustomer.account.bankAccountNr,
            [Validators.required],
            [bankAccountValidator]
        );
        this.m.edit = {
            paidToCustomerForm: form,
            loansToSettleForm: null,
        };
    }

    onCancelNewLoan(evt?: Event) {
        this.m.edit = null;
    }

    onSaveNewLoan(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.initialData.applicationNr;
        this.apiService
            .editBankAccounts({
                applicationNr: applicationNr,
                paidToCustomer: {
                    bankAccountNr: this.m.edit.paidToCustomerForm.getValue('bankAccountNr'),
                    bankAccountNrType: null,
                },
            })
            .then((_) => {
                this.eventService.signalReloadApplication(this.initialData.applicationNr);
            });
    }

    beginEditLoansToSettle(evt?: Event) {
        evt?.preventDefault();

        let form = new FormsHelper(this.fb.group({}));

        for (let account of this.m.view.loansToSettle) {
            let refType = this.currentSettlementPaymentReferenceType(account);
            form.addControlIfNotExists('bankAccountNr' + account.nr, account.account.bankAccountNr, [
                Validators.required,
            ]);
            form.addControlIfNotExists('bankAccountNrType' + account.nr, account.account.bankAccountNrType, [
                Validators.required,
            ]);
            form.addControlIfNotExists(
                'settlementPaymentReference' + account.nr,
                refType == RefTypeReference ? account.settlementPaymentReference : account.settlementPaymentMessage,
                [Validators.minLength(2)]
            );
            form.addControlIfNotExists('settlementPaymentReferenceType' + account.nr, refType, [Validators.required]);
        }

        let countryValidator = createCountrySpecificValidatorUsingCountryIsoCode(this.config.baseCountry());
        form.form.setAsyncValidators([
            (x) => {
                let request: Dictionary<BankAccountNrValidationRequest> = {};
                for (let loan of this.m.view.loansToSettle) {
                    let bankAccountNr = form.getValue('bankAccountNr' + loan.nr);
                    let bankAccountNrType = form.getValue('bankAccountNrType' + loan.nr);
                    if (bankAccountNr) {
                        request['bankAccountInvalid' + loan.nr] = {
                            bankAccountNr: bankAccountNr,
                            bankAccountNrType: bankAccountNrType,
                        };
                    }
                }
                let result = this.apiService.validateBankAccountNrsBatch(request, true).then((x) => {
                    let errors: Dictionary<boolean> = {};
                    for (let errorName of Object.keys(x.ValidatedAccountsByKey)) {
                        if (!x.ValidatedAccountsByKey[errorName].IsValid) {
                            errors[errorName] = true;
                        }
                    }
                    return errors;
                });
                return from(result);
            },
            (x) => {
                let errors: Dictionary<boolean> = {};
                for (let loan of this.m.view.loansToSettle) {
                    let bankAccountNrType = form.getValue('bankAccountNrType' + loan.nr);
                    let settlementPaymentReferenceType = this.currentSettlementPaymentReferenceType(loan.nr);
                    let settlementPaymentReference = form.getValue('settlementPaymentReference' + loan.nr);
                    if (
                        settlementPaymentReferenceType === RefTypeReference &&
                        !countryValidator.isValidPaymentFileReferenceNr(bankAccountNrType, settlementPaymentReference)
                    ) {
                        errors['settlementPaymentReferenceInvalid' + loan.nr] = true;
                    }
                }
                return of(errors);
            },
        ]);

        this.m.edit = {
            paidToCustomerForm: null,
            loansToSettleForm: form,
        };
    }

    currentSettlementPaymentReferenceType(loanViewOrEditNr: LoanToSettleView | number): string {
        if (typeof loanViewOrEditNr === 'number') {
            return this.m.edit.loansToSettleForm.getValue('settlementPaymentReferenceType' + loanViewOrEditNr);
        } else {
            let hasRef = !!loanViewOrEditNr.settlementPaymentReference;
            let hasMsg = !!loanViewOrEditNr.settlementPaymentMessage;
            return hasRef || (!hasRef && !hasMsg) ? RefTypeReference : RefTypeMessage;
        }
    }

    onCancelLoansToSettle(evt?: Event) {
        this.m.edit = null;
    }

    onSaveLoansToSettle(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.initialData.applicationNr;
        this.apiService
            .editBankAccounts({
                applicationNr: applicationNr,
                loansToSettle: {
                    Accounts: this.m.view.loansToSettle.map((x) => {
                        let refType = this.currentSettlementPaymentReferenceType(x.nr);
                        let f = this.m.edit.loansToSettleForm;
                        return {
                            nr: x.nr,
                            bankAccountNr: f.getValue('bankAccountNr' + x.nr),
                            bankAccountNrType: f.getValue('bankAccountNrType' + x.nr),
                            settlementPaymentReference:
                                refType === RefTypeReference ? f.getValue('settlementPaymentReference' + x.nr) : null,
                            settlementPaymentMessage:
                                refType === RefTypeMessage ? f.getValue('settlementPaymentReference' + x.nr) : null,
                        };
                    }),
                },
            })
            .then((_) => {
                this.eventService.signalReloadApplication(this.initialData.applicationNr);
            });
    }

    approveBankAccounts() {
        this.apiService.confirmBankAccounts(this.initialData.applicationNr).then((_) => {
            this.eventService.signalReloadApplicationAndClosePopup(this.initialData.applicationNr);
        });
    }

    getPossibleBankAccountNrTypes() {
        return getBankAccountTypeDropdownOptions(
            this.config.getClient().BaseCountry,
            true,
            this.config.uiLanguage(),
            true
        );
    }
}

class Model {
    view: {
        paidToCustomer: {
            amount: number;
            account: ViewAccount;
        };
        loansToSettle: LoanToSettleView[];
    };
    dummyViewForm: UntypedFormGroup; //Just so we can have the viewmode inside the form tag and not get yelled at for having a null form
    edit?: {
        paidToCustomerForm: FormsHelper;
        loansToSettleForm: FormsHelper;
    };
    isPossibleToEditPaidToCustomerBankAccount: boolean;
    isPossibleToEditLoansToSettleBankAccounts: boolean;
    isPossibleToConfirm: boolean;
    isBankAccountsConfirmed: boolean;
}

interface LoanToSettleView {
    nr: number;
    account: ViewAccount;
    loanType: string;
    currentDebtAmount: number;
    monthlyCostAmount: number;
    settlementPaymentReference: string;
    settlementPaymentMessage: string;
}

class ViewAccount {
    constructor(
        public bankAccountNr: string,
        public bankAccountNrType: string,
        private parsedAccount: BankAccountNrValidationResult
    ) {}

    public getAccountNrDisplay() {
        let v = this.parsedAccount?.ValidAccount;
        let result = v?.DisplayNr || this.bankAccountNr;
        if (v?.BankName) {
            result += ` (${v.BankName})`;
        }

        return result;
    }

    public getAccountTypeDisplay() {
        let v = this.parsedAccount?.ValidAccount;
        //@ts-ignore TODO remove unused locals
        let result = v?.DisplayNr || this.bankAccountNr;
        if (v?.BankName) {
            result += ` (${v.BankName})`;
        }
        let accountNrType = v?.BankAccountNrType || this.bankAccountNrType;
        if (accountNrType == 'BankGiroSe') {
            return 'Bankgiro';
        } else if (accountNrType == 'PlusGiroSe') {
            return 'Plusgiro';
        }

        return '';
    }
}

export class CustomerPagesBankAccountsInitialData {
    applicationNr: string;
    task: BankAccountsTaskModel;
    otherLoanTypes: { Code: string; DisplayName: string }[];
}
