namespace NTechTestFunctionsComponentNs {

    export class NTechTestFunctionsController extends NTechComponents.NTechComponentControllerBase {
        static $inject = ['$http', '$q', '$scope', '$timeout', '$filter', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            private $scope: IScopeWithForm,
            private $timeout: ng.ITimeoutService,
            private $filter: ng.IFilterService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);

        }

        testFunctions: NTechTestFunctionsComponentNs.TestFunction[]        

        componentName(): string {
            return 'ntechTestFunctions'
        }

        onChanges() {

        }
    }

    export class NTechTestFunctionsComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;
        public transclude: boolean;

        constructor() {
            this.transclude = true;
            this.bindings = {
                testFunctions: '<',
            };
            this.controller = NTechTestFunctionsController;
            this.template = `<div ng-show="$ctrl.isVisible" class="frame popup-position">           
            <div class="pt-1">
                <button class="btn btn-primary" ng-repeat="f in $ctrl.testFunctions" ng-click="f.run()">{{f.title}}</button>
            </div>
        </div>
        <div style="position:fixed;bottom:20px;right:5%;">
            <button class="btn btn-default" ng-class="{ 'toned-down' : $ctrl.isVisible }" ng-click="$ctrl.isVisible=!$ctrl.isVisible"><span class="glyphicon glyphicon-sort"></span></button>
        </div>`;
        }
    }

    interface IScopeWithForm extends ng.IScope {
        [index: string]: any
    }

    export class TestFunction {
        run: () => void
        title: string
      }
}

angular.module('ntech.components').component('ntechTestFunctions', new NTechTestFunctionsComponentNs.NTechTestFunctionsComponent())