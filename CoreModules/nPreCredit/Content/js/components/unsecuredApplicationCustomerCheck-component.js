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
var UnsecuredApplicationCustomerCheckComponentNs;
(function (UnsecuredApplicationCustomerCheckComponentNs) {
    var UnsecuredApplicationCustomerCheckController = /** @class */ (function (_super) {
        __extends(UnsecuredApplicationCustomerCheckController, _super);
        function UnsecuredApplicationCustomerCheckController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        UnsecuredApplicationCustomerCheckController.prototype.componentName = function () {
            return 'unsecuredApplicationCustomerCheck';
        };
        UnsecuredApplicationCustomerCheckController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            var crossModuleNavigationTargetToHere = NavigationTargetHelper.createCrossModule('UnsecuredLoanApplication', { applicationNr: this.initialData.applicationNr });
            this.apiClient.fetchApplicationInfoWithApplicants(this.initialData.applicationNr).then(function (x) {
                var m = {
                    IsKycScreenAllowed: x.Info.IsActive,
                    Applicants: []
                };
                for (var applicantNr = 1; applicantNr <= x.Info.NrOfApplicants; applicantNr++) {
                    m.Applicants.push({
                        ApplicantNr: applicantNr,
                        CustomerId: x.CustomerIdByApplicantNr[applicantNr],
                        PepKyc: null,
                        Fatca: null,
                        Name: null,
                        Email: null,
                        Address: null,
                        PepSanctionState: null
                    });
                    _this.m = m;
                }
                var _loop_1 = function (a) {
                    _this.apiClient.fetchCustomerKycScreenStatus(a.CustomerId).then(function (x) {
                        a.PepKyc = {
                            LatestScreeningDate: x.LatestScreeningDate
                        };
                    });
                    _this.apiClient.fetchCustomerComponentInitialData(_this.initialData.applicationNr, a.ApplicantNr, crossModuleNavigationTargetToHere.targetCode).then(function (x) {
                        a.Fatca = {
                            IncludeInFatcaExport: x.includeInFatcaExport,
                            CustomerFatcaCrsUrl: x.customerFatcaCrsUrl
                        };
                        a.Name = {
                            IsMissingName: !x.firstName,
                            CustomerCardUrl: x.customerCardUrl
                        };
                        a.Address = {
                            IsMissingAddress: x.isMissingAddress,
                            CustomerCardUrl: x.customerCardUrl
                        };
                        a.Email = {
                            IsMissingEmail: x.isMissingEmail,
                            CustomerCardUrl: x.customerCardUrl
                        };
                        a.PepSanctionState = {
                            PepKycCustomerUrl: x.pepKycCustomerUrl,
                            IsAccepted: (x.localIsPep === true || x.localIsPep === false) && x.localIsSanction === false,
                            IsRejected: x.localIsSanction === true
                        };
                    });
                };
                for (var _i = 0, _a = m.Applicants; _i < _a.length; _i++) {
                    var a = _a[_i];
                    _loop_1(a);
                }
            });
        };
        UnsecuredApplicationCustomerCheckController.prototype.iconClass = function (isAccepted, isRejected) {
            if (isAccepted) {
                return 'glyphicon glyphicon-ok text-success custom-glyph';
            }
            else if (isRejected) {
                return 'glyphicon glyphicon-remove text-danger custom-glyph';
            }
            else {
                return 'glyphicon glyphicon-minus custom-glyph';
            }
        };
        UnsecuredApplicationCustomerCheckController.prototype.iconClassEmailAddressName = function (a) {
            var isAccepted = true;
            if (a.Email && a.Email.IsMissingEmail) {
                isAccepted = false;
            }
            if (a.Address && a.Address.IsMissingAddress) {
                isAccepted = false;
            }
            if (a.Name && a.Name.IsMissingName) {
                isAccepted = false;
            }
            return this.iconClass(isAccepted, false);
        };
        UnsecuredApplicationCustomerCheckController.prototype.kycScreenNow = function (applicant, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.kycScreenCustomer(applicant.CustomerId, false).then(function (x) {
                if (!x.Success) {
                    toastr.warning('Screening failed: ' + x.FailureCode);
                }
                else if (x.Skipped) {
                    toastr.info('Customer has already been screened');
                }
                else {
                    _this.signalReloadRequired();
                }
            });
        };
        UnsecuredApplicationCustomerCheckController.$inject = ['$http', '$q', 'ntechComponentService'];
        return UnsecuredApplicationCustomerCheckController;
    }(NTechComponents.NTechComponentControllerBase));
    UnsecuredApplicationCustomerCheckComponentNs.UnsecuredApplicationCustomerCheckController = UnsecuredApplicationCustomerCheckController;
    var UnsecuredApplicationCustomerCheckComponent = /** @class */ (function () {
        function UnsecuredApplicationCustomerCheckComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = UnsecuredApplicationCustomerCheckController;
            this.templateUrl = 'unsecured-application-customer-check.html';
        }
        return UnsecuredApplicationCustomerCheckComponent;
    }());
    UnsecuredApplicationCustomerCheckComponentNs.UnsecuredApplicationCustomerCheckComponent = UnsecuredApplicationCustomerCheckComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    UnsecuredApplicationCustomerCheckComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    UnsecuredApplicationCustomerCheckComponentNs.Model = Model;
    var ApplicantModel = /** @class */ (function () {
        function ApplicantModel() {
        }
        return ApplicantModel;
    }());
    UnsecuredApplicationCustomerCheckComponentNs.ApplicantModel = ApplicantModel;
    var PepKycScreenApplicantModel = /** @class */ (function () {
        function PepKycScreenApplicantModel() {
        }
        return PepKycScreenApplicantModel;
    }());
    UnsecuredApplicationCustomerCheckComponentNs.PepKycScreenApplicantModel = PepKycScreenApplicantModel;
    var FatcaModel = /** @class */ (function () {
        function FatcaModel() {
        }
        return FatcaModel;
    }());
    UnsecuredApplicationCustomerCheckComponentNs.FatcaModel = FatcaModel;
    var NameModel = /** @class */ (function () {
        function NameModel() {
        }
        return NameModel;
    }());
    UnsecuredApplicationCustomerCheckComponentNs.NameModel = NameModel;
    var AddressModel = /** @class */ (function () {
        function AddressModel() {
        }
        return AddressModel;
    }());
    UnsecuredApplicationCustomerCheckComponentNs.AddressModel = AddressModel;
    var EmailModel = /** @class */ (function () {
        function EmailModel() {
        }
        return EmailModel;
    }());
    UnsecuredApplicationCustomerCheckComponentNs.EmailModel = EmailModel;
})(UnsecuredApplicationCustomerCheckComponentNs || (UnsecuredApplicationCustomerCheckComponentNs = {}));
angular.module('ntech.components').component('unsecuredApplicationCustomerCheck', new UnsecuredApplicationCustomerCheckComponentNs.UnsecuredApplicationCustomerCheckComponent());
