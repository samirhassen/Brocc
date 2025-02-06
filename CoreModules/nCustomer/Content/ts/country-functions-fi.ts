declare var ntech: any

if (ntech === undefined) {
  var ntech = {} as any;
}
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
}())