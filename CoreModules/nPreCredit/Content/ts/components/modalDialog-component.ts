namespace ModalDialogComponentNs {

    export class ModalDialogController extends NTechComponents.NTechComponentControllerBase {
        dialogTitle: string;
        dialogId: string;
        useFullWidth: boolean
        private removalId: string

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogService) {
            super(ntechComponentService, $http, $q);

        }

        componentName(): string {
            return 'modalDialog'
        }

        protected onDoCheck() {

        }

        onChanges() {
            if (this.removalId) {
                this.modalDialogService.unsubscribeFromDialogEvents(this.removalId)
                this.removalId = null
            }

            let localDialogId = this.dialogId
            if (!localDialogId) {
                return
            }
            this.modalDialogService.subscribeToDialogEvents(e => {
                if (e.dialogId !== localDialogId) {
                    return
                }                
                let isActuallyVisible = this.isActuallyVisible()
                if (isActuallyVisible === null) {
                    return
                }

                if (e.isOpenRequest && !isActuallyVisible) {
                    ($('#' + localDialogId) as any).modal('show')
                }

                if (e.isCloseRequest && isActuallyVisible) {
                    ($('#' + localDialogId) as any).modal('hide')
                }
            })
            $(document).on('hidden.bs.modal', '#' + localDialogId, (event) => {
                this.modalDialogService.signalDialogClosed(localDialogId)
            })
            $(document).on('shown.bs.modal', '#' + localDialogId, (event) => {
                this.modalDialogService.signalDialogOpened(localDialogId)
            })
        }

        isActuallyVisible(): boolean {
            let element: any = $(document.getElementById(this.dialogId))
            if (!element) {
                return null
            }
            return element.hasClass('in')
        }

        closeDialog(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.modalDialogService.closeDialog(this.dialogId)
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
                dialogId: '<',
                useFullWidth: '<'
            };
            this.controller = ModalDialogController;
            this.templateUrl = 'modal-dialog.html';
        }
    }

    export class ModalDialogService {
        private eventHost: NTechComponents.NTechEventHost<DialogEvent> = new NTechComponents.NTechEventHost<DialogEvent>()

        public generateDialogId(): string {
            return 'd' + NTechComponents.generateUniqueId(6)
        }

        public signalDialogClosed(dialogId: string) {
            this.eventHost.signalEvent({
                dialogId: dialogId,
                isClosed: true
            })
        }

        public signalDialogOpened(dialogId: string) {
            this.eventHost.signalEvent({
                dialogId: dialogId,
                isOpened: true
            })
        }

        public openDialog(dialogId: string, doAfterOpened?: () => void) {
            if (doAfterOpened) {
                this.eventHost.subscribeToOneEventOnly(x => x.dialogId === dialogId && x.isOpened, doAfterOpened)
            }
            this.eventHost.signalEvent({
                dialogId: dialogId,
                isOpenRequest: true
            })
        }

        public closeDialog(dialogId: string, doAfterClosed?: () => void) {
            if (doAfterClosed) {
                this.eventHost.subscribeToOneEventOnly(x => x.dialogId === dialogId && x.isClosed, doAfterClosed)
            }
            this.eventHost.signalEvent({
                dialogId: dialogId,
                isCloseRequest: true
            })
        }

        public subscribeToDialogEvents(f: (e: DialogEvent) => void) : string  {
            return this.eventHost.subscribeToEvent(f)
        }

        public unsubscribeFromDialogEvents(removalId: string) {
            this.eventHost.unSubscribeFromEvent(removalId)
        }
    }

    export class DialogEvent {
        dialogId: string
        isClosed?: boolean
        isOpened?: boolean
        isOpenRequest?: boolean
        isCloseRequest?: boolean
    }
}

angular.module('ntech.components').component('modalDialog', new ModalDialogComponentNs.ModalDialogComponent()).service('modalDialogService', ModalDialogComponentNs.ModalDialogService)
