var app = angular.module('app', ['ntech.forms']);

angular.module('app').directive('usernameValidator', ['$http', '$q', function ($http, $q) {
    return {
        require: 'ngModel',
        link: function (scope, element, attrs, ngModel) {
            ngModel.$asyncValidators.username = function (modelValue, viewValue) {
                return $http({
                    method: 'POST',
                    url: initialData.validateUserDisplayNameUrl,
                    data: { displayName: viewValue }
                }).then(function (response) {
                    if (response.data.userDisplayNameAlreadyInUse) {
                        return $q.reject(response.data.errorMessage);
                    }
                    if (response.data.userDisplayNameContainsNotLetters) {
                        return $q.reject(response.data.errorMessage);
                    }
                    if (response.data.userDisplayNameContainsNotLetters) {
                        toastr.error(response.data.errorMessage);
                    }
                    return true;
                })
            };
        }
    };
}])

app.controller("ctr", ['$scope', '$http', '$window', '$timeout', function ($scope, $http, $window, $timeout) {
    $scope.users = initialData.users;
    $scope.administerUser = function (userId) {
        document.location.href = '/Admin/AdministerUser?id=' + userId.toString()
    }
    $scope.createUserData = {}
    $scope.onCreateUserStarted = function () {
        $scope.createUserData = {}
    }
    $scope.createUser = function () {
        if ($scope.createUserForm.$invalid) {
            $scope.createUserForm.$setSubmitted()
            return
        }

        $scope.createUserIsWorking = true
        $http({
            method: 'POST',
            url: initialData.createUserUrl,
            data: $scope.createUserData
        }).then(function successCallback(response) {
            $scope.createUserIsWorking = false
            if (response.data.redirectToUrl) {
                document.location = response.data.redirectToUrl
            } else if (response.data.errorMessage) {
                toastr.error(response.data.errorMessage, 'Error');
            }
        }, function errorCallback(response) {
            if (response.data.message) {
                toastr.error(response.data.message, 'Error');
            } else {
                toastr.error('Something went wrong', 'Error');
            }
            $scope.createUserIsWorking = false
        })
    }

    $scope.loadUserList = function (showDeleted) {
        $http({
            method: 'POST',
            url: 'LoadUserList',
            data: { loadDeletedUsers: showDeleted }
        }).then(function successCallback(response) {
                $scope.users = response.data;
                if (response.data.errorMessage) {
                    toastr.error(response.data.errorMessage, "Error");
                } 
            },
            function errorCallback(response) {
                if (response.data.message) {
                    toastr.error(response.data.message, "Error");
                } else {
                    toastr.error("An error occured", "Error");
                }
            })
    }
    
    $scope.reactivateUser = function (userId) {
        $http({
            method: 'POST',
            url: 'ReactivateUser',
            data: { userId: userId }
        }).then(function successCallback(response) {
                if (response.data.errorMessage) {
                    toastr.error(response.data.errorMessage, "Error");
                } else {
                    toastr.success("User has been reactivated. ", "Reactivated");
                    $http({
                        method: 'POST',
                        url: 'LoadUserList',
                        data: { loadDeletedUsers: true }
                    }).then(function successCallback(response) {
                            $scope.users = response.data;
                            if (response.data.errorMessage) {
                                toastr.error(response.data.errorMessage, "Error");
                            }
                        },
                        function errorCallback(response) {
                            if (response.data.message) {
                                toastr.error(response.data.message, "Error");
                            } else {
                                toastr.error("An error occured", "Error");
                            }
                        })
                }
            },
            function errorCallback(response) {
                if (response.data.message) {
                    toastr.error(response.data.message, "Error");
                } else {
                    toastr.error("An error occured", "Error");
                }
            })
    }

    function isNullOrWhitespace(input) {
        if (typeof input === 'undefined' || input == null) return true;

        if ($.type(input) === 'string') {
            return $.trim(input).length < 1;
        } else {
            return false
        }
    }

    $scope.isValidDate = function (value) {
        if (isNullOrWhitespace(value)) {
            return true
        }
        return moment(value, 'YYYY-MM-DD', true).isValid()
    }

    $scope.isValidDisplayName = function (value) {
        if (isNullOrWhitespace(value)) {
            return true
        }

        return !($scope.createUserData && $scope.createUserData.alreadyInUseWarningName && $scope.createUserData.alreadyInUseWarningName === value)
    }

    window.scope = $scope
}])