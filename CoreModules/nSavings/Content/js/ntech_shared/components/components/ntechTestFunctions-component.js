var NTechTestFunctionsComponentNs;
(function (NTechTestFunctionsComponentNs) {
    class NTechTestFunctionsController extends NTechComponents.NTechComponentControllerBase {
        constructor($http, $q, $scope, $timeout, $filter, ntechComponentService) {
            super(ntechComponentService, $http, $q);
            this.$scope = $scope;
            this.$timeout = $timeout;
            this.$filter = $filter;
        }
        componentName() {
            return 'ntechTestFunctions';
        }
        onChanges() {
        }
    }
    NTechTestFunctionsController.$inject = ['$http', '$q', '$scope', '$timeout', '$filter', 'ntechComponentService'];
    NTechTestFunctionsComponentNs.NTechTestFunctionsController = NTechTestFunctionsController;
    class NTechTestFunctionsComponent {
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
    NTechTestFunctionsComponentNs.NTechTestFunctionsComponent = NTechTestFunctionsComponent;
    class TestFunction {
    }
    NTechTestFunctionsComponentNs.TestFunction = TestFunction;
})(NTechTestFunctionsComponentNs || (NTechTestFunctionsComponentNs = {}));
angular.module('ntech.components').component('ntechTestFunctions', new NTechTestFunctionsComponentNs.NTechTestFunctionsComponent());
