var app = angular.module('app', ['ntech.forms', 'ntech.components']);
var TreasuryAMLCtr = /** @class */ (function () {
    function TreasuryAMLCtr($scope, //ng.IScope
    $http, $q, $timeout) {
        this.$http = $http;
        this.$q = $q;
        this.$timeout = $timeout;
        $scope.p = {};
        $scope.backUrl = initialData.backUrl;
        $scope.exportProfileName = initialData.exportProfileName;
        $scope.createFile = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.isLoading = true;
            $http({
                method: 'POST',
                url: initialData.createExportUrl,
                data: {}
            }).then(function successCallback(response) {
                $scope.isLoading = false;
                $scope.result = response.data;
                $scope.gotoPage(0, { FromDate: $scope.dateSearch.fromDate, ToDate: $scope.dateSearch.toDate }, evt);
            }, function errorCallback(response) {
                $scope.isLoading = false;
                toastr.error(response.statusText, 'Error');
            });
        };
        var tableHelper = new NTechTables.PagingHelper($q, $http);
        $scope.gotoPage = function (pageNr, filter, evt) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.isLoading = true;
            tableHelper.gotoPage(pageNr, 50, initialData.getFilesPageUrl, filter, null).then(function (response) {
                $scope.isLoading = false;
                $scope.files = response.pagesData;
                $scope.filesPaging = response.pagingObject;
            }, function (err) {
                $scope.isLoading = false;
                toastr.error(err, 'Error');
            });
        };
        function updatePaging() {
            if (!$scope.files) {
                return {};
            }
            var h = $scope.files;
            var p = [];
            //9 items including separators are the most shown at one time
            //The two items before and after the current item are shown
            //The first and last item are always shown
            for (var i = 0; i < h.TotalNrOfPages; i++) {
                if (i >= (h.CurrentPageNr - 2) && i <= (h.CurrentPageNr + 2) || h.TotalNrOfPages <= 9) {
                    p.push({ pageNr: i, isCurrentPage: h.CurrentPageNr == i }); //Primary pages are always visible
                }
                else if (i == 0 || i == (h.TotalNrOfPages - 1)) {
                    p.push({ pageNr: i, isCurrentPage: h.CurrentPageNr == i }); //First and last page are always visible
                }
                else if (i == (h.CurrentPageNr - 3) || i == (h.CurrentPageNr + 3)) {
                    p.push({ pageNr: i, isSeparator: true }); //First and last page are always visible
                }
            }
            $scope.filesPaging = {
                pages: p,
                isPreviousAllowed: h.CurrentPageNr > 0,
                previousPageNr: h.CurrentPageNr - 1,
                isNextAllowed: h.CurrentPageNr < (h.TotalNrOfPages - 1),
                nextPageNr: h.CurrentPageNr + 1
            };
        }
        function setupDateSearch(today, timeIntervalInDays, scope) {
            return {
                fromDate: moment(today, 'YYYY-MM-DD', true).add(-timeIntervalInDays, 'days').format('YYYY-MM-DD'),
                toDate: moment(today, 'YYYY-MM-DD', true).format('YYYY-MM-DD'),
                search: function (evt) {
                    if (evt) {
                        evt.preventDefault();
                    }
                    scope.gotoPage(0, { FromDate: scope.dateSearch.fromDate, ToDate: scope.dateSearch.toDate }, evt);
                }
            };
        }
        $scope.dateSearch = setupDateSearch(initialData.today, 10, $scope);
        $scope.isValidDate = function (value) {
            if (ntech.forms.isNullOrWhitespace(value))
                return true;
            var d = moment(value, 'YYYY-MM-DD', true);
            return d.isValid();
        };
        $scope.dateSearch.search();
        window.scope = $scope;
    }
    TreasuryAMLCtr.$inject = ['$scope', '$http', '$q', '$timeout'];
    return TreasuryAMLCtr;
}());
app.controller('ctr', TreasuryAMLCtr);
