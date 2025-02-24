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
var CompanyLoanAgreementComponentNs;
(function (CompanyLoanAgreementComponentNs) {
    var CompanyLoanAgreementController = /** @class */ (function (_super) {
        __extends(CompanyLoanAgreementController, _super);
        function CompanyLoanAgreementController($http, $q, ntechComponentService, modalDialogService, $scope) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.modalDialogService = modalDialogService;
            _this.$scope = $scope;
            return _this;
        }
        CompanyLoanAgreementController.prototype.componentName = function () {
            return 'companyLoanAgreement';
        };
        CompanyLoanAgreementController.prototype.onChanges = function () {
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.referesh();
        };
        CompanyLoanAgreementController.prototype.cancel = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var ai = this.initialData.applicationInfo;
            this.initialData.companyLoanApiClient.removeSignedAgreement(ai.ApplicationNr).then(function (x) {
                _this.signalReloadRequired();
            });
        };
        CompanyLoanAgreementController.prototype.isAttachAllowed = function () {
            var _this = this;
            if (!this.m) {
                return false;
            }
            //Since everyone signs the same copy we can use attach as long as there are any pending signatures.
            //This will then replace all previous signatures also.
            return NTechLinq.any(this.m.session.Static.Signers, function (s) { return _this.isResendAllowed(s); });
        };
        CompanyLoanAgreementController.prototype.isResendAllowed = function (signer) {
            return this.m && this.m.isEditAllowed === true && this.m.haveAllSigned() === false && !this.m.getSignedDate(signer);
        };
        CompanyLoanAgreementController.prototype.attachDocument = function (event) {
            var _this = this;
            if (event) {
                event.preventDefault();
            }
            var input = document.createElement('input');
            input.type = 'file';
            var form = document.createElement('form');
            form.appendChild(input);
            var ul = new NtechAngularFileUpload.FileUploadHelper(input, form, this.$scope, this.$q);
            ul.addFileAttachedListener(function (filesNames) {
                if (filesNames.length > 1) {
                    toastr.warning('More than one file attached');
                }
                else if (filesNames.length == 1) {
                    ul.loadSingleAttachedFileAsDataUrl().then(function (result) {
                        _this.initialData.companyLoanApiClient.attachSignedAgreement(_this.initialData.applicationInfo.ApplicationNr, result.dataUrl, result.filename).then(function (x) {
                            _this.signalReloadRequired();
                        });
                    });
                }
            });
            ul.showFilePicker();
        };
        CompanyLoanAgreementController.prototype.referesh = function (resendFor) {
            var _this = this;
            var ai = this.initialData.applicationInfo;
            var step = this.initialData.step;
            if (step.areAllStepBeforeThisAccepted(ai)) {
                var isEditAllowed_1 = ai.IsActive && !ai.IsPartiallyApproved && !ai.IsFinalDecisionMade && step.isStatusInitial(ai);
                var options = resendFor ? {
                    RefreshSignatureSessionIfNeeded: true,
                    ResendLinkOnExistingCustomerIds: [resendFor.CustomerId]
                } : {};
                var isCancelAllowed_1 = step.isStatusAccepted(ai) && step.areAllStepsAfterInitial(ai) && ai.IsActive;
                this.initialData.companyLoanApiClient.getOrCreateAgreementSignatureSession(ai.ApplicationNr, options).then(function (x) {
                    _this.m = new Model(isEditAllowed_1, x.Session, false, isCancelAllowed_1);
                });
            }
            else {
                this.m = new Model(false, null, true, false);
            }
        };
        CompanyLoanAgreementController.prototype.resend = function (s, evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.referesh(s);
        };
        CompanyLoanAgreementController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService', '$scope'];
        return CompanyLoanAgreementController;
    }(NTechComponents.NTechComponentControllerBase));
    CompanyLoanAgreementComponentNs.CompanyLoanAgreementController = CompanyLoanAgreementController;
    var CompanyLoanAgreementComponent = /** @class */ (function () {
        function CompanyLoanAgreementComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanAgreementController;
            this.templateUrl = 'company-loan-agreement.html';
        }
        return CompanyLoanAgreementComponent;
    }());
    CompanyLoanAgreementComponentNs.CompanyLoanAgreementComponent = CompanyLoanAgreementComponent;
    var Model = /** @class */ (function () {
        function Model(isEditAllowed, session, isWaitingForPreviousStep, isCancelAllowed) {
            this.isEditAllowed = isEditAllowed;
            this.session = session;
            this.isWaitingForPreviousStep = isWaitingForPreviousStep;
            this.isCancelAllowed = isCancelAllowed;
        }
        Model.prototype.getSentButNotSignedDate = function (s) {
            if (this.getSignedDate(s)) {
                return null;
            }
            if (!this.session) {
                return null;
            }
            var d = this.session.State.LatestSentDateByCustomerId;
            if (!d) {
                return null;
            }
            return d[s.CustomerId];
        };
        Model.prototype.getSignedDate = function (s) {
            if (!this.session) {
                return null;
            }
            var d = this.session.State.SignedDateByCustomerId;
            if (!d) {
                return null;
            }
            return d[s.CustomerId];
        };
        Model.prototype.haveAllSigned = function () {
            if (!this.session) {
                return null;
            }
            var d = this.session.State.SignedDateByCustomerId;
            if (!d) {
                return false;
            }
            for (var _i = 0, _a = this.session.Static.Signers; _i < _a.length; _i++) {
                var s = _a[_i];
                if (!d[s.CustomerId]) {
                    return false;
                }
            }
            return true;
        };
        return Model;
    }());
    CompanyLoanAgreementComponentNs.Model = Model;
})(CompanyLoanAgreementComponentNs || (CompanyLoanAgreementComponentNs = {}));
angular.module('ntech.components').component('companyLoanAgreement', new CompanyLoanAgreementComponentNs.CompanyLoanAgreementComponent());
