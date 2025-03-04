var CustomerController = app.controller('customerCtr', ['$scope', '$http', '$q', '$timeout', '$route', '$routeParams', 'customerDetailsData', function ($scope, $http, $q, $timeout, $route, $routeParams, customerDetailsData) {
        function extendCustomer(customers) {
            var fetchcustomeritems = function (customerId, names, onsuccess, onerror) {
                $scope.isLoading = true;
                $http({
                    method: 'POST',
                    url: initialData.fetchCustomerItemsUrl,
                    data: { customerId: customerId, propertyNames: names }
                }).then(function (response) {
                    $scope.isLoading = false;
                    onsuccess(response.data.items);
                }, function (response) {
                    $scope.isLoading = false;
                    onerror(response.statusText);
                });
            };
            for (var _i = 0, customers_1 = customers; _i < customers_1.length; _i++) {
                var c = customers_1[_i];
                c.customerinfo = {
                    firstName: c.firstName,
                    birthDate: c.birthDate,
                    customerId: c.customerId,
                    customerCardUrl: c.customerCardUrl,
                    customerFatcaCrsUrl: c.customerFatcaCrsUrl,
                    customerPepKycUrl: c.customerPepKycUrl,
                    customerKycQuestionsUrl: c.customerKycQuestionsUrl
                };
            }
            return {
                customers: customers,
                fetchcustomeritems: fetchcustomeritems
            };
        }
        $scope.backUrl = initialData.backUrl;
        $scope.$routeParams = $routeParams;
        $scope.n = extendCustomer(customerDetailsData.customers);
        window.customerScope = $scope;
    }]);
