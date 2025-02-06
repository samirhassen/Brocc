import { Component, Input, SimpleChanges } from '@angular/core';
import { AbstractControl, UntypedFormBuilder, ValidationErrors, Validators } from '@angular/forms';
import { getOrderedCountryDropdownOptions } from 'projects/ntech-components/src/public-api';
import { ConfigService } from 'src/app/common-services/config.service';
import { IsoCountriesService, isValidTwoLetterIsoCountryCode } from 'src/app/common-services/iso-countries.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { Dictionary } from 'src/app/common.types';

const knownCountryItemNames = new Set<string>(['addressCountry']);

@Component({
    selector: 'customer-card-item-editor',
    templateUrl: './customer-card-item-editor.component.html',
    styles: [
    ]
})
export class CustomerCardItemEditorComponent {
    constructor(private formBuilder: UntypedFormBuilder, private apiService: NtechApiService, private validationService: NTechValidationService,
        private countryService: IsoCountriesService, private configService: ConfigService) { }

    @Input()
    public initialData: CustomerCardItemEditorComponentInitialData

    public m: Model

    async ngOnChanges(_: SimpleChanges) {
        this.m = null;

        if(!this.initialData) {
            return;
        }
        
        let result = await this.fetchCustomerContactInfoEditValueData(this.initialData.customerId, this.initialData.itemName);
        let translationsByLanguage: Dictionary<Dictionary<string>> = JSON.parse(result.translationTableJson);
        let translations = translationsByLanguage['en'];

        let templateName = result.templateName;
        let validators : ((control: AbstractControl) => ValidationErrors)[] = [Validators.required];

        let isCountry = knownCountryItemNames.has(this.initialData.itemName);
        let initialValue = this.validationService.normalizeString(result.currentValue?.Value) ?? '';
        let dropdownOptions: { code: string, displayName: string }[] = null;

        if(isCountry) {
            let countrySetup = await this.setupCountry(initialValue);
            initialValue = countrySetup.initialValue;
            validators.push(...countrySetup.validators);
            dropdownOptions = countrySetup.dropdownOptions;
        } else if(templateName == 'Email') {
            validators.push(this.validationService.getEmailValidator());
        } else if(templateName == 'Phonenr') {
            validators.push(this.validationService.getPhoneValidator());
        } else if(templateName == 'Date') {
            validators.push(this.validationService.getDateOnlyValidator());
        }

        let form = new FormsHelper(this.formBuilder.group({
            'itemValue': [initialValue, validators]
        }));
        
        this.m = {
            templateName: templateName,
            historicalValues: result.historicalValues,
            showClose: !!this.initialData.onClose,
            hideHeader : this.initialData.hideHeader,
            form: form,
            isSingleTextBox: !isCountry && (!templateName || templateName == 'Email' || templateName == 'Phonenr' || templateName == 'Date' || templateName == 'String'),
            itemLabelText: translations[`propertylabel.${this.initialData.itemName}`] ?? this.initialData.itemName,
            dropdownOptions: dropdownOptions
        };
    }

    private async setupCountry(initialValue: string) {
        let validators : ((control: AbstractControl) => ValidationErrors)[] = [];

        let isoCountries = await this.countryService.getIsoCountries();
        let extraOptions = [];
        if(!initialValue) {
            extraOptions.push({ code: '', displayName: '' });
        } else if(!isValidTwoLetterIsoCountryCode(initialValue, isoCountries)) {
            //If an invalid value has been entered some other way, always allow that to be preserved
            extraOptions.push({ code: initialValue, displayName: `${initialValue} (INVALID)` });
        }
        validators.push(this.validationService.getValidator('isoCountry', x => isValidTwoLetterIsoCountryCode(x, isoCountries)));

        let dropdownOptions = [
            ...extraOptions, 
            ...getOrderedCountryDropdownOptions(isoCountries, this.configService.baseCountry(), x => {
                let clientLanguageName = x.translatedNameByLang2[this.configService.clientLanguage()];
                return clientLanguageName 
                    ? `${x.iso2Name} (${clientLanguageName} - ${x.commonName})` 
                    : `${x.iso2Name} (${x.commonName})`;
            }, x => x.iso2Name)
        ];

        return {
            dropdownOptions,
            initialValue,
            validators
        };
    }

    async saveChange(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }
        
        if (!this.initialData || !this.m) {
            return
        }

        let newValue = this.validationService.normalizeString(this.m.form.getValue('itemValue'));

        await this.changeCustomerContactInfoValue(this.initialData.customerId, this.initialData.itemName, newValue, false);

        this.initialData.onClose(null);
    }

    getHeaderText() {
        return this?.initialData?.headerText ?? 'Edit contact information';
    }
    
    close(evt?: Event) {
        evt?.preventDefault();

        this.initialData.onClose(evt);
    }

    private fetchCustomerContactInfoEditValueData(customerId: number, name: string) {
        return this.apiService.post<FetchCustomerContactInfoEditValueDataResponse>('nCustomer', 'Api/ContactInfo/FetchEditValueData', { 
            customerId: customerId, 
            name: name,
            includeTranslationTable: true
        })
    }

    private changeCustomerContactInfoValue(customerId: number, name: string, value: string, includesNewValuesInResponse: boolean) {
        return this.apiService.post<ChangeCustomerContactInfoValueResponse>('nCustomer', 'Api/ContactInfo/ChangeValue', { customerId: customerId, name: name, value: value, includesNewValuesInResponse: includesNewValuesInResponse })
    }
}

interface Model {
    templateName: string
    historicalValues: ICustomerPropertyModelExtended[]
    hideHeader: boolean
    showClose: boolean
    form: FormsHelper
    isSingleTextBox: boolean
    itemLabelText: string
    dropdownOptions: { code: string, displayName: string }[]
}

interface FetchCustomerContactInfoEditValueDataResponse {
    customerId: number
    name: string
    templateName: string
    currentValue: ICustomerPropertyModelExtended
    historicalValues: ICustomerPropertyModelExtended[]
    translationTableJson: string
}

interface ICustomerPropertyModelExtended {
    Id: number
    ChangeDate: Date
    ChangedById: number
    ChangedByDisplayName: string
    Name: string
    Group: string
    CustomerId: number
    Value: string
    IsSensitive: boolean
}

interface ChangeCustomerContactInfoValueResponse {
    currentValue: ICustomerPropertyModelExtended
    historicalValues: ICustomerPropertyModelExtended[]
}


export interface CustomerCardItemEditorComponentInitialData {
    customerId: number
    itemName: string
    onClose: (evt: Event) => void
    headerText?: string
    hideHeader?: boolean
}