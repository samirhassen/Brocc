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
var CampaignComponentNs;
(function (CampaignComponentNs) {
    var CampaignController = /** @class */ (function (_super) {
        __extends(CampaignController, _super);
        function CampaignController($http, $q, ntechComponentService, $scope) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.$scope = $scope;
            return _this;
        }
        CampaignController.prototype.componentName = function () {
            return 'campaign';
        };
        CampaignController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData || !this.initialData.campaignId) {
                return;
            }
            this.apiClient.fetchCampaign(this.initialData.campaignId).then(function (campagin) {
                if (!campagin) {
                    return;
                }
                _this.m = {
                    Campaign: campagin,
                    HeaderData: {
                        backTarget: NavigationTargetHelper.createCrossModule('Campaigns', {}),
                        backContext: null,
                        host: _this.initialData
                    }
                };
            });
        };
        CampaignController.prototype.isActive = function () {
            return this.m && this.m.Campaign && this.m.Campaign.IsActive && !this.m.Campaign.IsDeleted;
        };
        CampaignController.prototype.inactivateCampaign = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.deleteOrInactivateCampaign(this.m.Campaign.Id, false).then(function (x) {
                location.href = '/Ui/Campaigns';
            });
        };
        CampaignController.prototype.deleteCampaign = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.deleteOrInactivateCampaign(this.m.Campaign.Id, true).then(function (x) {
                location.href = '/Ui/Campaigns';
            });
        };
        CampaignController.prototype.deleteCode = function (code, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.deleteCampaignCode(code.Id).then(function (x) {
                _this.onChanges();
            });
        };
        CampaignController.prototype.createCode = function (code, startDate, endDate, commentText, isGoogleCampaign, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.createCampaignCode(this.initialData.campaignId, code, startDate, endDate, commentText, isGoogleCampaign).then(function (x) {
                _this.onChanges();
            });
        };
        CampaignController.$inject = ['$http', '$q', 'ntechComponentService', '$scope'];
        return CampaignController;
    }(NTechComponents.NTechComponentControllerBase));
    CampaignComponentNs.CampaignController = CampaignController;
    var CampaignComponent = /** @class */ (function () {
        function CampaignComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CampaignController;
            this.template = "<div ng-if=\"$ctrl.m\">\n            <page-header initial-data=\"$ctrl.m.HeaderData\" title-text=\"'Campaign: ' + $ctrl.m.Campaign.Name\"></page-header>\n            \n            <div class=\"row pt-2\">\n                <div class=\"col-xs-3\">\n                    <span class=\"copyable\">{{$ctrl.m.Campaign.Id}}</span>\n                </div>\n                <div class=\"col-xs-9 text-right\" ng-if=\"$ctrl.isActive()\">\n                    <button class=\"n-direct-btn n-white-btn\" ng-if=\"$ctrl.m.Campaign.AppliedToApplicationCount > 0\" ng-click=\"$ctrl.inactivateCampaign($event)\">Inactivate campaign</button>\n                    <button class=\"n-direct-btn n-red-btn\" ng-if=\"$ctrl.m.Campaign.AppliedToApplicationCount === 0\" ng-click=\"$ctrl.deleteCampaign($event)\">Delete campaign</button>\n                </div>\n                <div class=\"col-xs-9 text-right\" ng-if=\"!$ctrl.isActive()\">\n                    <div ng-if=\"$ctrl.m.Campaign.IsDeleted\">Deleted</div>\n                    <div ng-if=\"!$ctrl.m.Campaign.IsDeleted\">Inactive</div>\n                </div>\n            </div>\n\n            <div class=\"row pt-2\">\n                <div class=\"col-xs-3\">\n                    \n                </div>\n                <div class=\"col-xs-9\">\n                    <toggle-block header-text=\"'Add new code'\" class=\"pb-3\" ng-if=\"$ctrl.isActive()\">\n                        <div class=\"row\">\n                            <div class=\"col-xs-10\">\n                                <div class=\"editblock\">\n                                    <div class=\"form-horizontal\">\n                                        <form name=\"addCodeForm\">\n                                            <div class=\"form-group\">\n                                                <label class=\"control-label col-xs-6\">Code</label>\n                                                <div class=\"col-xs-4\"><input required ng-model=\"code\" type=\"text\" class=\"form-control\"></div>\n                                            </div>\n                                            <div class=\"form-group\">\n                                                <label class=\"control-label col-xs-6\">Start date</label>\n                                                <div class=\"col-xs-4\"><input ng-model=\"startDate\" custom-validate=\"$ctrl.isValidDate\" type=\"text\" class=\"form-control\"></div>\n                                            </div>\n                                            <div class=\"form-group\">\n                                                <label class=\"control-label col-xs-6\">End date</label>\n                                                <div class=\"col-xs-4\"><input ng-model=\"endDate\" custom-validate=\"$ctrl.isValidDate\" type=\"text\" class=\"form-control\"></div>\n                                            </div>\n                                            <div class=\"form-group\">\n                                                <label class=\"control-label col-xs-6\">Comment</label>\n                                                <div class=\"col-xs-4\"><input type=\"text\" ng-model=\"commentText\" class=\"form-control\"></div>\n                                            </div>\n                                            <div class=\"form-group\">\n                                                <label class=\"control-label col-xs-6\">Google Ad</label>\n                                                <div class=\"col-xs-4\">\n                                                    <div class=\"checkbox\">\n                                                        <input ng-model=\"isGoogleCampaign\" type=\"checkbox\">\n                                                    </div>\n                                                </div>\n                                            </div>\n                                            <div class=\"text-center pt-2\"><button ng-disabled=\"addCodeForm.$invalid\" ng-click=\"$ctrl.createCode(code, startDate, endDate, commentText, isGoogleCampaign, $event)\" class=\"n-direct-btn n-green-btn\">Add</button></div>\n                                        </form>\n                                    </div>\n                                </div>\n                            </div>\n                        </div>\n                    </toggle-block>\n\n                    <div class=\"pt-3\">\n                        <h2 class=\"custom-header\">\n                            Codes\n                        </h2>\n                        <hr class=\"hr-section\">\n                        <div>\n                            <table class=\"table\">\n                                <thead>\n                                    <tr>\n                                        <th class=\"col-xs-2\">Code</th>\n                                        <th class=\"col-xs-2\">Start date</th>\n                                        <th class=\"col-xs-2\">End date</th>\n                                        <th class=\"col-xs-2\">Comment</th>\n                                        <th class=\"text-right col-xs-2\"></th>\n                                    </tr>\n                                </thead>\n                                <tbody>\n                                    <tr ng-repeat=\"code in $ctrl.m.Campaign.Codes\">\n                                        <td>{{code.Code}} <span ng-if=\"code.IsGoogleCampaign\" style=\"font-size:smaller\">(google)</span> </td>\n                                        <td>{{code.StartDate | date:'shortDate'}}</td>\n                                        <td>{{code.EndDate | date:'shortDate'}}</td>\n                                        <td>{{code.CommentText}}</td>\n                                        <td class=\"text-right\"><button ng-if=\"$ctrl.isActive()\" ng-click=\"$ctrl.deleteCode(code, $event)\" class=\"n-direct-btn n-red-btn\">Delete code</button></td>\n                                    </tr>\n                                </tbody>\n                            </table>\n                        </div>\n                    </div>\n                </div>\n            </div>\n            \n        </div>";
        }
        return CampaignComponent;
    }());
    CampaignComponentNs.CampaignComponent = CampaignComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    CampaignComponentNs.Model = Model;
})(CampaignComponentNs || (CampaignComponentNs = {}));
angular.module('ntech.components').component('campaign', new CampaignComponentNs.CampaignComponent());
