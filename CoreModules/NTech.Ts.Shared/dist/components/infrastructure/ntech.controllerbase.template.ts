namespace NTechComponents {
    export abstract class NTechComponentControllerBaseTemplate {
        constructor(protected ntechComponentService: NTechComponents.NTechComponentService) {

        }

        $onInit() {
            if (this.ntechComponentService.ntechLog.isDebugMode) {
                window['ntechDebug'] = window['ntechDebug'] || {};
                window['ntechDebug'][this.componentName()] = this;
            }
        }
        $postLink() {
            this.postLink();
        }

        $onDestroy() {
            
        }

        $onChanges(changesObj: any) {
            this.onChanges();
        }

        $doCheck() {
            this.onDoCheck();
        }

        protected onDoCheck() {

        }

        protected onChanges() {

        }

        protected postLink() {

        }

        protected logDebug(message: string) {
            this.ntechComponentService.ntechLog.logDebug(`${this.componentName()}: ${message}`);
        }

        abstract componentName(): string;

        signalReloadRequired() {
            this.logDebug('signalReloadRequired');
            this.ntechComponentService.signalReloadRequired({ sourceComponentName: this.componentName() });
        }

        public isNullOrWhitespace = (input: any) => {
            if (typeof input === 'undefined' || input == null) return true;

            if ($.type(input) === 'string') {
                return $.trim(input).length < 1;
            } else {
                return false
            }
        }

        public isValidPositiveDecimal = (value: any) => {
            if (this.isNullOrWhitespace(value))
                return true;
            var v = value.toString()
            return (/^([0]|[1-9]([0-9])*)([\.|,]([0-9])+)?$/).test(v)
        }

        public isValidDecimal = (value: any) => {
            if (this.isNullOrWhitespace(value))
                return true;
            var v = value.toString()
            return (/^([-]?[0]|[1-9]([0-9])*)([\.|,]([0-9])+)?$/).test(v)
        }

        public parseDecimalOrNull = (n: any) => {
            if (this.isNullOrWhitespace(n) || !this.isValidDecimal(n)) {
                return null
            }
            if ($.type(n) === 'string') {
                return parseFloat(n.replace(',', '.'))
            } else {
                return parseFloat(n)
            }
        }

        public isValidDate = (value: string) : boolean => {
            if (this.isNullOrWhitespace(value))
                return true
            return moment(value, "YYYY-MM-DD", true).isValid()
        }

        public isValidMonth = (value: string): boolean => {
            if (this.isNullOrWhitespace(value))
                return true
            return moment(value + '-01', "YYYY-MM-DD", true).isValid()
        }

        public isValidURL = (value: string): boolean => {
            //From: https://stackoverflow.com/questions/5717093/check-if-a-javascript-string-is-a-url
            var pattern = new RegExp('^(https?:\\/\\/)?' + // protocol
                '((([a-z\\d]([a-z\\d-]*[a-z\\d])*)\\.)+[a-z]{2,}|' + // domain name
                '((\\d{1,3}\\.){3}\\d{1,3}))' + // OR ip (v4) address
                '(\\:\\d+)?(\\/[-a-z\\d%_.~+]*)*' + // port and path
                '(\\?[;&a-z\\d%_.~+=-]*)?' + // query string
                '(\\#[-a-z\\d_]*)?$', 'i'); // fragment locator
            return !!pattern.test(value);
        }

        public isValidPositiveInt = (value: string) => {
            if (this.isNullOrWhitespace(value))
                return true;
            var v = value.toString()
            return (/^(\+)?([0-9]+)$/.test(v))
        }

        public isValidEmail = (value: any) : boolean => {
            //Just to help the user in case they mix up the fields. Not trying to ensure it's actually possible to send email here
            if (this.isNullOrWhitespace(value)) {
                return true
            }

            var i = value.indexOf('@')
            return value.length >= 3 && i > 0 && i < (value.length - 1)
        }

        public isValidPhoneNr = (value: any) :boolean => {
            if (this.isNullOrWhitespace(value)) {
                return true
            }
            return !(/[a-z]/i.test(value))
        }

        /**
         * https://stackoverflow.com/a/35599724/2928868
         * Minus the length-check. 
         * Should work for all valid IBANs. https://www.iban.com/structure
         * @param input
         */
         public isValidIBAN(input: any) {
            let mod97 = (value: any) => {
                var checksum = value.slice(0, 2), fragment;
                for (var offset = 2; offset < value.length; offset += 7) {
                    fragment = String(checksum) + value.substring(offset, offset + 7);
                    checksum = parseInt(fragment, 10) % 97;
                }
                return checksum;
            }

            var iban = String(input).toUpperCase().replace(/[^A-Z0-9]/g, ''), // keep only alphanumeric characters
                code = iban.match(/^([A-Z]{2})(\d{2})([A-Z\d]+)$/), // match and capture (1) the country code, (2) the check digits, and (3) the rest
                digits;
            // check syntax and length
            if (!code) {
                return false;
            }
            // rearrange country code and check digits, and convert chars to ints
            digits = (code[3] + code[1] + code[2]).replace(/[A-Z]/g, (letter) => String(letter.charCodeAt(0) - 55));
            // final check
            return mod97(digits) === 1;
        }
        
        public isValidIBANFI(v: any) {
            let isLetter = (str: string) => {
                return str.length === 1 && str.match(/[a-z]/i);
            }
            let parseNum = (x: string) => parseFloat(x.replace(',', '.'))

            let modulo = (divident: string, divisor: number) => {
                var partLength = 10;

                while (divident.length > partLength) {
                    var part = parseNum(divident.substring(0, partLength).replace(',', '.'))
                    divident = (part % divisor) + divident.substring(partLength)
                }

                return parseNum(divident) % divisor
            }

            let isNullOrWhitespace = (input:any) => {
                if (typeof input === 'undefined' || input == null) return true;

                if ($.type(input) === 'string') {
                    return $.trim(input).length < 1;
                } else {
                    return false
                }
            }

            if (isNullOrWhitespace(v)) {
                return true
            }

            let value = v.toString()

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

        public asDate = (d: NTechDates.DateOnly) : Date  => {
            if(!d) {
                return null
            }
            let dd = new NTechDates.DateOnly(d.yearMonthDay) //Serialization
            return dd.asDate()
        }

        public formatDateOnlyForEdit = (d: NTechDates.DateOnly) : string => {
            if(this.isNullOrWhitespace(d)) {
                return ''
            } else {
                return new NTechDates.DateOnly(d.yearMonthDay).toString()
            }            
        }

        public formatNumberForEdit = (n : number) => {
            if(this.isNullOrWhitespace(n)) {
                return ''
            } else {
                return n.toString().replace('.', ',')
            }
        }

        public formatNumberForStorage = (n: number) => {
            if (this.isNullOrWhitespace(n)) {
                return ''
            } else {
                return n.toString().replace(',', '.').replace(' ', '')
            }
        }

        public getUiGatewayUrl(moduleName: string, moduleLocalPath: string, queryStringParameters?: [string, string][]) {
            if (moduleLocalPath[0] === '/') {
                moduleLocalPath = moduleLocalPath.substring(1)
            }
            let p = `/Ui/Gateway/${moduleName}/${moduleLocalPath}` 
            if (queryStringParameters) {
                let s = moduleLocalPath.indexOf('?') >= 0 ? '&' : '?'
                for (let q of queryStringParameters) {
                    if (!this.isNullOrWhitespace(q[1])) {
                        p += `${s}${q[0]}=${encodeURIComponent(decodeURIComponent(q[1]))}`
                        s = '&'
                    }                    
                }
            }            
            return p
        }

        public getLocalModuleUrl(moduleLocalPath: string, queryStringParameters?: [string, string][]) {
            if (moduleLocalPath[0] === '/') {
                moduleLocalPath = moduleLocalPath.substring(1)
            }
            let p = `/${moduleLocalPath}`
            if (queryStringParameters) {
                let s = moduleLocalPath.indexOf('?') >= 0 ? '&' : '?'
                for (let q of queryStringParameters) {
                    if (!this.isNullOrWhitespace(q[1])) {
                        p += `${s}${q[0]}=${encodeURIComponent(decodeURIComponent(q[1]))}`
                        s = '&'
                    }
                }
            }
            return p
        }
    }    
};
