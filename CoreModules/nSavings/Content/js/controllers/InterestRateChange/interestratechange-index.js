class InterestRateChangeCtrl {
    constructor($scope, $http, $q, $interval) {
        this.$scope = $scope;
        this.$http = $http;
        this.$q = $q;
        this.$interval = $interval;
        window.scope = this;
        this.isRegularTabActive = true;
        this.backUrl = initialData.backUrl;
        if (initialData.currentInterestRate) {
            this.currentNewAccountsInterestRate = initialData.currentInterestRate;
        }
        else {
            this.currentNewAccountsInterestRate = null;
        }
        this.upcomingChanges = initialData.upcomingChanges;
        this.setUiStateFromChange(initialData.currentChangeState);
        this.$interval(() => {
            this.doBackgroundStateUpdate();
        }, 1000);
    }
    isValidDecimal(value) {
        return ntech.forms.isValidDecimal(value);
    }
    asMoment(value) {
        var v = moment(value, 'YYYY-MM-DD', true);
        if (v.isValid()) {
            return v;
        }
        else {
            return null;
        }
    }
    isValidDate(value) {
        if (ntech.forms.isNullOrWhitespace(value))
            return true;
        return moment(value, 'YYYY-MM-DD', true).isValid();
    }
    isLowering(fromRate, toRate) {
        /*Floating point math is evil*/
        return Math.round(toRate * 100) < Math.round(fromRate * 100);
    }
    calculateRegular(event) {
        if (event) {
            event.preventDefault();
        }
        let today = this.asMoment(initialData.today);
        let earliestAllowedAllAccountsLoweredDate = this.asMoment(initialData.earliestAllowedAllAccountsLoweredDate);
        let earliestAllowedNewAccountsOrRaisedDate = this.asMoment(initialData.earliestAllowedNewAccountsOrRaisedDate);
        let isLowering = false;
        let newValidFromDate = this.asMoment(this.regular.validFromDate);
        if (newValidFromDate <= today) {
            toastr.warning('From date must be after today');
            return;
        }
        let newInterestRate = parseFloat(parseFloat(this.regular.newInterestRate.replace(',', '.')).toFixed(2));
        if (this.currentNewAccountsInterestRate) {
            let currentInterestRate = this.currentNewAccountsInterestRate.InterestRatePercent;
            isLowering = this.isLowering(currentInterestRate, newInterestRate);
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
    calculateSplit(event) {
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
            toastr.warning('From date must be after today');
            return;
        }
        let newInterestRate = parseFloat(parseFloat(this.split.newInterestRate.replace(',', '.')).toFixed(2));
        if (this.currentNewAccountsInterestRate) {
            let currentInterestRate = this.currentNewAccountsInterestRate.InterestRatePercent;
            isLowering = this.isLowering(currentInterestRate, newInterestRate);
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
    initiateChange(event) {
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
        }).then((response) => {
            this.isLoading = false;
            this.historicalChangeItems = null;
            this.setUiStateFromChange(response.data.currentChangeState);
        }, (response) => {
            this.isLoading = false;
            toastr.error(response.statusText);
        });
    }
    cancelVerifyOrChange(url, event) {
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
        }).then((response) => {
            this.isLoading = false;
            this.historicalChangeItems = null;
            this.setUiStateFromChange(response.data.currentChangeState);
        }, (response) => {
            this.isLoading = false;
            toastr.error(response.statusText);
        });
    }
    cancelChange(event) {
        this.cancelVerifyOrChange(initialData.urls.cancelChange, event);
    }
    verifyChange(event) {
        this.cancelVerifyOrChange(initialData.urls.verifyChange, event);
    }
    rejectChange(event) {
        this.cancelVerifyOrChange(initialData.urls.rejectChange, event);
    }
    carryOutChange(event) {
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
        }).then((response) => {
            this.isLoading = false;
            this.historicalChangeItems = null;
            this.upcomingChanges = response.data.upcomingChanges;
            this.setUiStateFromChange(response.data.currentChangeState);
        }, (response) => {
            this.isLoading = false;
            toastr.error(response.statusText);
        });
    }
    cancelUpcomingChange(id, event) {
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
        }).then((response) => {
            this.isLoading = false;
            this.upcomingChanges = response.data.upcomingChanges;
            this.historicalChangeItems = null;
            this.setUiStateFromChange(response.data.currentChangeState);
        }, (response) => {
            this.isLoading = false;
            toastr.error(response.statusText);
        });
    }
    toggleHistoricalChangeItems(event) {
        if (event) {
            event.preventDefault();
        }
        if (this.historicalChangeItems) {
            this.historicalChangeItems = null;
        }
        else {
            this.fetchHistoricalChangeItems(event);
        }
    }
    fetchHistoricalChangeItems(event) {
        if (event) {
            event.preventDefault();
        }
        this.isLoading = true;
        this.$http({
            method: 'POST',
            url: initialData.urls.fetchHistoricalChangeItems,
            data: {}
        }).then((response) => {
            this.isLoading = false;
            this.historicalChangeItems = response.data.historicalChangeItems;
        }, (response) => {
            this.isLoading = false;
            toastr.error(response.statusText);
        });
    }
    setUiStateFromChange(s) {
        let m = this.convertResponseToPendingModel(s);
        if (m) {
            this.pending = m;
            this.preview = null;
            this.regular = null;
            this.split = null;
            //We wipe upcoming changes and history here to make sure this wont be stale after the change is added.
            this.upcomingChanges = null;
            this.historicalChangeItems = null;
        }
        else {
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
        }).then((response) => {
            let stateBefore = this.pending;
            let stateAfter = response.data.currentChangeState;
            if (stateBefore && !stateAfter) {
                //Canceled, we need to actually update here
                this.setUiStateFromChange(response.data.currentChangeState);
            }
            else if (!stateBefore && stateAfter) {
                //Most likely someelse initiated something. We should update here, even if the current user is doing something
                this.setUiStateFromChange(response.data.currentChangeState);
            }
            else if (stateBefore && stateAfter) {
                //Synch
                this.setUiStateFromChange(response.data.currentChangeState);
            }
            else {
                //No state before or after. Dont update here or we will wipe out any attempt of a user to initiate change on each synch
            }
        }, (response) => {
            if (response.statusText && response.statusText !== "") {
                toastr.error(response.statusText);
            }
            else {
                toastr.error("Lost connection to server");
            }
        });
    }
    convertResponseToPendingModel(s) {
        //Is cancel ever not allowed?
        if (!s) {
            return null;
        }
        const m = new InterestRateChangeNs.PendingChangeModel();
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
InterestRateChangeCtrl.$inject = ['$scope', '$http', '$q', '$interval'];
var app = angular.module('app', ['ntech.forms']);
app.controller('interestRateChangeCtrl', InterestRateChangeCtrl);
var InterestRateChangeNs;
(function (InterestRateChangeNs) {
    class RegularChangeModel {
    }
    InterestRateChangeNs.RegularChangeModel = RegularChangeModel;
    class SplitChangeModel {
    }
    InterestRateChangeNs.SplitChangeModel = SplitChangeModel;
    class PreviewModel {
    }
    InterestRateChangeNs.PreviewModel = PreviewModel;
    class PendingChangeModel {
    }
    InterestRateChangeNs.PendingChangeModel = PendingChangeModel;
})(InterestRateChangeNs || (InterestRateChangeNs = {}));
