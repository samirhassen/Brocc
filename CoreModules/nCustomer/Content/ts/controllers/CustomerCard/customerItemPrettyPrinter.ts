namespace CustomerCardPrettyPrinterNs {
    export class CustomerCardPrettyPrinter {
        translations: { [name: string]: string } = {
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
        }

        constructor() {
        }

        formatBool(v: any) {
            return (v && v.toString() == 'true') ? 'Yes' : 'No';
        }

        getFriendlyName(group: string, name: string) {
            if (this.translations[name]) {
                return this.translations[name]
            } else {
                return name
            }
        }

        getFriendlyValue(group: string, name: string, value: string) {
            if (name == 'externalIsPep' || name == 'sanction') {
                return this.formatBool(value)
            } else {
                return value
            }
        }
    }

    export interface ITranslationCountry {
        key: string
        sv: string
        fi: string
    }

    export interface IKycQuestions {
        [index: string]: any
    }

    export interface ITaxCountryItem {
        countryIsoCode: string
        taxNumber: string,
        isFlaggedForRemoval: boolean
    }
}