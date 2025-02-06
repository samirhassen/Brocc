import { Injectable } from '@angular/core';
import * as moment from 'moment'
import { AbstractControl, ValidationErrors, Validators } from '@angular/forms';
import { Dictionary, DateOnly } from './common.types';
import { Observable, of } from 'rxjs';
import { map, delay } from 'rxjs/operators';
import { ApiService } from './api-service';

const MonthDisplayPattern = 'YYYY-MM'
const MonthDatePattern = 'YYYY-MM-DD'

@Injectable({
    providedIn: 'root'
})
export class NTechValidationService {
    constructor() { }

    public isValidOrgnr(input: string): boolean {
        if(this.isValidCivicNr(input)) {
            //Enskild firma
            return true
        }

        if (!input) { return false; }

        let digits = input.replace(/\D/g,'')

        if(digits.length != 10) {
            return false
        }
        let mPart = parseInt(digits.substr(2, 2))
        if(mPart < 20) {
            return false
        }

        return this.hasValidMod10CheckDigit(digits)
    }

    public isValidCivicNr(input: string): boolean {
        // Check valid length & form
        if (!input) { return false; }

        if (input.indexOf('-') === -1) {
            if (input.length === 10) {
                input = input.slice(0, 6) + "-" + input.slice(6);
            } else {
                input = input.slice(0, 8) + "-" + input.slice(8);
            }
        }
        if (!input.match(/^(\d{2})(\d{2})(\d{2})\-(\d{4})|(\d{4})(\d{2})(\d{2})\-(\d{4})$/)) { return false };

        input = input.replace('-', '');
        if (input.length === 12) {
            input = input.substring(2);
        }

        let yearNr = parseInt((!!RegExp.$1) ? RegExp.$1 : RegExp.$5)
        let monthNr = (parseInt((!!RegExp.$2) ? RegExp.$2 : RegExp.$6)-1)
        let dayNr = parseInt((!!RegExp.$3) ? RegExp.$3 : RegExp.$7)
        var d = new Date(yearNr, monthNr, dayNr);

        // Check valid date
        if (Object.prototype.toString.call(d) !== "[object Date]" || isNaN(d.getTime())) return false;

        // Check luhn algorithm
        return this.hasValidMod10CheckDigit(input)
    }

    private hasValidMod10CheckDigit(digits: string) {
        let sum = 0
        let parity = digits.length % 2
        for (var i = 0; i < digits.length; i = i + 1) {
            let digit = parseInt(digits.charAt(i), 10);
            if (i % 2 === parity) { digit *= 2; }
            if (digit > 9) { digit -= 9; }
            sum += digit;
        }
        return (sum % 10) === 0;
    }

    public formatInteger(value: number) {
        if(value === undefined || value === null) {
            return ''
        }
        return value.toString()
    }

    public parseInteger(value: string): number {
        return parseInt(this.removeWhitespace(value), 10)
    }

    public isValidEmail(value: string) {
        if(!value) {
            return false
        }
        return value.indexOf('@') >= 0
    }

    //Not an attempt to actually validate, just to help the user from accidently mixing up fields with each other. Use googles phone library if actual validation is important.
    public isValidPhone(value: string) {
        if(!value) {
            return false
        }
        let digitCount = 0
        for(let c of value) {
            if(this.isLetter(c)) {
                return false
            } else if(this.isDigit(c)) {
                digitCount = digitCount + 1
            }
        }
        return digitCount >= 4
    }

    private removeWhitespace(s: string): string {
        if(s === null || s === undefined) {
            return s
        }
        return s.replace(/\s+/g, '')
    }

    public getAlwaysInvalidValidator() {
        return this.getValidator('alwaysInvalid', x => false)
    }

    public getBankAccountNrValidator(bankAccountNrType: string, apiService: ApiService) {        
        return this.getValidatorAsync('bankAccountNr', x => 
            apiService.validateBankAccountNr(x, bankAccountNrType).pipe(map(y => y.IsValid))
        )
    }

    public getCivicNrValidator() : ((control: AbstractControl) => ValidationErrors | null) {
        return this.getValidator('civicNr', x => this.isValidCivicNr(x))
    }

    public getOrgnrValidator() : ((control: AbstractControl) => ValidationErrors | null) {
        return this.getValidator('orgnr', x => this.isValidOrgnr(x))
    }    

    public getEmailValidator() : ((control: AbstractControl) => ValidationErrors | null) {
        return this.getValidator('email', x => this.isValidEmail(x))
    }

    public getPositiveIntegerWithBoundsValidator(minValue: number, maxValue: number) {
        return this.getValidator('positiveInt', x => {
            let noWhitespaceX = this.removeWhitespace(x)
            let isValidInt = /^(0|([1-9][0-9]*))$/.test(noWhitespaceX)
            if(!isValidInt) {
                return false
            }
            if(minValue == null && maxValue == null) {
                return true
            }
            let v = this.parseInteger(noWhitespaceX)
            if(minValue != null && v < minValue) {
                return false
            }            
            if(maxValue != null && v > maxValue) {
                return false
            }
            return true
        })
    }

    public getPositiveIntegerValidator() {
        return this.getPositiveIntegerWithBoundsValidator(null, null)
    }    

    public getPhoneValidator() : ((control: AbstractControl) => ValidationErrors | null) {
        return this.getValidator('phone', x => this.isValidPhone(x))
    }

    public getMonthValidator() {
        return this.getValidator('month', x => moment('01.' + x, MonthDatePattern, true).isValid())
    }

    public getBannedValueValidator(validatorName: string, bannedValue: string) {
        return this.getValidator(validatorName, x => bannedValue === null || x !== bannedValue)
    }

    public formatMonth(date: DateOnly): string {
        return DateOnly.format(date, MonthDisplayPattern)
    }

    public parseMonth(value: string) : DateOnly {
        return DateOnly.fromDateString('01.' + value, MonthDatePattern)
    }

    private getValidator(validatorName : string, isValid: (value: string) => boolean) : ((control: AbstractControl) => ValidationErrors | null) {
        return x => { 
            let isMissingOrValid = (x.value === '' || x.value === null || x.value === undefined || isValid(x.value))
            if(isMissingOrValid) {
                return null
            } else {
                let err : Dictionary<any> = {}
                err[validatorName] = 'invalid'
                return err
            }
        }
    }   

    private getValidatorAsync(validatorName : string, isValid: (value: string) => Observable<boolean>) : ((control: AbstractControl) => Observable<ValidationErrors> | null) {
        return x => { 
            let isMissing = (x.value === '' || x.value === null || x.value === undefined)
            if(isMissing) {
                return of(null)
            }

            return isValid(x.value).pipe(map(y => {
                if(y) {
                    return null
                } else {
                    let err : Dictionary<any> = {}
                    err[validatorName] = 'invalid'
                    return err
                }
            }))
        }
    }    
    
    private isLetter(str: string) {
        return str.length === 1 && str.match(/[a-z]/i);
    }

    private isDigit(str: string) {
        return str.length === 1 && str.match(/[0-9]/i);
    }    
}
