var ModalDialogComponentNs;
(function (ModalDialogComponentNs) {
    class ModalDialogController extends NTechComponents.NTechComponentControllerBase {
        constructor($http, $q, ntechComponentService) {
            super(ntechComponentService, $http, $q);
            this.dialogId = NTechComponents.generateUniqueId(6);
        }
        componentName() {
            return 'modalDialog';
        }
        postLink() {
            //NOTE: Idea is to pick up when the user clicks like outside the area make sure to do a proper close instead
            $(() => {
                $("body " + '#' + this.dialogId).on("hide.bs.modal", (evt) => {
                    if (this.isOpen) {
                        evt.preventDefault();
                        this.setIsOpen(false, null);
                    }
                });
            });
        }
        onDoCheck() {
            let element = $(document.getElementById(this.dialogId));
            let isActuallyVisible = element.hasClass('in');
            if (isActuallyVisible != this.isOpen) {
                element.modal(this.isOpen ? 'show' : 'hide');
            }
        }
        onChanges() {
            this.setIsOpen(this.isOpen, null);
        }
        setIsOpen(isOpen, evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.isOpen = isOpen;
            $('#' + this.dialogId).modal(isOpen ? 'show' : 'hide');
        }
    }
    ModalDialogController.$inject = ['$http', '$q', 'ntechComponentService'];
    ModalDialogComponentNs.ModalDialogController = ModalDialogController;
    class ModalDialogComponent {
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
    ModalDialogComponentNs.ModalDialogComponent = ModalDialogComponent;
})(ModalDialogComponentNs || (ModalDialogComponentNs = {}));
angular.module('ntech.components').component('modalDialog', new ModalDialogComponentNs.ModalDialogComponent());
