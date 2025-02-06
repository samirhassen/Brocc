var app = angular.module('app', ['ntech.forms', 'ngRoute', 'pascalprecht.translate', 'ngCookies',])

ntech.angular.setupTranslation(app)

app.config(['$routeProvider', '$locationProvider', '$provide', function ($routeProvider, $locationProvider, $provide) {
    $provide.factory('mainService', ['$http', '$q', function ($http, $q) {
        var d = { isLoading: false }

        d.backUrl = null

        d.transactionsBatchSize = 5
        d.currentMenuItemName = ''

        d.loadDataAsync = function (input, url) {
            var deferred = $q.defer()
            d.isLoading = true
            $http({
                method: 'POST',
                url: url,
                data: input
            }).then(function successCallback(response) {
                d.isLoading = false
                deferred.resolve(response.data)
            }, function errorCallback(response) {
                d.isLoading = false
                deferred.reject()
            })
            return deferred.promise
        }

        return d
    }])

    $routeProvider
        .when('/credits/:creditNr/details', {
            templateUrl: 'creditdetails.html',
            controller: 'creditDetailsCtr',
            resolve: {
                ctrData: ['mainService', '$route', '$location', function (mainService, $route, $location) {
                    return mainService.loadDataAsync({
                        creditNr: $route.current.params.creditNr,
                        maxTransactionsCount: mainService.transactionsBatchSize
                    }, initialData.apiUrls.creditDetails)
                }]
            }
        })        
        .when('/opennotifications', {
            templateUrl: 'opennotifications.html',
            controller: 'openNotificationsCtr',
            resolve: {
                ctrData: ['mainService', '$route', '$location', function (mainService, $route, $location) {
                    return mainService.loadDataAsync({}, initialData.apiUrls.openNotifications)
                }]
            }
        })
        .when('/accountdocuments', {
            templateUrl: 'accountdocuments.html',
            controller: 'accountdocumentsCtr',
            resolve: {
                ctrData: ['mainService', '$route', '$location', function (mainService, $route, $location) {
                    return mainService.loadDataAsync({}, initialData.apiUrls.accountdocuments)
                }]
            }
        })
        .otherwise({
            redirectTo: function (obj, requestedPath) {
                window.location.href = initialData.productsOverviewUrl;
            }
        });
}])

var topLevelBackUrl = null

app.controller('mainCtr', ['$scope', '$http', '$q', '$timeout', '$route', '$routeParams', '$location', 'mainService', function ($scope, $http, $q, $timeout, $route, $routeParams, $location, mainService) {
    $scope.$routeParams = $routeParams
    $scope.$location = $location
    $scope.$route = $route
    $scope.backUrl = topLevelBackUrl
    $scope.productsOverviewUrl = initialData.productsOverviewUrl
    window.scope = $scope
    
    $scope.$watch(function () { return mainService.isLoading }, function () { $scope.isLoading = mainService.isLoading })
    $scope.$watch(function () { return mainService.currentMenuItemName }, function () { $scope.currentMenuItemName = mainService.currentMenuItemName })
    $scope.$watch(function () { return mainService.backUrl }, function () {
        if (mainService.backUrl) {
            $scope.backUrl = mainService.backUrl            
        } else {
            $scope.backUrl = topLevelBackUrl
        }        
    })

    $scope.$on('$routeChangeSuccess', function () {
        $scope.showMenu = false
    })
}])