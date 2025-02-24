var app = angular.module('app', ['ntech.forms']);
app.controller('ctr', ['$scope', '$http', '$q', function ($scope, $http, $q) {
        $scope.s = {};
        $scope.s.providers = initialData.providers;
        $scope.categoryCodes = initialData.categoryCodes;
        $scope.categoryCounts = initialData.categoryCounts;
        $scope.isTest = initialData.isTest;
        $scope.showCategoryCodes = initialData.showCategoryCodes;
        var apiClient = new NTechPreCreditApi.ApiClient(toastr.error, $http, $q);
        $scope.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, apiClient, $q);
        };
        var removeFilterWatch = null;
        var initialFilter = {
            creditApplicationCategoryCode: 'PendingCreditCheck',
            providerName: ''
        };
        if (localStorage) {
            var storedFilter = localStorage.getItem('ntech_storedCreditApplicationsInitialFilter_v1');
            if (storedFilter) {
                initialFilter = JSON.parse(storedFilter);
                if (initialFilter && !_(initialData.categoryCodes).contains(initialFilter.creditApplicationCategoryCode)) {
                    //Happens when a category is removed
                    initialFilter.creditApplicationCategoryCode = 'PendingCreditCheck';
                }
            }
        }
        $scope.getProviderDisplayName = function (providerName) {
            var _a;
            if (initialData.providers) {
                for (var _i = 0, _b = initialData.providers; _i < _b.length; _i++) {
                    var provider = _b[_i];
                    if (provider.ProviderName === providerName) {
                        return (_a = provider.DisplayToEnduserName) !== null && _a !== void 0 ? _a : providerName;
                    }
                }
            }
            return providerName;
        };
        $scope.isSpecialSearchMode = function () {
            return $scope.omniSearchValue;
        };
        $scope.omniSearch = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            var f = {
                backUrl: initialData.backUrl,
                omniSearchValue: $scope.omniSearchValue,
                pageSize: $scope.s.filter.pageSize,
                pageNr: 0
            };
            $scope.isLoading = true;
            $http({
                method: 'POST',
                url: initialData.filterUrl,
                data: f
            }).then(function successCallback(response) {
                $scope.isLoading = false;
                $scope.s.hit = response.data;
                if (response.data.CategoryCounts) {
                    $scope.categoryCounts = response.data.CategoryCounts;
                }
                $scope.s.paging = null;
            }, function errorCallback(response) {
                $scope.isLoading = false;
                location.reload();
            });
        };
        $scope.doFilter = function (pageNr, evt) {
            if (evt) {
                evt.preventDefault();
            }
            var f = angular.copy($scope.s.filter);
            f.IsPartiallyApproved = false;
            f.pageNr = pageNr;
            f.backUrl = initialData.backUrl;
            f.workListDataWaitDays = initialData.workListDataWaitDays;
            f.creditApplicationWorkListIsNewMinutes = initialData.creditApplicationWorkListIsNewMinutes;
            $scope.isLoading = true;
            $http({
                method: 'POST',
                url: initialData.filterUrl,
                data: f
            }).then(function successCallback(response) {
                $scope.isLoading = false;
                $scope.s.hit = response.data;
                if (response.data.CategoryCounts) {
                    $scope.categoryCounts = response.data.CategoryCounts;
                }
                updatePaging();
                if (localStorage) {
                    var latestfilter = {
                        creditApplicationCategoryCode: f.creditApplicationCategoryCode,
                        providerName: f.providerName
                    };
                    localStorage.setItem('ntech_storedCreditApplicationsInitialFilter_v1', JSON.stringify(latestfilter));
                }
            }, function errorCallback(response) {
                $scope.isLoading = false;
                location.reload();
            });
        };
        $scope.resetFilter = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (removeFilterWatch) {
                removeFilterWatch();
            }
            $scope.s.filter = {
                providerName: initialFilter.providerName,
                creditApplicationCategoryCode: initialFilter.creditApplicationCategoryCode,
                includeInactive: false,
                pageSize: 12,
                $uniquenessToken: moment().format()
            };
            $scope.s.hit = {
                Page: [],
                TotalTotalNrOfPages: 0
            };
            removeFilterWatch = $scope.$watch('s.filter', function () {
                $scope.doFilter(0);
            }, true);
        };
        function updatePaging() {
            if (!$scope.s.hit) {
                return {};
            }
            var h = $scope.s.hit;
            var p = [];
            //9 items including separators are the most shown at one time
            //The two items before and after the current item are shown
            //The first and last item are always shown
            for (var i = 0; i < h.TotalNrOfPages; i++) {
                if (i >= (h.CurrentPageNr - 2) && i <= (h.CurrentPageNr + 2) || h.TotalNrOfPages <= 9) {
                    p.push({ pageNr: i, isCurrentPage: h.CurrentPageNr == i }); //Primary pages are always visible
                }
                else if (i == 0 || i == (h.TotalNrOfPages - 1)) {
                    p.push({ pageNr: i, isCurrentPage: h.CurrentPageNr == i }); //First and last page are always visible
                }
                else if (i == (h.CurrentPageNr - 3) || i == (h.CurrentPageNr + 3)) {
                    p.push({ pageNr: i, isSeparator: true }); //First and last page are always visible
                }
            }
            $scope.s.paging = {
                pages: p,
                isPreviousAllowed: h.CurrentPageNr > 0,
                previousPageNr: h.CurrentPageNr - 1,
                isNextAllowed: h.CurrentPageNr < (h.TotalNrOfPages - 1),
                nextPageNr: h.CurrentPageNr + 1
            };
        }
        $scope.getCategoryLabel = function (c) {
            if (c == 'PendingCreditCheck') {
                return 'Pending credit check';
            }
            else if (c == 'PendingCustomerCheck') {
                return 'Pending customer check';
            }
            else if (c == 'WaitingForData') {
                return 'Waiting for data';
            }
            else if (c == 'WaitingForSignature') {
                return 'Waiting for signature';
            }
            else if (c == 'PendingFraudCheck') {
                return 'Pending fraud check';
            }
            else if (c == 'PendingFinalDecision') {
                return 'Pending final decision';
            }
            else if (c == 'WaitingForAdditionalInformation') {
                return 'Waiting for additional information';
            }
            else if (c == 'PendingDocumentCheck') {
                return 'Pending document check';
            }
            else if (c == 'WaitingForDocument') {
                return 'Waiting for document';
            }
            else {
                return c;
            }
        };
        function init() {
            $scope.resetFilter();
        }
        if ($scope.isTest) {
            $scope.testGotoRandomApplication = function (applicationType, evt) {
                if (evt) {
                    evt.preventDefault();
                }
                $scope.isLoading = true;
                $http({
                    method: 'POST',
                    url: initialData.testFindRandomApplicationUrl,
                    data: { applicationType: applicationType }
                }).then(function successCallback(response) {
                    if (response.data.redirectToUrl) {
                        document.location = response.data.redirectToUrl;
                    }
                    else {
                        toastr.warning('No such application exists');
                        $scope.isLoading = false;
                    }
                }, function errorCallback(response) {
                    creditSharedService.isLoading = false;
                    toastr.error(response.statusText, 'Error');
                });
            };
            $scope.toggleCategoriesInResult = function (evt) {
                if (evt) {
                    evt.preventDefault();
                }
                var symbol = (document.location.href.indexOf('?') >= 0) ? '&' : '?';
                if (document.location.href.indexOf('showCategoryCodes') < 0) {
                    $scope.isLoading = true;
                    document.location.href = document.location.href + symbol + 'showCategoryCodes=True';
                }
                else if (document.location.href.indexOf('showCategoryCodes=True') > 0) {
                    $scope.isLoading = true;
                    document.location.href = document.location.href.replace('showCategoryCodes=True', 'showCategoryCodes=False');
                }
                else if (document.location.href.indexOf('showCategoryCodes=False') > 0) {
                    $scope.isLoading = true;
                    document.location.href = document.location.href.replace('showCategoryCodes=False', 'showCategoryCodes=True');
                }
            };
        }
        init();
        window.scope = $scope;
    }]);
