var NTechComponents;
(function (NTechComponents) {
    var NTechComponentControllerBaseTemplate = /** @class */ (function () {
        function NTechComponentControllerBaseTemplate(ntechComponentService) {
            var _this = this;
            this.ntechComponentService = ntechComponentService;
            this.isNullOrWhitespace = function (input) {
                if (typeof input === 'undefined' || input == null)
                    return true;
                if ($.type(input) === 'string') {
                    return $.trim(input).length < 1;
                }
                else {
                    return false;
                }
            };
            this.isValidPositiveDecimal = function (value) {
                if (_this.isNullOrWhitespace(value))
                    return true;
                var v = value.toString();
                return (/^([0]|[1-9]([0-9])*)([\.|,]([0-9])+)?$/).test(v);
            };
            this.isValidDecimal = function (value) {
                if (_this.isNullOrWhitespace(value))
                    return true;
                var v = value.toString();
                return (/^([-]?[0]|[1-9]([0-9])*)([\.|,]([0-9])+)?$/).test(v);
            };
            this.parseDecimalOrNull = function (n) {
                if (_this.isNullOrWhitespace(n) || !_this.isValidDecimal(n)) {
                    return null;
                }
                if ($.type(n) === 'string') {
                    return parseFloat(n.replace(',', '.'));
                }
                else {
                    return parseFloat(n);
                }
            };
            this.isValidDate = function (value) {
                if (_this.isNullOrWhitespace(value))
                    return true;
                return moment(value, "YYYY-MM-DD", true).isValid();
            };
            this.isValidMonth = function (value) {
                if (_this.isNullOrWhitespace(value))
                    return true;
                return moment(value + '-01', "YYYY-MM-DD", true).isValid();
            };
            this.isValidURL = function (value) {
                //From: https://stackoverflow.com/questions/5717093/check-if-a-javascript-string-is-a-url
                var pattern = new RegExp('^(https?:\\/\\/)?' + // protocol
                    '((([a-z\\d]([a-z\\d-]*[a-z\\d])*)\\.)+[a-z]{2,}|' + // domain name
                    '((\\d{1,3}\\.){3}\\d{1,3}))' + // OR ip (v4) address
                    '(\\:\\d+)?(\\/[-a-z\\d%_.~+]*)*' + // port and path
                    '(\\?[;&a-z\\d%_.~+=-]*)?' + // query string
                    '(\\#[-a-z\\d_]*)?$', 'i'); // fragment locator
                return !!pattern.test(value);
            };
            this.isValidPositiveInt = function (value) {
                if (_this.isNullOrWhitespace(value))
                    return true;
                var v = value.toString();
                return (/^(\+)?([0-9]+)$/.test(v));
            };
            this.isValidEmail = function (value) {
                //Just to help the user in case they mix up the fields. Not trying to ensure it's actually possible to send email here
                if (_this.isNullOrWhitespace(value)) {
                    return true;
                }
                var i = value.indexOf('@');
                return value.length >= 3 && i > 0 && i < (value.length - 1);
            };
            this.isValidPhoneNr = function (value) {
                if (_this.isNullOrWhitespace(value)) {
                    return true;
                }
                return !(/[a-z]/i.test(value));
            };
            this.asDate = function (d) {
                if (!d) {
                    return null;
                }
                var dd = new NTechDates.DateOnly(d.yearMonthDay); //Serialization
                return dd.asDate();
            };
            this.formatDateOnlyForEdit = function (d) {
                if (_this.isNullOrWhitespace(d)) {
                    return '';
                }
                else {
                    return new NTechDates.DateOnly(d.yearMonthDay).toString();
                }
            };
            this.formatNumberForEdit = function (n) {
                if (_this.isNullOrWhitespace(n)) {
                    return '';
                }
                else {
                    return n.toString().replace('.', ',');
                }
            };
            this.formatNumberForStorage = function (n) {
                if (_this.isNullOrWhitespace(n)) {
                    return '';
                }
                else {
                    return n.toString().replace(',', '.').replace(' ', '');
                }
            };
        }
        NTechComponentControllerBaseTemplate.prototype.$onInit = function () {
            if (this.ntechComponentService.ntechLog.isDebugMode) {
                window['ntechDebug'] = window['ntechDebug'] || {};
                window['ntechDebug'][this.componentName()] = this;
            }
        };
        NTechComponentControllerBaseTemplate.prototype.$postLink = function () {
            this.postLink();
        };
        NTechComponentControllerBaseTemplate.prototype.$onDestroy = function () {
        };
        NTechComponentControllerBaseTemplate.prototype.$onChanges = function (changesObj) {
            this.onChanges();
        };
        NTechComponentControllerBaseTemplate.prototype.$doCheck = function () {
            this.onDoCheck();
        };
        NTechComponentControllerBaseTemplate.prototype.onDoCheck = function () {
        };
        NTechComponentControllerBaseTemplate.prototype.onChanges = function () {
        };
        NTechComponentControllerBaseTemplate.prototype.postLink = function () {
        };
        NTechComponentControllerBaseTemplate.prototype.logDebug = function (message) {
            this.ntechComponentService.ntechLog.logDebug("".concat(this.componentName(), ": ").concat(message));
        };
        NTechComponentControllerBaseTemplate.prototype.signalReloadRequired = function () {
            this.logDebug('signalReloadRequired');
            this.ntechComponentService.signalReloadRequired({ sourceComponentName: this.componentName() });
        };
        /**
         * https://stackoverflow.com/a/35599724/2928868
         * Minus the length-check.
         * Should work for all valid IBANs. https://www.iban.com/structure
         * @param input
         */
        NTechComponentControllerBaseTemplate.prototype.isValidIBAN = function (input) {
            var mod97 = function (value) {
                var checksum = value.slice(0, 2), fragment;
                for (var offset = 2; offset < value.length; offset += 7) {
                    fragment = String(checksum) + value.substring(offset, offset + 7);
                    checksum = parseInt(fragment, 10) % 97;
                }
                return checksum;
            };
            var iban = String(input).toUpperCase().replace(/[^A-Z0-9]/g, ''), // keep only alphanumeric characters
            code = iban.match(/^([A-Z]{2})(\d{2})([A-Z\d]+)$/), // match and capture (1) the country code, (2) the check digits, and (3) the rest
            digits;
            // check syntax and length
            if (!code) {
                return false;
            }
            // rearrange country code and check digits, and convert chars to ints
            digits = (code[3] + code[1] + code[2]).replace(/[A-Z]/g, function (letter) { return String(letter.charCodeAt(0) - 55); });
            // final check
            return mod97(digits) === 1;
        };
        NTechComponentControllerBaseTemplate.prototype.isValidIBANFI = function (v) {
            var isLetter = function (str) {
                return str.length === 1 && str.match(/[a-z]/i);
            };
            var parseNum = function (x) { return parseFloat(x.replace(',', '.')); };
            var modulo = function (divident, divisor) {
                var partLength = 10;
                while (divident.length > partLength) {
                    var part = parseNum(divident.substring(0, partLength).replace(',', '.'));
                    divident = (part % divisor) + divident.substring(partLength);
                }
                return parseNum(divident) % divisor;
            };
            var isNullOrWhitespace = function (input) {
                if (typeof input === 'undefined' || input == null)
                    return true;
                if ($.type(input) === 'string') {
                    return $.trim(input).length < 1;
                }
                else {
                    return false;
                }
            };
            if (isNullOrWhitespace(v)) {
                return true;
            }
            var value = v.toString();
            //https://en.wikipedia.org/wiki/International_Bank_Account_Number#Check_digits
            if (!value) {
                return false;
            }
            var c = value.trim().replace(/ /g, '').toUpperCase();
            if (c.length != 18) {
                return false;
            }
            if (c.slice(0, 2) != "FI") {
                return false;
            }
            //Move the four initial characters to the end of the string
            c = c.slice(4, 18) + c.slice(0, 4);
            //Replace each letter in the string with two digits, thereby expanding the string, where A = 10, B = 11, ..., Z = 35
            var d = '';
            for (var i = 0; i < 18; i++) {
                if (isLetter(c[i])) {
                    var index = c.charCodeAt(i) - 55;
                    if (index < 10 || index > 35) {
                        return false;
                    }
                    d = d + index.toString();
                }
                else {
                    d = d + c[i];
                }
            }
            //Interpret the string as a decimal integer and compute the remainder of that number on division by 97
            var rem = modulo(d, 97);
            return rem === 1;
        };
        NTechComponentControllerBaseTemplate.prototype.getUiGatewayUrl = function (moduleName, moduleLocalPath, queryStringParameters) {
            if (moduleLocalPath[0] === '/') {
                moduleLocalPath = moduleLocalPath.substring(1);
            }
            var p = "/Ui/Gateway/".concat(moduleName, "/").concat(moduleLocalPath);
            if (queryStringParameters) {
                var s = moduleLocalPath.indexOf('?') >= 0 ? '&' : '?';
                for (var _i = 0, queryStringParameters_1 = queryStringParameters; _i < queryStringParameters_1.length; _i++) {
                    var q = queryStringParameters_1[_i];
                    if (!this.isNullOrWhitespace(q[1])) {
                        p += "".concat(s).concat(q[0], "=").concat(encodeURIComponent(decodeURIComponent(q[1])));
                        s = '&';
                    }
                }
            }
            return p;
        };
        NTechComponentControllerBaseTemplate.prototype.getLocalModuleUrl = function (moduleLocalPath, queryStringParameters) {
            if (moduleLocalPath[0] === '/') {
                moduleLocalPath = moduleLocalPath.substring(1);
            }
            var p = "/".concat(moduleLocalPath);
            if (queryStringParameters) {
                var s = moduleLocalPath.indexOf('?') >= 0 ? '&' : '?';
                for (var _i = 0, queryStringParameters_2 = queryStringParameters; _i < queryStringParameters_2.length; _i++) {
                    var q = queryStringParameters_2[_i];
                    if (!this.isNullOrWhitespace(q[1])) {
                        p += "".concat(s).concat(q[0], "=").concat(encodeURIComponent(decodeURIComponent(q[1])));
                        s = '&';
                    }
                }
            }
            return p;
        };
        return NTechComponentControllerBaseTemplate;
    }());
    NTechComponents.NTechComponentControllerBaseTemplate = NTechComponentControllerBaseTemplate;
})(NTechComponents || (NTechComponents = {}));
;
