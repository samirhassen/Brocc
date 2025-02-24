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
var MortgageApplicationDocumentsComponentNs;
(function (MortgageApplicationDocumentsComponentNs) {
    var MortgageApplicationDocumentsController = /** @class */ (function (_super) {
        __extends(MortgageApplicationDocumentsController, _super);
        function MortgageApplicationDocumentsController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        MortgageApplicationDocumentsController.prototype.componentName = function () {
            return 'mortgageApplicationDocuments';
        };
        MortgageApplicationDocumentsController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            var id = new ApplicationDocumentsComponentNs.InitialData(this.initialData.applicationInfo);
            id.onDocumentsAddedOrRemoved = function (x) { return _this.onDocumentsAddedOrRemoved(x); };
            var customData = this.initialData.workflowModel.getCustomStepData();
            for (var _i = 0, _a = customData.RequiredDocuments; _i < _a.length; _i++) {
                var d = _a[_i];
                if (d.Scope == 'ForAllApplicants') {
                    id.addDocumentForAllApplicants(d.DocumentType, d.Text);
                }
                else if (d.Scope == 'Shared') {
                    id.addSharedDocument(d.DocumentType, d.Text);
                }
                else if (d.Scope == 'ForSingleApplicant') {
                    id.addDocumentForSingleApplicant(d.DocumentType, d.Text, d.ApplicantNr);
                }
            }
            this.m = {
                documentCheckInitialData: id
            };
        };
        MortgageApplicationDocumentsController.prototype.onDocumentsAddedOrRemoved = function (areAllDocumentAdded) {
            var _this = this;
            //TODO: Find a way to move this serverside while allowing potentially multiple document steps on a single application
            //      so this can be completed when documents are added from other sources indirectly
            if (!this.initialData) {
                return;
            }
            var i = this.initialData;
            var w = i.workflowModel;
            var changeToStatus = null;
            if (areAllDocumentAdded && !w.isStatusAccepted(i.applicationInfo)) {
                changeToStatus = 'Accepted';
            }
            else if (!areAllDocumentAdded && w.isStatusAccepted(i.applicationInfo)) {
                changeToStatus = 'Initial';
            }
            if (changeToStatus) {
                this.apiClient.setMortgageApplicationWorkflowStatus(i.applicationInfo.ApplicationNr, w.currentStep.Name, changeToStatus).then(function (x) {
                    if (x.WasChanged) {
                        _this.signalReloadRequired();
                    }
                });
            }
        };
        MortgageApplicationDocumentsController.$inject = ['$http', '$q', 'ntechComponentService'];
        return MortgageApplicationDocumentsController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageApplicationDocumentsComponentNs.MortgageApplicationDocumentsController = MortgageApplicationDocumentsController;
    var MortgageApplicationDocumentsComponent = /** @class */ (function () {
        function MortgageApplicationDocumentsComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationDocumentsController;
            this.templateUrl = 'mortgage-application-documents.html';
        }
        return MortgageApplicationDocumentsComponent;
    }());
    MortgageApplicationDocumentsComponentNs.MortgageApplicationDocumentsComponent = MortgageApplicationDocumentsComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageApplicationDocumentsComponentNs.Model = Model;
})(MortgageApplicationDocumentsComponentNs || (MortgageApplicationDocumentsComponentNs = {}));
angular.module('ntech.components').component('mortgageApplicationDocuments', new MortgageApplicationDocumentsComponentNs.MortgageApplicationDocumentsComponent());
