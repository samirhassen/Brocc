var app = angular.module('app', ['ntech.forms', 'ntech.components']);

class CustomsAccountsExportCtr {
    static $inject = ['$scope', '$http', '$q', '$timeout']
    constructor(
        $scope: any, //ng.IScope
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $timeout: ng.ITimeoutService
    ) {
        let apiClient = new NTechSavingsApi.ApiClient(x => {
            toastr.error(x);
            $scope.isLoading = false;
        }, $http, $q)

        let skipDeliver: boolean = initialData.skipDeliver

        $scope.skipDeliver = skipDeliver
        $scope.p = {}
        $scope.backUrl = initialData.backUrl

        $scope.createFile = function (evt) {
            if (evt) {
                evt.preventDefault()
            }
            $scope.isLoading = true
            apiClient.createAndDeliverFinnishCustomsAccountsExportFile(skipDeliver).then(x => {
                $scope.gotoPage(0, evt)
            })
        }

        let tableHelper = new NTechTables.PagingHelper($q, $http);

        $scope.gotoPage = (pageNr, evt) => {
            if (evt) {
                evt.preventDefault()
            }
            $scope.isLoading = true
            apiClient.fetchFinnishCustomsAccountsExportFiles(50, pageNr).then(x => {
                $scope.files = {
                    Page: x.PageExports,
                    TotalNrOfPages: x.TotalPageCount
                }
                $scope.filesPaging = tableHelper.createPagingObjectFromPageResult({
                    CurrentPageNr: pageNr,
                    TotalNrOfPages: x.TotalPageCount
                })
                $scope.isLoading = false
            })
        }

        $scope.toggleStatus = (file: any, evt: Event) => {
            if (evt) {
                evt.preventDefault()
            }
            if (!file) {
                return
            }
            if (file.localDetails) {
                file.localDetails = null
                return
            }

            if (file.CustomData) {
                let customDataParsed = JSON.parse(file.CustomData)
                let customDataItems = []
                for (let key of Object.keys(customDataParsed)) {
                    if (customDataParsed[key]) {
                        customDataItems.push({ key: key, value: customDataParsed[key] })
                    }
                }
                file.localDetails = {
                    items: customDataItems
                }
            }
        }

        $scope.gotoPage(0, null)

        window.scope = $scope
    }
}

app.controller('ctr', CustomsAccountsExportCtr);