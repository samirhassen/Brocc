var app = angular.module('app', ['ntech.forms']);
app
    .controller('ctr', ['$scope', '$http', '$timeout', function ($scope, $http, $timeout) {
        $scope.p = {};
        $scope.backUrl = initialData.backUrl;
        $scope.pending = initialData.pending;
        $scope.createFile = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.isLoading = true;
            $http({
                method: 'POST',
                url: initialData.createFileUrl,
                data: {}
            }).then(function successCallback(response) {
                toastr.info('File created');
                //NOTE: Dont set loading to false here as we want it to keep showing while the page reloads
                location.reload();
            }, function errorCallback(response) {
                $scope.isLoading = false;
                toastr.error(response.statusText, 'Error');
            });
        };
        $scope.gotoPage = function (pageNr, evt) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.isLoading = true;
            $http({
                method: 'POST',
                url: initialData.getOutgoingFilesPageUrl,
                data: { pageSize: 50, pageNr: pageNr }
            }).then(function successCallback(response) {
                $scope.isLoading = false;
                $scope.files = response.data;
                updatePaging();
            }, function errorCallback(response) {
                $scope.isLoading = false;
                toastr.error(response.statusText, 'Error');
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
        $scope.gotoPage(0);
        window.scope = $scope;
    }]);
