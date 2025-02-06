namespace ApplicationStatusBlockComponentNs {
    export function getIconClass(isAccepted: boolean, isRejected: boolean) {
        var isOther = !isAccepted && !isRejected
        return { 'glyphicon-ok': isAccepted, 'glyphicon-remove': isRejected, 'glyphicon-minus': isOther, 'glyphicon': true, 'text-success': isAccepted, 'text-danger': isRejected }
    }
    export function getHeaderClass(isAccepted: boolean, isRejected: boolean, isActive: boolean) {
        var isOther = !isAccepted && !isRejected
        return { 'text-success': isAccepted, 'text-danger': isRejected, 'text-ntech-inactive': isOther && !isActive }
    }
    export class ApplicationStatusBlockController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData;

        isExpanded: boolean = false;

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'applicationStatusBlock'
        }

        onChanges() {
            this.setExpanded(this.initialData ? this.initialData.isInitiallyExpanded : false);
        }

        toggleExpanded(evt: Event) {
            this.setExpanded(!this.isExpanded)
        }

        setExpanded(isExpanded: boolean) {
            this.isExpanded = isExpanded;
        }

        headerClassFromStatus(status, isActive) {
            var isAccepted = status === 'Accepted'
            var isRejected = status === 'Rejected'
            return getHeaderClass(isAccepted, isRejected, isActive)
        }

        iconClassFromStatus(status) {
            var isAccepted = status === 'Accepted'
            var isRejected = status === 'Rejected'
            return getIconClass(isAccepted, isRejected)
        }
    }

    export class ApplicationStatusBlockComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;
        public transclude: boolean;

        constructor() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = ApplicationStatusBlockController;
            this.templateUrl = 'application-status-block.html';
        }
    }

    export class InitialData {
        title: string
        status: string
        isInitiallyExpanded: boolean
        isActive: boolean
    }
}

angular.module('ntech.components').component('applicationStatusBlock', new ApplicationStatusBlockComponentNs.ApplicationStatusBlockComponent())