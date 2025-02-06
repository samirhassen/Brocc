app.controller('searchCtr', ['$scope', '$http', '$q', '$location', 'mainService', function ($scope, $http, $q, $location, mainService) {
    $scope.searchInput = {}

    $scope.isValidCivicNr = function (value) {
        if (isNullOrWhitespace(value))
            return true;
        return ntech.clientCountry.isValidCivicNr(value)
    }

    function isNullOrWhitespace(input) {
        if (typeof input === 'undefined' || input == null) return true;

        if ($.type(input) === 'string') {
            return $.trim(input).length < 1;
        } else {
            return false
        }
    }

    $scope.onOmnisearchKeyUp = function (evt) {
        if (evt.keyCode == 13) {
            $scope.searchSavingsAccount({ omniSearchValue: $scope.searchInput.omniSearchValue }, evt)
        }
    }

    $scope.searchSavingsAccount = function (input, evt) {
        if (evt) {
            evt.preventDefault()
        }
        if (!$scope.searchform.$valid) {
            //NOTE: User should never actually get here, this is just guard code.
            toastr.warning('Invalid search data')
            return
        }
        $location.path('/')
        mainService.isLoading = true
        $http({
            method: 'POST',
            url: initialData.searchUrl,
            data: input
        }).then(function successCallback(response) {
            mainService.isLoading = false
            if (response.data.hits && response.data.hits.length == 1) {
                $location.path('/Details/' + response.data.hits[0].SavingsAccountNr)
            } else {
                $scope.searchhits = response.data.hits
            }
        }, function errorCallback(response) {
            $scope.searchhits = null
            mainService.isLoading = false
            toastr.error('Failed!')
        })
    }

    $scope.$watch(function () { return $location.$$path }, function () {
        $scope.searchhits = null
    })

    if (ntechClientCountry === 'FI') {
        $scope.civicRegNrMask = '(DDMMYYSNNNK)'
    } else if (ntechClientCountry === 'SE') {
        $scope.civicRegNrMask = '(YYYYMMDDRRRC)'
    }

    window.searchScope = $scope
}])