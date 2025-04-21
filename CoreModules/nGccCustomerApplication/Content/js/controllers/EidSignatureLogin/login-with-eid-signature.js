var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms'])
ntech.angular.setupTranslation(app)

app.controller('ctr', ['$scope', '$http', '$q', '$translate', '$timeout', function ($scope, $http, $q, $translate, $timeout) {
    window.scope = $scope
    $scope.targetName = initialData.targetName
    $scope.isSavingsApplicationActive = initialData.isSavingsApplicationActive
    $scope.baseCountry = initialData.baseCountry
    if (initialData.prePopulateCivicRegNr) {
        $scope.civicRegNr = initialData.prePopulateCivicRegNr
    }

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
            return false;
        if ($scope.baseCountry === 'SE') {
            return ntech.se.isValidCivicNr(value)
        } else {
            return ntech.fi.isValidCivicNr(value)
        }
        
    }

    $scope.onSubmit = function (evt) {
        if ($scope.fc.$invalid) {
            evt.preventDefault()
            $scope.fc.setSubmitted()
        }
    }
}])