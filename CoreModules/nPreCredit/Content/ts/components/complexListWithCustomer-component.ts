namespace ComplexListWithCustomerComponentNs {
    export class ComplexListWithCustomerController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'complexListWithCustomer'
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            let i = this.initialData
            let a = ApplicationEditorComponentNs.createInitialData(i.ai.ApplicationNr, i.ai.ApplicationType, NavigationTargetHelper.createCodeTarget(i.backTarget), i.apiClient, this.$q, x => {
                for (let fieldName of i.fieldNames) {
                    x.addComplexApplicationListItem(`${i.listName}#${i.listNr}#u#${fieldName}`)
                }
            }, {
                isInPlaceEditAllowed: !i.isReadonly,
                afterInPlaceEditsCommited: x => {
                    this.ntechComponentService.emitNTechCustomDataEvent<LocalInitialData>(AfterEditEventName, this.initialData)
                }
            })

            this.m = {
                a: a,
                c: {
                    applicationInfo: i.ai,
                    backUrl: encodeURIComponent(decodeURIComponent(i.urlToHereFromOtherModule)), //NOTE: This is a little sketchy. We would rather extend the navigation target concept to nCustomer or rather to be globally shared
                    backTarget: this.initialData.navigationTargetCodeToHere,
                    isEditable: i.ai.IsActive && !i.ai.IsFinalDecisionMade && !i.isReadonly,
                    editorService: new ApplicationCustomerListComponentNs.ComplexApplicationListEditorService(i.ai.ApplicationNr, i.listName, i.listNr, this.apiClient)
                }
            }
        }
    }

    export class Model {
        a: ApplicationEditorComponentNs.InitialData
        c: ApplicationCustomerListComponentNs.InitialData
    }

    export interface LocalInitialData {
        isReadonly: boolean
        ai: NTechPreCreditApi.ApplicationInfoModel
        listName: string
        listNr: number
        fieldNames: string[]
        correlationId?: string //Can be used to filter events for instance
    }

    export interface InitialData extends LocalInitialData, ComponentHostNs.ComponentHostInitialData {
    }

    export const AfterEditEventName: string = 'complexListWithCustomerAfterInPlaceEditsCommited'

    export class ComplexListWithCustomerComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ComplexListWithCustomerController;
            this.template = `<div ng-if="$ctrl.m">
<div class="row">
    <div class="col-sm-5"><div class="editblock"><application-editor initial-data="$ctrl.m.a"></application-editor></div></div>
    <div class="col-sm-7"><application-customer-list initial-data="$ctrl.m.c"></application-customer-list></div>
</div>`
        }
    }
}

angular.module('ntech.components').component('complexListWithCustomer', new ComplexListWithCustomerComponentNs.ComplexListWithCustomerComponent())