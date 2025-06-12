namespace MortgageLoanApplicationBasisComponentNs {

    export class MortgageLoanApplicationBasisController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
 
        }

        componentName(): string {
            return 'mortgageLoanApplicationBasis'
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            this.apiClient.fetchMortgageLoanApplicationBasisCurrentValues(this.initialData.applicationInfo.ApplicationNr).then(x => {
                this.m = {
                    onBack: (this.initialData.onBack || this.initialData.backUrl) ? (evt => {
                        if (evt) {
                            evt.preventDefault()
                        }
                        if (this.initialData.onBack) {
                            this.initialData.onBack(this.m.wasChanged)
                        } else if (this.initialData.backUrl) {
                            document.location.href = this.initialData.backUrl
                        }
                    }) : null,
                    current: x,
                    wasChanged: false
                }
            })
        }

        editHouseholdIncome(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (this.initialData == null || this.m == null) {
                return
            }
            this.m.editMode = 'householdIncome'
            this.m.householdIncomeInitialData = {
                onBack: (newCombinedGrossMonthlyIncome) => {
                    if (newCombinedGrossMonthlyIncome !== null) {
                        this.m.wasChanged = true
                        this.m.current.CombinedGrossMonthlyIncome = newCombinedGrossMonthlyIncome
                    }
                    this.m.editMode = null
                    this.m.householdIncomeInitialData = null
                },
                applicationInfo: this.initialData.applicationInfo
            }
        }
    }

    export class MortgageLoanApplicationBasisComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationBasisController;
            this.templateUrl = 'mortgage-loan-application-basis.html';
        }
    }

    export class InitialData {
        applicationInfo : NTechPreCreditApi.ApplicationInfoModel
        onBack?: (wasChanged: boolean) => void
        backUrl?: string
    }

    export class Model {
        onBack?: (evt: Event) => void
        current: NTechPreCreditApi.MortgageLoanApplicationBasisCurrentValuesModel
        editMode?: string
        householdIncomeInitialData?: MortgageLoanApplicationHouseholdIncomeComponentNs.InitialData
        wasChanged: boolean
    }
}

angular.module('ntech.components').component('mortgageLoanApplicationBasis', new MortgageLoanApplicationBasisComponentNs.MortgageLoanApplicationBasisComponent())