app.controller('messageCtr', ['$scope', '$http', '$q', '$timeout', '$route', '$routeParams', 'mainService', '$location', function ($scope, $http, $q, $timeout, $route, $routeParams, mainService, $location) {
    mainService.currentMenuItemName = 'accounts'
    mainService.backUrl = null
    $scope.messageTypeCode = $routeParams.messageTypeCode
    $scope.hide = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        $('#savingsOverviewMessageModal').modal('hide')
        $('body').removeClass('modal-open')
        $('.modal-backdrop').remove()
        $location.path('/accounts')
    }
    $('#savingsOverviewMessageModal').modal('show')
    foo = $routeParams
}])