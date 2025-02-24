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
        function ModalDialogController($http, $q, ntechComponentService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.dialogId = NTechComponents.generateUniqueId(6);
            return _this;
        }
        ModalDialogController.prototype.componentName = function () {
            return 'modalDialog';
        };
        ModalDialogController.prototype.postLink = function () {
            var _this = this;
            //NOTE: Idea is to pick up when the user clicks like outside the area make sure to do a proper close instead
            $(function () {
                $("body " + '#' + _this.dialogId).on("hide.bs.modal", function (evt) {
                    if (_this.isOpen) {
                        evt.preventDefault();
                        _this.setIsOpen(false, null);
                    }
                });
            });
        };
        ModalDialogController.prototype.onDoCheck = function () {
            var element = $(document.getElementById(this.dialogId));
            var isActuallyVisible = element.hasClass('in');
            if (isActuallyVisible != this.isOpen) {
                element.modal(this.isOpen ? 'show' : 'hide');
            }
        };
        ModalDialogController.prototype.onChanges = function () {
            this.setIsOpen(this.isOpen, null);
        };
        ModalDialogController.prototype.setIsOpen = function (isOpen, evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.isOpen = isOpen;
            $('#' + this.dialogId).modal(isOpen ? 'show' : 'hide');
        };
        ModalDialogController.$inject = ['$http', '$q', 'ntechComponentService'];
        return ModalDialogController;
    }(NTechComponents.NTechComponentControllerBase));
    ModalDialogComponentNs.ModalDialogController = ModalDialogController;
    var ModalDialogComponent = /** @class */ (function () {
        function ModalDialogComponent() {
            this.transclude = true;
            this.bindings = {
                dialogTitle: '<',
                isOpen: '='
            };
            this.controller = ModalDialogController;
            this.templateUrl = 'modal-dialog.html';
        }
        return ModalDialogComponent;
    }());
    ModalDialogComponentNs.ModalDialogComponent = ModalDialogComponent;
})(ModalDialogComponentNs || (ModalDialogComponentNs = {}));
angular.module('ntech.components').component('modalDialog', new ModalDialogComponentNs.ModalDialogComponent());
//# sourceMappingURL=modalDialog-component.js.map