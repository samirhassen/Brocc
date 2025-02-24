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
var UnsecuredApplicationAdditionalQuestionsComponentNs;
(function (UnsecuredApplicationAdditionalQuestionsComponentNs) {
    var UnsecuredApplicationAdditionalQuestionsController = /** @class */ (function (_super) {
        __extends(UnsecuredApplicationAdditionalQuestionsController, _super);
        function UnsecuredApplicationAdditionalQuestionsController($http, $q, $scope, ntechComponentService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$http = $http;
            _this.ntechComponentService = ntechComponentService;
            _this.agreementSigned1FileUpload = new NtechAngularFileUpload.FileUploadHelper(document.getElementById('signedagreementfile1'), document.getElementById('signedagreementfileform1'), $scope, $q);
            _this.agreementSigned2FileUpload = new NtechAngularFileUpload.FileUploadHelper(document.getElementById('signedagreementfile2'), document.getElementById('signedagreementfileform2'), $scope, $q);
            var _loop_1 = function (applicantNr) {
                this_1.agreementSignedFileUpload(applicantNr).addFileAttachedListener(function (filenames) {
                    if (!_this.m) {
                        return;
                    }
                    if (filenames.length == 0) {
                        _this.m.agreementSigningStatus['applicant' + applicantNr].attachedFileName = null;
                    }
                    else if (filenames.length == 1) {
                        _this.m.agreementSigningStatus['applicant' + applicantNr].attachedFileName = filenames[0];
                    }
                    else {
                        _this.m.agreementSigningStatus['applicant' + applicantNr].attachedFileName = 'Error - multiple files selected!';
                    }
                });
            };
            var this_1 = this;
            for (var _i = 0, _a = [1, 2]; _i < _a.length; _i++) {
                var applicantNr = _a[_i];
                _loop_1(applicantNr);
            }
            return _this;
        }
        UnsecuredApplicationAdditionalQuestionsController.prototype.componentName = function () {
            return 'unsecuredApplicationAdditionalQuestions';
        };
        UnsecuredApplicationAdditionalQuestionsController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.apiClient.fetchProviderInfo(this.initialData.applicationInfo.ProviderName).then(function (providerInfo) {
                _this.apiClient.fetchUnsecuredLoanAdditionalQuestionsStatus(_this.initialData.applicationInfo.ApplicationNr).then(function (x) {
                    _this.m = {
                        provider: providerInfo,
                        agreementSigningStatus: x.AgreementSigningStatus,
                        additionalQuestionsStatus: x.AdditionalQuestionsStatus,
                        showMoreAgreementSigningOptions: false,
                        userDirectLinkUrl: null,
                        consentAnswers: null
                    };
                });
            });
        };
        UnsecuredApplicationAdditionalQuestionsController.prototype.agreementSignedFileUpload = function (applicantNr) {
            if (applicantNr === 1) {
                return this.agreementSigned1FileUpload;
            }
            else if (applicantNr === 2) {
                return this.agreementSigned2FileUpload;
            }
            else {
                return null;
            }
        };
        UnsecuredApplicationAdditionalQuestionsController.prototype.headerClassFromStatus = function (status) {
            var isAccepted = status === 'Accepted';
            var isRejected = status === 'Rejected';
            return { 'text-success': isAccepted, 'text-danger': isRejected };
        };
        UnsecuredApplicationAdditionalQuestionsController.prototype.iconClassFromStatus = function (status) {
            var isAccepted = status === 'Accepted';
            var isRejected = status === 'Rejected';
            var isOther = !isAccepted && !isRejected;
            return { 'glyphicon-ok': isAccepted, 'glyphicon-remove': isRejected, 'glyphicon-minus': isOther, 'glyphicon': true, 'text-success': isAccepted, 'text-danger': isRejected };
        };
        UnsecuredApplicationAdditionalQuestionsController.prototype.selectAttachedSignedAgreement = function (applicantNr, evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.agreementSignedFileUpload(applicantNr).showFilePicker();
        };
        UnsecuredApplicationAdditionalQuestionsController.prototype.acceptAttachedSignedAgreement = function (applicantNr, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.agreementSignedFileUpload(applicantNr).loadSingleAttachedFileAsDataUrl().then(function (result) {
                _this.$http({
                    method: 'POST',
                    url: '/CreditManagement/AddSignedAgreementDocument',
                    data: {
                        applicationNr: _this.initialData.applicationInfo.ApplicationNr,
                        applicantNr: applicantNr,
                        attachedFileAsDataUrl: result.dataUrl,
                        attachedFileName: result.filename
                    }
                }).then(function (response) {
                    _this.m.agreementSigningStatus['applicant' + applicantNr].attachedFileName = null;
                    var updatedAgreementSigningStatus = response.data.updatedAgreementSigningStatus;
                    if (response.data.wasAgreementStatusChanged || response.data.wasCustomerCheckStatusChanged) {
                        //Could be made cleaner by updating all the sections that changed
                        location.reload();
                    }
                    else if (updatedAgreementSigningStatus != null) {
                        _this.m.agreementSigningStatus['applicant' + applicantNr] = updatedAgreementSigningStatus['applicant' + applicantNr];
                    }
                }, function (response) {
                    toastr.error(response.statusText, "Error");
                });
            }, function (err) {
                toastr.warning(err);
            });
        };
        UnsecuredApplicationAdditionalQuestionsController.prototype.cancelAttachedSignedAgreement = function (applicantNr, evt) {
            if (evt) {
                evt.preventDefault();
            }
            var s = this.m.agreementSigningStatus['applicant' + applicantNr];
            if (s) {
                s.attachedFileName = null;
            }
            this.agreementSignedFileUpload(applicantNr).reset();
        };
        UnsecuredApplicationAdditionalQuestionsController.prototype.showDirectLink = function (evt) {
            var _this = this;
            this.$http({
                method: 'POST',
                url: '/CreditManagement/GetOrCreateApplicationWrapperLink',
                data: { applicationNr: initialData.ApplicationNr }
            }).then(function (response) {
                _this.m.userDirectLinkUrl = response.data.wrapperLink;
                $('#userDirectLinkDialog').modal('show');
            }, function (response) {
            });
        };
        UnsecuredApplicationAdditionalQuestionsController.prototype.openConsentAnswersDialog = function () {
            var _this = this;
            this.apiClient.fetchConsentAnswers(this.initialData.applicationInfo.ApplicationNr).then(function (x) {
                if (x.ConsentItems.length < 1) {
                    _this.m.consentAnswers = "No consent items.";
                    $('#consentAnswersDialog').modal('show');
                    return;
                }
                var objArr = [];
                x.ConsentItems.forEach(function (x) {
                    var _a;
                    objArr.push((_a = {},
                        _a[x.GroupName] = JSON.parse(x.Item),
                        _a));
                });
                _this.m.consentAnswers = JSON.stringify(objArr, null, 2);
                $('#consentAnswersDialog').modal('show');
            });
        };
        UnsecuredApplicationAdditionalQuestionsController.prototype.canResetSigning = function () {
            if (!this.m || !this.m.agreementSigningStatus || !this.m.agreementSigningStatus.applicant1) {
                return false;
            }
            if (!this.initialData.applicationInfo.IsActive) {
                return false;
            }
            var s = this.m.agreementSigningStatus;
            var statuses = [s.applicant1.status];
            if (s.applicant2) {
                statuses.push(s.applicant2.status);
            }
            for (var _i = 0, statuses_1 = statuses; _i < statuses_1.length; _i++) {
                var status_1 = statuses_1[_i];
                if (status_1 !== 'Success' && status_1 !== 'NotSent') {
                    return true;
                }
            }
            return false;
        };
        UnsecuredApplicationAdditionalQuestionsController.prototype.resetSigning = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var applicationNr = this.initialData.applicationInfo.ApplicationNr;
            this.apiClient.cancelUnsecuredLoanApplicationSignatureSession(applicationNr).then(function (_) {
                _this.signalReloadRequired();
            });
        };
        // To avoid onclick as inline-script due to CSP. 
        UnsecuredApplicationAdditionalQuestionsController.prototype.focusAndSelect = function (evt) {
            evt.currentTarget.focus();
            evt.currentTarget.select();
        };
        UnsecuredApplicationAdditionalQuestionsController.$inject = ['$http', '$q', '$scope', 'ntechComponentService'];
        return UnsecuredApplicationAdditionalQuestionsController;
    }(NTechComponents.NTechComponentControllerBase));
    UnsecuredApplicationAdditionalQuestionsComponentNs.UnsecuredApplicationAdditionalQuestionsController = UnsecuredApplicationAdditionalQuestionsController;
    var UnsecuredApplicationAdditionalQuestionsComponent = /** @class */ (function () {
        function UnsecuredApplicationAdditionalQuestionsComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = UnsecuredApplicationAdditionalQuestionsController;
            this.templateUrl = 'unsecured-application-additional-questions.html';
        }
        return UnsecuredApplicationAdditionalQuestionsComponent;
    }());
    UnsecuredApplicationAdditionalQuestionsComponentNs.UnsecuredApplicationAdditionalQuestionsComponent = UnsecuredApplicationAdditionalQuestionsComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    UnsecuredApplicationAdditionalQuestionsComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    UnsecuredApplicationAdditionalQuestionsComponentNs.Model = Model;
})(UnsecuredApplicationAdditionalQuestionsComponentNs || (UnsecuredApplicationAdditionalQuestionsComponentNs = {}));
angular.module('ntech.components').component('unsecuredApplicationAdditionalQuestions', new UnsecuredApplicationAdditionalQuestionsComponentNs.UnsecuredApplicationAdditionalQuestionsComponent());
