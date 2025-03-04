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
var SavingsAccountDocumentsComponentNs;
(function (SavingsAccountDocumentsComponentNs) {
    var SavingsAccountDocumentsController = /** @class */ (function (_super) {
        __extends(SavingsAccountDocumentsController, _super);
        function SavingsAccountDocumentsController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        SavingsAccountDocumentsController.prototype.componentName = function () {
            return 'savingsAccountDocuments';
        };
        SavingsAccountDocumentsController.prototype.onChanges = function () {
            this.m = null;
            if (!this.initialData) {
                return;
            }
            if (this.initialData.documents) {
                this.m = {
                    documents: this.initialData.documents
                };
            }
        };
        SavingsAccountDocumentsController.prototype.getDocumentDisplayName = function (d) {
            if (d.DocumentType === 'YearlySummary') {
                return 'Annual summary for ' + d.DocumentData;
            }
            else {
                return d.DocumentType;
            }
        };
        SavingsAccountDocumentsController.$inject = ['$http', '$q', 'ntechComponentService'];
        return SavingsAccountDocumentsController;
    }(NTechComponents.NTechComponentControllerBase));
    SavingsAccountDocumentsComponentNs.SavingsAccountDocumentsController = SavingsAccountDocumentsController;
    var SavingsAccountDocumentsComponent = /** @class */ (function () {
        function SavingsAccountDocumentsComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = SavingsAccountDocumentsController;
            this.templateUrl = 'savings-account-documents.html';
        }
        return SavingsAccountDocumentsComponent;
    }());
    SavingsAccountDocumentsComponentNs.SavingsAccountDocumentsComponent = SavingsAccountDocumentsComponent;
    var DocumentModel = /** @class */ (function () {
        function DocumentModel() {
        }
        return DocumentModel;
    }());
    SavingsAccountDocumentsComponentNs.DocumentModel = DocumentModel;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    SavingsAccountDocumentsComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    SavingsAccountDocumentsComponentNs.Model = Model;
})(SavingsAccountDocumentsComponentNs || (SavingsAccountDocumentsComponentNs = {}));
angular.module('ntech.components').component('savingsAccountDocuments', new SavingsAccountDocumentsComponentNs.SavingsAccountDocumentsComponent());
