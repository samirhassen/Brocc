var app = angular.module('app', ['ntech.forms']);
app
.controller('ctr', ['$scope', '$http', '$timeout', function ($scope, $http, $timeout) {
    $scope.p = {}
    $scope.backUrl = initialData.backUrl
    $scope.timeSlots = initialData.timeSlots
    $scope.latestRuns = initialData.latestRuns
    
    function isNullOrWhitespace(input) {
        if (typeof input === 'undefined' || input == null) return true;

        if ($.type(input) === 'string') {
            return $.trim(input).length < 1;
        } else {
            return false
        }
    }

    $scope.isValidPositiveDecimal = function (value) {
        if (isNullOrWhitespace(value))
            return true;
        var v = value.toString()
        return (/^([0]|[1-9]([0-9])*)([\.|,]([0-9])+)?$/).test(v)
    }

    $scope.isValidDate = function (value) {
        if (isNullOrWhitespace(value))
            return true;
        return moment(value, "YYYY-MM-DD", true).isValid()
    }

    $scope.gotoPage = function (pageNr, evt) {
        if (evt) {
            evt.preventDefault()
        }
        $scope.isLoading = true
        $http({
            method: 'POST',
            url: initialData.getHistoryPageUrl,
            data: { pageSize: 20, pageNr: pageNr }
        }).then(function successCallback(response) {
            $scope.isLoading = false
            $scope.historicRuns = response.data
            updatePaging()
        }, function errorCallback(response) {
            $scope.isLoading = false
            $scope.historicRuns = null
            toastr.error(response.statusText, 'Error')
        })
    }

    $scope.triggerManually = function (run, evt) {
        if (evt) {
            evt.preventDefault()
        }

        $scope.isLoading = true
        $http({
            method: 'POST',
            url: initialData.triggerManuallyUrl,
            data: { serviceName: run.JobName }
        }).then(function successCallback(response) {
            angular.extend(run, response.data)
            $scope.isLoading = false
        }, function errorCallback(response) {
            $scope.isLoading = false
            $scope.historicRuns = null
        })
    }

    function updatePaging() {
        if (!$scope.historicRuns) {
            return {}
        }
        var h = $scope.historicRuns
        var p = []

        //9 items including separators are the most shown at one time
        //The two items before and after the current item are shown
        //The first and last item are always shown
        for (var i = 0; i < h.TotalNrOfPages; i++) {
            if (i >= (h.CurrentPageNr - 2) && i <= (h.CurrentPageNr + 2) || h.TotalNrOfPages <= 9) {
                p.push({ pageNr: i, isCurrentPage: h.CurrentPageNr == i }) //Primary pages are always visible
            } else if (i == 0 || i == (h.TotalNrOfPages - 1)) {
                p.push({ pageNr: i, isCurrentPage: h.CurrentPageNr == i }) //First and last page are always visible
            } else if (i == (h.CurrentPageNr - 3) || i == (h.CurrentPageNr + 3)) {
                p.push({ pageNr: i, isSeparator: true }) //First and last page are always visible
            }
        }

        $scope.historicRunsPaging = {
            pages: p,
            isPreviousAllowed: h.CurrentPageNr > 0,
            previousPageNr: h.CurrentPageNr - 1,
            isNextAllowed: h.CurrentPageNr < (h.TotalNrOfPages - 1),
            nextPageNr: h.CurrentPageNr + 1
        }
    }

    $scope.loadHistory = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        $scope.gotoPage(0)
    }    

    window.scope = $scope
}])
