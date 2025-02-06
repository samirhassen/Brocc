namespace MortgageApplicationObjectValuationManualSearchComponentNs {

    export class MortgageApplicationObjectValuationManualSearchController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData;

        ucbvSearchAddressInput: UcbvSearchAddressInputModel;
        ucbvSearchAddressHits: NTechPreCreditApi.UcbvSokAdressHit[]
        ucbvSearchForm: SimpleFormComponentNs.InitialData
        
        ucbvFetchObjectForm: SimpleFormComponentNs.InitialData
        ucbvFetchObjectInput: UcbvFetchObjectInputModel
        ucbvFetchObjectHit: UcbvFetchObjectResult

        ucbvVarderaBostadsrattForm: SimpleFormComponentNs.InitialData
        ucbvVarderaBostadsrattInput: NTechPreCreditApi.IUcbvVarderaBostadsrattRequest
        ucbvVarderaBostadsrattHit: UcbvVarderaBostadsResult

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'mortgageApplicationObjectValuationManualSearch'
        }

        onChanges() {
            //Search address
            this.ucbvSearchAddressInput = {
                city: '',
                streetAddress: '',
                municipality: '',
                zipcode: ''
            }
            this.ucbvSearchForm = {
                modelBase: this.ucbvSearchAddressInput,
                items: [
                    SimpleFormComponentNs.textField({ labelText: 'Street address', model: 'streetAddress', required: true }),
                    SimpleFormComponentNs.textField({ labelText: 'Zipcode', model: 'zipcode' }),
                    SimpleFormComponentNs.textField({ labelText: 'City', model: 'city' }),
                    SimpleFormComponentNs.textField({ labelText: 'Municipality', model: 'municipality' }),
                    SimpleFormComponentNs.button({ buttonText: 'Search', onClick: () => { this.ucbvSearchAddress(this.ucbvSearchAddressInput, null); } })
                ]
            }
            this.ucbvSearchAddressHits = null
            
            //Fetch object
            this.ucbvFetchObjectInput = {
                objectId: ''
            }
            this.ucbvFetchObjectForm = {
                modelBase: this.ucbvFetchObjectInput,
                items: [
                    SimpleFormComponentNs.textField({ labelText: 'Object Id', model: 'objectId', required: true }),
                    SimpleFormComponentNs.button({ buttonText: 'Fetch', onClick: () => { this.ucbvFetchObject(this.ucbvFetchObjectInput, null); } })
                ]
            }

            //Vardera bostadsratt
            this.ucbvVarderaBostadsrattHit = null;
            this.ucbvVarderaBostadsrattInput = {
                objektID: ''
            }
            this.ucbvVarderaBostadsrattForm = {
                modelBase: this.ucbvVarderaBostadsrattInput,
                items: [
                    SimpleFormComponentNs.textField({ labelText: 'objektID', model: 'objektID', required: true }),
                    SimpleFormComponentNs.textField({ labelText: 'yta', model: 'yta' }),
                    SimpleFormComponentNs.button({ buttonText: 'Vardera', onClick: () => { this.ucbvVarderaBostadsratt(this.ucbvVarderaBostadsrattInput, null); } })
                ]
            }            
        }        

        ucbvSearchAddress(input: UcbvSearchAddressInputModel, evt: Event) {
            if (evt) {
                evt.preventDefault();
            }

            this.apiClient.ucbvSokAddress(input.streetAddress, input.zipcode, input.city, input.municipality).then(result => {
                this.ucbvSearchAddressHits = result
            })
        }

        ucbvFetchObject(input: UcbvFetchObjectInputModel, evt: Event) {
            if (evt) {
                evt.preventDefault();
            }

            this.apiClient.ucbvHamtaObjekt(input.objectId).then(result => {
                if (result) {
                    this.ucbvFetchObjectHit = {
                        hit: result
                    }
                } else {
                    this.ucbvFetchObjectHit = { hit: null }
                }
            })
        }

        ucbvVarderaBostadsratt(input: NTechPreCreditApi.IUcbvVarderaBostadsrattRequest, evt: Event) {
            if (evt) {
                evt.preventDefault();
            }

            this.apiClient.ucbvVarderaBostadsratt(input).then(result => {
                if (result) {
                    this.ucbvVarderaBostadsrattHit = {
                        hit: result
                    }
                } else {
                    this.ucbvVarderaBostadsrattHit = { hit: null }
                }
            })
        }

        ucbvVarderaBostadsrattJson(hit : NTechPreCreditApi.UcbvVarderaBostadsrattResponse) {
            if (hit) {
                return JSON.parse(hit.RawJson)
            } else {
                return null;
            }
        }

        arrayToCommaList(a: string[]) {
            if (!a) {
                return null
            } else {
                let s = ''
                angular.forEach(a, x => {
                    if (s.length > 0) {
                        s += ', '
                    }
                    s += x
                })
                return s
            }
        }

        brfSignalToCode(value: number): string {
            if (value === 0) {
                return "Okand"
            } else if (value === 1) {
                return "Ok"
            } else if (value === 2) {
                return "Varning"
            } else if (!value) {
                return null
            } else {
                return 'Kod' + value.toString()
            }
        }
    }

    export class MortgageApplicationObjectValuationManualSearchComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationObjectValuationManualSearchController;
            this.templateUrl = 'mortgage-application-object-valuation-manual-search.html';
        }
    }

    export class InitialData {
        applicationInfo: NTechPreCreditApi.ApplicationInfoModel
        backUrl: string
    }

    export class UcbvSearchAddressInputModel {
        streetAddress: string
        zipcode: string
        city: string
        municipality: string
    }

    export class UcbvFetchObjectInputModel {
        objectId: string
    }
    export class UcbvFetchObjectResult {
        hit: NTechPreCreditApi.UcbvObjectInfo
    }

    export class UcbvVarderaBostadsResult {
        hit : NTechPreCreditApi.UcbvVarderaBostadsrattResponse
    }
}

angular.module('ntech.components').component('mortgageApplicationObjectValuationManualSearch', new MortgageApplicationObjectValuationManualSearchComponentNs.MortgageApplicationObjectValuationManualSearchComponent())