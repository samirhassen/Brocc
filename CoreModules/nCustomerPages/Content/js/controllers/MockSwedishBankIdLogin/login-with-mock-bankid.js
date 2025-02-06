var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms'])

ntech.angular.setupTranslation(app)

app.controller('ctr', ['$scope', '$http', '$q', '$translate', '$timeout', function ($scope, $http, $q, $translate, $timeout) {
    window.scope = $scope
    $scope.targetName = initialData.targetName
    $scope.isMortgageLoanApplicationActive = initialData.isMortgageLoanApplicationActive
    
    function isNullOrWhitespace(input) {
        if (typeof input === 'undefined' || input == null) return true;

        if ($.type(input) === 'string') {
            return $.trim(input).length < 1;
        } else {
            return false
        }
    }

    $scope.isValidCivicNr = function (value) {
        if (isNullOrWhitespace(value))
            return true;
        return ntech.se.isValidCivicNr(value)
    }

    $scope.onSubmit = function (evt) {
        $scope.isLoading = true
        if ($scope.fc.$invalid) {
            evt.preventDefault()
            $scope.fc.setSubmitted()
        }
    }

    $scope.generateAndUseTestCivicRegNr = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        var client = new NTechCustomerPagesApi.ApiClient(null, $http, $q)
        $scope.isLoading = true
        client.createTestPerson(true).then(function (result) {
            $scope.civicRegNr = result.civicRegNr
            $scope.isTestFunctionsVisible = false
            $scope.isLoading = false
        })
    }
}])