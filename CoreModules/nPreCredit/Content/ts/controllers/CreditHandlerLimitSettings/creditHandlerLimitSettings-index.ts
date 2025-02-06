var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components']);

class CreditHandlerLimitSettingsCtr {
    static $inject = ['$scope', '$http', '$q', '$timeout', '$translate']
    constructor(
        $scope: any, //ng.IScope
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $timeout: ng.ITimeoutService
    ) {
        $scope.levels = initialData.levels
        $scope.users = initialData.users

        $scope.onBack = (evt: Event) => {
            if (evt) {
                evt.preventDefault()
            }

            NavigationTargetHelper.handleBack(
                NavigationTargetHelper.create(initialData.backUrl, initialData.backTarget, null),
                new NTechPreCreditApi.ApiClient(toastr.error, $http, $q),
                $q,
                null)
        }

        $scope.beginEdit = function (user, evt) {
            if (evt) {
                evt.preventDefault()
            }
            user.edit = {
                LimitLevel: user.LimitLevel.toString(),
                IsOverrideAllowed: user.IsOverrideAllowed
            }
        }

        $scope.cancelEdit = function (user, evt) {
            if (evt) {
                evt.preventDefault()
            }
            user.edit = null
        }

        $scope.saveEdit = function (user, evt) {
            if (evt) {
                evt.preventDefault()
            }

            $scope.isLoading = true
            $http({
                method: 'POST',
                url: '/CreditHandlerLimitSettings/Edit',
                data: {
                    userId: user.UserId,
                    limitLevel: user.edit.LimitLevel,
                    isOverrideAllowed: user.edit.IsOverrideAllowed
                }
            }).then(function successCallback(response) {
                user.LimitLevel = parseInt(user.edit.LimitLevel, 10)
                user.IsOverrideAllowed = user.edit.IsOverrideAllowed
                user.edit = null
                $scope.isLoading = false
            }, function errorCallback(response) {
                toastr.error(response.data.message, 'Error');
            })
        }

        $scope.filterUsers = function (user) {
            if (!$scope.nameFilter) {
                return true
            }
            return user.DisplayName.toLowerCase().indexOf($scope.nameFilter.toLowerCase()) !== -1
        }

        $scope.isEditingAny = function () {
            return _.any($scope.users, function (x: User) { return !!x.edit })
        }

        window.scope = $scope
    }
}

app.controller('ctr', CreditHandlerLimitSettingsCtr);

class User {
    public edit: User
    public LimitLevel: number
    public isOverrideAllowed: boolean
}

module CreditHandlerLimitSettingsNs {
}