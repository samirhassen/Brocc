var app = angular.module('app', ['ntech.forms', 'ntech.components']);
var CustomsAccountsExportCtr = /** @class */ (function () {
    function CustomsAccountsExportCtr($scope, //ng.IScope
    $http, $q, $timeout) {
        this.$http = $http;
        this.$q = $q;
        this.$timeout = $timeout;
        var apiClient = new NTechSavingsApi.ApiClient(function (x) {
            toastr.error(x);
            $scope.isLoading = false;
        }, $http, $q);
        var skipDeliver = initialData.skipDeliver;
        $scope.skipDeliver = skipDeliver;
        $scope.p = {};
        $scope.backUrl = initialData.backUrl;
        $scope.createFile = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.isLoading = true;
            apiClient.createAndDeliverFinnishCustomsAccountsExportFile(skipDeliver).then(function (x) {
                $scope.gotoPage(0, evt);
            });
        };
        var tableHelper = new NTechTables.PagingHelper($q, $http);
        $scope.gotoPage = function (pageNr, evt) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.isLoading = true;
            apiClient.fetchFinnishCustomsAccountsExportFiles(50, pageNr).then(function (x) {
                $scope.files = {
                    Page: x.PageExports,
                    TotalNrOfPages: x.TotalPageCount
                };
                $scope.filesPaging = tableHelper.createPagingObjectFromPageResult({
                    CurrentPageNr: pageNr,
                    TotalNrOfPages: x.TotalPageCount
                });
                $scope.isLoading = false;
            });
        };
        $scope.toggleStatus = function (file, evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (!file) {
                return;
            }
            if (file.localDetails) {
                file.localDetails = null;
                return;
            }
            if (file.CustomData) {
                var customDataParsed = JSON.parse(file.CustomData);
                var customDataItems = [];
                for (var _i = 0, _a = Object.keys(customDataParsed); _i < _a.length; _i++) {
                    var key = _a[_i];
                    if (customDataParsed[key]) {
                        customDataItems.push({ key: key, value: customDataParsed[key] });
                    }
                }
                file.localDetails = {
                    items: customDataItems
                };
            }
        };
        $scope.gotoPage(0, null);
        window.scope = $scope;
    }
    CustomsAccountsExportCtr.$inject = ['$scope', '$http', '$q', '$timeout'];
    return CustomsAccountsExportCtr;
}());
app.controller('ctr', CustomsAccountsExportCtr);
