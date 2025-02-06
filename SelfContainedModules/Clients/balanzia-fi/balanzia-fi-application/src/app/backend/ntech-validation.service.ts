import { Injectable } from '@angular/core';
import * as moment from 'moment'
import { AbstractControl, ValidationErrors, Validators } from '@angular/forms';
import { Dictionary, DateOnly } from './common.types';

const MonthDisplayPattern = 'MM.YYYY'
const MonthDatePattern = 'DD.MM.YYYY'

@Injectable({
    providedIn: 'root'
})
export class NTechValidationService {
    private civicNrDigits = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y']

    constructor() { }

    public isValidCivicNr(value: string) {
        //https://sv.wikipedia.org/wiki/Personnummer#Finland
        if (!value) {
            return false
        }
        var c = value.trim()
        if (c.length != 11) {
            return false
        }


        if (!moment(c.slice(0, 6), "DDMMYY", true).isValid()) {
            return false
        }

        return (c[10].toUpperCase() === this.computeCheckDigitCivicNrFi(c))
    }

    public formatInteger(value: number) {
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

    public getCivicNrValidator() : ((control: AbstractControl) => ValidationErrors | null) {
        return this.getValidator('civicNr', x => this.isValidCivicNr(x))
    }

    public getEmailValidator() : ((control: AbstractControl) => ValidationErrors | null) {
        return this.getValidator('email', x => this.isValidEmail(x))
    }

    public getPositiveIntegerValidator() {
        return this.getValidator('positiveInt', x => /0|([1-9][0-9]*)/g.test(this.removeWhitespace(x)))        
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

    public isValidIBAN(value: string) {
        //https://en.wikipedia.org/wiki/International_Bank_Account_Number#Check_digits
        if (!value) {
            return false
        }
        var c = value.trim().replace(/ /g, '').toUpperCase()
        if (c.length != 18) {
            return false
        }
        if (c.slice(0, 2) != "FI") {
            return false
        }
        //Move the four initial characters to the end of the string
        c = c.slice(4, 18) + c.slice(0, 4)

        //Replace each letter in the string with two digits, thereby expanding the string, where A = 10, B = 11, ..., Z = 35
        var d = ''
        for (var i = 0; i < 18; i++) {
            if (this.isLetter(c[i])) {
                var index = c.charCodeAt(i) - 55
                if (index < 10 || index > 35) {
                    return false
                }
                d = d + index.toString()
            } else {
                d = d + c[i]
            }
        }

        //Interpret the string as a decimal integer and compute the remainder of that number on division by 97
        var rem = this.modulo(d, 97)
        return rem === 1
    }

    private computeCheckDigitCivicNrFi(prefix: string) {
        let i = parseInt(prefix.slice(0, 6) + prefix.slice(7, 10), 10) % 31
        return this.civicNrDigits[i]
    }

    private modulo(divident: any, divisor: any) {
        var partLength = 10;

        while (divident.length > partLength) {
            var part = divident.substring(0, partLength);
            divident = (part % divisor) + divident.substring(partLength);
        }

        return divident % divisor;
    }
    
    private isLetter(str: string) {
        return str.length === 1 && str.match(/[a-z]/i);
    }

    private isDigit(str: string) {
        return str.length === 1 && str.match(/[0-9]/i);
    }    
}
