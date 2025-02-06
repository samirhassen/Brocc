import * as moment from 'moment';

export interface CountrySpecificValidator {
    isValidCivicNr(input: string): boolean;
    isValidOrgnr(input: string): boolean;
    isValidPaymentFileReferenceNr(bankAccountNrType: string, referenceNr: string): boolean;
    isRegularBankAccount(bankAccountNrType: string): boolean;
}

export function createCountrySpecificValidatorUsingCountryIsoCode(
    twoLetterCountryIsoCode: string
): CountrySpecificValidator {
    if (twoLetterCountryIsoCode === 'SE') {
        return new CountrySpecificValidatorSe();
    } else if (twoLetterCountryIsoCode === 'FI') {
        return new CountrySpecificValidatorFi();
    } else {
        throw new Error(`Country ${twoLetterCountryIsoCode} not supported`);
    }
}

export function createCountrySpecificValidator(twoLetterClientCountryIsoCode: string): CountrySpecificValidator {
    return createCountrySpecificValidatorUsingCountryIsoCode(twoLetterClientCountryIsoCode);
}

class CountrySpecificValidatorFi implements CountrySpecificValidator {
    private civicNrDigits = [
        '0',
        '1',
        '2',
        '3',
        '4',
        '5',
        '6',
        '7',
        '8',
        '9',
        'A',
        'B',
        'C',
        'D',
        'E',
        'F',
        'H',
        'J',
        'K',
        'L',
        'M',
        'N',
        'P',
        'R',
        'S',
        'T',
        'U',
        'V',
        'W',
        'X',
        'Y',
    ];

    constructor() {}

    public isRegularBankAccount(bankAccountNrType: string): boolean {
        return false;
    }

    public isValidCivicNr(value: string) {
        //https://sv.wikipedia.org/wiki/Personnummer#Finland
        if (!value) {
            return false;
        }
        var c = value.trim();
        if (c.length != 11) {
            return false;
        }

        if (!moment(c.slice(0, 6), 'DDMMYY', true).isValid()) {
            return false;
        }

        return c[10].toUpperCase() === this.computeCheckDigitCivicNrFi(c);
    }

    public isValidOrgnr(input: string): boolean {
        throw new Error('Not implemented for country FI');
    }

    public isValidPaymentFileReferenceNr(bankAccountNrType: string, referenceNr: string): boolean {
        throw new Error('Not implemented for country FI');
    }

    private computeCheckDigitCivicNrFi(prefix: string) {
        let i = parseInt(prefix.slice(0, 6) + prefix.slice(7, 10), 10) % 31;
        return this.civicNrDigits[i];
    }
}

class CountrySpecificValidatorSe implements CountrySpecificValidator {
    public isRegularBankAccount(bankAccountNrType: string): boolean {
        return bankAccountNrType === 'BankAccountSe';
    }

    public isValidOrgnr(input: string): boolean {
        if (this.isValidCivicNr(input)) {
            //Enskild firma
            return true;
        }

        if (!input) {
            return false;
        }

        let digits = input.replace(/\D/g, '');

        if (digits.length != 10) {
            return false;
        }
        let mPart = parseInt(digits.substr(2, 2));
        if (mPart < 20) {
            return false;
        }

        return this.hasValidMod10CheckDigit(digits);
    }

    public isValidCivicNr(input: string): boolean {
        // Check valid length & form
        if (!input) {
            return false;
        }

        if (input.indexOf('-') === -1) {
            if (input.length === 10) {
                input = input.slice(0, 6) + '-' + input.slice(6);
            } else {
                input = input.slice(0, 8) + '-' + input.slice(8);
            }
        }
        if (!input.match(/^(\d{2})(\d{2})(\d{2})\-(\d{4})|(\d{4})(\d{2})(\d{2})\-(\d{4})$/)) {
            return false;
        }

        input = input.replace('-', '');
        if (input.length === 12) {
            input = input.substring(2);
        }

        let yearNr = parseInt(!!RegExp.$1 ? RegExp.$1 : RegExp.$5);
        let monthNr = parseInt(!!RegExp.$2 ? RegExp.$2 : RegExp.$6) - 1;
        let dayNr = parseInt(!!RegExp.$3 ? RegExp.$3 : RegExp.$7);
        var d = new Date(yearNr, monthNr, dayNr);

        // Check valid date
        if (Object.prototype.toString.call(d) !== '[object Date]' || isNaN(d.getTime())) return false;

        // Check luhn algorithm
        return this.hasValidMod10CheckDigit(input);
    }

    public isValidPaymentFileReferenceNr(bankAccountNrType: string, referenceNr: string): boolean {
        if (!referenceNr || !bankAccountNrType) {
            return false;
        }
        if (!/^[\d ]*$/.test(referenceNr)) {
            //digits and whitespace only
            return false;
        }
        let cleanedNr = referenceNr.replace(/\D/g, '');
        if (bankAccountNrType == 'BankGiroSe' || bankAccountNrType == 'PlusGiroSe') {
            return cleanedNr.length >= 2 && cleanedNr.length <= 25 && this.hasValidMod10CheckDigit(cleanedNr);
        } else {
            throw new Error('Not implemented for ' + bankAccountNrType);
        }
    }

    private hasValidMod10CheckDigit(digits: string) {
        let sum = 0;
        let parity = digits.length % 2;
        for (var i = 0; i < digits.length; i = i + 1) {
            let digit = parseInt(digits.charAt(i), 10);
            if (i % 2 === parity) {
                digit *= 2;
            }
            if (digit > 9) {
                digit -= 9;
            }
            sum += digit;
        }
        return sum % 10 === 0;
    }
}
