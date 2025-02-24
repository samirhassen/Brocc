var app = angular.module('app', ['ntech.forms']);
app
    .controller('ctr', ['$scope', '$http', '$timeout', function ($scope, $http, $timeout) {
        $scope.backUrl = initialData.backUrl;
        $scope.dwAgeInDays = initialData.lastDwUpdateAgeInDays;
        $scope.reportUrls = initialData.reportUrls;
        window.scope = $scope;
        $scope.isValidDate = function (value) {
            if (ntech.forms.isNullOrWhitespace(value))
                return true;
            return moment(value, 'YYYY-MM-DD', true).isValid();
        };
        $scope.$watch('reportName', function () {
            var r = $scope.reportName;
            if (r == 'savingsLedger') {
                $scope.current = {
                    modelType: 'savingsLedger',
                    model: {
                        date: moment(initialData.today).format('YYYY-MM-DD'),
                        includeCustomerDetails: 'no',
                        useBookKeepingDate: 'no'
                    }
                };
            }
            else if (r == 'currentInterestRates' || r == 'amlReportingAidFi') {
                $scope.current = {
                    modelType: 'none',
                    model: {}
                };
            }
            else if (r == 'interestRatesPerAccount') {
                $scope.current = {
                    modelType: 'dateOnly',
                    model: {
                        date: moment(initialData.today).subtract(1, 'days').format('YYYY-MM-DD')
                    }
                };
            }
            else if (r == 'dailyOutgoingPayments') {
                $scope.current = {
                    modelType: 'dateOnly',
                    model: {
                        date: moment(initialData.today).format('YYYY-MM-DD')
                    }
                };
            }
            else if (r == 'unplacedBalance') {
                $scope.current = {
                    modelType: 'dateAndDropdown',
                    model: {
                        date: moment(initialData.today).format('YYYY-MM-DD'),
                        dropdown1: 'false'
                    },
                    date1LabelText: 'Date',
                    dropdown1LabelText: 'Date type',
                    dropdown1Options: [['false', 'Bookkeeping'], ['true', 'Transaction']],
                    dropdown1ParameterName: 'useTransactionDate'
                };
            }
            else if (r == 'providerFeedback') {
                $scope.current = {
                    modelType: 'twoDates',
                    model: {
                        date1: moment(initialData.today).startOf('month').subtract(1, 'months').format('YYYY-MM-DD'),
                        date2: moment(initialData.today).startOf('month').format('YYYY-MM-DD')
                    }
                };
            }
            else {
                $scope.current = {
                    modelType: '',
                    model: {}
                };
            }
        });
        $scope.createReport = function (evt) {
            var baseUrl = initialData.reportUrls[$scope.reportName];
            if ($scope.current.modelType === 'none') {
                $scope.current.reportUrl = baseUrl;
            }
            else if ($scope.current.modelType === 'dateOnly') {
                $scope.current.reportUrl = baseUrl + '?date=' + $scope.current.model.date;
            }
            else if ($scope.current.modelType === 'twoDates') {
                $scope.current.reportUrl = baseUrl + '?date1=' + $scope.current.model.date1 + '&date2=' + $scope.current.model.date2;
            }
            else if ($scope.current.modelType === 'recentQuarter') {
                $scope.current.reportUrl = baseUrl + '?quarterEndDate=' + $scope.current.model.quarter.value;
            }
            else if ($scope.current.modelType === 'recentMonth') {
                $scope.current.reportUrl = baseUrl + '?monthEndDate=' + $scope.current.model.month.value;
            }
            else if ($scope.current.modelType === 'dateAndDropdown') {
                $scope.current.reportUrl = baseUrl + '?date=' + $scope.current.model.date + '&' + $scope.current.dropdown1ParameterName + '=' + $scope.current.model.dropdown1;
            }
            else if ($scope.current.modelType === 'savingsLedger') {
                var extras = '';
                if ($scope.current.model.includeCustomerDetails === 'yes') {
                    extras = extras + '&includeCustomerDetails=true';
                }
                if ($scope.current.model.useBookKeepingDate === 'yes') {
                    extras = extras + '&useBookKeepingDate=true';
                }
                $scope.current.reportUrl = baseUrl + '?date=' + $scope.current.model.date + extras;
            }
            else {
                evt.preventDefault();
                return;
            }
        };
    }]);
