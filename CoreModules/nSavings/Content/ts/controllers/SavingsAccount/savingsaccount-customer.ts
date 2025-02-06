var CustomerController = app.controller('customerCtr', ['$scope', '$http', '$q', '$timeout', '$route', '$routeParams', 'customerDetailsData', function ($scope, $http, $q, $timeout, $route, $routeParams, customerDetailsData) {
    function extendCustomer(customers) {
        var fetchcustomeritems = (customerId: number,
            names: Array<string>,
            onsuccess: (items: Array<CustomerItem>) => void,
            onerror: (msg: string) => void) => {

            $scope.isLoading = true
            $http({
                method: 'POST',
                url: initialData.fetchCustomerItemsUrl,
                data: { customerId: customerId, propertyNames: names }
            }).then((response: ng.IHttpResponse<any>) => {
                $scope.isLoading = false
                onsuccess(response.data.items)
            }, (response: ng.IHttpResponse<any>) => {
                $scope.isLoading = false
                onerror(response.statusText)
            })
        }
        for (let c of customers) {
            c.customerinfo = {
                firstName: c.firstName,
                birthDate: c.birthDate,
                customerId: c.customerId,
                customerCardUrl: c.customerCardUrl,
                customerFatcaCrsUrl: c.customerFatcaCrsUrl,
                customerPepKycUrl: c.customerPepKycUrl,
                customerKycQuestionsUrl: c.customerKycQuestionsUrl
            }
        }
        return {
            customers: customers,
            fetchcustomeritems: fetchcustomeritems
        }
    }

    $scope.backUrl = initialData.backUrl
    $scope.$routeParams = $routeParams
    $scope.n = extendCustomer(customerDetailsData.customers)
    window.customerScope = $scope
}])