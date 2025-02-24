var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (Object.prototype.hasOwnProperty.call(b, p)) d[p] = b[p]; };
        return extendStatics(d, b);
    };
    return function (d, b) {
        if (typeof b !== "function" && b !== null)
            throw new TypeError("Class extends value " + String(b) + " is not a constructor or null");
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
var ModalDialogComponentNs;
(function (ModalDialogComponentNs) {
    var ModalDialogController = /** @class */ (function (_super) {
        __extends(ModalDialogController, _super);
        function ModalDialogController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.modalDialogService = modalDialogService;
            return _this;
        }
        ModalDialogController.prototype.componentName = function () {
            return 'modalDialog';
        };
        ModalDialogController.prototype.onDoCheck = function () {
        };
        ModalDialogController.prototype.onChanges = function () {
            var _this = this;
            if (this.removalId) {
                this.modalDialogService.unsubscribeFromDialogEvents(this.removalId);
                this.removalId = null;
            }
            var localDialogId = this.dialogId;
            if (!localDialogId) {
                return;
            }
            this.modalDialogService.subscribeToDialogEvents(function (e) {
                if (e.dialogId !== localDialogId) {
                    return;
                }
                var isActuallyVisible = _this.isActuallyVisible();
                if (isActuallyVisible === null) {
                    return;
                }
                if (e.isOpenRequest && !isActuallyVisible) {
                    $('#' + localDialogId).modal('show');
                }
                if (e.isCloseRequest && isActuallyVisible) {
                    $('#' + localDialogId).modal('hide');
                }
            });
            $(document).on('hidden.bs.modal', '#' + localDialogId, function (event) {
                _this.modalDialogService.signalDialogClosed(localDialogId);
            });
            $(document).on('shown.bs.modal', '#' + localDialogId, function (event) {
                _this.modalDialogService.signalDialogOpened(localDialogId);
            });
        };
        ModalDialogController.prototype.isActuallyVisible = function () {
            var element = $(document.getElementById(this.dialogId));
            if (!element) {
                return null;
            }
            return element.hasClass('in');
        };
        ModalDialogController.prototype.closeDialog = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.modalDialogService.closeDialog(this.dialogId);
        };
        ModalDialogController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return ModalDialogController;
    }(NTechComponents.NTechComponentControllerBase));
    ModalDialogComponentNs.ModalDialogController = ModalDialogController;
    var ModalDialogComponent = /** @class */ (function () {
        function ModalDialogComponent() {
            this.transclude = true;
            this.bindings = {
                dialogTitle: '<',
                dialogId: '<',
                useFullWidth: '<'
            };
            this.controller = ModalDialogController;
            this.templateUrl = 'modal-dialog.html';
        }
        return ModalDialogComponent;
    }());
    ModalDialogComponentNs.ModalDialogComponent = ModalDialogComponent;
    var ModalDialogService = /** @class */ (function () {
        function ModalDialogService() {
            this.eventHost = new NTechComponents.NTechEventHost();
        }
        ModalDialogService.prototype.generateDialogId = function () {
            return 'd' + NTechComponents.generateUniqueId(6);
        };
        ModalDialogService.prototype.signalDialogClosed = function (dialogId) {
            this.eventHost.signalEvent({
                dialogId: dialogId,
                isClosed: true
            });
        };
        ModalDialogService.prototype.signalDialogOpened = function (dialogId) {
            this.eventHost.signalEvent({
                dialogId: dialogId,
                isOpened: true
            });
        };
        ModalDialogService.prototype.openDialog = function (dialogId, doAfterOpened) {
            if (doAfterOpened) {
                this.eventHost.subscribeToOneEventOnly(function (x) { return x.dialogId === dialogId && x.isOpened; }, doAfterOpened);
            }
            this.eventHost.signalEvent({
                dialogId: dialogId,
                isOpenRequest: true
            });
        };
        ModalDialogService.prototype.closeDialog = function (dialogId, doAfterClosed) {
            if (doAfterClosed) {
                this.eventHost.subscribeToOneEventOnly(function (x) { return x.dialogId === dialogId && x.isClosed; }, doAfterClosed);
            }
            this.eventHost.signalEvent({
                dialogId: dialogId,
                isCloseRequest: true
            });
        };
        ModalDialogService.prototype.subscribeToDialogEvents = function (f) {
            return this.eventHost.subscribeToEvent(f);
        };
        ModalDialogService.prototype.unsubscribeFromDialogEvents = function (removalId) {
            this.eventHost.unSubscribeFromEvent(removalId);
        };
        return ModalDialogService;
    }());
    ModalDialogComponentNs.ModalDialogService = ModalDialogService;
    var DialogEvent = /** @class */ (function () {
        function DialogEvent() {
        }
        return DialogEvent;
    }());
    ModalDialogComponentNs.DialogEvent = DialogEvent;
})(ModalDialogComponentNs || (ModalDialogComponentNs = {}));
angular.module('ntech.components').component('modalDialog', new ModalDialogComponentNs.ModalDialogComponent()).service('modalDialogService', ModalDialogComponentNs.ModalDialogService);
