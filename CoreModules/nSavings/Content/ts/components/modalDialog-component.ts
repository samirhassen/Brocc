namespace ModalDialogComponentNs {

    export class ModalDialogController extends NTechComponents.NTechComponentControllerBase {
        dialogTitle: string;
        isOpen: boolean;
        dialogId: string;

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);

            this.dialogId = NTechComponents.generateUniqueId(6);
        }

        componentName(): string {
            return 'modalDialog'
        }

        protected postLink() {
            //NOTE: Idea is to pick up when the user clicks like outside the area make sure to do a proper close instead
            $(() => {
                $("body " + '#' + this.dialogId).on("hide.bs.modal", (evt: Event) => {
                    if (this.isOpen) {
                        evt.preventDefault();
                        this.setIsOpen(false, null);
                    }
                })
            })
        }

        protected onDoCheck() {
            let element: any = $(document.getElementById(this.dialogId));
            let isActuallyVisible = element.hasClass('in');
            if (isActuallyVisible != this.isOpen) {
                element.modal(this.isOpen ? 'show' : 'hide');
            }
        }

        onChanges() {
            this.setIsOpen(this.isOpen, null);
        }

        setIsOpen(isOpen: boolean, evt: Event) {
            if (evt) {
                evt.preventDefault();
            }
            this.isOpen = isOpen;
            (<any>$('#' + this.dialogId)).modal(isOpen ? 'show' : 'hide');
        }
    }

    export class ModalDialogComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;
        public transclude: boolean;

        constructor() {
            this.transclude = true;
            this.bindings = {
                dialogTitle: '<',
                isOpen: '='
            };
            this.controller = ModalDialogController;
            this.templateUrl = 'modal-dialog.html';
        }
    }
}

angular.module('ntech.components').component('modalDialog', new ModalDialogComponentNs.ModalDialogComponent())