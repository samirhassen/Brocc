import { Component, Input, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import {
    BankAccountNrValidationResult,
    BankAccountNrValidationService,
} from 'src/app/common-services/bank-account-nr-validation.service';
import { ConfigService } from 'src/app/common-services/config.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { Dictionary } from 'src/app/common.types';
import { SettingsApiService } from 'src/app/settings/services/settings-api.service';
import { SettingModel } from 'src/app/settings/services/settings-model';

@Component({
    selector: 'setting-bankaccount',
    templateUrl: './setting-bankaccount.component.html',
    styles: [],
})
export class SettingBankaccountComponent {
    constructor(
        private fb: UntypedFormBuilder,
        private apiService: SettingsApiService,
        private bankAccountNrValidationService: BankAccountNrValidationService,
        private toastr: ToastrService,
        private config: ConfigService
    ) {}

    @Input()
    public initialData: BankAccountComponentInitialData;

    public m: Model;

    async ngOnChanges(changes: SimpleChanges) {
        this.reload();
    }

    private async reload() {
        this.m = null;

        let setting = this.initialData.setting;
        let settingsResult = await this.apiService.apiService.shared.getCurrentSettingValues(
            this.initialData.setting.Code
        );

        let currentAccount: BankAccountNrValidationResult;
        if (settingsResult.SettingValues['bankAccountNr'] !== 'none') {
            let bankAccountValidationResult = await this.bankAccountNrValidationService.validateBankAccountNr(
                {
                    bankAccountNr: settingsResult.SettingValues['bankAccountNr'],
                    bankAccountNrType: settingsResult.SettingValues['bankAccountNrTypeCode'],
                },
                { allowExternalSources: true }
            );

            currentAccount = bankAccountValidationResult;
        }

        let canEditIsEnabled =
            setting.BankAccountData.IsInitiallyEnabled === true || setting.BankAccountData.IsInitiallyEnabled === false;

        let allowedBankAccountNrTypeCodes: string[] = [];
        if (canEditIsEnabled) {
            allowedBankAccountNrTypeCodes.push('none');
        }
        if (setting.BankAccountData.OnlyTheseBankAccountNrTypes) {
            allowedBankAccountNrTypeCodes.push(...setting.BankAccountData.OnlyTheseBankAccountNrTypes);
        } else {
            allowedBankAccountNrTypeCodes.push(...this.bankAccountNrValidationService.getAllBankAccountNrTypeCodes());
            for (let excludedType of setting.BankAccountData.ExcludedAccountNrTypes ?? []) {
                let i = allowedBankAccountNrTypeCodes.indexOf(excludedType);
                if (i >= 0) {
                    allowedBankAccountNrTypeCodes.splice(i);
                }
            }
        }

        this.m = {
            settingCode: setting.Code,
            allowedBankAccountNrTypeCodes: allowedBankAccountNrTypeCodes,
            canEditIsEnabled: canEditIsEnabled,
            isEnabled: settingsResult.SettingValues['isEnabled'] === 'true',
            currentAccount: currentAccount,
        };
    }

    beginEdit(evt?: Event) {
        evt?.preventDefault();

        let c = this.m.currentAccount?.IsValid ? this.m.currentAccount : null;
        let f = new FormsHelper(
            this.fb.group({
                isEnabled: [this.m.isEnabled ? 'true' : 'false', [Validators.required]],
                bankAccountNrTypeCode: [c?.ValidAccount?.BankAccountNrType ?? 'none', [Validators.required]],
                bankAccountNr: [c?.ValidAccount?.NormalizedNr ?? '', []],
            })
        );

        f.form.setAsyncValidators(async (x) => {
            let bankAccountNrTypeCode = f.getValue('bankAccountNrTypeCode');
            let bankAccountNr = f.getValue('bankAccountNr');

            let isEnabled = f.getValue('isEnabled') === 'true';
            if (bankAccountNrTypeCode === 'none') {
                return isEnabled ? { cannotBeEnabled: true } : null;
            } else {
                if (!bankAccountNr && isEnabled) {
                    return { missingRequiredBankAccountNr: true };
                }

                if (!bankAccountNr && bankAccountNrTypeCode === 'none') {
                    return null;
                }

                let validationResult = await this.bankAccountNrValidationService.validateBankAccountNr(
                    {
                        bankAccountNr: bankAccountNr,
                        bankAccountNrType: bankAccountNrTypeCode,
                    },
                    { skipLoadingIndicator: true }
                );
                return validationResult.IsValid ? null : { invalidBankAccountNr: true };
            }
        });

        this.m.edit = {
            form: f,
        };
    }

    onCancelEdit(evt?: Event) {
        evt?.preventDefault();
        this.m.edit = null;
    }

    onCancelPending(evt?: Event) {
        evt?.preventDefault();
        this.m.pendingCommit = null;
    }

    async commit(evt?: Event) {
        evt?.preventDefault();

        let m = this.m;

        let newValues: Dictionary<string> = {
            isEnabled: m.pendingCommit.isEnabled ? 'true' : 'false',
            bankAccountNr: m.pendingCommit.newAccount?.ValidAccount?.NormalizedNr ?? 'none',
            bankAccountNrTypeCode: m.pendingCommit?.newAccount?.ValidAccount?.BankAccountNrType ?? 'none',
            twoLetterIsoCountryCode: m.pendingCommit?.newAccount?.ValidAccount ? this.config.baseCountry() : 'none',
        };

        await this.apiService.saveSettingValues(m.settingCode, newValues);
        await this.reload();
    }

    async onSetEditAsPending(evt?: Event) {
        evt?.preventDefault();

        let f = this.m.edit.form;
        let isEnabled = f.getValue('isEnabled') === 'true';
        let bankAccountNr = f.getValue('bankAccountNr');
        let bankAccountNrTypeCode = f.getValue('bankAccountNrTypeCode');
        let validationResult = null;

        if (bankAccountNrTypeCode !== 'none') {
            validationResult = await this.bankAccountNrValidationService.validateBankAccountNr(
                {
                    bankAccountNr,
                    bankAccountNrType: bankAccountNrTypeCode,
                },
                { allowExternalSources: true }
            );

            if (!validationResult.IsValid) {
                //If people ever end up here it's a bug
                this.toastr.error('Account invalid');
                return;
            }
        }

        this.m.pendingCommit = {
            isEnabled: isEnabled,
            newAccount: validationResult,
        };
    }

    getAccountTypeDisplayName(code: string) {
        if (code === 'none') {
            return '-';
        } else if (code === 'BankAccountSe') return 'Regular account';
        else if (code === 'BankGiroSe') return 'Bankgiro';
        else if (code === 'PlusGiroSe') return 'Plusgiro';
        else if (code === 'IBANFi') return 'Iban';
        else return code;
    }
}

interface Model {
    allowedBankAccountNrTypeCodes: string[];
    settingCode: string;
    canEditIsEnabled: boolean;
    isEnabled: boolean;
    currentAccount?: BankAccountNrValidationResult;
    edit?: {
        form: FormsHelper;
    };
    pendingCommit?: {
        isEnabled: boolean;
        newAccount?: BankAccountNrValidationResult;
    };
}

export interface BankAccountComponentInitialData {
    setting: SettingModel;
}
