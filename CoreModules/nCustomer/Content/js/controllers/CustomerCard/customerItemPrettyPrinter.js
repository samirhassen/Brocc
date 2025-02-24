var CustomerCardPrettyPrinterNs;
(function (CustomerCardPrettyPrinterNs) {
    var CustomerCardPrettyPrinter = /** @class */ (function () {
        function CustomerCardPrettyPrinter() {
            this.translations = {
                civicRegNr: 'Civic reg. nr.',
                firstName: 'First name',
                lastName: 'Last name',
                addressZipcode: 'Zip',
                addressCountry: 'Country',
                addressStreet: 'Street',
                addressCity: 'City',
                phone: 'Phone',
                email: 'Email',
                ispep: 'Answered yes on the PEP Question?',
                pep_name: 'Who is PEP?',
                pep_text: 'How PEP?',
                mainoccupation: 'Main occupation',
                sanction: 'Sanction',
                externalIsPep: 'On Pep list?',
                externalKycScreeningDate: 'Kyc Screening Date'
            };
        }
        CustomerCardPrettyPrinter.prototype.formatBool = function (v) {
            return (v && v.toString() == 'true') ? 'Yes' : 'No';
        };
        CustomerCardPrettyPrinter.prototype.getFriendlyName = function (group, name) {
            if (this.translations[name]) {
                return this.translations[name];
            }
            else {
                return name;
            }
        };
        CustomerCardPrettyPrinter.prototype.getFriendlyValue = function (group, name, value) {
            if (name == 'externalIsPep' || name == 'sanction') {
                return this.formatBool(value);
            }
            else {
                return value;
            }
        };
        return CustomerCardPrettyPrinter;
    }());
    CustomerCardPrettyPrinterNs.CustomerCardPrettyPrinter = CustomerCardPrettyPrinter;
})(CustomerCardPrettyPrinterNs || (CustomerCardPrettyPrinterNs = {}));
