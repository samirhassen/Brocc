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
var AffiliateReportingLogComponentNs;
(function (AffiliateReportingLogComponentNs) {
    var AffiliateReportingLogController = /** @class */ (function (_super) {
        __extends(AffiliateReportingLogController, _super);
        function AffiliateReportingLogController($http, $q, ntechComponentService, modalDialogService, $timeout) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.modalDialogService = modalDialogService;
            _this.$timeout = $timeout;
            _this.dialogId = modalDialogService.generateDialogId();
            return _this;
        }
        AffiliateReportingLogController.prototype.componentName = function () {
            return 'affiliateReportingLog';
        };
        AffiliateReportingLogController.prototype.onChanges = function () {
            if (this.m != null) {
                this.modalDialogService.closeDialog(this.dialogId);
            }
            this.m = {
                hasIntegration: null
            };
        };
        AffiliateReportingLogController.prototype.refresh = function () {
            var _this = this;
            var parseJson = function (s) {
                try {
                    return JSON.parse(s);
                }
                catch (ex) {
                    return null;
                }
            };
            this.apiClient.fetchAllAffiliateReportingEventsForApplication(this.initialData.applicationNr, true).then(function (x) {
                _this.m.hasIntegration = x.AffiliateMetadata.HasDispatcher;
                if (_this.m.hasIntegration) {
                    _this.m.events = x.Events;
                    for (var _i = 0, _a = _this.m.events; _i < _a.length; _i++) {
                        var e = _a[_i];
                        e.EventDataJson = parseJson(e.EventData);
                        for (var _b = 0, _c = e.Items; _b < _c.length; _b++) {
                            var i = _c[_b];
                            i.OutgoingRequestBodyJson = parseJson(i.OutgoingRequestBody);
                            i.OutgoingResponseBodyJson = parseJson(i.OutgoingResponseBody);
                        }
                    }
                }
                else {
                    _this.m.events = null;
                }
                _this.modalDialogService.openDialog(_this.dialogId);
            });
        };
        AffiliateReportingLogController.prototype.showLog = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.refresh();
        };
        AffiliateReportingLogController.prototype.resendEvent = function (e, evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.resendAffiliateReportingEvent(e.Id).then(function () {
                e.ProcessedStatus = 'Pending';
                e.ProcessedDate = null;
            });
        };
        AffiliateReportingLogController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService', '$timeout'];
        return AffiliateReportingLogController;
    }(NTechComponents.NTechComponentControllerBase));
    AffiliateReportingLogComponentNs.AffiliateReportingLogController = AffiliateReportingLogController;
    var AffiliateReportingLogComponent = /** @class */ (function () {
        function AffiliateReportingLogComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = AffiliateReportingLogController;
            this.templateUrl = 'affiliate-reporting-log.html';
        }
        return AffiliateReportingLogComponent;
    }());
    AffiliateReportingLogComponentNs.AffiliateReportingLogComponent = AffiliateReportingLogComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    AffiliateReportingLogComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    AffiliateReportingLogComponentNs.Model = Model;
})(AffiliateReportingLogComponentNs || (AffiliateReportingLogComponentNs = {}));
angular.module('ntech.components').component('affiliateReportingLog', new AffiliateReportingLogComponentNs.AffiliateReportingLogComponent());
