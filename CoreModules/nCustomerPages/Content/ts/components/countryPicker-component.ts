namespace CountryPickerComponentNs {
    export class CountryPickerController extends NTechComponents.NTechComponentControllerBase {
        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        selectedCountries: CountryModel[]

        onCountrySelected() {
            if (this.selectedCountryCode) {
                let c: CountryModel = _.find(this.countries, x => x.code === this.selectedCountryCode)
                if (c) {
                    this.selectedCountries.push(c)
                    this.selectedCountryCode = '';
                }                
            }
        }

        removeSelected(c: CountryModel, evt: Event) {
            if (evt) {
                evt.preventDefault();
            }
            let i = _.findIndex(this.selectedCountries, x => x.code === c.code)
            if (i >= 0) {
                this.selectedCountries.splice(i, 1)
            }
        }

        isSelected(code: string) {
            return _.findIndex(this.selectedCountries, x => x.code === code) >= 0
        }

        countries: CountryModel[]
        label: string
        showRequiredMessage: boolean
        
        selectedCountryCode: string = ''
        selectedCountryCodes: any = {}

        componentName(): string {
            return 'countryPicker'
        }

        onChanges() {
            
        }
    }

    export class CountryPickerComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;
        public transclude: boolean;

        constructor() {
            this.transclude = true;
            this.bindings = {
                countries: '<',
                label: '<',
                showRequiredMessage: '<',
                selectedCountries: '='
            };
            this.controller = CountryPickerController;
            this.templateUrl = 'country-picker.html';
        }
    }

    export class CountryModel {
        code: string
        name: string
    }

    export class OnUpdateModel {
        selectedCountries : CountryModel[]
    }
}

angular.module('ntech.components').component('countryPicker', new CountryPickerComponentNs.CountryPickerComponent())