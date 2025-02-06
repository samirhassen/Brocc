namespace KycManagementLocalDecisionComponentNs {

    export class KycManagementLocalDecisionController extends NTechComponents.NTechComponentControllerBase {
        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        initialData: InitialData
        modeChanged: (mode: IEditMode) => void
        m: Model

        componentName(): string {
            return 'kycManagementLocalDecision'
        }

        onChanges() {
            this.setup(null)
        }

        setup(currentData: NTechCustomerApi.KycLocalDecisionCurrentModel) {
            this.m = null
            this.setEditModel(null)
            if (!this.initialData) {
                return
            }
            let withCd = (x: NTechCustomerApi.KycLocalDecisionCurrentModel) => {
                this.m = {
                    localIsPep: x.IsPep,
                    localIsSanction: x.IsSanction,
                    amlRiskClass: x.AmlRiskClass
                }
            }
            if (currentData) {
                withCd(currentData)
            } else {
                this.apiClient.kycManagementFetchLocalDecisionData(this.initialData.customerId).then(result => {
                    withCd(result)
                })
            }
        }

        getKycStateDisplayName(b: boolean) {
            if (b === true) {
                return 'Yes'
            } else if (b === false) {
                return 'No'
            } else {
                return 'Unknown'
            }
        }

        setEditModel(e: EditModel) {
            if (this.m) {
                this.m.editModel = e
            }
            if (this.modeChanged) {
                let isEditMode = !!e
                if (isEditMode) {
                    this.modeChanged({
                        isEditMode: isEditMode,
                        cancelEdit: evt => this.cancelEdit(evt),
                        isEditingPep: e.isEditingPep
                    })
                } else {
                    this.modeChanged({ isEditMode: false, isEditingPep: null, cancelEdit: null })
                }
            }
        }

        edit(isEditingPep: boolean, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.m) {
                return
            }
            this.apiClient.kycManagementFetchLocalDecisionHistoryData(this.initialData.customerId, isEditingPep).then(result => {
                let historicalValues = []
                if (result.CurrentValue) {
                    historicalValues.push(result.CurrentValue)
                }
                if (result.HistoricalValues) {
                    for (let h of result.HistoricalValues) {
                        historicalValues.push(h)
                    }
                }
                this.setEditModel({
                    currentState: result.CurrentValue ? this.boolToString(result.CurrentValue.Value) : this.boolToString(null),
                    isEditingPep: result.IsModellingPep,
                    historicalValues: historicalValues
                })
            })
        }

        editAmlRiskClass(evt?: Event) {
            evt?.preventDefault();

            this.m.editAmlRiskModel = {
                customerId: this.initialData.customerId,
                itemName: 'amlRiskClass',
                onClose: () => {
                    this.setup(null)
                },
                hideHeader: true
            }
        }

        saveEdit(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.m || !this.m.editModel) {
                return
            }
            let newLocalValue = this.stringToBool(this.m.editModel.currentState)

            if (newLocalValue !== true && newLocalValue !== false) {
                toastr.warning('Cannot change back to unknown')
                return
            }

            this.apiClient.kycManagementSetLocalDecision(this.initialData.customerId, this.m.editModel.isEditingPep, newLocalValue, true).then(result => {
                this.setup(result.NewCurrentData)
            })
        }

        cancelEdit(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.m) {
                return
            }
            this.setEditModel(null)
        }

        boolToString(b: boolean) {
            if (b === true) {
                return 'true'
            } else if (b === false) {
                return 'false'
            } else {
                return ''
            }
        }

        stringToBool(s: string) {
            if (s === 'true') {
                return true
            } else if (s === 'false') {
                return false
            } else {
                return null
            }
        }
    }

    export class KycManagementLocalDecisionComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;
        public transclude: boolean;

        constructor() {
            this.transclude = true;
            this.bindings = {
                initialData: '<',
                modeChanged: '<'
            };
            this.controller = KycManagementLocalDecisionController;
            this.templateUrl = 'kyc-management-local-decision.html';
        }
    }

    export class InitialData {
        customerId: number
    }

    export class Model {
        localIsPep: boolean
        localIsSanction: boolean
        amlRiskClass: string
        editModel?: EditModel
        editAmlRiskModel?: EditCustomerContactInfoValueComponentNs.InitialData
    }
    export class EditModel {
        currentState: string
        isEditingPep: boolean
        historicalValues: NTechCustomerApi.KycLocalDecisionHistoryItem[]
    }

    export interface IEditMode {
        isEditMode: boolean
        isEditingPep: boolean
        cancelEdit: (evt: Event) => void
    }
}

angular.module('ntech.components').component('kycManagementLocalDecision', new KycManagementLocalDecisionComponentNs.KycManagementLocalDecisionComponent())