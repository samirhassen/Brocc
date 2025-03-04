var ErrorListCtrl = /** @class */ (function () {
    function ErrorListCtrl($scope, $http, $q, $timeout) {
        this.$scope = $scope;
        this.$http = $http;
        this.$q = $q;
        this.$timeout = $timeout;
        window.scope = this;
        this.backUrl = initialData.backUrl;
        this.fetchUnhandledItems(null);
        this.lastHistoryPageNr = null;
        this.userDisplayNameByUserId = initialData.userDisplayNameByUserId;
    }
    ErrorListCtrl.prototype.getUnhandledCount = function () {
        var count = 0;
        if (this.unhandled && this.unhandled.pageItems) {
            for (var _i = 0, _a = this.unhandled.pageItems; _i < _a.length; _i++) {
                var i = _a[_i];
                if (!i.IsHandledLocally) {
                    count = count + 1;
                }
            }
        }
        return count;
    };
    ErrorListCtrl.prototype.getUserDisplayNameByUserId = function (userId) {
        var v = this.userDisplayNameByUserId[userId];
        if (v) {
            return v;
        }
        else {
            return 'User ' + userId;
        }
    };
    ErrorListCtrl.prototype.fetchUnhandledItems = function (evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        this.fetchErrorListActionItems(false, 0, 100, false, function (r) {
            _this.unhandled = r;
        });
    };
    ErrorListCtrl.prototype.toggleHistory = function (evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        if (this.lastHistoryPageNr === null) {
            this.fetchHistoryPage(0, null, function () {
                _this.isHistoryVisible = true;
            });
        }
        else {
            this.isHistoryVisible = !this.isHistoryVisible;
        }
    };
    ErrorListCtrl.prototype.fetchHistoryPage = function (pageNr, evt, onSuccess) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        this.fetchErrorListActionItems(true, pageNr, 30, true, function (r) {
            if (pageNr == 0) {
                _this.loadedHistoryItems = r.pageItems;
            }
            else {
                for (var _i = 0, _a = r.pageItems; _i < _a.length; _i++) {
                    var x = _a[_i];
                    _this.loadedHistoryItems.push(x);
                }
            }
            _this.lastHistoryPageNr = pageNr;
            _this.isAllHistoryLoaded = _this.loadedHistoryItems.length >= r.totalCount;
            if (onSuccess) {
                onSuccess();
            }
        });
    };
    ErrorListCtrl.prototype.fetchErrorListActionItems = function (isHandled, pageNr, pageSize, isOrderedByHandledDate, onSuccess) {
        var _this = this;
        this.isLoading = true;
        this.$http({
            method: 'POST',
            url: initialData.fetchErrorListActionItemsUrl,
            data: {
                isHandled: isHandled,
                pageNr: pageNr,
                pageSize: pageSize,
                isOrderedByHandledDate: isOrderedByHandledDate
            }
        }).then(function (response) {
            _this.isLoading = false;
            onSuccess(response.data);
        }, function (response) {
            _this.isLoading = false;
            toastr.error(response.statusText);
        });
    };
    ErrorListCtrl.prototype.markActionAsHandled = function (item, evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        this.isLoading = true;
        this.$http({
            method: 'POST',
            url: initialData.markActionAsHandledUrl,
            data: {
                actionId: item.Id
            }
        }).then(function (response) {
            _this.isLoading = false;
            item.IsHandledLocally = true;
            _this.isHistoryVisible = false;
            _this.loadedHistoryItems = null;
            _this.lastHistoryPageNr = null;
            _this.isAllHistoryLoaded = false;
        }, function (response) {
            _this.isLoading = false;
            toastr.error(response.statusText);
        });
    };
    ErrorListCtrl.prototype.unlockHistoryItem = function (item, evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        this.isLoading = true;
        this.$http({
            method: 'POST',
            url: initialData.fetchActionDetailsUrl,
            data: { actionId: item.Id }
        }).then(function (response) {
            item.ItemDetails = response.data;
            _this.isLoading = false;
        }, function (response) {
            _this.isLoading = false;
            toastr.error('Failed!');
        });
    };
    ErrorListCtrl.$inject = ['$scope', '$http', '$q'];
    return ErrorListCtrl;
}());
var app = angular.module('app', ['ntech.forms', 'ntech.components']);
app.controller('errorListCtrl', ErrorListCtrl);
