namespace ApplicationAssignedHandlersComponentNs {
    export class ApplicationAssignedHandlersController extends NTechComponents.NTechComponentControllerBase {
        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        initialData : InitialData
        m: Model

        componentName(): string {
            return 'applicationAssignedHandlers'
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            this.apiClient.fetchApplicationAssignedHandlers({
                applicationNr: this.initialData.applicationNr,
                returnPossibleHandlers: true,
                returnAssignedHandlers: true
            }).then(x => {
                let m = new Model(false, x.PossibleHandlers)
                m.setAssignedHandlers(x.AssignedHandlers)
                this.m = m
            })
        }

        toggleExpanded(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }

            this.m.isExpanded = !this.m.isExpanded
        }

        removeAssignedHandler(h: NTechPreCreditApi.AssignedHandlerModel, evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.setApplicationAssignedHandlers(this.initialData.applicationNr, null, [h.UserId]).then(x => {
                this.m.setAssignedHandlers(x.AllAssignedHandlers)
            })
        }

        beginEdit(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.m.isAddUserMode = true
        }

        commitEdit(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }

            if (!this.m.selectedAddHandlerUserId) {
                return
            }

            this.apiClient.setApplicationAssignedHandlers(this.initialData.applicationNr, [parseInt(this.m.selectedAddHandlerUserId)], null).then(x => {
                this.m.setAssignedHandlers(x.AllAssignedHandlers)
                this.m.selectedAddHandlerUserId = null
                this.m.isAddUserMode = false
            })
        }

        cancelEdit(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.m.selectedAddHandlerUserId = null
            this.m.isAddUserMode = false
        }

    }

    export class ApplicationAssignedHandlersComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;        
        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ApplicationAssignedHandlersController;            
            this.template = `<div ng-if="$ctrl.m">
<div>
    <div class="row pt-1">
        <div class="col-xs-2">
            <span ng-click="$ctrl.toggleExpanded($event)" class="glyphicon" ng-class="{ 'chevron-bg glyphicon-chevron-down' : $ctrl.m.isExpanded, 'chevron-bg glyphicon-chevron-right' : !$ctrl.m.isExpanded }"></span>
        </div>
        <div class="col-xs-4 text-right">
            <span>Assigned handler</span>
        </div>
        <div class="col-xs-6">
            <span><b>{{ $ctrl.m.firstAssignedHandler ? $ctrl.m.firstAssignedHandler.UserDisplayName : 'None' }}</b></span>
        </div>
    </div>
    <div ng-if="$ctrl.m.isExpanded">
        <hr class="hr-section dotted">
        <div>
            <div class="form-horizontal">
                <div class="form-group pt-1" ng-repeat="h in $ctrl.m.assignedHandlers">
                    <div>
                        <label class="control-label col-xs-9">{{h.UserDisplayName}}</label>
                        <div class="col-xs-3">
                            <span style="float:right"><button ng-click="$ctrl.removeAssignedHandler(h, $event)" class="n-icon-btn n-red-btn"><span class="glyphicon glyphicon-remove"></span></button></span>                            
                        </div>
                    </div>
                </div>
                <div class="form-group" ng-if="!$ctrl.m.isAddUserMode">
                    <div class="pt-2">
                        <div class="col-xs-9"><a ng-click="$ctrl.beginEdit($event)" class="n-icon-btn n-blue-btn pull-right"><span class="glyphicon glyphicon-plus"></span></a></div>
                    </div>
                </div>
                <div class="form-group" ng-if="$ctrl.m.isAddUserMode">
                    <div class="pt-2">
                        <label class="col-xs-9">
                            <select class="form-control" ng-model="$ctrl.m.selectedAddHandlerUserId">
                                <option value="">Select user</option>
                                <option ng-repeat="h in $ctrl.m.addableHandlers" value="{{h.UserId}}">{{h.UserDisplayName}}</option>
                            </select>
                        </label>
                        <div class="col-xs-3">
                            <span style="float:right">
                                <button ng-click="$ctrl.cancelEdit($event)" class="n-icon-btn n-white-btn"><span class="glyphicon glyphicon-remove"></span></button> <button ng-click="$ctrl.commitEdit($event)" ng-disabled="!$ctrl.m.selectedAddHandlerUserId" class="n-icon-btn n-green-btn"><span class="glyphicon glyphicon-ok"></span></button>
                            </span>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>
</div>`;
        }
    }

    export class InitialData {
        applicationNr: string
        hostData: ComponentHostNs.ComponentHostInitialData
    }

    export class Model {
        constructor(public isExpanded: boolean,
            public allPossibleHandlers: NTechPreCreditApi.AssignedHandlerModel[]
            ) {
        }

        public firstAssignedHandler: NTechPreCreditApi.AssignedHandlerModel
        public assignedHandlers : NTechPreCreditApi.AssignedHandlerModel[]
        public addableHandlers : NTechPreCreditApi.AssignedHandlerModel[]
        public setAssignedHandlers(assignedHandlers: NTechPreCreditApi.AssignedHandlerModel[]) {
            this.assignedHandlers = assignedHandlers ? assignedHandlers : []
            this.firstAssignedHandler = assignedHandlers && assignedHandlers.length > 0 ? assignedHandlers[0] : null
            let ah : NTechPreCreditApi.AssignedHandlerModel[] = []
            for (let h of this.allPossibleHandlers) {
                if (!NTechLinq.any(this.assignedHandlers, x => x.UserId === h.UserId)) {
                    ah.push(h)
                }
            }
            this.addableHandlers = ah
        } 
        public selectedAddHandlerUserId: string
        public isAddUserMode: boolean
    }
}

angular.module('ntech.components').component('applicationAssignedHandlers', new ApplicationAssignedHandlersComponentNs.ApplicationAssignedHandlersComponent())