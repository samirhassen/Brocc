namespace ApplicationDataEditorComponentNs {
    export function isNumberType(dt: string) {
        let d = (dt ? dt : '').toLowerCase()
        return d.indexOf('int') >= 0 || d.indexOf('decimal') >= 0
    }

    export function isIntegerType(dt: string) {
        return isNumberType(dt) && dt.toLowerCase().indexOf('int') > 0
    }

    export function getDropdownDisplayValue(value: string, m: NTechPreCreditApi.FetchApplicationEditItemDataResponseEditModel) {
        if (!m || !m.DropdownRawOptions || !m.DropdownRawDisplayTexts) {
            return value
        }
        for (let i = 0; i < m.DropdownRawOptions.length; i++) {
            if (m.DropdownRawOptions[i] === value && m.DropdownRawDisplayTexts.length > i) {
                return m.DropdownRawDisplayTexts[i]
            }
        }
        return value
    }

    export class ApplicationDataEditorController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', '$scope']
        constructor(private $http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private $scope: ng.IScope) {
            super(ntechComponentService, $http, $q);

            this.$scope['formContainer'] = {}
        }

        componentName(): string {
            return 'applicationDataEditor'
        }

        isReadOnly() {
            if (!this.m) {
                return true
            }
            return this.initialData.isReadOnly
        }

        onBack(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            NavigationTargetHelper.handleBack(
                this.m.BackTarget,
                this.apiClient,
                this.$q,
                { applicationNr: this.initialData.applicationNr })
        }

        onChanges() {
            this.m = null;

            if (!this.initialData) {
                return
            }

            let i = this.initialData

            let backTarget = i && i.backTarget ? NavigationTargetHelper.createCodeTarget(i.backTarget) : (i.backUrl ? NavigationTargetHelper.createUrlTarget(i.backUrl) : null)

            this.m = {
                HistoricalChanges: null,
                EditorInitialData: null,
                EditModel: null,
                BackTarget: backTarget
            }

            this.m.EditorInitialData = ApplicationEditorComponentNs.createInitialData(
                i.applicationNr,
                i.applicationType,
                backTarget,
                this.apiClient,
                this.$q,
                x => {
                    x.addDataSourceItem(i.dataSourceName, i.itemName, i.isReadOnly, true)
                },
                {
                    afterInPlaceEditsCommited: commitedEdits => {
                        this.reloadHistory()
                    },
                    afterDataLoaded: data => {
                        this.m.EditModel = data.Results[0].Items[0].EditorModel
                        this.reloadHistory()
                    },
                    isInPlaceEditAllowed: !i.isReadOnly
                })
        }

        private reloadHistory() {
            let i = this.initialData
            this.apiClient.fetchApplicationEditItemData(i.applicationNr, i.dataSourceName, i.itemName, ApplicationDataSourceHelper.MissingItemReplacementValue, true).then(edits => {
                this.m.HistoricalChanges = edits.HistoricalChanges
            })
        }

        parseHistoryValue(v: string) {
            if (v === '-') {
                return null
            }
            return ApplicationItemEditorComponentNs.getItemDisplayValueShared(v, this.m.EditModel, x => this.parseDecimalOrNull(x))
        }

        getUserDisplayName(userId: number) {
            if (!this.initialData || !this.initialData.userDisplayNameByUserId[userId.toString()]) {
                return 'User ' + userId
            } else {
                return this.initialData.userDisplayNameByUserId[userId.toString()]
            }
        }

        form = () => {
            if (!this.$scope) {
                return null
            }
            let c = this.$scope['formContainer']
            if (!c) {
                return null
            }
            return c['editValueForm'] as ng.IFormController
        }
    }

    export class ApplicationDataEditorComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ApplicationDataEditorController;
            this.templateUrl = 'application-data-editor.html';
        }
    }

    export interface InitialData extends ComponentHostNs.ComponentHostInitialData {
        applicationNr: string
        applicationType: string
        isReadOnly?: boolean
        dataSourceName: string
        itemName: string
    }

    export class Model {
        HistoricalChanges: NTechPreCreditApi.FetchApplicationEditItemDataResponseHistoryItemModel[]
        EditorInitialData: ApplicationEditorComponentNs.InitialData
        EditModel: NTechPreCreditApi.FetchApplicationEditItemDataResponseEditModel
        BackTarget: NavigationTargetHelper.CodeOrUrl
    }
}

angular.module('ntech.components').component('applicationDataEditor', new ApplicationDataEditorComponentNs.ApplicationDataEditorComponent())