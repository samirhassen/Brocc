namespace MortgageLoanLeadComponentNs {
    export class MortgageLoanLeadController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData;
        m: Model;

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'mortgageLoanLead'
        }

        onBack(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            let after = () => {
                let target : NavigationTargetHelper.CodeOrUrl
                if (this.m && this.m.isWorkListMode) {
                     target = NavigationTargetHelper.createCodeTarget(NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreateLeadWorkList)
                } else {
                    target = NavigationTargetHelper.createCodeTarget(NavigationTargetHelper.NavigationTargetCode.MortgageLoanSearch)
                }

                NavigationTargetHelper.handleBack(
                    target,
                    this.apiClient,
                    this.$q, { 
                        applicationNr: this.m && this.m.applicationNr ? this.m.applicationNr : null, 
                        workListId: this.m && this.m.workListStatus ? this.m.workListStatus.WorkListHeaderId.toString() : null 
                    }
                )
            }

            if (this.m.isWorkListMode) {
                //Try to replace beforing returning
                let ws = this.m.workListStatus
                this.apiClient.tryCompleteOrReplaceMortgageLoanWorkListItem(ws.WorkListHeaderId, ws.ItemId, true).then(x => { 
                    after()
                })
            } else {
                after()
            }
        }

        onChanges() {
            this.reload()
        }

        reload() {
            this.m = null

            if (!this.initialData) {
                return
            }

            let i = this.initialData
            let isWorkListMode = !!i.workListApplicationNr
            let applicationNr = isWorkListMode ? i.workListApplicationNr : i.leadOnlyApplicationNr

            if (!applicationNr) {
                return
            }

            this.apiClient.fetchApplicationInfo(applicationNr).then(ai => {
                LeadDataHelper.fetch(applicationNr, this.apiClient).then(lead => {
                    this.apiClient.fetchApplicationAssignedHandlers({ applicationNr: applicationNr, returnPossibleHandlers: true, returnAssignedHandlers: false }).then(handlers => {
                            MortgageLoanApplicationDualCreditCheckSharedNs.getApplicantDataByApplicantNr(ai.ApplicationNr, ai.NrOfApplicants > 1, this.apiClient).then(applicantDataByApplicantNr => {
                                if (isWorkListMode) {
                                    this.apiClient.fetchMortgageLoanWorkListItemStatus(parseInt(this.initialData.workListId), this.initialData.workListApplicationNr).then(workListStatus => {
                                        if (workListStatus.IsTakenByCurrentUser) {
                                            this.m = this.createModel(ai, lead, workListStatus, applicantDataByApplicantNr, handlers.PossibleHandlers)
                                        } else {
                                            toastr.warning('Lead is not taken by the current user in this worklist, ignoring the worklist')
                                            this.m = this.createModel(ai, lead, null, applicantDataByApplicantNr, handlers.PossibleHandlers)
                                        }
                                    })
                                } else {
                                    this.m = this.createModel(ai, lead, null, applicantDataByApplicantNr, handlers.PossibleHandlers)
                                }
                            })
                        })
                })
            })
        }
        
        createModel(ai: NTechPreCreditApi.ApplicationInfoModel, leadData : LeadDataHelper, 
            workListStatus: NTechPreCreditApi.MortgageLoanLeadsWorkListItemStatus, 
            applicantDataByApplicantNr: NTechPreCreditApi.INumberDictionary<{ firstName: string, birthDate: string }>,
            assignableHandlers: NTechPreCreditApi.AssignedHandlerModel[]): Model {
            return {
                isActiveLead: ai.IsLead && ai.IsActive,
                lead: leadData,
                isWorkListMode: !!workListStatus,
                applicationNr: ai.ApplicationNr,
                workListStatus: workListStatus,
                assignableHandlers: assignableHandlers,
                assignedHandlerUserId: null,
                providerDisplayName: ai.ProviderDisplayName,
                b: this.createDecisionModel(ai, workListStatus ? workListStatus.WorkListHeaderId.toString() : null, applicantDataByApplicantNr),
                commentsInitialData: {
                    applicationInfo: ai,
                    hideAdditionalInfoToggle: true,
                    reloadPageOnWaitingForAdditionalInformation: false
                },
                tabs: {
                    activeName: 'qualifiedLead',
                    tryLaterOptions: NTechLinq.select(TryLaterDaysOptions, x => x.toString()),
                    selectedTryLaterDays: this.inferNextTryLaterDays(leadData),
                    rejectModel: RejectModel.create(this.initialData.rejectionReasonToDisplayNameMapping)
                }
            }
        }        

        inferNextTryLaterDays(d: LeadDataHelper) : string {
            //Idea is to "take the next one" from TryLaterDaysOptions so for 5->6 we take 7 and so on until we reach the highest one and then stop there
            let tryLaterDays = d.tryLaterDays()
            for (let tryLaterLimit of TryLaterDaysOptions) {
                if (tryLaterDays < tryLaterLimit) {
                    return tryLaterLimit.toString()
                }
            }
            return tryLaterDays.toString()
        }

        private createCrossModuleNavigationTargetToHere(applicationNr: string, workListId: string) {
            let code : string 
            if (workListId) {
                code = this.ntechComponentService.createCrossModuleNavigationTargetCode('MortgageLoanApplicationWorkListLead', { applicationNr: applicationNr, workListId: workListId })
            } else {
                code = code = this.ntechComponentService.createCrossModuleNavigationTargetCode('MortgageLoanApplicationLead', { applicationNr: applicationNr })
            }
            return NavigationTargetHelper.createCodeTarget(code, null)
        }

        createDecisionModel(x: NTechPreCreditApi.ApplicationInfoModel, workListId: string, 
            applicantDataByApplicantNr : NTechPreCreditApi.INumberDictionary<{ firstName: string, birthDate: string }>) : DecisionBasisModel {
            let b = new DecisionBasisModel()

            let targetToHere = this.createCrossModuleNavigationTargetToHere(x.ApplicationNr, workListId)
            
            let isInPlaceEditAllowed = true
            let isReadOnly = false
            let afterInPlaceEditCommited = () => {

            }
            MortgageLoanApplicationDualCreditCheckSharedNs.initializeSharedDecisionModel(b,
                x.NrOfApplicants > 1, x, targetToHere, this.apiClient, this.$q, isInPlaceEditAllowed,
                afterInPlaceEditCommited, targetToHere.targetCode, isReadOnly, applicantDataByApplicantNr)

            let createCustomerInfoInitialData = applicantNr => {
                let d: ApplicationCustomerInfoComponentNs.InitialData = {
                    applicationNr: x.ApplicationNr,
                    applicantNr: applicantNr,
                    customerIdCompoundItemName: null,
                    backTarget: targetToHere.targetCode
                }
                return d
            }

            b.applicationCustomerInfo1InitialData = createCustomerInfoInitialData(1)
            if (x.NrOfApplicants > 1) {
                b.applicationCustomerInfo2InitialData = createCustomerInfoInitialData(2)
            }

            return b
        }

        skipItem(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }

            this.afterComplete(null)            
        }

        goToApplication(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            NavigationTargetHelper.tryNavigateTo(NavigationTargetHelper.NavigationTargetCode.MortgageLoanApplication, { applicationNr: this.m.applicationNr })
        }
        
        setActiveTab(tabName: string, evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.m.tabs.activeName = tabName
        }

        changeToQualifiedLead(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }

            this.apiClient.tryComplateMortgageLoanLead(this.m.applicationNr, ChangeToQualifiedLeadCode, null, null, null).then(x => {
                if (!x.WasChangedToQualifiedLead) {
                    toastr.warning('Could not change to qualified lead')
                    return
                }
                this.apiClient.setApplicationAssignedHandlers(this.m.applicationNr, this.m.assignedHandlerUserId ? [this.m.assignedHandlerUserId] : null, null).then(() => {
                        this.afterComplete(ChangeToQualifiedLeadCode)
                    })
            })
        }

        cancelLead(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.apiClient.tryComplateMortgageLoanLead(this.m.applicationNr, CancelCode, null, null, null).then(x => {
                if (!x.WasCancelled) {
                    toastr.warning('Could not cancel lead')
                    return
                }
                this.afterComplete(CancelCode)
            })
        }

        tryLater(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            let tryLaterDays = parseInt(this.m.tabs.selectedTryLaterDays)
            this.apiClient.tryComplateMortgageLoanLead(this.m.applicationNr, TryLaterCode, null, null, tryLaterDays).then(x => {
                if (!x.WasTryLaterScheduled) {
                    toastr.warning('Could not schedule try later')
                    return
                }
                this.afterComplete(TryLaterCode)
            })
        }

        reject(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }

            let r = this.m.tabs.rejectModel.getSelectedReasons()
            
            this.apiClient.tryComplateMortgageLoanLead(this.m.applicationNr, RejectCode, r.reasonCodes, r.otherReasonText, null).then(x => {
                if (!x.WasRejected) {
                    toastr.warning('Could not reject')
                    return
                }
                this.afterComplete(RejectCode)
            })
        }

        private afterComplete(completionCode: string) {
            if (this.m.isWorkListMode) {
                //Complete and take a new one
                let ws = this.m.workListStatus
                this.apiClient.tryCompleteOrReplaceMortgageLoanWorkListItem(ws.WorkListHeaderId, ws.ItemId, false).then(x => {
                    if (!x.WasCompleted) {
                        toastr.warning('Could not complete worklist item')
                        return
                    }
                    this.apiClient.tryTakeMortgageLoanWorkListItem(ws.WorkListHeaderId).then(y => {
                        if (!y.WasItemTaken) {
                            NavigationTargetHelper.tryNavigateTo(NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreateLeadWorkList, null)
                        } else {
                            NavigationTargetHelper.tryNavigateTo(NavigationTargetHelper.NavigationTargetCode.MortgageLoanLead, {
                                applicationNr: y.TakenItemId,
                                workListId: ws.WorkListHeaderId.toString()
                            })
                        }
                    })
                })
            } else {
                //TODO: Sending back to search ... could alternatively reload the page but will get wierd for things like try later
                NavigationTargetHelper.tryNavigateTo(NavigationTargetHelper.NavigationTargetCode.MortgageLoanSearch, null)
            }
        }
    }

    const ChangeToQualifiedLeadCode = 'ChangeToQualifiedLead'
    const CancelCode = 'Cancel'
    const TryLaterCode = 'TryLater'
    const RejectCode = 'Reject'
    const TryLaterDaysOptions = [0, 1, 2, 4, 7, 14]

    export class MortgageLoanLeadComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanLeadController;

            let decisionBasisTemplate = `<div>
    <h2 class="custom-header">Decision basis</h2>
    <hr class="hr-section" />

    <div class="row">
        <div class="col-xs-8">
            <div class="editblock">
                <div class="row pb-3">
                    <div class="col-xs-6">
                        <application-editor initial-data="$ctrl.m.b.applicationBasisFields"></application-editor>
                    </div>
                </div>

                <div class="row">
                    <div class="col-xs-6">
                        <h2 class="custom-header text-center">{{$ctrl.m.b.applicant1DetailInfo}}</h2>
                        <hr class="hr-section" />
                    </div>
                    <div class="col-xs-6">
                        <h2 class="custom-header text-center" ng-if="$ctrl.m.b.hasCoApplicant">{{$ctrl.m.b.applicant2DetailInfo}}</h2>
                        <hr ng-if="$ctrl.m.b.hasCoApplicant" class="hr-section" />
                    </div>
                </div>
                <div class="row pb-3">
                    <div class="col-xs-6">
                        <application-editor initial-data="$ctrl.m.b.applicant1BasisFields"></application-editor>
                    </div>
                    <div class="col-xs-6">
                        <application-editor ng-if="$ctrl.m.b.hasCoApplicant" initial-data="$ctrl.m.b.applicant2BasisFields"></application-editor>
                    </div>
                </div>
            </div>
        </div>
        <div class="col-xs-4">
            <div class="pb-1">
                <application-customerinfo initial-data="$ctrl.m.b.applicationCustomerInfo1InitialData"></application-customerinfo>
            </div>
            <hr class="hr-section dotted" />
            <div ng-if="$ctrl.m.b.applicationCustomerInfo2InitialData" class="pb-3">
                <application-customerinfo initial-data="$ctrl.m.b.applicationCustomerInfo2InitialData"></application-customerinfo>
            </div>
            <hr ng-if="$ctrl.m.b.applicationCustomerInfo2InitialData" class="hr-section dotted" />
            <h2 class="custom-header text-center">Other Applications</h2>
            <hr class="hr-section" />
            <mortgage-loan-other-connected-applications-compact initial-data="$ctrl.m.b.otherApplicationsData"></mortgage-loan-other-connected-applications-compact>
            <h2 class="custom-header text-center">Object</h2>
            <hr class="hr-section" />
            <mortgage-loan-dual-collateral-compact initial-data="$ctrl.m.b.objectCollateralData"></mortgage-loan-dual-collateral-compact>
            <h2 class="custom-header text-center">Other</h2>
            <hr class="hr-section" />
            <mortgage-loan-dual-collateral-compact initial-data="$ctrl.m.b.otherCollateralData"></mortgage-loan-dual-collateral-compact>
        </div>
    </div>

</div>`
                       
            let sharedNotActiveLeadTemplate = `
                <p ng-if="$ctrl.m.lead.wasAccepted()" class="text-center">Converted to a <a href="#" ng-click="$ctrl.goToApplication($event)">qualified lead</a> on {{$ctrl.m.lead.acceptedDate() | date:shortDate}}</p>
                              
                <div ng-if="$ctrl.m.lead.wasCancelled()">
                    <div class="form-horizontal">
                        <div class="form-group">
                            <label class="control-label col-xs-6">Cancelled on</label>
                            <div class="form-control-static col-xs-6">{{$ctrl.m.lead.cancelledDate() | date:shortDate}}</div>
                        </div>
                    </div>
                </div>

                <div ng-if="$ctrl.m.lead.wasRejected()">
                    <div class="form-horizontal">
                        <div class="form-group">
                            <label class="control-label col-xs-6">Rejected on</label>
                            <div class="form-control-static col-xs-6">{{$ctrl.m.lead.rejectedDate() | date:shortDate}}</div>
                        </div>
                        <div class="form-group">
                            <label class="control-label col-xs-6">Reasons</label>
                            <div class="form-control-static col-xs-6"><span ng-repeat="r in $ctrl.m.lead.displayRejectionReasons($ctrl.initialData.rejectionReasonToDisplayNameMapping)" class="comma">{{r}}</span></div>
                        </div>
                    </div>                    
                </div>                
                
                <p ng-if="!$ctrl.m.lead.isLead() && !$ctrl.m.lead.wasAccepted()">Not a lead</p>`           
            
            let sharedTemplate = `<div ng-show="$ctrl.m.isActiveLead"><div class="row">
        <div class="col-xs-10 col-sm-offset-1">
            <div class="row" ng-init="tabs=[['Qualified Lead', 'qualifiedLead'], ['Reject', 'reject'], ['Try later', 'tryLater'], ['Cancel', 'cancel']]">
                <div class="{{$first ? 'col-sm-offset-2 ' : ''}}col-xs-2" ng-repeat="t in tabs">
                    <span ng-click="$ctrl.setActiveTab(t[1], $event)" type="button" class="btn" ng-class="{ disabled : false, 'decision-form-active-btn' : $ctrl.m.tabs.activeName === t[1], 'decision-form-inactive-btn' : $ctrl.m.tabs.activeName !== t[1] }">
                        {{t[0]}}
                    </span>
                </div>
            </div>

            <form class="decision-form" name="qualifiedLeadForm" bootstrap-validation="'parent'" novalidate ng-show="$ctrl.m.tabs.activeName == 'qualifiedLead'">
                
                <div class="form-horizontal">
                    <div class="form-group">
                        <label class="col-xs-6 control-label">Qualified lead</label>
                        <div class="col-xs-6"><div class="checkbox"><input type="checkbox" ng-model="isQualifiedLeadChecked"></div></div>
                    </div>
                    <div class="form-group">
                        <label for="assignedHandler" class="col-xs-6 control-label">Assign handler</label>
                        <div class="col-xs-4">
                            <select id="assignedHandler" class="form-control" ng-model="$ctrl.m.assignedHandlerUserId" ng-options="handler.UserId as handler.UserDisplayName for handler in $ctrl.m.assignableHandlers">
                            <option value="" selected="true">None</option>
                            </select>
                        </div>                  
                    </div>
                </div>                    
                <div class="text-center pt-3">
                    <button type="button" class="n-main-btn n-green-btn" ng-disabled="!isQualifiedLeadChecked" ng-click="$ctrl.changeToQualifiedLead($event)">Next</button>
                </div>
            </form>

            <form class="form-horizontal decision-form" name="rejectForm" bootstrap-validation="'parent'" novalidate ng-show="$ctrl.m.tabs.activeName == 'reject'">
                <h4 class="text-center">Rejection reasons</h4>
                <div class="row">
                    <div class="col-sm-6 col-md-6">
                        <div class="form-group" ng-repeat="b in $ctrl.m.tabs.rejectModel.rejectModelCheckboxesCol1">
                            <label class="col-md-8 control-label">{{b.displayName}}</label>
                            <div class="col-md-4"><div class="checkbox"><input type="checkbox" ng-model="$ctrl.m.tabs.rejectModel.reasons[b.reason]"></div></div>
                        </div>
                    </div>
                    <div class="col-sm-6 col-md-6">
                        <div class="form-group" ng-repeat="b in $ctrl.m.tabs.rejectModel.rejectModelCheckboxesCol2">
                            <label class="col-md-6 control-label">{{b.displayName}}</label>
                            <div class="col-md-4"><div class="checkbox"><input type="checkbox" ng-model="$ctrl.m.tabs.rejectModel.reasons[b.reason]"></div></div>
                        </div>
                    </div>
                </div>
                <div class="form-group">
                    <label class="col-md-4 control-label">Other</label>
                    <div class="col-md-6"><input type="text" class="form-control" ng-model="$ctrl.m.tabs.rejectModel.otherReason"></div>
                </div>
                <div class="text-center pt-3">
                    <button type="button" class="n-main-btn n-red-btn" ng-disabled="!$ctrl.m.tabs.rejectModel.anyRejectionReasonGiven()" ng-click="$ctrl.reject($event)">Reject</button>
                </div>
            </form>
            <form class="form-horizontal decision-form" name="tryLaterForm" bootstrap-validation="'parent'" novalidate ng-show="$ctrl.m.tabs.activeName == 'tryLater'">
                <div class="row">
                    <div class="col-sm-offset-2 col-sm-6 col-md-6">
                        <div class="form-group">
                            <label class="col-md-8 control-label">Try again after</label>
                            <div class="col-md-4">
                                <select class="form-control" ng-model="$ctrl.m.tabs.selectedTryLaterDays">
                                    <option ng-repeat="d in $ctrl.m.tabs.tryLaterOptions" value="{{d}}">{{d}} days</option>
                                </select>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="text-center pt-3">
                    <button type="button" class="n-main-btn n-green-btn" ng-click="$ctrl.tryLater($event)">Next</button>
                </div>
            </form>
            <form class="form-horizontal decision-form" name="cancelForm" bootstrap-validation="'parent'" novalidate ng-show="$ctrl.m.tabs.activeName == 'cancel'">
                <div class="row">
                    <div class="col-sm-offset-2 col-sm-6 col-md-6">
                        <div class="form-group">
                            <label class="col-md-8 control-label">Cancel</label>
                            <div class="col-md-4"><div class="checkbox"><input type="checkbox" ng-model="isCancelChecked"></div></div>
                        </div>
                    </div>
                </div>
                <div class="text-center pt-3">
                    <button type="button" class="n-main-btn n-green-btn" ng-disabled="!isCancelChecked" ng-click="$ctrl.cancelLead($event)">Next</button>
                </div>
            </form>
        </div>
    </div>

    ${decisionBasisTemplate}
    </div>

    <div ng-hide="$ctrl.m.isActiveLead">
        ${sharedNotActiveLeadTemplate}
   </div>

    <div class="pt-3"><application-comments initial-data="$ctrl.m.commentsInitialData"></application-comments></div>`

            let standAloneTemplate = `<div class="pt-1 pb-2">
        <div class="pull-left"><a class="n-back" href="#" ng-click="$ctrl.onBack($event)"><span class="glyphicon glyphicon-arrow-left"></span></a></div>
        <h1 class="adjusted">Lead {{$ctrl.m.applicationNr}} 
            <span class="adjusted-subtitle"> {{$ctrl.m.providerDisplayName}}</span> 
        </h1>
    </div>
    
    ${sharedTemplate}`

            let workListTemplate = `<div class="pt-1 pb-2">
        <div class="row">
            <div class="col-xs-1"><a class="n-back" href="#" ng-click="$ctrl.onBack($event)"><span class="glyphicon glyphicon-arrow-left"></span></a></div>
            <div class="col-xs-3">
                <div class="text-center worklist-counter">
                    <label>{{$ctrl.m.providerDisplayName}}</label>
                    <p>{{$ctrl.m.workListStatus.CurrentUserActiveItemId}}</p>
                </div>
            </div>
            <div class="col-xs-2">
                <div class="text-center worklist-counter">
                    <label>Selection</label>
                    <p>Leads</p>
                </div>
            </div>
            <div class="col-xs-2">
                <div class="text-center worklist-counter">
                    <label>My count</label>
                    <p>{{$ctrl.m.workListStatus.TakeOrCompletedByCurrentUserCount}}</p>
                </div>
            </div>
            <div class="col-xs-2">
                <div class="text-center worklist-counter">
                    <label>Total</label>
                    <p>{{$ctrl.m.workListStatus.TakenCount + $ctrl.m.workListStatus.CompletedCount}}/{{$ctrl.m.workListStatus.TotalCount}}
                    </p>
                </div>
            </div>
            <div class="col-xs-2 text-right pt-1">
                <button ng-click="$ctrl.skipItem($event)" type="button"
                    class="n-main-btn n-blue-btn">Skip <span
                        class="glyphicon glyphicon-arrow-right"></span></button>
            </div>
        </div>
        <div style="border-bottom: 2px solid #2d7fc1;padding-top:5px;"></div>
    </div>

    ${sharedTemplate}`

            this.template = `<div ng-if="$ctrl.m && $ctrl.m.isWorkListMode === false">${standAloneTemplate}</div>
                             <div ng-if="$ctrl.m && $ctrl.m.isWorkListMode === true">${workListTemplate}</div>`
        }
    }

    class LeadDataHelper {
        constructor(private leadData: ComplexApplicationListHelper.ComplexApplicationListData) {

        }

        isLead() {
            return this.leadData.getOptionalUniqueValue(1, 'IsLead') === 'true'
        }

        wasAccepted() {
            return this.leadData.getOptionalUniqueValue(1, 'WasAccepted') === 'true'
        }

        acceptedDate() : Date {
            let d = this.leadData.getOptionalUniqueValue(1, 'AcceptedDate')
            if (!d) {
                return null
            }
            return moment(d).toDate()
        }

        wasCancelled() {
            return this.leadData.getOptionalUniqueValue(1, 'WasCancelled') === 'true'
        }

        cancelledDate() : Date {
            let d = this.leadData.getOptionalUniqueValue(1, 'CancelledDate')
            if (!d) {
                return null
            }
            return moment(d).toDate()
        }

        wasRejected() {
            return this.leadData.getOptionalUniqueValue(1, 'WasRejected') === 'true'
        }

        rejectedDate() : Date {
            let d = this.leadData.getOptionalUniqueValue(1, 'RejectedDate')
            if (!d) {
                return null
            }
            return moment(d).toDate()
        }

        displayRejectionReasons(rejectionReasonToDisplayNameMapping: NTechPreCreditApi.IStringDictionary<string>): string[] {
            let rejectionReasons = this.leadData.getOptionalRepeatingValue(1, 'RejectionReasons')
            if (!rejectionReasons) {
                return null
            }

            let displayRejectionReasons: string[] = []
            for (let r of rejectionReasons) {
                if (r === 'other') {
                    displayRejectionReasons.push('other: ' + this.leadData.getOptionalUniqueValue(1, 'OtherRejectionReasonText'))
                } else {
                    displayRejectionReasons.push(rejectionReasonToDisplayNameMapping[r] ? rejectionReasonToDisplayNameMapping[r] : r)
                }
            }
            return displayRejectionReasons
        }


        tryLaterDays() {
            let d = this.leadData.getOptionalUniqueValue(1, 'TryLaterDays')
            if (d) {
                return parseInt(d)
            } else {
                return 0
            }
        }

        static fetch(applicationNr: string, apiClient: NTechPreCreditApi.ApiClient): ng.IPromise<LeadDataHelper> {
            return ComplexApplicationListHelper.fetch(applicationNr, 'Lead', apiClient, ['IsLead', 'WasAccepted', 'AcceptedDate', 'TryLaterDays', 'WasCancelled', 'CancelledDate', 'WasRejected', 'RejectedDate', 'OtherRejectionReasonText'], ['RejectionReasons']).then(leadData => {
                return new LeadDataHelper(leadData)
            })
        }

        static ComplexListItemNamesUsed = ['IsLead', 'WasAccepted', 'TryLaterDays']
    }

    export interface LocalInitialData {
        leadOnlyApplicationNr: string
        contextId: string
        workListApplicationNr: string
        workListId: string
        rejectionReasonToDisplayNameMapping: NTechPreCreditApi.IStringDictionary<string>
    }

    export interface InitialData extends LocalInitialData, ComponentHostNs.ComponentHostInitialData {
    }

    export class Model {
        isActiveLead: boolean
        isWorkListMode: boolean
        lead: LeadDataHelper
        applicationNr: string
        b: DecisionBasisModel
        workListStatus: NTechPreCreditApi.MortgageLoanLeadsWorkListItemStatus
        assignableHandlers: NTechPreCreditApi.AssignedHandlerModel[]
        assignedHandlerUserId: number
        providerDisplayName: string
        commentsInitialData: ApplicationCommentsComponentNs.InitialData
        tabs: {
            activeName: string
            tryLaterOptions: string[]
            selectedTryLaterDays: string
            rejectModel: RejectModel
        }        
    }

    export class RejectModel {        
        constructor() {                        
            this.otherReason = '',
            this.reasons = {}
            this.rejectModelCheckboxesCol1 = []
            this.rejectModelCheckboxesCol2 = []
        }

        public rejectModelCheckboxesCol1: RejectionCheckboxModel[]
        public rejectModelCheckboxesCol2: RejectionCheckboxModel[]
        public reasons: NTechPreCreditApi.IStringDictionary<boolean>
        public otherReason: string
        
        static create(rejectionReasonToDisplayNameMapping: NTechPreCreditApi.IStringDictionary<string>): RejectModel {
            let r: RejectModel = new RejectModel()

            for (let reasonName of Object.keys(rejectionReasonToDisplayNameMapping)) {
                let displayName = rejectionReasonToDisplayNameMapping[reasonName]
                if (r.rejectModelCheckboxesCol1.length > r.rejectModelCheckboxesCol2.length) {
                    r.rejectModelCheckboxesCol2.push(new RejectionCheckboxModel(reasonName, displayName))
                } else {
                    r.rejectModelCheckboxesCol1.push(new RejectionCheckboxModel(reasonName, displayName))
                }
            }

            return r
        }

        getSelectedReasons(): { reasonCodes : string[], otherReasonText: string } {
            let reasonCodes: string[] = []
            for (let key of Object.keys(this.reasons)) {
                if (this.reasons[key] === true) {
                    reasonCodes.push(key)
                }
            }
            if (this.otherReason) {
                reasonCodes.push('other')
            }

            return { reasonCodes: reasonCodes, otherReasonText: this.otherReason ? this.otherReason : null }
        }        

        anyRejectionReasonGiven() {
            return this.getSelectedReasons().reasonCodes.length > 0
        }
    }

    export class RejectionCheckboxModel {
        constructor(public reason: string, public displayName: string) {
        }
    }
    
    export class DecisionBasisModel implements MortgageLoanApplicationDualCreditCheckSharedNs.DecisionBasisModelSharedWithLeads {
        public hasCoApplicant: boolean
        public applicationBasisFields: ApplicationEditorComponentNs.InitialData
        public applicant1BasisFields: ApplicationEditorComponentNs.InitialData
        public applicant2BasisFields: ApplicationEditorComponentNs.InitialData
        public isEditAllowed: boolean
        public applicant1DetailInfo: string
        public applicant2DetailInfo: string
        public otherApplicationsData: MortgageLoanOtherConnectedApplicationsCompactComponentNs.InitialData
        public objectCollateralData: MortgageLoanDualCollateralCompactComponentNs.InitialData
        public otherCollateralData: MortgageLoanDualCollateralCompactComponentNs.InitialData
        //Not shared with regular applications
        applicationCustomerInfo1InitialData: ApplicationCustomerInfoComponentNs.InitialData
        applicationCustomerInfo2InitialData: ApplicationCustomerInfoComponentNs.InitialData
    }
}

angular.module('ntech.components').component('mortgageLoanLead', new MortgageLoanLeadComponentNs.MortgageLoanLeadComponent())