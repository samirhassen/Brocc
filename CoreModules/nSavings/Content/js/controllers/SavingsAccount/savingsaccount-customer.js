var CustomerController = app.controller('customerCtr', ['$scope', '$http', '$q', '$timeout', '$route', '$routeParams', 'customerDetailsData', function ($scope, $http, $q, $timeout, $route, $routeParams, customerDetailsData) {
        function extendCustomer(customers) {
            var fetchcustomeritems = (customerId, names, onsuccess, onerror) => {
                $scope.isLoading = true;
                $http({
                    method: 'POST',
                    url: initialData.fetchCustomerItemsUrl,
                    data: { customerId: customerId, propertyNames: names }
                }).then((response) => {
                    $scope.isLoading = false;
                    onsuccess(response.data.items);
                }, (response) => {
                    $scope.isLoading = false;
                    onerror(response.statusText);
                });
            };
            for (let c of customers) {
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
