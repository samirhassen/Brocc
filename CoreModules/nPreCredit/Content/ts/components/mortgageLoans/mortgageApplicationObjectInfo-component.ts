namespace MortgageApplicationObjectInfoComponentNs {

    export class MortgageApplicationObjectInfoController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model

        static $inject = ['$http', '$q', '$filter', '$translate', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            private $filter: ng.IFilterService,
            private $translate: any,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);

            ntechComponentService.subscribeToNTechEvents(evt => {
                if (evt.eventName === 'showMortgageObjectValuationDetails' && this.initialData && evt.eventData === this.initialData.applicationInfo.ApplicationNr) {
                    this.reload(false)
                }
            })
        }

        componentName(): string {
            return 'mortgageApplicationObjectInfo'
        }

        reload(isCompactMode: boolean) {
            this.m = null

            if (!this.initialData) {
                return;
            }

            this.apiClient.fetchCreditApplicationItemSimple(this.initialData.applicationInfo.ApplicationNr, ['mortageLoanObject.*'], '').then(x => {
                let d: NTechPreCreditApi.IStringDictionary<string> = {}
                for (let name of Object.keys(x)) {
                    d[name.substr('mortageLoanObject.'.length)] = x[name]
                }
                this.m = {
                    isCompactMode: isCompactMode,
                    infoBlockInitialData: new TwoColumnInformationBlockComponentNs.InitialDataFromObjectBuilder(
                        d,
                        null,
                        Object.keys(d),
                        [])
                        .buildInitialData()
                }
            })
        }

        onChanges() {
            this.reload(true)
        }
    }

    export class MortgageApplicationObjectInfoComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;
        public transclude: boolean;

        constructor() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationObjectInfoController;
            this.templateUrl = 'mortgage-application-object-info.html';
        }
    }

    export class InitialData {
        applicationInfo: NTechPreCreditApi.ApplicationInfoModel
    }       

    export class Model {
        isCompactMode: boolean
        infoBlockInitialData: TwoColumnInformationBlockComponentNs.InitialData
    }
}

angular.module('ntech.components').component('mortgageApplicationObjectInfo', new MortgageApplicationObjectInfoComponentNs.MortgageApplicationObjectInfoComponent())