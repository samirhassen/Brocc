var app = angular.module('app', ['ntech.forms']);
app.controller('ctr', ['$scope', '$http', '$q', function ($scope, $http, $q) {
        $scope.hit = initialData;
        window.scope = $scope;
    }]);
