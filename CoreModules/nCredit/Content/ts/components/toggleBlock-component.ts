namespace ToggleBlockComponentNs {
    export class ToggleBlockController extends NTechComponents.NTechComponentControllerBase {
        headerText: string
        isExpanded: boolean
        onExpanded: (service: Service) => void
        isLocked: boolean

        private service = new Service(this)

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'toggleBlock'
        }

        onChanges() {
        }

        toggleExpanded(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.isExpanded = !this.isExpanded

            if (this.isExpanded && this.onExpanded) {
                this.onExpanded(this.service)
            }
        }
    }

    export class ToggleBlockComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;
        public transclude: boolean;
        constructor() {
            this.transclude = true
            this.bindings = {
                headerText: '<',
                onExpanded: '<',
                isLocked: '<'
            };
            this.controller = ToggleBlockController;
            this.template = `<div class="block">
        <div class="row" ng-if="$ctrl.isLocked && !$ctrl.isExpanded">
            <div class="col-xs-1">
                <span class="n-unlock" ng-click="$ctrl.toggleExpanded($event)"><a href="#"><span class="glyphicon glyphicon-chevron-right"></span><span class="glyphicon glyphicon-lock"></span></a></span>
            </div>
            <div class="col-xs-11"><h2>{{$ctrl.headerText}}</h2></div>
        </div>
        <div class="row" ng-if="!$ctrl.isLocked || $ctrl.isExpanded">
            <div class="col-xs-1">
                <span ng-click="$ctrl.toggleExpanded($event)" class="glyphicon" ng-class="{ 'chevron-bg glyphicon-chevron-down' : $ctrl.isExpanded, 'chevron-bg glyphicon-chevron-right' : !$ctrl.isExpanded }"></span>
            </div>
            <div class="col-xs-11"><h2>{{$ctrl.headerText}}</h2></div>
        </div>
        <div ng-if="$ctrl.isExpanded" style="padding-bottom: 70px; border-top:1px solid #000000; margin-top: 3px;" class="pt-2">
            <div ng-transclude></div>
        </div>
    </div>`;
        }
    }

    export class Service {
        constructor(private ctrl: ToggleBlockController) {
        }

        public setLocked(isLocked: boolean) {
            this.ctrl.isLocked = isLocked
        }
    }
}

angular.module('ntech.components').component('toggleBlock', new ToggleBlockComponentNs.ToggleBlockComponent())