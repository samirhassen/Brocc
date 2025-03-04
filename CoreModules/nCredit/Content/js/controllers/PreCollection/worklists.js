var WorkListsCtrl = /** @class */ (function () {
    function WorkListsCtrl($scope, $http, $q, $timeout, ntechComponentService) {
        this.$http = $http;
        this.$q = $q;
        this.$timeout = $timeout;
        this.ntechComponentService = ntechComponentService;
        window.scope = this;
        this.phoneListUrl = initialData.phoneListUrl;
        this.currentUserId = initialData.userId;
        this.setupWorklists(initialData.worklists);
        this.calculateModel = {};
        this.isAlternatePaymentPlansEnabled = initialData.isAlternatePaymentPlansEnabled;
    }
    WorkListsCtrl.prototype.setupWorklists = function (worklists) {
        var d = [];
        for (var _i = 0, worklists_1 = worklists; _i < worklists_1.length; _i++) {
            var w = worklists_1[_i];
            d.push({
                workListSummary: w.workListSummary,
                filterModel: this.formatFiltersForDisplay(w.filterSummary, w.workListSummary)
            });
        }
        this.worklists = d;
    };
    WorkListsCtrl.prototype.onBack = function (evt) {
        if (evt) {
            evt.preventDefault();
        }
        NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, new NTechCreditApi.ApiClient(toastr.error, this.$http, this.$q), this.$q);
    };
    WorkListsCtrl.prototype.formatFiltersForDisplay = function (filterSummary, workListSummary) {
        if (!filterSummary) {
            return null;
        }
        var result = {
            totalCount: workListSummary.TotalCount,
            filterShortText: '',
            filterCounts: []
        };
        var addToFilterShortText = function (s) {
            if (result.filterShortText.length > 0) {
                result.filterShortText = result.filterShortText + ', ';
            }
            result.filterShortText = result.filterShortText + s;
        };
        for (var _i = 0, _a = filterSummary.Filters; _i < _a.length; _i++) {
            var filter = _a[_i];
            if (filter.Name == 'NrOfDueDatesPassed') {
                var kk1NrOfDueDatesPassedItem = null;
                var kk2NrOfDueDatesPassedItem = null;
                var kk3NrOfDueDatesPassedItem = null;
                var kk4plusNrOfDueDatesPassedItem = null;
                if (filter.Value.indexOf('1') !== -1) {
                    kk1NrOfDueDatesPassedItem = {
                        displayName: 'KK1',
                        count: 0
                    };
                    addToFilterShortText('KK1');
                }
                if (filter.Value.indexOf('2') !== -1) {
                    kk2NrOfDueDatesPassedItem = {
                        displayName: 'KK2',
                        count: 0
                    };
                    addToFilterShortText('KK2');
                }
                if (filter.Value.indexOf('3') !== -1) {
                    kk3NrOfDueDatesPassedItem = {
                        displayName: 'KK3',
                        count: 0
                    };
                    addToFilterShortText('KK3');
                }
                if (filter.Value.indexOf('4+') !== -1) {
                    kk4plusNrOfDueDatesPassedItem = {
                        displayName: 'KK4+',
                        count: 0
                    };
                    addToFilterShortText('KK4+');
                }
                for (var _b = 0, _c = filterSummary.FilterDataNrOfDueDatesPassed; _b < _c.length; _b++) {
                    var entry = _c[_b];
                    if (entry.NrOfPassedDueDatesWithoutFullPaymentSinceNotification === 1) {
                        kk1NrOfDueDatesPassedItem.count = kk1NrOfDueDatesPassedItem.count + entry.Count;
                    }
                    if (entry.NrOfPassedDueDatesWithoutFullPaymentSinceNotification === 2) {
                        kk2NrOfDueDatesPassedItem.count = kk2NrOfDueDatesPassedItem.count + entry.Count;
                    }
                    if (entry.NrOfPassedDueDatesWithoutFullPaymentSinceNotification === 3) {
                        kk3NrOfDueDatesPassedItem.count = kk3NrOfDueDatesPassedItem.count + entry.Count;
                    }
                    if (entry.NrOfPassedDueDatesWithoutFullPaymentSinceNotification >= 4) {
                        kk4plusNrOfDueDatesPassedItem.count = kk4plusNrOfDueDatesPassedItem.count + entry.Count;
                    }
                }
                if (kk1NrOfDueDatesPassedItem != null) {
                    result.filterCounts.push(kk1NrOfDueDatesPassedItem);
                }
                if (kk2NrOfDueDatesPassedItem != null) {
                    result.filterCounts.push(kk2NrOfDueDatesPassedItem);
                }
                if (kk3NrOfDueDatesPassedItem != null) {
                    result.filterCounts.push(kk3NrOfDueDatesPassedItem);
                }
                if (kk4plusNrOfDueDatesPassedItem != null) {
                    result.filterCounts.push(kk4plusNrOfDueDatesPassedItem);
                }
            }
        }
        return result;
    };
    WorkListsCtrl.prototype.isAnyFilterSelected = function () {
        var m = this.calculateModel;
        var isAnyKkFilterSelected = m.kk1 || m.kk2 || m.kk3 || m.kk4plus;
        return isAnyKkFilterSelected;
    };
    WorkListsCtrl.prototype.calculate = function (evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        var kkFilterModel = {};
        var nrOfDueDatesPassedFilter = [];
        if (this.calculateModel.kk1) {
            nrOfDueDatesPassedFilter.push('1');
            kkFilterModel['kk1'] = true;
        }
        if (this.calculateModel.kk2) {
            nrOfDueDatesPassedFilter.push('2');
            kkFilterModel['kk2'] = true;
        }
        if (this.calculateModel.kk3) {
            nrOfDueDatesPassedFilter.push('3');
            kkFilterModel['kk3'] = true;
        }
        if (this.calculateModel.kk4plus) {
            nrOfDueDatesPassedFilter.push('4+');
            kkFilterModel['kk4plus'] = true;
        }
        var calculateRequestData = {
            nrOfDueDatesPassedFilter: nrOfDueDatesPassedFilter,
            includeActiveAlternatePaymentPlans: this.calculateModel.includeActiveAlternatePaymentPlans
        };
        this.isLoading = true;
        this.$http({
            method: 'POST',
            url: initialData.calculateWorkListUrl,
            data: calculateRequestData
        }).then(function (response) {
            _this.isLoading = false;
            var kkViewModel = { 'kk1': 0, 'kk2': 0, 'kk3': 0, 'kk4plus': 0 };
            var d = response.data;
            angular.forEach(d.countByNrOfPassedDueDatesWithoutFullPaymentSinceNotification, function (v, k) {
                if (k === '1') {
                    kkViewModel['kk1'] = kkViewModel['kk1'] + v;
                }
                else if (k === '2') {
                    kkViewModel['kk2'] = kkViewModel['kk2'] + v;
                }
                else if (k === '3') {
                    kkViewModel['kk3'] = kkViewModel['kk3'] + v;
                }
                else {
                    kkViewModel['kk4plus'] = kkViewModel['kk4plus'] + v;
                }
            });
            _this.calculateResult = {
                calculateRequestData: calculateRequestData,
                kkViewModel: kkViewModel,
                kkFilterModel: kkFilterModel,
                totalCount: d.totalCount
            };
        }, function (err) {
            toastr.error('Error');
            _this.isLoading = false;
        });
    };
    WorkListsCtrl.prototype.createWorkList = function (evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        this.isLoading = true;
        var d = angular.copy(this.calculateResult.calculateRequestData);
        d.testUserId = initialData.testUserId;
        d.backUrl = initialData.backUrl;
        d.includeWorkListsInResponse = true;
        this.$http({
            method: 'POST',
            url: initialData.createWorkListUrl,
            data: d,
        }).then(function (response) {
            var data = response.data;
            _this.setupWorklists(data.worklists);
            _this.calculateModel = {};
            _this.calculateResult = null;
            _this.isLoading = false;
        }, function (err) {
            toastr.error('Error');
            _this.isLoading = false;
        });
    };
    WorkListsCtrl.prototype.closeWorkList = function (w, evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        this.isLoading = true;
        this.$http({
            method: 'POST',
            url: initialData.closeWorkListUrl,
            data: { workListHeaderId: w.workListSummary.WorkListHeaderId, userId: this.currentUserId, includeWorkListsInResponse: true }
        }).then(function (response) {
            var data = response.data;
            _this.setupWorklists(data.worklists);
            _this.calculateModel = {};
            _this.calculateResult = null;
            _this.isLoading = false;
        }, function (response) {
            _this.isLoading = false;
            toastr.error('Failed!');
        });
    };
    WorkListsCtrl.prototype.openItem = function (wl, itemId, evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        this.isLoading = true;
        this.$http({
            method: 'POST',
            url: initialData.openWorkListItemUrl,
            data: { workListHeaderId: wl.workListSummary.WorkListHeaderId, userId: this.currentUserId, itemId: itemId }
        }).then(function (response) {
            angular.forEach(response.data.applicants, function (a) {
                a.customerCardUrl = ntech.forms.replaceAll(initialData.customerCardUrlPattern, 'NNNNN', a.customerId);
                a.phoneParsed = ntech.libphonenumber.parsePhoneNr(a.phone, ntechClientCountry);
            });
            _this.wl = {
                workListSummary: response.data.workListSummary,
                current: response.data,
                openedFrom: wl,
                commentsInitialData: { CreditNr: response.data.itemId }
            };
            $('#itemDialog').modal('show');
            _this.isLoading = false;
        }, function (response) {
            _this.isLoading = false;
            toastr.error('Failed!');
        });
    };
    WorkListsCtrl.prototype.closeOpenItem = function (evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        this.isLoading = true;
        this.$http({
            method: 'POST',
            url: initialData.replaceWorkListItemUrl,
            data: { workListHeaderId: this.wl.workListSummary.WorkListHeaderId, itemId: this.wl.current.itemId }
        }).then(function (response) {
            if (!response.data.wasReplaced) {
                toastr.warning('The item could not be replaced');
            }
            document.location.reload();
        }, function (response) {
            _this.isLoading = false;
            toastr.error('Failed!');
        });
    };
    WorkListsCtrl.prototype.takeAndOpenItem = function (wl, evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        this.takeItem(wl, evt, function (takenItemId) {
            _this.openItem(wl, takenItemId, null);
        });
    };
    WorkListsCtrl.prototype.takeItem = function (wl, evt, onItemTaken) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        this.isLoading = true;
        this.$http({
            method: 'POST',
            url: initialData.takeWorkListItemUrl,
            data: { workListHeaderId: wl.workListSummary.WorkListHeaderId, userId: this.currentUserId }
        }).then(function (response) {
            if (!response.data.wasItemTaken) {
                if (response.data.isConcurrencyProblem) {
                    toastr.warning('Take item failed but there might be more items. Please try again in a few seconds.');
                }
                else {
                    toastr.warning('There were no more items available');
                }
                document.location.reload();
            }
            else {
                onItemTaken(response.data.takenItemId);
            }
        }, function (response) {
            _this.isLoading = false;
            toastr.error('Failed!');
        });
    };
    WorkListsCtrl.prototype.beginEditPromisedToPayDate = function (evt) {
        if (evt) {
            evt.preventDefault();
        }
        if (this.wl.current.isPromisedToPayDateEditMode) {
            return;
        }
        this.wl.current.isPromisedToPayDateEditMode = true;
        if (this.wl.current.creditSummary.promisedToPayDate) {
            this.wl.current.promisedToPayDateEdit = this.wl.current.creditSummary.promisedToPayDate;
        }
    };
    WorkListsCtrl.prototype.cancelAddPromisedToPayDate = function (evt) {
        if (evt) {
            evt.preventDefault();
        }
        this.wl.current.isPromisedToPayDateEditMode = false;
        this.wl.current.promisedToPayDateEdit = null;
    };
    WorkListsCtrl.prototype.removePromisedToPayDate = function (evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        this.isLoading = true;
        this.$http({
            method: 'POST',
            url: '/Api/Credit/PromisedToPayDate/Remove',
            data: { creditNr: this.wl.current.itemId }
        }).then(function (response) {
            _this.isLoading = false;
            _this.wl.current.creditSummary.promisedToPayDate = null;
            _this.wl.current.isPromisedToPayDateEditMode = false;
            _this.wl.current.promisedToPayDateEdit = null;
            _this.ntechComponentService.emitNTechEvent('reloadComments', _this.wl.current.itemId);
        }, function (response) {
            _this.isLoading = false;
            toastr.error('Failed!');
        });
    };
    WorkListsCtrl.prototype.isValidPromisedToPayDate = function (value) {
        if (ntech.forms.isNullOrWhitespace(value))
            return false;
        return moment(value, "YYYY-MM-DD", true).isValid();
    };
    WorkListsCtrl.prototype.addPromisedToPayDate = function (evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        var editValue = this.wl.current.promisedToPayDateEdit;
        this.isLoading = true;
        if (!this.isValidPromisedToPayDate(editValue)) {
            var e = $('#promisedToPayDateEdit');
            e.popover({ content: 'Invalid date', animation: true });
            e.popover('show');
            this.$timeout(function () {
                e.popover('hide');
            }, 2000);
            this.isLoading = false;
            return;
        }
        this.$http({
            method: 'POST',
            url: '/Api/Credit/PromisedToPayDate/Add',
            data: { creditNr: this.wl.current.itemId, promisedToPayDate: editValue, avoidReaddingSameValue: true }
        }).then(function (response) {
            _this.isLoading = false;
            _this.wl.current.creditSummary.promisedToPayDate = editValue;
            _this.wl.current.isPromisedToPayDateEditMode = false;
            _this.wl.current.promisedToPayDateEdit = null;
            _this.ntechComponentService.emitNTechEvent('reloadComments', _this.wl.current.itemId);
        }, function (response) {
            _this.isLoading = false;
            toastr.error('Failed!');
        });
    };
    WorkListsCtrl.prototype.isNextOnActionAllowed = function (evt, hasFuturePromisedToPayDateCallback) {
        if (evt) {
            evt.preventDefault();
        }
        if (!this.wl || !this.wl.current || !this.wl.current.creditSummary) {
            return false;
        }
        var hasFuturePromisedToPayDate = false;
        if (this.wl.current.creditSummary.promisedToPayDate) {
            var ppdate = moment(this.wl.current.creditSummary.promisedToPayDate, 'YYYY-MM-DD', true);
            var today = moment(initialData.today, 'YYYY-MM-DD', true);
            hasFuturePromisedToPayDate = ppdate > today;
        }
        if (hasFuturePromisedToPayDateCallback) {
            hasFuturePromisedToPayDateCallback(hasFuturePromisedToPayDate);
        }
        return hasFuturePromisedToPayDate || this.wl.current.isSettlementDateAdded || this.wl.current.isNewTermsSent;
    };
    WorkListsCtrl.prototype.nextOnAction = function (evt) {
        var hadFuturePromisedToPayDate = false;
        this.isNextOnActionAllowed(null, function (h) { hadFuturePromisedToPayDate = h; });
        this.next({
            isSkipped: false,
            isNewTermsSent: this.wl.current.isNewTermsSent,
            isSettlementDateAdded: this.wl.current.isSettlementDateAdded,
            hadFuturePromisedToPayDate: hadFuturePromisedToPayDate,
            tryLaterChoice: null
        }, evt);
    };
    WorkListsCtrl.prototype.nextOnTryLater = function (evt) {
        this.next({
            isSkipped: false,
            isNewTermsSent: false,
            isSettlementDateAdded: false,
            hadFuturePromisedToPayDate: false,
            tryLaterChoice: this.wl.current.tryLaterChoice
        }, evt);
    };
    WorkListsCtrl.prototype.nextOnSkipped = function (evt) {
        this.next({
            isSkipped: true,
            isNewTermsSent: false,
            isSettlementDateAdded: false,
            hadFuturePromisedToPayDate: false,
            tryLaterChoice: null
        }, evt);
    };
    WorkListsCtrl.prototype.next = function (actionTaken, evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        this.isLoading = true;
        this.$http({
            method: 'POST',
            url: initialData.completeWorkListItemUrl,
            data: {
                workListHeaderId: this.wl.workListSummary.WorkListHeaderId, userId: this.currentUserId, itemId: this.wl.current.itemId, actionTaken: actionTaken
            }
        }).then(function (response) {
            if (response.data.wasCompleted) {
                _this.takeAndOpenItem(_this.wl.openedFrom, evt);
            }
            else {
                toastr.warning('Could not be completed!');
                document.location.reload();
            }
        }, function (response) {
            _this.isLoading = false;
            toastr.error('Failed!');
        });
    };
    WorkListsCtrl.prototype.getAlternatePaymentPlanStatusText = function () {
        if (!this.wl || !this.wl.current || !this.wl.current.creditSummary.alternatePaymentPlanStateCode) {
            return '';
        }
        var code = this.wl.current.creditSummary.alternatePaymentPlanStateCode;
        if (code === 'active') {
            return 'Active';
        }
        else if (code === 'activeButLate') {
            return 'Active but late';
        }
        else if (code === 'recentlyCancelled') {
            return 'Recently cancelled';
        }
        else {
            return code;
        }
    };
    WorkListsCtrl.$inject = ['$scope', '$http', '$q', '$timeout', 'ntechComponentService'];
    return WorkListsCtrl;
}());
var app = angular.module('app', ['ntech.forms', 'ntech.components']);
app.controller('workListsCtr', WorkListsCtrl);
var WorkListsNs;
(function (WorkListsNs) {
    var ActionTaken = /** @class */ (function () {
        function ActionTaken() {
        }
        return ActionTaken;
    }());
    WorkListsNs.ActionTaken = ActionTaken;
})(WorkListsNs || (WorkListsNs = {}));
$(function () {
    $("#itemDialog").on("hide.bs.modal", function (evt) {
        document.location.reload();
    });
});
