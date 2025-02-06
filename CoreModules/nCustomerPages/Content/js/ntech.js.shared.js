if(ntech === undefined) {
  var ntech = {};
}

ntech.util = (function () {
    function b64DecodeUnicode(str) {
        //Undestroy euro signs and similar: https://stackoverflow.com/questions/30106476/using-javascripts-atob-to-decode-base64-doesnt-properly-decode-utf-8-strings
        return decodeURIComponent(Array.prototype.map.call(atob(str), function (c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
    }

    function parseUtf8Base64InitialData(d) {
        return JSON.parse(b64DecodeUnicode(d))
    }

    return {
        parseUtf8Base64InitialData: parseUtf8Base64InitialData
    }
}());

ntech.se = (function() {
    function isValidCivicNr(input) {
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
        var d = new Date(((!!RegExp.$1) ? RegExp.$1 : RegExp.$5), (((!!RegExp.$2) ? RegExp.$2 : RegExp.$6)-1), ((!!RegExp.$3) ? RegExp.$3 : RegExp.$7)),
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
    
    return { isValidCivicNr : isValidCivicNr }
}());

ntech.fi = (function() {
    var civicNrDigits = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y']
    function computeCheckDigitCivicNrFi(prefix) {
        var i = parseInt(prefix.slice(0, 6) + prefix.slice(7, 10), 10) % 31
        return civicNrDigits[i]
    }
    function isValidCivicNr(value) {
        //https://sv.wikipedia.org/wiki/Personnummer#Finland
        if(!value) {
            return false
        }
        var c = value.trim()
        if(c.length != 11) {
            return false
        }
        
        if(!moment(c.slice(0, 6), "DDMMYY", true).isValid()) {
            return false
        }
        
        return (c[10].toUpperCase() === computeCheckDigitCivicNrFi(c))
    }
    
    function modulo(divident, divisor) {
        var partLength = 10;

        while (divident.length > partLength) {
            var part = divident.substring(0, partLength);
            divident = (part % divisor) +  divident.substring(partLength);          
        }

        return divident % divisor;
    }

    function isLetter(str) {
        return str.length === 1 && str.match(/[a-z]/i);
    }

    function isValidIBAN(value) {
        //https://en.wikipedia.org/wiki/International_Bank_Account_Number#Check_digits
        if(!value) {
            return false
        }
        var c = value.trim().replace(/ /g, '').toUpperCase()
        if(c.length != 18) {
            return false
        }        
        if(c.slice(0, 2) != "FI") {
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
    
    return { isValidCivicNr : isValidCivicNr, isValidIBAN : isValidIBAN }
}());

ntech.clientCountry = (function() {
    function getCountryFns() {
        if(!ntechClientCountry) {
            throw 'Missing ntechClientCountry';
        }
        clientCountry = ntechClientCountry.toLowerCase();
        var countryFns = ntech[clientCountry];
        if(!countryFns) {
            throw 'Missing country functions for country: ' + clientCountry;
        }
        return countryFns;
    }
    function isValidCivicNr(input) {
        var countryFns = getCountryFns();
        return countryFns.isValidCivicNr(input);

    }

    return { isValidCivicNr : isValidCivicNr }
}());

ntech.libphonenumber = (function () {
    function parsePhoneNr(number, regionCode) {  //regionCode = two letter iso code
        var result = {
            raw: number,
            isValid: false,
            validNumber: null,
            errorCode: null
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
                    isLocal : false
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