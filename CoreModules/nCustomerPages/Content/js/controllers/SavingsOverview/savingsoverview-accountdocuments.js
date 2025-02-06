app.controller('accountdocumentsCtr', ['$scope', '$http', '$q', '$timeout', '$route', '$routeParams', 'mainService', 'ctrData', function ($scope, $http, $q, $timeout, $route, $routeParams, mainService, ctrData) {
    mainService.currentMenuItemName = 'accountdocuments'    
    mainService.backUrl = null
    $scope.d = { Documents : ctrData.Documents }
    window.accountdocumentsScope = $scope
}])