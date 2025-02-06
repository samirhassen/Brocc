namespace NotFoundComponentNs {

    export class NotFoundController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
 
        }

        componentName(): string {
            return 'notFound'
        }

        onChanges() {

        }
    }

    export class NotFoundComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = NotFoundController;
            this.templateUrl = 'not-found.html';
        }
    }

    export class InitialData {

    }
}

angular.module('ntech.components').component('notFound', new NotFoundComponentNs.NotFoundComponent())