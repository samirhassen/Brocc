import { Component, Input, SimpleChanges } from '@angular/core';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { SettingModel, SettingsHelper } from '../../services/settings-model';
import { SettingAddRemoveSingleFormComponentInitialData } from '../setting-types/setting-addremovesingle/setting-add-remove-rows.component';
import { BankAccountComponentInitialData } from '../setting-types/setting-bankaccount/setting-bankaccount.component';
import { SettingFormComponentInitialData } from '../setting-types/setting-form/setting-form.component';
import { SettingHtmlTemplateComponentInitialData } from '../setting-types/setting-htmltemplate/setting-htmltemplate.component';
import { SettingKycUpdateFrequencyComponentInitialData } from '../setting-types/setting-kyc-update-frequency/setting-kyc-update-frequency.component';
import { SettingComponentInitialData } from '../setting-types/setting-component/setting-component.component';

@Component({
    selector: 'setting-single',
    templateUrl: './setting-single.component.html',
    styles: [],
})
export class SettingSingleComponent {
    constructor() {}

    @Input()
    public initialData: SettingSingleComponentInitialData;

    public m: Model;

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let m: Model;

        let helper = this.initialData.settingsHelper;
        let setting = this.initialData.setting;

        if (helper.isComponentSetting(setting)) {
            m = {
                code: setting.Code,
                isPage: true,
                displayName: setting.DisplayName,
                componentInitialData: {
                    componentSetting: setting,
                    backTarget: CrossModuleNavigationTarget.create('Settings', {}),
                },
            };
        } else if (helper.isFormSetting(setting)) {
            m = {
                code: setting.Code,
                isForm: true,
                displayName: setting.DisplayName,
                formInitialData: {
                    formSetting: setting,
                },
            };
        } else if (helper.isHtmlTemplateSetting(setting)) {
            m = {
                code: setting.Code,
                isHtmlTemplate: true,
                displayName: setting.DisplayName,
                htmlTemplateInitialData: {
                    setting: setting,
                },
            };
        } else if (helper.isAddRemoveRowsSetting(setting)) {
            m = {
                code: setting.Code,
                isAddRemoveRows: true,
                displayName: setting.DisplayName,
                addRemoveRowsInitialData: {
                    setting: setting,
                },
            };
        } else if (helper.isBankAccountSetting(setting)) {
            m = {
                code: setting.Code,
                isBankAccount: true,
                displayName: setting.DisplayName,
                bankAccountInitialData: {
                    setting: setting,
                },
            };
        } else if (helper.isKycUpdateFrequencySetting(setting)) {
            m = {
                code: setting.Code,
                isKycUpdateFrequency: true,
                displayName: setting.DisplayName,
                kycUpdateFrequencyInitialData: {
                    setting: setting,
                },
            };
        }
        this.m = m;
    }
}

export class SettingSingleComponentInitialData {
    settingsHelper: SettingsHelper;
    setting: SettingModel;
}

class Model {
    code: string;
    isForm?: boolean;
    isPage?: boolean;
    isHtmlTemplate?: boolean;
    isAddRemoveRows?: boolean;
    isBankAccount?: boolean;
    isKycUpdateFrequency?: boolean;
    isGroup?: boolean;
    displayName: string;
    componentInitialData?: SettingComponentInitialData;
    formInitialData?: SettingFormComponentInitialData;
    htmlTemplateInitialData?: SettingHtmlTemplateComponentInitialData;
    addRemoveRowsInitialData?: SettingAddRemoveSingleFormComponentInitialData;
    bankAccountInitialData?: BankAccountComponentInitialData;
    kycUpdateFrequencyInitialData?: SettingKycUpdateFrequencyComponentInitialData;
}
