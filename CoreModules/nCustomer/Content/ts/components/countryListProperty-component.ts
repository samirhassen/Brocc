namespace CountryListPropertyComponentNs {

    export class CountryListPropertyController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
 
        }

        componentName(): string {
            return 'countryListProperty'
        }

        onChanges() {
            this.m = null
            if (!this.initialData) {
                return
            }
            this.m = {
                allCountryIsoCodes: this.getFilteredCodes(Object.keys(this.initialData.allCountryCodesAndNames), this.initialData.countryIsoCodes),
                countryIsoCodes: angular.copy(this.initialData.countryIsoCodes),
            }
        }

        private getFilteredCodes(allCodes: string[], exceptCodes: string[]) {
            let cs = angular.copy(allCodes)
            for (let c of exceptCodes) {
                let i = cs.indexOf(c)
                if (i >= 0) {
                    cs.splice(i, 1)
                }
            }
            return cs
        }

        getCountryName(countryIsoCode: string) {
            return this.initialData.allCountryCodesAndNames[countryIsoCode]
        }

        onCountryChosen() {
            if (!this.m || !this.m.editCountryCode || this.m.countryIsoCodes.indexOf(this.m.editCountryCode) >= 0) {
                return
            }
            this.m.countryIsoCodes.push(this.m.editCountryCode)
            this.m.allCountryIsoCodes = this.getFilteredCodes(Object.keys(this.initialData.allCountryCodesAndNames), this.m.countryIsoCodes)
            this.m.editCountryCode = null
        }

        removeCountry(countryIsoCode: string, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!countryIsoCode) {
                return
            }
            let i = this.m.countryIsoCodes.indexOf(countryIsoCode)
            if (i < 0) {
                return
            }
            this.m.countryIsoCodes.splice(i, 1)
        }

        saveEdit(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.initialData.onSaveEdit(this.m.countryIsoCodes)
        }

        cancelEdit(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.initialData.onCancelEdit()
        }
    }

    export class CountryListPropertyComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CountryListPropertyController;
            this.templateUrl = 'country-list-property.html';
        }
    }

    export class InitialData {
        countryIsoCodes: string[]
        allCountryCodesAndNames: { [index: string]: string }
        labelText: string
        onSaveEdit: (countryIsoCodes: string[]) => void
        onCancelEdit: () => void
        historyItems?: HistoryItem[]
    }

    export class HistoryItem {
        ByUserDisplayName: string
        Date: Date
        CountryIsoCodes: string[]
    }

    export class Model {
        allCountryIsoCodes: string[]
        countryIsoCodes: string[]
        editCountryCode?: string
    }

    export function createEditHistoryItems(items: NTechCustomerApi.HistoryItem[], getValue: (i: NTechCustomerApi.HistoryItem) => string[]): CountryListPropertyComponentNs.HistoryItem[] {
        let result: CountryListPropertyComponentNs.HistoryItem[] = []
        for (let i of items) {
            result.push({
                ByUserDisplayName: i.UserDisplayName,
                Date: i.EditDate,
                CountryIsoCodes: getValue(i)
            })
        }
        return result
    }
}

angular.module('ntech.components').component('countryListProperty', new CountryListPropertyComponentNs.CountryListPropertyComponent())