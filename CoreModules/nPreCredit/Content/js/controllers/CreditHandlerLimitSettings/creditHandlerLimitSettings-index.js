var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components']);
var CreditHandlerLimitSettingsCtr = /** @class */ (function () {
    function CreditHandlerLimitSettingsCtr($scope, //ng.IScope
    $http, $q, $timeout) {
        this.$http = $http;
        this.$q = $q;
        this.$timeout = $timeout;
        $scope.levels = initialData.levels;
        $scope.users = initialData.users;
        $scope.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.handleBack(NavigationTargetHelper.create(initialData.backUrl, initialData.backTarget, null), new NTechPreCreditApi.ApiClient(toastr.error, $http, $q), $q, null);
        };
        $scope.beginEdit = function (user, evt) {
            if (evt) {
                evt.preventDefault();
            }
            user.edit = {
                LimitLevel: user.LimitLevel.toString(),
                IsOverrideAllowed: user.IsOverrideAllowed
            };
        };
        $scope.cancelEdit = function (user, evt) {
            if (evt) {
                evt.preventDefault();
            }
            user.edit = null;
        };
        $scope.saveEdit = function (user, evt) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.isLoading = true;
            $http({
                method: 'POST',
                url: '/CreditHandlerLimitSettings/Edit',
                data: {
                    userId: user.UserId,
                    limitLevel: user.edit.LimitLevel,
                    isOverrideAllowed: user.edit.IsOverrideAllowed
                }
            }).then(function successCallback(response) {
                user.LimitLevel = parseInt(user.edit.LimitLevel, 10);
                user.IsOverrideAllowed = user.edit.IsOverrideAllowed;
                user.edit = null;
                $scope.isLoading = false;
            }, function errorCallback(response) {
                toastr.error(response.data.message, 'Error');
            });
        };
        $scope.filterUsers = function (user) {
            if (!$scope.nameFilter) {
                return true;
            }
            return user.DisplayName.toLowerCase().indexOf($scope.nameFilter.toLowerCase()) !== -1;
        };
        $scope.isEditingAny = function () {
            return _.any($scope.users, function (x) { return !!x.edit; });
        };
        window.scope = $scope;
    }
    CreditHandlerLimitSettingsCtr.$inject = ['$scope', '$http', '$q', '$timeout', '$translate'];
    return CreditHandlerLimitSettingsCtr;
}());
app.controller('ctr', CreditHandlerLimitSettingsCtr);
var User = /** @class */ (function () {
    function User() {
    }
    return User;
}());
