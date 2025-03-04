(function (angular) {
    'use strict';
    angular.module('ntech.components').component('notificationslist', {
        templateUrl: 'notificationslist.html',
        bindings: {
            notifications: '=',
            ngDisabled: '=',
            onClickNotification: '<'
        }
    });
})(window.angular);
