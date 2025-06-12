namespace CampaignsComponentNs {
    export class CampaignsController extends NTechComponents.NTechComponentControllerBase {
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
            return 'campaigns'
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            this.apiClient.fetchCampaigns({}).then(x => {
                  this.m = {
                    Campaigns: x.Campaigns,
                    HeaderData: {
                        backTarget: null,
                        backContext: null,
                        host: this.initialData
                    }
                }
            })
        }

        addCampaign(name: string, evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }

            this.apiClient.createCampaignReturningId(name).then(x => {
                this.onChanges()
            })
        }
    }

    export class CampaignsComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CampaignsController;

            this.template = `<div>
            <page-header initial-data="$ctrl.m.HeaderData" title-text="'Campaigns'"></page-header>

            <toggle-block header-text="'Add new campaign'" class="pb-3">
                <div class="row">
                    <div class="col-xs-6">
                        <div class="editblock">
                            <form name="addform">
                                <div class="form-horizontal">
                                    <div class="form-group">
                                        <label class="control-label col-xs-6">Campaign name</label>
                                        <div class="col-xs-4"><input type="text" ng-model="name" required class="form-control"></div>
                                    </div>
                                    <div class="text-center pt-2"><button ng-disabled="addform.$invalid" ng-click="$ctrl.addCampaign(name, $event)" class="n-direct-btn n-green-btn">Add</button></div>
                                </div>
                            </form>
                        </div>
                    </div>
                </div>
            </toggle-block>

            <div class="pt-3">
                <h2 class="custom-header">
                    Campaigns
                </h2>
                <hr class="hr-section">
                <div>
                    <table class="table">
                        <thead>
                            <tr>
                                <th class="col-xs-2">Name</th>
                                <th class="col-xs-6">Creation date</th>
                                <th class="col-xs-2">Application count</th>
                                <th class="text-right col-xs-4"></th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr ng-repeat="c in $ctrl.m.Campaigns">
                                <td>{{c.Name}}</td>
                                <td>{{c.CreatedDate | date:'short'}}</td>
                                <td>{{c.AppliedToApplicationCount}}</td>
                                <td class="text-right"><a ng-href="{{'/Ui/Campaign?campaignId=' + c.Id}}" class="n-anchor">View details</a></td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>`
        }
    }
    export class Model {
        HeaderData : PageHeaderComponentNs.InitialData
        Campaigns: NTechPreCreditApi.CampaignModel[]
    }

    export interface LocalInitialData {
    }

    export interface InitialData extends LocalInitialData, ComponentHostNs.ComponentHostInitialData {
    }
}

angular.module('ntech.components').component('campaigns', new CampaignsComponentNs.CampaignsComponent())