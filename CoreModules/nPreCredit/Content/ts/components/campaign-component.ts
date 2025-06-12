namespace CampaignComponentNs {
    export class CampaignController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', '$scope']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private $scope: ng.IScope) {
            super(ntechComponentService, $http, $q);            
        }

        componentName(): string {
            return 'campaign'
        }

        onChanges() {
            this.m = null

            if (!this.initialData || !this.initialData.campaignId) {
                return
            }

            this.apiClient.fetchCampaign(this.initialData.campaignId).then(campagin => {
                if (!campagin) {
                    return
                }
                this.m = {
                    Campaign: campagin,
                    HeaderData: {
                        backTarget: NavigationTargetHelper.createCrossModule('Campaigns', {  }),
                        backContext: null,
                        host: this.initialData
                    }
                }
            })
        }

        isActive(): boolean {
            return this.m && this.m.Campaign && this.m.Campaign.IsActive && !this.m.Campaign.IsDeleted
        }

        inactivateCampaign(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.deleteOrInactivateCampaign(this.m.Campaign.Id, false).then(x => {
                location.href = '/Ui/Campaigns'
            })
        }

        deleteCampaign(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.deleteOrInactivateCampaign(this.m.Campaign.Id, true).then(x => {
                location.href = '/Ui/Campaigns'
            })
        }

        deleteCode(code: NTechPreCreditApi.CampaignCodeModel, evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.deleteCampaignCode(code.Id).then(x => {
                this.onChanges()
            })
        }

        createCode(code: string,
                startDate: string,
                endDate: string,
                commentText: string,
                isGoogleCampaign: boolean,
                evt: Event) {
            
            if (evt) {
                evt.preventDefault()
            }
            
            this.apiClient.createCampaignCode(this.initialData.campaignId, code, startDate, endDate, commentText, isGoogleCampaign).then(x => {
                this.onChanges()
            })
        }
    }

    export class CampaignComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CampaignController;
            this.template = `<div ng-if="$ctrl.m">
            <page-header initial-data="$ctrl.m.HeaderData" title-text="'Campaign: ' + $ctrl.m.Campaign.Name"></page-header>
            
            <div class="row pt-2">
                <div class="col-xs-3">
                    <span class="copyable">{{$ctrl.m.Campaign.Id}}</span>
                </div>
                <div class="col-xs-9 text-right" ng-if="$ctrl.isActive()">
                    <button class="n-direct-btn n-white-btn" ng-if="$ctrl.m.Campaign.AppliedToApplicationCount > 0" ng-click="$ctrl.inactivateCampaign($event)">Inactivate campaign</button>
                    <button class="n-direct-btn n-red-btn" ng-if="$ctrl.m.Campaign.AppliedToApplicationCount === 0" ng-click="$ctrl.deleteCampaign($event)">Delete campaign</button>
                </div>
                <div class="col-xs-9 text-right" ng-if="!$ctrl.isActive()">
                    <div ng-if="$ctrl.m.Campaign.IsDeleted">Deleted</div>
                    <div ng-if="!$ctrl.m.Campaign.IsDeleted">Inactive</div>
                </div>
            </div>

            <div class="row pt-2">
                <div class="col-xs-3">
                    
                </div>
                <div class="col-xs-9">
                    <toggle-block header-text="'Add new code'" class="pb-3" ng-if="$ctrl.isActive()">
                        <div class="row">
                            <div class="col-xs-10">
                                <div class="editblock">
                                    <div class="form-horizontal">
                                        <form name="addCodeForm">
                                            <div class="form-group">
                                                <label class="control-label col-xs-6">Code</label>
                                                <div class="col-xs-4"><input required ng-model="code" type="text" class="form-control"></div>
                                            </div>
                                            <div class="form-group">
                                                <label class="control-label col-xs-6">Start date</label>
                                                <div class="col-xs-4"><input ng-model="startDate" custom-validate="$ctrl.isValidDate" type="text" class="form-control"></div>
                                            </div>
                                            <div class="form-group">
                                                <label class="control-label col-xs-6">End date</label>
                                                <div class="col-xs-4"><input ng-model="endDate" custom-validate="$ctrl.isValidDate" type="text" class="form-control"></div>
                                            </div>
                                            <div class="form-group">
                                                <label class="control-label col-xs-6">Comment</label>
                                                <div class="col-xs-4"><input type="text" ng-model="commentText" class="form-control"></div>
                                            </div>
                                            <div class="form-group">
                                                <label class="control-label col-xs-6">Google Ad</label>
                                                <div class="col-xs-4">
                                                    <div class="checkbox">
                                                        <input ng-model="isGoogleCampaign" type="checkbox">
                                                    </div>
                                                </div>
                                            </div>
                                            <div class="text-center pt-2"><button ng-disabled="addCodeForm.$invalid" ng-click="$ctrl.createCode(code, startDate, endDate, commentText, isGoogleCampaign, $event)" class="n-direct-btn n-green-btn">Add</button></div>
                                        </form>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </toggle-block>

                    <div class="pt-3">
                        <h2 class="custom-header">
                            Codes
                        </h2>
                        <hr class="hr-section">
                        <div>
                            <table class="table">
                                <thead>
                                    <tr>
                                        <th class="col-xs-2">Code</th>
                                        <th class="col-xs-2">Start date</th>
                                        <th class="col-xs-2">End date</th>
                                        <th class="col-xs-2">Comment</th>
                                        <th class="text-right col-xs-2"></th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr ng-repeat="code in $ctrl.m.Campaign.Codes">
                                        <td>{{code.Code}} <span ng-if="code.IsGoogleCampaign" style="font-size:smaller">(google)</span> </td>
                                        <td>{{code.StartDate | date:'shortDate'}}</td>
                                        <td>{{code.EndDate | date:'shortDate'}}</td>
                                        <td>{{code.CommentText}}</td>
                                        <td class="text-right"><button ng-if="$ctrl.isActive()" ng-click="$ctrl.deleteCode(code, $event)" class="n-direct-btn n-red-btn">Delete code</button></td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
            
        </div>`
        }
    }
    export class Model {
        Campaign: NTechPreCreditApi.CampaignModel
        HeaderData : PageHeaderComponentNs.InitialData
    }

    export interface LocalInitialData {
        campaignId: string
    }

    export interface InitialData extends LocalInitialData, ComponentHostNs.ComponentHostInitialData {
    }
}

angular.module('ntech.components').component('campaign', new CampaignComponentNs.CampaignComponent())