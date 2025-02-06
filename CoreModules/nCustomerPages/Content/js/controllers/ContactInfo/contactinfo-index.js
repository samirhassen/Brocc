var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms'])

ntech.angular.setupTranslation(app)

app.controller('ctr', ['$scope', '$http', '$q', '$translate', '$timeout', function ($scope, $http, $q, $translate, $timeout) {
    window.scope = $scope
    $scope.ci = initialData.customerContactInfo
    $scope.productsOverviewUrl = initialData.productsOverviewUrl
}])