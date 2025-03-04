var PreCollectionManagementHistoryCtrl = /** @class */ (function () {
    function PreCollectionManagementHistoryCtrl($scope, $http, $timeout, $q) {
        this.$http = $http;
        this.$timeout = $timeout;
        this.$q = $q;
        this.apiClient = new NTechCreditApi.ApiClient(function (x) { return toastr.error(x); }, $http, $q);
        this.backUrl = initialData.backUrl;
        this.pending = initialData.pending;
        this.fromCreatedDate = this.today().add(-10, 'days').format('YYYY-MM-DD');
        this.toCreatedDate = this.today().format('YYYY-MM-DD');
        this.gotoPage(0, { FromDate: this.fromCreatedDate, ToDate: this.toCreatedDate, WorkListTypeName: 'PreCollection1', OnlyClosed: true }, null);
        window.scope = this;
    }
    PreCollectionManagementHistoryCtrl.prototype.today = function () {
        return moment(initialData.today, 'YYYY-MM-DD', true);
    };
    PreCollectionManagementHistoryCtrl.prototype.isValidDate = function (value) {
        if (ntech.forms.isNullOrWhitespace(value))
            return true;
        var d = moment(value, 'YYYY-MM-DD', true);
        return d.isValid();
    };
    PreCollectionManagementHistoryCtrl.prototype.onBack = function (evt) {
        if (evt) {
            evt.preventDefault();
        }
        NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, this.apiClient, this.$q, {});
    };
    PreCollectionManagementHistoryCtrl.prototype.gotoPage = function (pageNr, filter, evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        this.isLoading = true;
        this.$http({
            method: 'POST',
            url: initialData.getFilesPageUrl,
            data: { pageSize: 50, pageNr: pageNr, filter: filter }
        }).then(function (response) {
            _this.isLoading = false;
            _this.files = response.data;
            _this.updatePaging();
        }, function (response) {
            _this.isLoading = false;
            toastr.error(response.statusText, 'Error');
        });
    };
    PreCollectionManagementHistoryCtrl.prototype.updatePaging = function () {
        if (!this.files) {
            return {};
        }
        var h = this.files;
        var p = [];
        //9 items including separators are the most shown at one time
        //The two items before and after the current item are shown
        //The first and last item are always shown
        for (var i = 0; i < h.TotalNrOfPages; i++) {
            if (i >= (h.CurrentPageNr - 2) && i <= (h.CurrentPageNr + 2) || h.TotalNrOfPages <= 9) {
                p.push({ pageNr: i, isCurrentPage: h.CurrentPageNr == i, isSeparator: false }); //Primary pages are always visible
            }
            else if (i == 0 || i == (h.TotalNrOfPages - 1)) {
                p.push({ pageNr: i, isCurrentPage: h.CurrentPageNr == i, isSeparator: false }); //First and last page are always visible
            }
            else if (i == (h.CurrentPageNr - 3) || i == (h.CurrentPageNr + 3)) {
                p.push({ pageNr: i, isCurrentPage: false, isSeparator: true }); //First and last page are always visible
            }
        }
        this.filesPaging = {
            pages: p,
            isPreviousAllowed: h.CurrentPageNr > 0,
            previousPageNr: h.CurrentPageNr - 1,
            isNextAllowed: h.CurrentPageNr < (h.TotalNrOfPages - 1),
            nextPageNr: h.CurrentPageNr + 1
        };
    };
    PreCollectionManagementHistoryCtrl.$inject = ['$scope', '$http', '$timeout', '$q'];
    return PreCollectionManagementHistoryCtrl;
}());
var app = angular.module('app', ['ntech.forms']);
app.controller('preCollectionManagementHistoryCtrl', PreCollectionManagementHistoryCtrl);
var PreCollectionManagementHistoryNs;
(function (PreCollectionManagementHistoryNs) {
    var PagingObject = /** @class */ (function () {
        function PagingObject() {
        }
        return PagingObject;
    }());
    PreCollectionManagementHistoryNs.PagingObject = PagingObject;
    var PagingObjectPage = /** @class */ (function () {
        function PagingObjectPage() {
        }
        return PagingObjectPage;
    }());
    PreCollectionManagementHistoryNs.PagingObjectPage = PagingObjectPage;
})(PreCollectionManagementHistoryNs || (PreCollectionManagementHistoryNs = {}));
