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
var CampaignsComponentNs;
(function (CampaignsComponentNs) {
    var CampaignsController = /** @class */ (function (_super) {
        __extends(CampaignsController, _super);
        function CampaignsController($http, $q, ntechComponentService, $scope) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.$scope = $scope;
            return _this;
        }
        CampaignsController.prototype.componentName = function () {
            return 'campaigns';
        };
        CampaignsController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.apiClient.fetchCampaigns({}).then(function (x) {
                _this.m = {
                    Campaigns: x.Campaigns,
                    HeaderData: {
                        backTarget: null,
                        backContext: null,
                        host: _this.initialData
                    }
                };
            });
        };
        CampaignsController.prototype.addCampaign = function (name, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.createCampaignReturningId(name).then(function (x) {
                _this.onChanges();
            });
        };
        CampaignsController.$inject = ['$http', '$q', 'ntechComponentService', '$scope'];
        return CampaignsController;
    }(NTechComponents.NTechComponentControllerBase));
    CampaignsComponentNs.CampaignsController = CampaignsController;
    var CampaignsComponent = /** @class */ (function () {
        function CampaignsComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CampaignsController;
            this.template = "<div>\n            <page-header initial-data=\"$ctrl.m.HeaderData\" title-text=\"'Campaigns'\"></page-header>\n\n            <toggle-block header-text=\"'Add new campaign'\" class=\"pb-3\">\n                <div class=\"row\">\n                    <div class=\"col-xs-6\">\n                        <div class=\"editblock\">\n                            <form name=\"addform\">\n                                <div class=\"form-horizontal\">\n                                    <div class=\"form-group\">\n                                        <label class=\"control-label col-xs-6\">Campaign name</label>\n                                        <div class=\"col-xs-4\"><input type=\"text\" ng-model=\"name\" required class=\"form-control\"></div>\n                                    </div>\n                                    <div class=\"text-center pt-2\"><button ng-disabled=\"addform.$invalid\" ng-click=\"$ctrl.addCampaign(name, $event)\" class=\"n-direct-btn n-green-btn\">Add</button></div>\n                                </div>\n                            </form>\n                        </div>\n                    </div>\n                </div>\n            </toggle-block>\n\n            <div class=\"pt-3\">\n                <h2 class=\"custom-header\">\n                    Campaigns\n                </h2>\n                <hr class=\"hr-section\">\n                <div>\n                    <table class=\"table\">\n                        <thead>\n                            <tr>\n                                <th class=\"col-xs-2\">Name</th>\n                                <th class=\"col-xs-6\">Creation date</th>\n                                <th class=\"col-xs-2\">Application count</th>\n                                <th class=\"text-right col-xs-4\"></th>\n                            </tr>\n                        </thead>\n                        <tbody>\n                            <tr ng-repeat=\"c in $ctrl.m.Campaigns\">\n                                <td>{{c.Name}}</td>\n                                <td>{{c.CreatedDate | date:'short'}}</td>\n                                <td>{{c.AppliedToApplicationCount}}</td>\n                                <td class=\"text-right\"><a ng-href=\"{{'/Ui/Campaign?campaignId=' + c.Id}}\" class=\"n-anchor\">View details</a></td>\n                            </tr>\n                        </tbody>\n                    </table>\n                </div>\n            </div>\n        </div>";
        }
        return CampaignsComponent;
    }());
    CampaignsComponentNs.CampaignsComponent = CampaignsComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    CampaignsComponentNs.Model = Model;
})(CampaignsComponentNs || (CampaignsComponentNs = {}));
angular.module('ntech.components').component('campaigns', new CampaignsComponentNs.CampaignsComponent());
