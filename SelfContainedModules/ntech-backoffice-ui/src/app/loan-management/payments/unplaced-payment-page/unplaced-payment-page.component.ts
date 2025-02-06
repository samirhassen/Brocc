import { Component, OnInit } from '@angular/core';
import { AsyncValidatorFn, UntypedFormBuilder, UntypedFormGroup, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { BankAccountNrValidationService } from 'src/app/common-services/bank-account-nr-validation.service';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { RepaymentInitialData } from '../repayment/repayment.component';
import { PlaceCreditsInitialData } from '../place-credits/place-credits.component';
import { PaymentOrderService, PaymentOrderUiItem } from 'src/app/common-services/payment-order-service';
import { NullableNumber } from 'src/app/common.types';

@Component({
    selector: 'app-unplaced-payment-page',
    templateUrl: './unplaced-payment-page.component.html',
    styles: [
    ]
})
export class UnplacedPaymentPageComponent implements OnInit {
    constructor(private route: ActivatedRoute, private apiService: NtechApiService, private config: ConfigService, private formBuilder: UntypedFormBuilder,
        private validationService: NTechValidationService, private bankAccountValidationService: BankAccountNrValidationService,
        private paymentOrderService: PaymentOrderService) { }

    public m: Model;

    async ngOnInit(): Promise<void> {
        this.route.paramMap.subscribe((params: ParamMap) => {
            this.reload(parseInt(params.get('paymentId')));
        });
    }

    private async reload(paymentId: number) {
        let initialPayment = await this.apiService.post<PaymentInitialDataModel>('NTechHost', 'Api/Credit/PaymentPlacement/Placement-InitialData', { paymentId });
        let paymentOrderUiItems = await this.paymentOrderService.getPaymentOrderUiItems();

        let m: Model = {
            isPlaceMode: true,
            paymentId: paymentId,
            payment: initialPayment,
            matchedCreditNrsText: initialPayment?.matchedCreditNrs?.join(', '),
            bankAccountNrTypes: [],
            repayForm: null,
            placeForm: null,
            placeFailedMessage: null,
            paymentOrderUiItems: paymentOrderUiItems
        }

        if (this.config.baseCountry() === 'FI') {
            m.bankAccountNrTypes = [{ code: 'IBANFi', text: 'IBAN' }]
        } else {
            m.bankAccountNrTypes = [{ code: 'BankAccountSe', text: 'Bank account nr' }]
            if (this.config.isFeatureEnabled('ntech.feature.companyloans')) {
                m.bankAccountNrTypes.push({ code: 'BankGiroSe', text: 'Bankgiro nr' })
                m.bankAccountNrTypes.push({ code: 'PlusGiroSe', text: 'Plusgiro nr' })
            }
        }

        let repayForm: UntypedFormGroup;

        let repaymentIbanValidator : AsyncValidatorFn = async ctrl => {
            let p = new Promise<ValidationErrors>(async resolve =>  {
                if(!repayForm) {
                    resolve(null);
                    return;
                }
                m.repaymentFormBankAccountInfo = null;
                let errors : ValidationErrors = {};
                let bankAccountNrType = repayForm.get('bankAccountNrType')?.value;
                let repaymentIBAN = repayForm.get('repaymentIBAN')?.value;
                if(bankAccountNrType && repaymentIBAN) {
                    let bankAccountResult = await this.bankAccountValidationService.validateBankAccountNr({ bankAccountNr: repaymentIBAN, bankAccountNrType: bankAccountNrType }, { skipLoadingIndicator: true });
                    if(!bankAccountResult.IsValid) {
                        errors['repaymentIBAN'] = 'Invalid'
                    } else {
                        let v = bankAccountResult.ValidAccount;
                        m.repaymentFormBankAccountInfo = {
                            displayValue: v.DisplayNr + (v.BankName ? ` (${v.BankName})` : '')
                        }
                    }
                }

                resolve(Object.keys(errors).length > 0 ? errors : null);
            });
            return p;
        }

        repayForm = this.formBuilder.group({
            'bankAccountNrType': [m.bankAccountNrTypes[0].code, [Validators.required]],
            'repaymentIBAN': ['', [Validators.required], [repaymentIbanValidator]],
            'repaymentAmount': ['', [Validators.required, this.validationService.getPositiveDecimalValidator()]],
            'repaymentName': ['', [Validators.required]]
        });

        m.repayForm = new FormsHelper(repayForm);

        let placeForm = this.formBuilder.group({
            'searchString': ['', [Validators.required]],
            'onlyPlaceAgainstNotified': [false, [Validators.required]],
            'maxPlacedAmount': ['', [this.validationService.getPositiveDecimalValidator()]],
            'onlyPlaceAgainstPaymentOrderItemUniqueId': ['', []]

        });
        m.placeForm = new FormsHelper(placeForm);

        this.m = m;

        if(m.payment && m.payment.matchedCreditNrs && m.payment.matchedCreditNrs.length === 1) {
            setTimeout(() => {
                m.placeForm.setValue('searchString', m.payment.matchedCreditNrs[0]);
                this.verifyPlace();
            }, 0);
        }
    }

    itemExists(name: string) {
        if(!this.m) {
            return false;
        }
        return !!this.m.payment.items.find(x => x.name === name);
    }


    itemValue(name: string) {
        if(!this.m) {
            return false;
        }
        let item = this.m.payment.items.find(x => x.name === name);
        if(!item) {
            return null;
        }
        if(item.isEncrypted) {
            return '----';
        } else {
            return item.value;
        }
    }

    itemLabel(name: string) {
        return name;
    }

    itemsExcept(skippedNames: string[]) {
        if(!this.m) {
            return [];
        }
        return this.m.payment.items.filter(x => !skippedNames.includes(x.name));
    }

    async unlock(item: PaymentInitialDataItemModel, evt ?: Event) {
        evt?.preventDefault();
        let result = await this.apiService.post<{ decryptedValue: string }>('NTechHost', 'Api/Credit/Encryption/Decrypt', { id: item.decryptionId });
        item.isEncrypted = false;
        item.value = result.decryptedValue;
    }

    getAccountNrFieldLabel() {
        if(!this.m) {
            return null;
        }

        if (this.m.bankAccountNrTypes.length > 1) {
            return 'Account nr';
        }

        let bankAccountNrType = this.m.repayForm.getValue('bankAccountNrType');
        for (var i = 0; i < this.m.bankAccountNrTypes.length; i++) {
            if (this.m.bankAccountNrTypes[i].code === bankAccountNrType) {
                return this.m.bankAccountNrTypes[i].text
            }
        }
        return 'Account nr'
    }

    getAccountNrMask() {
        //TODO: Does not seem to work where we are migrating from. Fix maybe?
        return ''
    }



    async verifyRepay(evt ?: Event) {
        evt?.preventDefault();

        this.m.place = null;

        if (this.m.repayForm.invalid()) {
            return
        }
        let f = this.m.repayForm;
        let repaymentAmount = this.validationService.parsePositiveDecimalOrNull(f.getValue('repaymentAmount'));
        if(!repaymentAmount) {
            return;
        }
        if(repaymentAmount > this.m.payment.unplacedAmount) {
            this.m.isRepaymentAmountNotWithinBounds = true;
            return;
        }
        this.m.isRepaymentAmountNotWithinBounds = false;

        let bankAccountNrType = f.getValue('bankAccountNrType');
        let repaymentIBAN = f.getValue('repaymentIBAN');

        let result = await this.bankAccountValidationService.validateBankAccountNr({
            bankAccountNr: repaymentIBAN,
            bankAccountNrType:bankAccountNrType
        });
        if(!result.IsValid) {
            //NOTE: This should never happen since it's already been validated
            return;
        }

        this.m.repayment = {
            unplacedAmount: this.m.payment.unplacedAmount,
            paymentId: this.m.paymentId,
            repaymentAmount: repaymentAmount,
            repayToBankAccount: result.ValidAccount,
            repayToName: f.getValue('repaymentName')
        }
    }

    async verifyPlace(evt ?: Event) {
        evt?.preventDefault();

        this.m.repayment = null;
        this.m.place = null;
        this.m.placeFailedMessage = null;

        let result = await this.apiService.post<{
            creditNrs: string[],
            failedMessage ?: string
        }>('NTechHost', 'Api/Credit/PaymentPlacement/Find-PaymentPlacement-CreditNrs', {
            searchString: this.m.placeForm.getValue('searchString')
        }, {
            handleNTechError: error => {
                return {
                    creditNrs: null,
                    failedMessage: error.errorMessage
                };
            }
        });

        if(result.failedMessage || !result.creditNrs || result.creditNrs.length === 0) {
            this.m.placeFailedMessage = result.failedMessage ?? 'No credits found';
            return;
        }

        let onlyPlaceAgainstPaymentOrderItemUniqueId =  this.m.placeForm.getValue('onlyPlaceAgainstPaymentOrderItemUniqueId');
        let maxPlacedAmountRaw = this.m.placeForm.getValue('maxPlacedAmount');

        this.m.place = {
            onlyPlaceAgainstNotified: this.m.placeForm.getValue('onlyPlaceAgainstNotified'),
            creditNrs: result.creditNrs,
            paymentId: this.m.paymentId,
            onlyPlaceAgainstPaymentOrderItemUniqueId: onlyPlaceAgainstPaymentOrderItemUniqueId ? onlyPlaceAgainstPaymentOrderItemUniqueId : null,
            paymentOrderUiItems: this.m.paymentOrderUiItems,
            maxPlacedAmount: this.validationService.isNullOrWhitespace(maxPlacedAmountRaw) ? null : new NullableNumber(this.validationService.parsePositiveDecimalOrNull(maxPlacedAmountRaw))
        }
    }
}

interface PaymentInitialDataModel {
    id: number,
    items: PaymentInitialDataItemModel[],
    matchedCreditNrs: string[],
    paymentDate: string,
    unplacedAmount: number
}

interface PaymentInitialDataItemModel {
    itemId: number,
    name: string,
    isEncrypted: boolean,
    value: string
    decryptionId: number
}

interface Model {
    isPlaceMode: boolean,
    paymentId: number,
    payment: PaymentInitialDataModel
    matchedCreditNrsText: string
    //Repay
    bankAccountNrTypes: {
        code: string,
        text: string
    }[]
    repayForm: FormsHelper
    repaymentFormBankAccountInfo ?: {
        displayValue: string
    }
    isRepaymentAmountNotWithinBounds?:boolean
    repayment ?: RepaymentInitialData
    //Place
    placeForm: FormsHelper
    placeFailedMessage: string
    paymentOrderUiItems : PaymentOrderUiItem[]
    place ?: PlaceCreditsInitialData
}