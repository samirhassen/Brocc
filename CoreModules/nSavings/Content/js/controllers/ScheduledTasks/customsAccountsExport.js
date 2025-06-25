var app = angular.module('app', ['ntech.forms', 'ntech.components']);
class CustomsAccountsExportCtr {
    constructor($scope, //ng.IScope
    $http, $q, $timeout) {
        this.$http = $http;
        this.$q = $q;
        this.$timeout = $timeout;
        let apiClient = new NTechSavingsApi.ApiClient(x => {
            toastr.error(x);
            $scope.isLoading = false;
        }, $http, $q);
        let skipDeliver = initialData.skipDeliver;
        $scope.skipDeliver = skipDeliver;
        $scope.p = {};
        $scope.backUrl = initialData.backUrl;
        $scope.createFile = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.isLoading = true;
            apiClient.createAndDeliverFinnishCustomsAccountsExportFile(skipDeliver).then(x => {
                $scope.gotoPage(0, evt);
            });
        };
        let tableHelper = new NTechTables.PagingHelper($q, $http);
        $scope.gotoPage = (pageNr, evt) => {
            if (evt) {
                evt.preventDefault();
            }
            $scope.isLoading = true;
            apiClient.fetchFinnishCustomsAccountsExportFiles(50, pageNr).then(x => {
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
        $scope.toggleStatus = (file, evt) => {
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
                let customDataParsed = JSON.parse(file.CustomData);
                let customDataItems = [];
                for (let key of Object.keys(customDataParsed)) {
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
}
CustomsAccountsExportCtr.$inject = ['$scope', '$http', '$q', '$timeout'];
app.controller('ctr', CustomsAccountsExportCtr);
