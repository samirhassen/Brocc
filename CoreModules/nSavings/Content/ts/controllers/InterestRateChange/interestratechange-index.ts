class InterestRateChangeCtrl {
    static $inject = ['$scope', '$http', '$q', '$interval']
    constructor(
        private $scope: ng.IScope,
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $interval: ng.IIntervalService
    ) {
        window.scope = this;

        this.isRegularTabActive = true;

        this.backUrl = initialData.backUrl;
        if (initialData.currentInterestRate) {
            this.currentNewAccountsInterestRate = initialData.currentInterestRate
        } else {
            this.currentNewAccountsInterestRate = null
        }

        this.upcomingChanges = initialData.upcomingChanges;
        
        this.setUiStateFromChange(initialData.currentChangeState)

        this.$interval(() => {
            this.doBackgroundStateUpdate();
        }, 1000)
    }
    
    currentNewAccountsInterestRate: InterestRateChangeNs.IInterestRate;
    isLoading: boolean;
    backUrl: string;
    isRegularTabActive: boolean;
    regular: InterestRateChangeNs.RegularChangeModel;
    split: InterestRateChangeNs.SplitChangeModel;
    preview: InterestRateChangeNs.PreviewModel;
    pending: InterestRateChangeNs.PendingChangeModel;
    upcomingChanges: Array<InterestRateChangeNs.IUpcomingChange>;
    historicalChangeItems: Array<InterestRateChangeNs.IHistoricalChangeItem>;

    isValidDecimal(value:string) : boolean {
        return ntech.forms.isValidDecimal(value)
    }

    asMoment(value: string) {        
        var v = moment(value, 'YYYY-MM-DD', true)
        if (v.isValid()) {
            return v;
        } else {
            return null;
        }
    }

    isValidDate(value: string) : boolean {
        if (ntech.forms.isNullOrWhitespace(value))
            return true
        return moment(value, 'YYYY-MM-DD', true).isValid()
    }

    isLowering(fromRate: number, toRate: number) {
        /*Floating point math is evil*/
        return Math.round(toRate * 100) < Math.round(fromRate * 100) 
    }

    calculateRegular(event: Event) {
        if (event) {
            event.preventDefault();
        }

        let today = this.asMoment(initialData.today);
        let earliestAllowedAllAccountsLoweredDate = this.asMoment(initialData.earliestAllowedAllAccountsLoweredDate);
        let earliestAllowedNewAccountsOrRaisedDate = this.asMoment(initialData.earliestAllowedNewAccountsOrRaisedDate);
        
        let isLowering = false;
        let newValidFromDate = this.asMoment(this.regular.validFromDate);

        if (newValidFromDate <= today) {
            toastr.warning('From date must be after today')
            return;
        }

        let newInterestRate = parseFloat(parseFloat(this.regular.newInterestRate.replace(',', '.')).toFixed(2));
        if (this.currentNewAccountsInterestRate) {
            let currentInterestRate = this.currentNewAccountsInterestRate.InterestRatePercent;            
            isLowering = this.isLowering(currentInterestRate, newInterestRate)
        }

        let p = new InterestRateChangeNs.PreviewModel();
        p.isRegularChange = true;
        p.initiatedDate = today;
        p.allAccountsDate = newValidFromDate;
        p.newAccountsDate = null;
        p.newInterestRate = newInterestRate;
        p.showRegularChangeLoweringTwoMonthWarning = isLowering && newValidFromDate < earliestAllowedAllAccountsLoweredDate;
        
        this.preview = p;
    }

    calculateSplit(event: Event) {
        if (event) {
            event.preventDefault();
        }

        let today = this.asMoment(initialData.today);
        let earliestAllowedAllAccountsLoweredDate = this.asMoment(initialData.earliestAllowedAllAccountsLoweredDate);
        let earliestAllowedNewAccountsOrRaisedDate = this.asMoment(initialData.earliestAllowedNewAccountsOrRaisedDate);

        let isLowering = false;
        let newAllAccountsValidFromDate = this.asMoment(this.split.validFromDateExistingAccounts);
        let newNewAccountsValidFromDate = this.asMoment(this.split.validFromDateNewAccounts);

        if (newAllAccountsValidFromDate <= today || newNewAccountsValidFromDate <= today) {
            toastr.warning('From date must be after today')
            return;
        }

        let newInterestRate = parseFloat(parseFloat(this.split.newInterestRate.replace(',', '.')).toFixed(2));
        if (this.currentNewAccountsInterestRate) {
            let currentInterestRate = this.currentNewAccountsInterestRate.InterestRatePercent;
            isLowering = this.isLowering(currentInterestRate, newInterestRate)
        }

        let p = new InterestRateChangeNs.PreviewModel();
        p.isRegularChange = false;
        p.initiatedDate = today;
        p.allAccountsDate = newAllAccountsValidFromDate;
        p.newAccountsDate = newNewAccountsValidFromDate;
        p.newInterestRate = newInterestRate;
        p.showSplitChangeLoweringTwoMonthWarning = isLowering && newAllAccountsValidFromDate < earliestAllowedAllAccountsLoweredDate;
        p.showSplitChangeSameDateWarning = newAllAccountsValidFromDate.isSame(newNewAccountsValidFromDate);

        this.preview = p;
    }

    initiateChange(event: Event) {
        if (event) {
            event.preventDefault();
        }
        this.isLoading = true;
        this.$http({
            method: 'POST',
            url: initialData.urls.initiateChange,
            data: {
                testUserId: initialData.testUserId,
                newInterestRatePercent: this.preview.newInterestRate,
                allAccountsValidFromDate: this.preview.allAccountsDate.format('YYYY-MM-DD'),
                newAccountsValidFromDate: this.preview.newAccountsDate ? this.preview.newAccountsDate.format('YYYY-MM-DD') : null
            }
        }).then((response: ng.IHttpResponse<InterestRateChangeNs.IResponseWithChangeState>) => {
            this.isLoading = false;
            this.historicalChangeItems = null;
            this.setUiStateFromChange(response.data.currentChangeState);
        }, (response) => {
            this.isLoading = false
            toastr.error(response.statusText)
        })
    }

    cancelVerifyOrChange(url: string, event: Event) {
        if (event) {
            event.preventDefault();
        }
        this.isLoading = true;
        this.$http({
            method: 'POST',
            url: url,
            data: {
                testUserId: initialData.testUserId,
                changeToken: this.pending.changeToken
            }
        }).then((response: ng.IHttpResponse<InterestRateChangeNs.IResponseWithChangeState>) => {
            this.isLoading = false;
            this.historicalChangeItems = null;
            this.setUiStateFromChange(response.data.currentChangeState);
        }, (response) => {
            this.isLoading = false
            toastr.error(response.statusText)
        })
    }

    cancelChange(event: Event) {
        this.cancelVerifyOrChange(initialData.urls.cancelChange, event)
    }

    verifyChange(event: Event) {
        this.cancelVerifyOrChange(initialData.urls.verifyChange, event)
    }

    rejectChange(event: Event) {
        this.cancelVerifyOrChange(initialData.urls.rejectChange, event)
    }

    carryOutChange(event: Event) {
        if (event) {
            event.preventDefault();
        }
        this.isLoading = true;
        this.$http({
            method: 'POST',
            url: initialData.urls.carryOutChange,
            data: {
                testUserId: initialData.testUserId,
                changeToken: this.pending.changeToken,
                returnUpcomingChanges: true
            }
        }).then((response: ng.IHttpResponse<InterestRateChangeNs.IResponseWithChangeStateAndUpcoming>) => {
            this.isLoading = false;
            this.historicalChangeItems = null;
            this.upcomingChanges = response.data.upcomingChanges;
            this.setUiStateFromChange(response.data.currentChangeState)
        }, (response) => {
            this.isLoading = false
            toastr.error(response.statusText)
        })
    }

    cancelUpcomingChange(id: number, event : Event) {
        if (event) {
            event.preventDefault();
        }
        this.isLoading = true;
        this.$http({
            method: 'POST',
            url: initialData.urls.cancelUpcomingChange,
            data: {
                testUserId: initialData.testUserId,
                rateChangeHeaderId: id
            }
        }).then((response: ng.IHttpResponse<InterestRateChangeNs.IResponseWithChangeStateAndUpcoming>) => {
            this.isLoading = false
            this.upcomingChanges = response.data.upcomingChanges;
            this.historicalChangeItems = null;
            this.setUiStateFromChange(response.data.currentChangeState)
        }, (response) => {
            this.isLoading = false
            toastr.error(response.statusText)
        })
    }

    toggleHistoricalChangeItems(event: Event) {
        if (event) {
            event.preventDefault();
        }
        if (this.historicalChangeItems) {
            this.historicalChangeItems = null;
        } else {
            this.fetchHistoricalChangeItems(event);
        }
    }

    fetchHistoricalChangeItems(event: Event) {
        if (event) {
            event.preventDefault();
        }
        this.isLoading = true;
        this.$http({
            method: 'POST',
            url: initialData.urls.fetchHistoricalChangeItems,
            data: {

            }
        }).then((response: ng.IHttpResponse<InterestRateChangeNs.IResponseWithHistoricalChangeItems>) => {
            this.isLoading = false
            this.historicalChangeItems = response.data.historicalChangeItems
        }, (response) => {
            this.isLoading = false
            toastr.error(response.statusText)
        })
    }

    setUiStateFromChange(s: InterestRateChangeNs.IResponseChangeState) {
        let m = this.convertResponseToPendingModel(s);

        if (m) {
            this.pending = m;
            this.preview = null;
            this.regular = null;
            this.split = null;
            //We wipe upcoming changes and history here to make sure this wont be stale after the change is added.
            this.upcomingChanges = null; 
            this.historicalChangeItems = null;
        } else {
            this.pending = null;
            this.preview = null;
            this.regular = new InterestRateChangeNs.RegularChangeModel();
            this.split = new InterestRateChangeNs.SplitChangeModel();
        }
    }

    doBackgroundStateUpdate() {
        this.$http({
            method: 'POST',
            url: initialData.urls.getCurrentChangeState,
            data: {
                testUserId: initialData.testUserId,
                changeToken: this.pending ? this.pending.changeToken : null
            }
        }).then((response: ng.IHttpResponse<InterestRateChangeNs.IResponseWithChangeState>) => {
            let stateBefore = this.pending
            let stateAfter = response.data.currentChangeState
            if (stateBefore && !stateAfter) {
                //Canceled, we need to actually update here
                this.setUiStateFromChange(response.data.currentChangeState)
            } else if (!stateBefore && stateAfter) {
                //Most likely someelse initiated something. We should update here, even if the current user is doing something
                this.setUiStateFromChange(response.data.currentChangeState)
            } else if (stateBefore && stateAfter) {
                //Synch
                this.setUiStateFromChange(response.data.currentChangeState)
            } else {
                //No state before or after. Dont update here or we will wipe out any attempt of a user to initiate change on each synch
            }  
        }, (response) => {
            toastr.error(response.statusText)
        })        
    }
    
    convertResponseToPendingModel(s: InterestRateChangeNs.IResponseChangeState): InterestRateChangeNs.PendingChangeModel {
        //Is cancel ever not allowed?
        if (!s) {
            return null;
        }
        var m = new InterestRateChangeNs.PendingChangeModel();
        m.isInitiatedByCurrentUser = s.CurrentUserId == s.InitiatedByUserId;
        m.newInterestRatePercent = s.NewInterestRatePercent;
        m.isRegularChange = !s.NewAccountsValidFromDate;
        m.allAccountsValidFromDate = this.asMoment(s.AllAccountsValidFromDate);
        if (s.NewAccountsValidFromDate) {
            m.newAccountsValidFromDate = this.asMoment(s.NewAccountsValidFromDate);
        }
        m.initiatedDate = this.asMoment(s.InitiatedDate);
        m.initiatedByUserDisplayName = s.InitiatedByUserDisplayName;
        m.verifiedByUserDisplayName = s.VerifiedByUserDisplayName;
        m.rejectedByUserDisplayName = s.RejectedByUserDisplayName;
        m.isViolatingTwoMonthLoweringRule = s.IsViolatingTwoMonthLoweringRule;
        m.changeToken = s.ChangeToken;

        m.showSplitChangeSameDateWarning = !m.isRegularChange && m.allAccountsValidFromDate.isSame(m.newAccountsValidFromDate);
        m.showSplitChangeLoweringTwoMonthWarning = !m.isRegularChange && s.IsViolatingTwoMonthLoweringRule;
        m.showRegularChangeLoweringTwoMonthWarning = m.isRegularChange && s.IsViolatingTwoMonthLoweringRule;

        return m;
    }
}
var app = angular.module('app', ['ntech.forms'])
app.controller('interestRateChangeCtrl', InterestRateChangeCtrl)

module InterestRateChangeNs {
    export interface IResponseChangeState {
        ChangeToken: string,
        OldInterestRatePercent: number,
        NewInterestRatePercent: number,
        AllAccountsValidFromDate: string,
        NewAccountsValidFromDate: string,
        CurrentUserId: string,
        CurrentUserDisplayName: string,
        InitiatedByUserId: string,
        InitiatedByUserDisplayName: string,
        InitiatedDate: string,
        VerifiedByUserId: string,
        VerifiedByUserDisplayName: string,
        RejectedByUserId: string,
        RejectedByUserDisplayName: string,
        VerifiedOrRejectedDate: string,
        IsViolatingTwoMonthLoweringRule : boolean
    }
    export interface IResponseWithChangeState {
        currentChangeState: IResponseChangeState
    }
    export interface IResponseWithChangeStateAndUpcoming extends IResponseWithChangeState {
        upcomingChanges: Array<IUpcomingChange>
    }
    export interface IInterestRate {
        AccountTypeCode : string,
        AppliesToAccountsSinceBusinessEventId : number,
        InterestRatePercent : number,
        TransactionDate : string,
        ValidFromDate : string
    }

    export class RegularChangeModel {
        newInterestRate: string;
        validFromDate: string;
    }

    export class SplitChangeModel {
        newInterestRate: string;
        validFromDateNewAccounts: string;
        validFromDateExistingAccounts: string; 
    }

    export class PreviewModel {
        showSplitChangeSameDateWarning: boolean;
        showSplitChangeLoweringTwoMonthWarning: boolean;
        newInterestRate: number;
        isRegularChange: boolean;
        allAccountsDate: moment.Moment;
        newAccountsDate: moment.Moment;
        initiatedDate: moment.Moment; //NOTE: Dont actually save this
        showRegularChangeLoweringTwoMonthWarning: boolean;
    }

    export class PendingChangeModel {
        isViolatingTwoMonthLoweringRule: boolean;
        rejectedByUserDisplayName: string;
        verifiedByUserDisplayName: string;
        initiatedByUserDisplayName: string;
        initiatedDate: moment.Moment;
        newAccountsValidFromDate: moment.Moment;
        allAccountsValidFromDate: moment.Moment;
        newInterestRatePercent: number;
        isInitiatedByCurrentUser: boolean;
        isRegularChange: boolean;
        changeToken: string;
        showSplitChangeSameDateWarning: boolean;
        showSplitChangeLoweringTwoMonthWarning: boolean;
        showRegularChangeLoweringTwoMonthWarning: boolean;
    }

    export interface IUpcomingChange {
        Id: number,
        HadNewAccountsOnlyRate: boolean,
        NewInterestRatePercent: number,
        NewAccountsOnlyRateValidFromDate: string,
        NewAccountsOnlyRateIsPending: boolean,
        AllAccountsRateValidFromDate: string,
        AllAccountsRateIsPending: boolean,
        InitiatedAndCreatedByUserDisplayName: string,
        InitiatedDate: string,
        CreatedDate: string,
        VerifiedByUserDisplayName: string,
        VerifiedDate: string
    }
    
    export interface IHistoricalChangeItem {
        Id: number,
        AccountTypeCode: string,
        InterestRatePercent: number,
        ValidFromDate: string,
        RemovedByBusinessEventId: number,
        AppliesToAccountsSinceBusinessEventId: number,
        IsPartOfSplitChange: boolean,
        InitiatedDate: string,
        CreatedByUserId: number,
        CreatedByUserDisplayName: string,
        VerifiedByUserId: number,
        VerifiedByUserDisplayName: string,
        RemovedByUserId: number,
        RemovedByUserDisplayName: string,
        RemovedDate: string
    }
    export interface IResponseWithHistoricalChangeItems {
        historicalChangeItems: Array<IHistoricalChangeItem>
    }
}