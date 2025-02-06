import { ConfigService } from 'src/app/common-services/config.service';
import { Dictionary } from 'src/app/common.types';

export interface SettingsModel {
    Settings: SettingModel[];
    UiGroupDisplayNames: Dictionary<string>;
}

export interface SettingModel {
    Code: string;
    DisplayName: string;
    Type: string;
    UiGroupName?: string;
    Features?: {
        RequireAny?: string[];
        RequireAll?: string[];
    };
    Groups?: {
        RequireAny?: string[];
    };
    ComponentData?: {
        ComponentName: string;
    };
    FormData?: {
        Fields: SettingFormDataFieldModel[];
    };
    KycUpdateFrequencyData?: {
        DefaultMonthCount: number;
    };
    BankAccountData?: BankAccountDataModel;
}

export interface SettingFormDataFieldModel {
    Name: string;
    Type: string;
    DisplayName: string;
    EnumOptions: { Code: string; DisplayName: string }[];
    NrOfRows?: number;
}

export class SettingsHelper {
    constructor(public model: SettingsModel) {}

    public isSettingEnabled(s: SettingModel, config: ConfigService) {
        if (
            s?.Features?.RequireAny &&
            s.Features.RequireAny.length > 0 &&
            !config.isAnyFeatureEnabled(s.Features.RequireAny)
        ) {
            return false;
        }

        if (
            s?.Features?.RequireAll &&
            s.Features.RequireAll.length > 0 &&
            !config.areAllFeaturesEnabled(s.Features.RequireAll)
        ) {
            return false;
        }

        if (s?.Groups?.RequireAny && s.Groups.RequireAny.length > 0 && !config.hasAnyUserGroup(s.Groups.RequireAny)) {
            return false;
        }
        return true;
    }

    public isComponentSetting(setting: SettingModel) {
        return setting?.Type == SettingTypeCode.Component;
    }

    public isFormSetting(setting: SettingModel) {
        return setting?.Type == SettingTypeCode.Form;
    }

    public isHtmlTemplateSetting(settings: SettingModel) {
        return settings?.Type === SettingTypeCode.HtmlTemplate;
    }

    public isAddRemoveRowsSetting(settings: SettingModel) {
        return settings?.Type === SettingTypeCode.AddRemoveRows;
    }

    public isBankAccountSetting(settings: SettingModel) {
        return settings?.Type === SettingTypeCode.BankAccount;
    }

    public isKycUpdateFrequencySetting(settings: SettingModel) {
        return settings?.Type === SettingTypeCode.KycUpdateFrequency;
    }

    public getUiGroupDisplayName(uiGroupName: string) {
        return this.model.UiGroupDisplayNames && this.model.UiGroupDisplayNames[uiGroupName]
            ? this.model.UiGroupDisplayNames[uiGroupName]
            : uiGroupName;
    }
}

export enum SettingsFormFieldTypeCode {
    InterestRate = 'InterestRate',
    PositiveInteger = 'PositiveInteger',
    Enum = 'Enum',
    Url = 'Url',
    HiddenText = 'HiddenText',
    TextArea = 'TextArea',
    Text = 'Text'
}

export enum SettingTypeCode {
    Form = 'Form',
    Component = 'Component',
    HtmlTemplate = 'HtmlTemplate',
    AddRemoveRows = 'AddRemoveRows',
    BankAccount = 'BankAccount',
    KycUpdateFrequency = 'KycUpdateFrequency',
}

export interface BankAccountDataModel {
    DefaultBankAccountNr: {
        Source: string;
        Value: string;
    };
    IsInitiallyEnabled?: boolean;
    ExcludedAccountNrTypes?: string[];
    OnlyTheseBankAccountNrTypes?: string[];
}
