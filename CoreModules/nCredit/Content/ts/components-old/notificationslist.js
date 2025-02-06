(function (angular) {
    'use strict';

    angular.module('ntech.components').component('notificationslist', {
        templateUrl: 'notificationslist.html', //In Shared/Component_Notificationslist.cshtml
        bindings: {
            notifications: '=',
            ngDisabled: '=',
            onClickNotification: '<'
        }
    })
})(window.angular);