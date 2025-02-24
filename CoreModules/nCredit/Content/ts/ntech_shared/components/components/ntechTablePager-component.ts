namespace NTechTablePagerComponentNs {
    export class NTechTablePagerController extends NTechComponents.NTechComponentControllerBase {
        pagingObject: NTechTables.PagingObject

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'ntechTablePager'
        }

        onChanges() {
        }

        onGotoPage(pageNr: number, event: Event) {
            if (event) {
                event.preventDefault()
            }
            if (this.pagingObject.onGotoPage) {
                this.pagingObject.onGotoPage({ pageNr: pageNr, pagingObject: this.pagingObject, event: event })
            }
        }
    }

    export class NTechTablePagerComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;
        public transclude: boolean;
        constructor() {
            this.transclude = true
            this.bindings = {
                pagingObject: '<',
            };
            this.controller = NTechTablePagerController;
            this.template = `<div ng-if="$ctrl.pagingObject && $ctrl.pagingObject.pages && $ctrl.pagingObject.pages.length > 1" class="dataTables_paginate paging_simple_numbers custom-pagination">
                <ul class="pagination">
                    <li class="paginate_button previous" ng-show="$ctrl.pagingObject.isPreviousAllowed"><a href="#" ng-click="$ctrl.onGotoPage($ctrl.pagingObject.previousPageNr, $event)">Previous</a></li>
                    <li class="paginate_button previous disabled" ng-hide="$ctrl.pagingObject.isPreviousAllowed"><a href="#" ng-click="$event.preventDefault()">Previous</a></li>

                    <li ng-repeat="p in $ctrl.pagingObject.pages" class="paginate_button" ng-class="{ 'active' : p.isCurrentPage, 'disabled' : p.isSeparator }">
                        <a href="#" ng-click="$ctrl.onGotoPage(p.pageNr, $event)" ng-hide="p.isSeparator">{{p.pageNr+1}}</a>
                        <a href="#" ng-show="p.isSeparator" ng-click="$event.preventDefault()">...</a>
                    </li>

                    <li class="paginate_button next" ng-show="$ctrl.pagingObject.isNextAllowed"><a href="#" ng-click="$ctrl.onGotoPage($ctrl.pagingObject.nextPageNr, $event)">Next</a></li>
                    <li class="paginate_button next disabled" ng-hide="$ctrl.pagingObject.isNextAllowed"><a href="#" ng-click="$event.preventDefault()">Next</a></li>
                </ul>
            </div>` ;
        }
    }
}

angular.module('ntech.components').component('ntechTablePager', new NTechTablePagerComponentNs.NTechTablePagerComponent())