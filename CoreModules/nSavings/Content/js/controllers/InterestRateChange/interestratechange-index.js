var InterestRateChangeCtrl = /** @class */ (function () {
    function InterestRateChangeCtrl($scope, $http, $q, $interval) {
        var _this = this;
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
        this.$interval(function () {
            _this.doBackgroundStateUpdate();
        }, 1000);
    }
    InterestRateChangeCtrl.prototype.isValidDecimal = function (value) {
        return ntech.forms.isValidDecimal(value);
    };
    InterestRateChangeCtrl.prototype.asMoment = function (value) {
        var v = moment(value, 'YYYY-MM-DD', true);
        if (v.isValid()) {
            return v;
        }
        else {
            return null;
        }
    };
    InterestRateChangeCtrl.prototype.isValidDate = function (value) {
        if (ntech.forms.isNullOrWhitespace(value))
            return true;
        return moment(value, 'YYYY-MM-DD', true).isValid();
    };
    InterestRateChangeCtrl.prototype.isLowering = function (fromRate, toRate) {
        /*Floating point math is evil*/
        return Math.round(toRate * 100) < Math.round(fromRate * 100);
    };
    InterestRateChangeCtrl.prototype.calculateRegular = function (event) {
        if (event) {
            event.preventDefault();
        }
        var today = this.asMoment(initialData.today);
        var earliestAllowedAllAccountsLoweredDate = this.asMoment(initialData.earliestAllowedAllAccountsLoweredDate);
        var earliestAllowedNewAccountsOrRaisedDate = this.asMoment(initialData.earliestAllowedNewAccountsOrRaisedDate);
        var isLowering = false;
        var newValidFromDate = this.asMoment(this.regular.validFromDate);
        if (newValidFromDate <= today) {
            toastr.warning('From date must be after today');
            return;
        }
        var newInterestRate = parseFloat(parseFloat(this.regular.newInterestRate.replace(',', '.')).toFixed(2));
        if (this.currentNewAccountsInterestRate) {
            var currentInterestRate = this.currentNewAccountsInterestRate.InterestRatePercent;
            isLowering = this.isLowering(currentInterestRate, newInterestRate);
        }
        var p = new InterestRateChangeNs.PreviewModel();
        p.isRegularChange = true;
        p.initiatedDate = today;
        p.allAccountsDate = newValidFromDate;
        p.newAccountsDate = null;
        p.newInterestRate = newInterestRate;
        p.showRegularChangeLoweringTwoMonthWarning = isLowering && newValidFromDate < earliestAllowedAllAccountsLoweredDate;
        this.preview = p;
    };
    InterestRateChangeCtrl.prototype.calculateSplit = function (event) {
        if (event) {
            event.preventDefault();
        }
        var today = this.asMoment(initialData.today);
        var earliestAllowedAllAccountsLoweredDate = this.asMoment(initialData.earliestAllowedAllAccountsLoweredDate);
        var earliestAllowedNewAccountsOrRaisedDate = this.asMoment(initialData.earliestAllowedNewAccountsOrRaisedDate);
        var isLowering = false;
        var newAllAccountsValidFromDate = this.asMoment(this.split.validFromDateExistingAccounts);
        var newNewAccountsValidFromDate = this.asMoment(this.split.validFromDateNewAccounts);
        if (newAllAccountsValidFromDate <= today || newNewAccountsValidFromDate <= today) {
            toastr.warning('From date must be after today');
            return;
        }
        var newInterestRate = parseFloat(parseFloat(this.split.newInterestRate.replace(',', '.')).toFixed(2));
        if (this.currentNewAccountsInterestRate) {
            var currentInterestRate = this.currentNewAccountsInterestRate.InterestRatePercent;
            isLowering = this.isLowering(currentInterestRate, newInterestRate);
        }
        var p = new InterestRateChangeNs.PreviewModel();
        p.isRegularChange = false;
        p.initiatedDate = today;
        p.allAccountsDate = newAllAccountsValidFromDate;
        p.newAccountsDate = newNewAccountsValidFromDate;
        p.newInterestRate = newInterestRate;
        p.showSplitChangeLoweringTwoMonthWarning = isLowering && newAllAccountsValidFromDate < earliestAllowedAllAccountsLoweredDate;
        p.showSplitChangeSameDateWarning = newAllAccountsValidFromDate.isSame(newNewAccountsValidFromDate);
        this.preview = p;
    };
    InterestRateChangeCtrl.prototype.initiateChange = function (event) {
        var _this = this;
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
        }).then(function (response) {
            _this.isLoading = false;
            _this.historicalChangeItems = null;
            _this.setUiStateFromChange(response.data.currentChangeState);
        }, function (response) {
            _this.isLoading = false;
            toastr.error(response.statusText);
        });
    };
    InterestRateChangeCtrl.prototype.cancelVerifyOrChange = function (url, event) {
        var _this = this;
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
        }).then(function (response) {
            _this.isLoading = false;
            _this.historicalChangeItems = null;
            _this.setUiStateFromChange(response.data.currentChangeState);
        }, function (response) {
            _this.isLoading = false;
            toastr.error(response.statusText);
        });
    };
    InterestRateChangeCtrl.prototype.cancelChange = function (event) {
        this.cancelVerifyOrChange(initialData.urls.cancelChange, event);
    };
    InterestRateChangeCtrl.prototype.verifyChange = function (event) {
        this.cancelVerifyOrChange(initialData.urls.verifyChange, event);
    };
    InterestRateChangeCtrl.prototype.rejectChange = function (event) {
        this.cancelVerifyOrChange(initialData.urls.rejectChange, event);
    };
    InterestRateChangeCtrl.prototype.carryOutChange = function (event) {
        var _this = this;
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
        }).then(function (response) {
            _this.isLoading = false;
            _this.historicalChangeItems = null;
            _this.upcomingChanges = response.data.upcomingChanges;
            _this.setUiStateFromChange(response.data.currentChangeState);
        }, function (response) {
            _this.isLoading = false;
            toastr.error(response.statusText);
        });
    };
    InterestRateChangeCtrl.prototype.cancelUpcomingChange = function (id, event) {
        var _this = this;
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
        }).then(function (response) {
            _this.isLoading = false;
            _this.upcomingChanges = response.data.upcomingChanges;
            _this.historicalChangeItems = null;
            _this.setUiStateFromChange(response.data.currentChangeState);
        }, function (response) {
            _this.isLoading = false;
            toastr.error(response.statusText);
        });
    };
    InterestRateChangeCtrl.prototype.toggleHistoricalChangeItems = function (event) {
        if (event) {
            event.preventDefault();
        }
        if (this.historicalChangeItems) {
            this.historicalChangeItems = null;
        }
        else {
            this.fetchHistoricalChangeItems(event);
        }
    };
    InterestRateChangeCtrl.prototype.fetchHistoricalChangeItems = function (event) {
        var _this = this;
        if (event) {
            event.preventDefault();
        }
        this.isLoading = true;
        this.$http({
            method: 'POST',
            url: initialData.urls.fetchHistoricalChangeItems,
            data: {}
        }).then(function (response) {
            _this.isLoading = false;
            _this.historicalChangeItems = response.data.historicalChangeItems;
        }, function (response) {
            _this.isLoading = false;
            toastr.error(response.statusText);
        });
    };
    InterestRateChangeCtrl.prototype.setUiStateFromChange = function (s) {
        var m = this.convertResponseToPendingModel(s);
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
    };
    InterestRateChangeCtrl.prototype.doBackgroundStateUpdate = function () {
        var _this = this;
        this.$http({
            method: 'POST',
            url: initialData.urls.getCurrentChangeState,
            data: {
                testUserId: initialData.testUserId,
                changeToken: this.pending ? this.pending.changeToken : null
            }
        }).then(function (response) {
            var stateBefore = _this.pending;
            var stateAfter = response.data.currentChangeState;
            if (stateBefore && !stateAfter) {
                //Canceled, we need to actually update here
                _this.setUiStateFromChange(response.data.currentChangeState);
            }
            else if (!stateBefore && stateAfter) {
                //Most likely someelse initiated something. We should update here, even if the current user is doing something
                _this.setUiStateFromChange(response.data.currentChangeState);
            }
            else if (stateBefore && stateAfter) {
                //Synch
                _this.setUiStateFromChange(response.data.currentChangeState);
            }
            else {
                //No state before or after. Dont update here or we will wipe out any attempt of a user to initiate change on each synch
            }
        }, function (response) {
            toastr.error(response.statusText);
        });
    };
    InterestRateChangeCtrl.prototype.convertResponseToPendingModel = function (s) {
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
    };
    InterestRateChangeCtrl.$inject = ['$scope', '$http', '$q', '$interval'];
    return InterestRateChangeCtrl;
}());
var app = angular.module('app', ['ntech.forms']);
app.controller('interestRateChangeCtrl', InterestRateChangeCtrl);
var InterestRateChangeNs;
(function (InterestRateChangeNs) {
    var RegularChangeModel = /** @class */ (function () {
        function RegularChangeModel() {
        }
        return RegularChangeModel;
    }());
    InterestRateChangeNs.RegularChangeModel = RegularChangeModel;
    var SplitChangeModel = /** @class */ (function () {
        function SplitChangeModel() {
        }
        return SplitChangeModel;
    }());
    InterestRateChangeNs.SplitChangeModel = SplitChangeModel;
    var PreviewModel = /** @class */ (function () {
        function PreviewModel() {
        }
        return PreviewModel;
    }());
    InterestRateChangeNs.PreviewModel = PreviewModel;
    var PendingChangeModel = /** @class */ (function () {
        function PendingChangeModel() {
        }
        return PendingChangeModel;
    }());
    InterestRateChangeNs.PendingChangeModel = PendingChangeModel;
})(InterestRateChangeNs || (InterestRateChangeNs = {}));
