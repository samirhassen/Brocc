app.controller('openNotificationsCtr', ['$scope', '$http', '$q', '$timeout', '$route', '$routeParams', 'ctrData', 'mainService', function ($scope, $http, $q, $timeout, $route, $routeParams, ctrData, mainService) {
    mainService.currentMenuItemName = 'opennotifications'
    mainService.backUrl = null
    $scope.backUrl = mainService.backUrl
    var open = []
    var closed = []
    
    ctrData.Notifications.forEach(addNotifications);

    function addNotifications(n, index) {
        if (n.IsOpen)
            open.push(n)
        else
            closed.push(n)
    }

    $scope.NotificationGroups = [{
        IsOpen: true, Notifications: open
    }, {
            IsOpen: false, Notifications: closed
        }]
    window.openNotificationsScope = $scope
}])