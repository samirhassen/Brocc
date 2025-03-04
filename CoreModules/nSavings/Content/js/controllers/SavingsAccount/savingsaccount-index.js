var app = angular.module('app', ['ntech.forms', 'ngRoute', 'ntech.components']);
app.config(['$routeProvider', '$locationProvider', '$provide', function ($routeProvider, $locationProvider, $provide) {
        $provide.factory('mainService', ['$http', '$q', '$location', function ($http, $q, $location) {
                var d = { isLoading: false };
                if (initialData && initialData.backUrl) {
                    d.backUrl = initialData.backUrl;
                }
                else {
                    d.backUrl = null;
                }
                d.loadBySavingsAccountNr = function (savingsAccountNr, url, customAccept) {
                    var deferred = $q.defer();
                    d.isLoading = true;
                    $http({
                        method: 'POST',
                        url: url,
                        data: { savingsAccountNr: savingsAccountNr, testUserId: initialData.testUserId }
                    }).then(function successCallback(response) {
                        d.isLoading = false;
                        if (customAccept) {
                            customAccept(deferred, response.data);
                        }
                        else {
                            deferred.resolve(response.data);
                        }
                    }, function errorCallback(response) {
                        toastr.error(response.statusText);
                        $location.path('/Error');
                        d.isLoading = false;
                        deferred.reject();
                    });
                    return deferred.promise;
                };
                return d;
            }]);
        $routeProvider
            .when('/Details/:savingsAccountNr', {
            templateUrl: 'details.html',
            controller: 'detailsCtr',
            resolve: {
                savingsAccountDetailsData: ['mainService', '$route', function (mainService, $route) {
                        return mainService.loadBySavingsAccountNr($route.current.params.savingsAccountNr, initialData.detailsUrl);
                    }]
            }
        })
            .when('/Customer/:savingsAccountNr', {
            templateUrl: 'customer.html',
            controller: 'customerCtr',
            resolve: {
                customerDetailsData: ['mainService', '$route', function (mainService, $route) {
                        return mainService.loadBySavingsAccountNr($route.current.params.savingsAccountNr, initialData.customerUrl);
                    }]
            }
        })
            .when('/Withdrawals/:savingsAccountNr', {
            templateUrl: 'withdrawals.html',
            controller: 'withdrawalsCtr',
            resolve: {
                ctrData: ['mainService', '$route', function (mainService, $route) {
                        return mainService.loadBySavingsAccountNr($route.current.params.savingsAccountNr, initialData.getWithdrawalInitialDataUrl);
                    }]
            }
        })
            .when('/AccountClosure/:savingsAccountNr', {
            templateUrl: 'accountclosure.html',
            controller: 'accountclosureCtr',
            resolve: {
                ctrData: ['mainService', '$route', function (mainService, $route) {
                        return mainService.loadBySavingsAccountNr($route.current.params.savingsAccountNr, initialData.initialDataCloseAccountUrl);
                    }]
            }
        })
            .when('/WithdrawalAccount/:savingsAccountNr', {
            templateUrl: 'withdrawalaccount.html',
            controller: 'withdrawalaccountCtr',
            resolve: {
                ctrData: ['mainService', '$route', '$location', function (mainService, $route, $location) {
                        return mainService.loadBySavingsAccountNr($route.current.params.savingsAccountNr, initialData.initialDataWithdrawalAccountUrl, function (deferred, data) {
                            if (data.PendingWithdrawalAccountChangeId) {
                                $location.path('/WithdrawalAccountChange/' + $route.current.params.savingsAccountNr);
                            }
                            else {
                                deferred.resolve(data);
                            }
                        });
                    }]
            }
        })
            .when('/WithdrawalAccountChange/:savingsAccountNr', {
            templateUrl: 'withdrawalaccountchange.html',
            controller: 'withdrawalaccountchangeCtr',
            resolve: {
                ctrData: ['mainService', '$route', function (mainService, $route) {
                        return mainService.loadBySavingsAccountNr($route.current.params.savingsAccountNr, initialData.initialDataWithdrawalAccountChangeUrl);
                    }]
            }
        })
            .when('/Documents/:savingsAccountNr', {
            name: 'documents',
            template: '<savings-account-documents initial-data="$resolve.ctrData"></savings-account-documents>',
            resolve: {
                ctrData: ['mainService', '$route', function (mainService, $route) {
                        return mainService.loadBySavingsAccountNr($route.current.params.savingsAccountNr, initialData.initialDataDocumentsUrl);
                    }]
            }
        })
            .when('/Error', {
            templateUrl: 'error.html',
        })
            .when('/', {
            templateUrl: 'searchonly.html'
        })
            .otherwise({ redirectTo: '/' });
    }]);
app.controller('mainCtr', ['$scope', '$http', '$q', '$timeout', '$route', '$routeParams', '$location', 'mainService', 'savingsAccountCommentsService', function ($scope, $http, $q, $timeout, $route, $routeParams, $location, mainService, savingsAccountCommentsService) {
        $scope.backUrl = initialData.backUrl;
        $scope.$routeParams = $routeParams;
        $scope.$location = $location;
        $scope.$route = $route;
        window.scope = $scope;
        $scope.currentCtr = function () {
            if (!$route | !$route.current) {
                return '';
            }
            if ($route.current.controller) {
                return $route.current.controller;
            }
            if ($route.current.$$route) {
                return $route.current.$$route.name;
            }
            return '';
        };
        $scope.$watch(function () { return mainService.isLoading; }, function () { $scope.isLoading = mainService.isLoading || savingsAccountCommentsService.isLoading; });
        $scope.$watch(function () { return savingsAccountCommentsService.isLoading; }, function () { $scope.isLoading = mainService.isLoading || savingsAccountCommentsService.isLoading; });
        $scope.$watch(function () { return $route.current; }, function () {
            if (!$route.current || !$route.current.params || !$route.current.params.savingsAccountNr) {
                savingsAccountCommentsService.savingsAccountNr = null;
            }
            else {
                savingsAccountCommentsService.savingsAccountNr = $route.current.params.savingsAccountNr;
            }
        });
        $scope.gotoRandomSavingsAccount = function (opts, evt) {
            if (evt) {
                evt.preventDefault();
            }
            //opts : { mustContainBusinessEventType, mustHaveStatus  }
            $location.path('/');
            mainService.isLoading = true;
            $http({
                method: 'POST',
                url: '/Api/SavingsAccount/TestFindRandom',
                data: opts
            }).then(function successCallback(response) {
                mainService.isLoading = false;
                if (response.data.savingsAccountNr) {
                    $location.path('/Details/' + response.data.savingsAccountNr);
                }
                else {
                    toastr.warning('No such account found');
                }
            }, function errorCallback(response) {
                mainService.isLoading = false;
                toastr.error('Failed!');
            });
        };
    }]);
