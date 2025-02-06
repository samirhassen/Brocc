import * as moment from 'moment';
import { AbstractControl, ValidationErrors, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { Dictionary } from '../common.types';
import {
    CountrySpecificValidator,
    createCountrySpecificValidator,
} from '../country-specific/country-specific-validator';
import { ConfigService } from './config.service';
import { formatNumber } from '@angular/common';
import { FormsHelper } from './ntech-forms-helper';

export abstract class NTechValidationSharedBaseService {
    constructor(private twoLetterClientCountryIsoCode: string) {
        this.perCountry = createCountrySpecificValidator(twoLetterClientCountryIsoCode);
    }

    private locale: string = 'sv-SE';

    public perCountry: CountrySpecificValidator;

    public parseDatetimeOffset(input: string): moment.Moment {
        let m = moment(input, moment.ISO_8601, true);
        if (!m.isValid()) {
            throw new Error('Invalid date');
        }
        return m;
    }

    public formatInteger(value: number) {
        if (value === undefined || value === null) {
            return '';
        }
        return value.toString();
    }

    public formatIntegerForDisplay(value: number | string) {
        let n: number;
        if (typeof value === 'string') {
            n = this.parseIntegerOrNull(value);
        } else {
            n = value;
        }
        if (n === undefined || n === null) {
            return '';
        }
        let result = formatNumber(n, this.locale, '1.0-0');
        return result;
    }

    public formatDecimalForDisplay(value: number | string, nrOfDecimals: number = 2) {
        let n: number;
        if (typeof value === 'string') {
            n = this.parseIntegerOrNull(value);
        } else {
            n = value;
        }
        if (n === undefined || n === null) {
            return '';
        }
        let result = formatNumber(n, this.locale, `1.0-${nrOfDecimals}`);
        return result;
    }

    public formatIntegerForEdit(value: number) {
        if (value === undefined || value === null) {
            return '';
        }
        return value.toString();
    }
    public formatDateForEdit(value: moment.Moment) {
        if (value === undefined || value === null) {
            return '';
        }
        return value.format('YYYY-MM-DD');
    }

    public parseInteger(value: string): number {
        return parseInt(this.removeWhitespace(value), 10);
    }

    public parseIntegerOrNull(value: string, treatEmptyAsZero = false): number | null {
        if (this.isNullOrWhitespace(value)) {
            return treatEmptyAsZero ? 0 : null;
        } else {
            let result = this.parseInteger(value);
            return Number.isNaN(result) ? null : result;
        }
    }

    public normalizeString(value: string) {
        let s = this.trim(value ?? '');
        return s.length === 0 ? null : s;
    }

    public isValidEmail(value: string) {
        if (!value) {
            return false;
        }
        return !!value.match(/^([^\.].*[\@]+.*[^\.])$/);
    }

    //Not an attempt to actually validate, just to help the user from accidently mixing up fields with each other. Use googles phone library if actual validation is important.
    public isValidPhone(value: string) {
        if (!value) {
            return false;
        }
        let digitCount = 0;
        for (let c of value) {
            if (this.isLetter(c)) {
                return false;
            } else if (this.isDigit(c)) {
                digitCount = digitCount + 1;
            }
        }
        return digitCount >= 4;
    }

    /** https://stackoverflow.com/a/43467144/2928868
     * Note: this is just a simple healthcheck and not a full validator.
     */
    public isValidHttpUrl(value: string) {
        let url;

        try {
            url = new URL(value);
        } catch (_) {
            return false;
        }

        return url.protocol === 'http:' || url.protocol === 'https:';
    }

    private removeWhitespace(s: string): string {
        if (s === null || s === undefined) {
            return s;
        }
        return s.replace(/\s+/g, '');
    }

    public getAlwaysInvalidValidator() {
        return this.getValidator('alwaysInvalid', (x) => false);
    }

    public getEmailValidator(): (control: AbstractControl) => ValidationErrors | null {
        return this.getValidator('email', (x) => this.isValidEmail(x));
    }

    public getUrlValidator(): (control: AbstractControl) => ValidationErrors | null {
        return this.getValidator('url', (x) => this.isValidHttpUrl(x));
    }

    private getIntegerValidator(
        name: string,
        minValue: number | null,
        maxValue: number | null,
        allowNegativeValues: boolean
    ) {
        return this.getValidator(name, (x) => {
            let noWhitespaceX = this.removeWhitespace(x);
            let isValidInt = allowNegativeValues
                ? /^(0|([-]?[1-9][0-9]*))$/.test(noWhitespaceX)
                : /^(0|([1-9][0-9]*))$/.test(noWhitespaceX);
            if (!isValidInt) {
                return false;
            }
            if (minValue == null && maxValue == null) {
                return true;
            }
            let v = this.parseInteger(noWhitespaceX);
            if (minValue != null && v < minValue) {
                return false;
            }
            if (maxValue != null && v > maxValue) {
                return false;
            }
            return true;
        });
    }

    public getIntegerWithBoundsValidator(minValue: number | null, maxValue: number | null) {
        return this.getIntegerValidator('anyInt', minValue, maxValue, true);
    }

    public getPositiveIntegerWithBoundsValidator(minValue: number | null, maxValue: number | null) {
        return this.getIntegerValidator('positiveInt', minValue, maxValue, false);
    }

    public getPositiveIntegerValidator() {
        return this.getPositiveIntegerWithBoundsValidator(null, null);
    }

    public getPhoneValidator(): (control: AbstractControl) => ValidationErrors | null {
        return this.getValidator('phone', (x) => this.isValidPhone(x));
    }

    public getBannedValueValidator(validatorName: string, bannedValue: string) {
        return this.getValidator(validatorName, (x) => bannedValue === null || x !== bannedValue);
    }

    public getBankAccountNrAsyncValidator(
        config: ConfigService,
        validatorName: string,
        isValidBankAccountNr: (nr: string, bankAccountType: string) => Promise<boolean>
    ) {
        var client = config.getClient();
        let bankAccountType: string;
        if (client.BaseCountry == 'SE') {
            bankAccountType = 'BankAccountSe';
        } else if (client.BaseCountry == 'FI') {
            bankAccountType = 'IBANFi';
        } else {
            bankAccountType = 'NotImplemented';
        }
        return {
            defaultBankAccountType: bankAccountType,
            validator: this.getValidatorAsync(validatorName, (x: string) => {
                return new Observable((observer) => {
                    if (x.length < 5) {
                        //Basic sanity check to avoid server calls for no reason.
                        observer.next(false);
                        observer.complete();
                    } else {
                        isValidBankAccountNr(x, bankAccountType).then((x) => {
                            observer.next(x);
                            observer.complete();
                        });
                    }
                });
            }),
        };
    }

    public isRegularBankAccount(bankAccountNrType: string) {
        return this.perCountry.isRegularBankAccount(bankAccountNrType);
    }

    private trim(s: string): string {
        if (!s) return s;
        //https://stackoverflow.com/questions/196925/what-is-the-best-way-to-trim-in-javascript
        return s.replace(/^\s\s*/, '').replace(/\s\s*$/, '');
    }

    public isNullOrWhitespace = (input: any) => {
        if (typeof input === 'undefined' || input == null) return true;

        if (typeof input === 'string') {
            return this.trim(input).length < 1;
        } else {
            return false;
        }
    };

    public isValidPositiveDecimal = (value: string) => {
        return this.isValidDecimal(value, false);
    };

    public isValidDecimal = (value: string, allowNegativeValues: boolean) => {
        if (this.isNullOrWhitespace(value)) return true;
        var v = this.removeWhitespace(value.toString());
        if (allowNegativeValues) {
            v = v.replace('-', '');
        }
        return /^([0]|[1-9]([0-9])*)([\.|,]([0-9])+)?$/.test(v);
    };

    public formatPositiveDecimalForEdit(value: number): string {
        return this.formatDecimalForEdit(value);
    }

    public formatDecimalForEdit(value: number): string {
        if (this.isNullOrWhitespace(value)) {
            return '';
        }
        return value.toLocaleString('sv-SE', { maximumFractionDigits: 4, minimumFractionDigits: 0 });
    }

    public parsePositiveDecimalOrNull(n: any) {
        return this.parseDecimalOrNull(n, false);
    }

    public parseDecimalOrNull(n: any, allowNegativeValues: boolean) {
        if (this.isNullOrWhitespace(n)) {
            return null;
        }
        let tmp = this.removeWhitespace(n.replace(',', '.'));
        if (!this.isValidDecimal(tmp, allowNegativeValues)) {
            return null;
        }
        if (typeof n === 'string') {
            return parseFloat(tmp);
        } else {
            return parseFloat(tmp);
        }
    }

    public getPositiveDecimalValidator() {
        return this.getValidator('positiveDecimal', this.isValidPositiveDecimal);
    }

    public getDecimalValidator() {
        return this.getValidator('anyDecimal', (x) => this.isValidDecimal(x, true));
    }

    public parseDateOnlyOrNull(value: string): string {
        let parse = () => {
            let m = moment(value, 'YYYY-MM-DD', true);
            if (m.isValid()) {
                return m;
            }
            m = moment(value + '-01', 'YYYY-MM-DD', true);
            if (m.isValid()) {
                return m;
            }
            m = moment(value, 'YYYYMMDD', true);
            if (m.isValid()) {
                return m;
            }
            m = moment(value + '01', 'YYYYMMDD', true);
            return m;
        };
        let m = parse();
        if (m.isValid()) {
            return m.format('YYYY-MM-DD');
        } else {
            return null;
        }
    }

    public isValidDateOnly(value: string) {
        return this.parseDateOnlyOrNull(value) !== null;
    }

    public parseDateOnly(value: string) {
        let v = this.parseDateOnlyOrNull(value);
        if (v === null) {
            throw new Error('Invalid date');
        }
        return v;
    }

    public getDateOnlyValidator() {
        return this.getValidator('dateOnly', (x) => this.isValidDateOnly(x));
    }

    public getValidator(
        validatorName: string,
        isValid: (value: string) => boolean
    ): (control: AbstractControl) => ValidationErrors | null {
        return (x) => {
            let isMissingOrValid = x.value === '' || x.value === null || x.value === undefined || isValid(x.value);
            if (isMissingOrValid) {
                return null;
            } else {
                let err: Dictionary<any> = {};
                err[validatorName] = 'invalid';
                return err;
            }
        };
    }

    public getValidatorAsync(
        validatorName: string,
        isValid: (value: string) => Observable<boolean>
    ): (control: AbstractControl) => Observable<ValidationErrors> | null {
        return FormsHelper.createValidatorAsync(validatorName, isValid);
    }

    public getFormValidatorAsync(
        getFormErrors: () => Promise<ValidationErrors>
    ): (control: AbstractControl) => Observable<ValidationErrors> | null {
        return _ => new Observable((observer) => {
            getFormErrors().then(x => {
                observer.next(x);
            })
        });
    }

    private isLetter(str: string) {
        return str.length === 1 && str.match(/[a-z]/i);
    }

    private isDigit(str: string) {
        return str.length === 1 && str.match(/[0-9]/i);
    }

    public isTextLimitExceeded(txt: string, charLimit: number) {
        if (txt.length > charLimit) return true;
        else return false;
    }

    public trimLimitChars(txt: string, maxChars: number) {
        if (txt.length > maxChars) {
            txt = txt.substring(0, maxChars) + '...';
        }
        return txt;
    }

    public clone<T>(value: T): T | null {
        if (!value) {
            return null;
        }
        return JSON.parse(JSON.stringify(value));
    }

    public isValidOrgnr(value: string) {
        return this.perCountry.isValidOrgnr(value);
    }

    public isValidCivicNr(value: string) {
        return this.perCountry.isValidCivicNr(value);
    }

    public getCivicRegNrValidator(options ?: {
        require12DigitCivicRegNrSe: boolean
    }) {
        return this.getValidator('civicRegNr', (x) => {
            let isValidCivicNr = this.isValidCivicNr(x);

            if(options?.require12DigitCivicRegNrSe && isValidCivicNr && this.twoLetterClientCountryIsoCode === 'SE') {
                return x.replace(/\D/g, '').length === 12;
            }

            return isValidCivicNr;
        });
    }

    public getOrgNrValidator() {
        return this.getValidator('orgnr', (x) => this.isValidOrgnr(x));
    }

    /**
     * 10d for 10 days or 10m for 10 months
     */
    public parseRepaymentTimeWithPeriodMarker(value: string, allowMissingValue: boolean) : RepaymentTimeWithPeriodMarker {
        let v = (value ?? '').trim();
        if(v.length === 0) {
            return allowMissingValue ? {
                isValid: true,
                isMissingValue: true
            } : {
                isValid: false
            }
        }
        if(!(v.endsWith('m') || v.endsWith('d'))) {
            return {
                isValid: false
            }
        }
        let isDays = v.endsWith('d');
        v = v.substring(0, v.length - 1);
        let parsedValue = this.parseIntegerOrNull(v, false);
        if(parsedValue === null || parsedValue <= 0) {
            return {
                isValid: false
            }
        }
        return {
            isValid: true,
            isMissingValue: false,
            isDays: isDays,
            repaymentTime: parsedValue
        }
    }

    /**
     * 10d for 10 days or 10m for 10 months
     */
    public formatRepaymentTimeWithPeriodMarkerForStorage(repaymentTime: number, isDays: boolean) {
        if(repaymentTime === undefined || repaymentTime === null) {
            return null;
        } else {
            return `${repaymentTime.toFixed(0)}${isDays ? 'd' : 'm'}`;
        }
    }

    public formatJsonForDisplay(json: string) {
        if(!json) {
            return json;
        }
        try {
            //Try to indent the json
            return JSON.stringify(JSON.parse(json), null, 2);
        } catch {
            return json;
        }
    }
}

/**
 * Makes the field required if the predicate function returns true.
 * Use like this:
 * getValidators: () => [requiredIfValidator(() => form.form.get('paidToCustomerAmount').value > 0)],
 */
export function requiredIfValidator(predicate: any) {
    return (formControl: any) => {
        if (!formControl.parent) {
            return null;
        }
        if (predicate()) {
            return Validators.required(formControl);
        }
        return null;
    };
}

export interface RepaymentTimeWithPeriodMarker {
    isValid: boolean,
    isMissingValue ?: boolean,
    repaymentTime ?: number,
    isDays ?: boolean
}