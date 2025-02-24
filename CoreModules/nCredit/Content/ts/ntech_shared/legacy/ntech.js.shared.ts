declare var ntechClientCountry: any
declare var clientCountry: any

declare var i18n: any

if (ntech === undefined) {
    var ntech: any = {}
}

ntech.util = (function () {
    function b64DecodeUnicode(str: any) {
        //Undestroy euro signs and similar: https://stackoverflow.com/questions/30106476/using-javascripts-atob-to-decode-base64-doesnt-properly-decode-utf-8-strings
        return decodeURIComponent(Array.prototype.map.call(atob(str), function (c: any) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
    }

    function parseUtf8Base64InitialData(d: any) {
        return JSON.parse(b64DecodeUnicode(d))
    }

    return {
        parseUtf8Base64InitialData: parseUtf8Base64InitialData
    }
}());

ntech.se = (function () {
    function isValidOrgNr(input: any) {
        // Check valid length & form
        if (!input) { return false; }
        
        input = input.replace('-', '');

        if (input.length != 10 && input.length != 12)
            return false;

        let monthNr: number = input.length == 10 ? parseInt(input.slice(2, 4)) : parseInt(input.slice(4, 6));

        if (monthNr < 20)
            return (isValidCivicNr(input))

        let parity: number, sum: number
        sum = 0;
        parity = 0;
        // Check luhn algorithm
        for (var i = 0; i < input.length; i = i + 1) {
            var digit = parseInt(input.charAt(i), 10);
            if (i % 2 ===  parity) { digit *= 2; }
            if (digit > 9) { digit -= 9; }
            sum += digit;
        }
        return (sum % 10) === 0;


    }

    function isValidCivicNr(input: any) {
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

        // Declare variables
        let yearNr = parseInt((!!RegExp.$1) ? RegExp.$1 : RegExp.$5)
        let monthNr = (parseInt((!!RegExp.$2) ? RegExp.$2 : RegExp.$6) - 1)
        let dayNr = parseInt((!!RegExp.$3) ? RegExp.$3 : RegExp.$7)
        var d = new Date(yearNr, monthNr, dayNr),
            sum = 0,
            numdigits = input.length,
            parity = numdigits % 2,
            i,
            digit;

        // Check valid date
        if (Object.prototype.toString.call(d) !== "[object Date]" || isNaN(d.getTime())) return false;

        // Check luhn algorithm
        for (i = 0; i < numdigits; i = i + 1) {
            digit = parseInt(input.charAt(i), 10);
            if (i % 2 === parity) { digit *= 2; }
            if (digit > 9) { digit -= 9; }
            sum += digit;
        }
        return (sum % 10) === 0;
    }

    return { isValidCivicNr: isValidCivicNr, isValidOrgNr: isValidOrgNr }
}());

ntech.fi = (function () {
    var civicNrDigits = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y']
    function computeCheckDigitCivicNrFi(prefix: any) {
        var i = parseInt(prefix.slice(0, 6) + prefix.slice(7, 10), 10) % 31
        return civicNrDigits[i]
    }
    function isValidCivicNr(value: any) {
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

        return (c[10].toUpperCase() === computeCheckDigitCivicNrFi(c))
    }

    function modulo(divident: any, divisor: any) {
        var partLength = 10;

        while (divident.length > partLength) {
            var part = divident.substring(0, partLength);
            divident = (part % divisor) + divident.substring(partLength);
        }

        return divident % divisor;
    }

    function isLetter(str: any) {
        return str.length === 1 && str.match(/[a-z]/i);
    }

    function isValidIBAN(value: any) {
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
            if (isLetter(c[i])) {
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
        var rem = modulo(d, 97)
        return rem === 1
    }

    return { isValidCivicNr: isValidCivicNr, isValidIBAN: isValidIBAN }
}());

ntech.clientCountry = (function () {
    function getCountryFns() {
        if (!ntechClientCountry) {
            throw 'Missing ntechClientCountry';
        }
        clientCountry = ntechClientCountry.toLowerCase();
        var countryFns = ntech[clientCountry];
        if (!countryFns) {
            throw 'Missing country functions for country: ' + clientCountry;
        }
        return countryFns;
    }
    function isValidCivicNr(input: any) {
        var countryFns = getCountryFns();
        return countryFns.isValidCivicNr(input);

    }

    return { isValidCivicNr: isValidCivicNr }
}());

ntech.libphonenumber = (function () {
    function parsePhoneNr(number: any, regionCode: any) {  //regionCode = two letter iso code
        var result = {
            raw: number,
            isValid: false,
            validNumber: null as any,
            errorCode: null as any
        }
        try {
            var phoneUtil = i18n.phonenumbers.PhoneNumberUtil.getInstance();
            var number = phoneUtil.parseAndKeepRawInput(number, regionCode);

            var isPossible = phoneUtil.isPossibleNumber(number);
            if (!isPossible) {
                var PNV = i18n.phonenumbers.PhoneNumberUtil.ValidationResult;
                switch (phoneUtil.isPossibleNumberWithReason(number)) {
                    case PNV.INVALID_COUNTRY_CODE:
                        result.errorCode = 'INVALID_COUNTRY_CODE'
                        break;
                    case PNV.TOO_SHORT:
                        result.errorCode = 'TOO_SHORT'
                        break;
                    case PNV.TOO_LONG:
                        result.errorCode = 'TOO_LONG'
                        break;
                }
                return result
            } else {
                var isNumberValid = phoneUtil.isValidNumber(number);
                if (!isNumberValid) {
                    result.errorCode = 'INVALID'
                    return result
                }
                result.isValid = true
                result.validNumber = {
                    isLocal: false
                }
                if (isNumberValid && regionCode && regionCode != 'ZZ') {
                    result.validNumber.isLocal = phoneUtil.isValidNumberForRegion(number, regionCode)
                }
                result.validNumber.regionCode = phoneUtil.getRegionCodeForNumber(number)
                var PNT = i18n.phonenumbers.PhoneNumberType;
                switch (phoneUtil.getNumberType(number)) {
                    case PNT.FIXED_LINE:
                        result.validNumber.numberType = 'FIXED_LINE'
                        break;
                    case PNT.MOBILE:
                        result.validNumber.numberType = 'MOBILE'
                        break;
                    case PNT.FIXED_LINE_OR_MOBILE:
                        result.validNumber.numberType = 'FIXED_LINE_OR_MOBILE'
                        break;
                    case PNT.TOLL_FREE:
                        result.validNumber.numberType = 'TOLL_FREE'
                        break;
                    case PNT.PREMIUM_RATE:
                        result.validNumber.numberType = 'PREMIUM_RATE'
                        break;
                    case PNT.SHARED_COST:
                        result.validNumber.numberType = 'SHARED_COST'
                        break;
                    case PNT.VOIP:
                        result.validNumber.numberType = 'VOIP'
                        break;
                    case PNT.PERSONAL_NUMBER:
                        result.validNumber.numberType = 'PERSONAL_NUMBER'
                        break;
                    case PNT.PAGER:
                        result.validNumber.numberType = 'PAGER'
                        break;
                    case PNT.UAN:
                        result.validNumber.numberType = 'UAN'
                        break;
                    case PNT.UNKNOWN:
                        result.validNumber.numberType = 'UNKNOWN'
                        break;
                }
            }
            var PNF = i18n.phonenumbers.PhoneNumberFormat;
            result.validNumber.localNumber = phoneUtil.format(number, PNF.NATIONAL)
            result.validNumber.internationalNumber = phoneUtil.format(number, PNF.E164)
            result.validNumber.mobileDialingNumber = phoneUtil.formatNumberForMobileDialing(number, regionCode, false)
            result.validNumber.standardDialingNumber = i18n.phonenumbers.PhoneNumberUtil.normalizeDiallableCharsOnly(phoneUtil.formatOutOfCountryCallingNumber(number, regionCode))

            return result
        } catch (e) {
            result.errorCode = e.toString()
            return result
        }
    }

    return {
        parsePhoneNr: parsePhoneNr
    }
})();

module NTechTables {
    export class PagingHelper {
        constructor(private $q: ng.IQService, private $http: ng.IHttpService) {

        }

        createPagingObjectFromPageResult(h: IPagingResult, onGotoPage?: (data: { pageNr: number, pagingObject: PagingObject, event: Event }) => void): PagingObject {
            if (!h) {
                return {
                    pages: [],
                    isNextAllowed: false,
                    isPreviousAllowed: false,
                    nextPageNr: 0,
                    previousPageNr: 0,
                    onGotoPage: onGotoPage ? onGotoPage: null
                }
            }

            let p: Page[] = [];

            //9 items including separators are the most shown at one time
            //The two items before and after the current item are shown
            //The first and last item are always shown
            for (var i = 0; i < h.TotalNrOfPages; i++) {
                if (i >= (h.CurrentPageNr - 2) && i <= (h.CurrentPageNr + 2) || h.TotalNrOfPages <= 9) {
                    p.push({ pageNr: i, isCurrentPage: h.CurrentPageNr == i, isSeparator: false }) //Primary pages are always visible
                } else if (i == 0 || i == (h.TotalNrOfPages - 1)) {
                    p.push({ pageNr: i, isCurrentPage: h.CurrentPageNr == i, isSeparator: false }) //First and last page are always visible
                } else if (i == (h.CurrentPageNr - 3) || i == (h.CurrentPageNr + 3)) {
                    p.push({ pageNr: i, isSeparator: true, isCurrentPage: false }) //First and last page are always visible
                }
            }

            return {
                pages: p,
                isPreviousAllowed: h.CurrentPageNr > 0,
                previousPageNr: h.CurrentPageNr - 1,
                isNextAllowed: h.CurrentPageNr < (h.TotalNrOfPages - 1),
                nextPageNr: h.CurrentPageNr + 1
            }
        }

        gotoPage<T>(pageNr: number, pageSize: number, url: string, filter: IFilter, evt: Event): ng.IPromise<GotoPageResult<T>> {
            if (evt) {
                evt.preventDefault()
            }
            let deferred: ng.IDeferred<GotoPageResult<T>> = this.$q.defer();
            this.$http({
                method: 'POST',
                url: url,
                data: { pageSize: pageSize, filter: filter, pageNr: pageNr }
            }).then((response: ng.IHttpResponse<IPagingServerResponse<T>>) => {
                let data = response.data;
                deferred.resolve({
                    pagesData: data,
                    pagingObject: this.createPagingObjectFromPageResult(data)
                });
            }, (response) => {
                deferred.reject(response.statusText);
            })
            return deferred.promise;
        }
    }

    export interface IPagingResult {
        CurrentPageNr: number;
        TotalNrOfPages: number;
    }

    interface IPagingServerResponse<T> extends IPagingResult {
        CurrentPageNr: number,
        TotalNrOfPages: number,
        Page: Array<T>,
        Filter: IFilter
    }

    export class GotoPageResult<T> {
        pagesData: IPagingServerResponse<T>;
        pagingObject: PagingObject;
    }

    export interface IFilter {

    }

    export class PagingObject {
        pages: Page[];
        isPreviousAllowed: boolean;
        previousPageNr: number;
        isNextAllowed: boolean;
        nextPageNr: number;
        onGotoPage?: (data: { pageNr: number, pagingObject: PagingObject, event: Event }) => void;
    }

    export class Page {
        pageNr: number;
        isSeparator: boolean;
        isCurrentPage: boolean;
    }
}


namespace NTechComponents {
    export class Dictionary<T extends number | string, U>{
        private _keys: T[] = [];
        private _values: U[] = [];

        private undefinedKeyErrorMessage: string = "Key is either undefined, null or an empty string.";

        private isEitherUndefinedNullOrStringEmpty(object: any): boolean {
            return (typeof object) === "undefined" || object === null || object.toString() === "";
        }

        private checkKeyAndPerformAction(action: { (key: T, value?: U): void | U | boolean }, key: T, value?: U): void | U | boolean {

            if (this.isEitherUndefinedNullOrStringEmpty(key)) {
                throw new Error(this.undefinedKeyErrorMessage);
            }

            return action(key, value);
        }


        public add(key: T, value: U): void {

            var addAction = (key: T, value: U): void => {
                if (this.containsKey(key)) {
                    throw new Error("An element with the same key already exists in the dictionary.");
                }

                this._keys.push(key);
                this._values.push(value);
            };

            this.checkKeyAndPerformAction(addAction, key, value);
        }

        public remove(key: T): boolean {

            var removeAction = (key: T): boolean => {
                if (!this.containsKey(key)) {
                    return false;
                }

                var index = this._keys.indexOf(key);
                this._keys.splice(index, 1);
                this._values.splice(index, 1);

                return true;
            };

            return <boolean>(this.checkKeyAndPerformAction(removeAction, key));
        }

        public getValue(key: T): U {

            var getValueAction = (key: T): U => {
                if (!this.containsKey(key)) {
                    return null;
                }

                var index = this._keys.indexOf(key);
                return this._values[index];
            }

            return <U>this.checkKeyAndPerformAction(getValueAction, key);
        }

        public containsKey(key: T): boolean {

            var containsKeyAction = (key: T): boolean => {
                if (this._keys.indexOf(key) === -1) {
                    return false;
                }
                return true;
            };

            return <boolean>this.checkKeyAndPerformAction(containsKeyAction, key);
        }

        public changeValueForKey(key: T, newValue: U): void {

            var changeValueForKeyAction = (key: T, newValue: U): void => {
                if (!this.containsKey(key)) {
                    throw new Error("In the dictionary there is no element with the given key.");
                }

                var index = this._keys.indexOf(key);
                this._values[index] = newValue;
            }

            this.checkKeyAndPerformAction(changeValueForKeyAction, key, newValue);
        }

        public keys(): T[] {
            return this._keys;
        }

        public values(): U[] {
            return this._values;
        }

        public count(): number {
            return this._values.length;
        }
    }

    class UniqueIdGenerator {
        alphabet = '23456789abdegjkmnpqrvwxyz'
        generateUniqueId(length: number) {
            var rtn = '';
            for (var i = 0; i < length; i++) {
                rtn += this.alphabet.charAt(Math.floor(Math.random() * this.alphabet.length));
            }
            return rtn;
        }
    }
    let uGen = new UniqueIdGenerator();
    export function generateUniqueId(length: number) {
        return uGen.generateUniqueId(length);
    }
}