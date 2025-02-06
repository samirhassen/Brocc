namespace PageHeaderComponentNs {
    export class PageHeaderController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        titleText: string

        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'pageHeader'
        }

        onBack(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (this.initialData.backTarget) {
                NavigationTargetHelper.handleBack(this.initialData.backTarget, this.apiClient, this.$q, this.initialData.backContext)
            } else {
                NavigationTargetHelper.handleBackWithInitialDataDefaults(this.initialData.host, this.apiClient, this.$q, this.initialData.backContext)
            }
        }

        onChanges() {
            this.m = null

            if (!this.initialData || !this.titleText) {
                return
            }

            this.m = {
                titleText: this.titleText
            }
        }
    }

    export class PageHeaderComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<',
                titleText: '<'
            };
            this.controller = PageHeaderController;
            this.template = `<div class="pt-1 pb-2" ng-if="$ctrl.m">
        <div class="pull-left"><a class="n-back" href="#" ng-click="$ctrl.onBack($event)"><span class="glyphicon glyphicon-arrow-left"></span></a></div>
        <h1 class="adjusted">{{$ctrl.m.titleText}}</h1>
    </div>`
        }
    }

    export class Model {
        titleText: string
    }

    export interface InitialData {
        host: ComponentHostNs.ComponentHostInitialData
        backTarget: NavigationTargetHelper.CodeOrUrl
        backContext: NavigationTargetHelper.NavigationContext
    }
}

angular.module('ntech.components').component('pageHeader', new PageHeaderComponentNs.PageHeaderComponent())