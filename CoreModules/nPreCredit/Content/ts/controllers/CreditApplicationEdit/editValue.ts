var app = angular.module('app', ['pascalprecht.translate', 'ntech.forms', 'ntech.components']);

ntech.angular.setupTranslation(app)

class CreditApplicationEditCtr {
    static $inject = ['$scope', '$http', '$q', '$translate', '$timeout']
    constructor(
        $scope: any, //ng.IScope
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $timeout: ng.ITimeoutService
    ) {
        $scope.mode = initialData.mode
        $scope.applicationNr = initialData.applicationNr
        $scope.name = initialData.name
        $scope.translatedName = initialData.translation.translations.en[initialData.name]
        if (!$scope.translatedName) {
            $scope.translatedName = initialData.name
        }
        $scope.groupName = initialData.groupName
        $scope.oldValue = initialData.value
        $scope.edit = { value: initialData.value }
        $scope.logItems = initialData.logItems

        let apiClient = new NTechPreCreditApi.ApiClient(toastr.error, $http, $q)
        $scope.onBack = (evt) => {
            if (evt) {
                evt.preventDefault()
            }
            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, apiClient, $q, { applicationNr: initialData.applicationNr })
        }

        if (initialData.mode === 'insert') {
            $scope.actionUrl = initialData.insertValueUrl
        } else if (initialData.mode === 'edit') {
            $scope.actionUrl = initialData.saveEditValueUrl
        }

        var booleanFieldNames = ['approvedSat', 'forceExternalScoring']
        var positiveIntFieldNames = ['loansToSettleAmount', 'housingCostPerMonthAmount', 'incomePerMonthAmount', 'nrOfChildren', 'mortgageLoanAmount', 'mortgageLoanCostPerMonthAmount', 'carOrBoatLoanAmount', 'carOrBoatLoanCostPerMonthAmount', 'studentLoanAmount', 'studentLoanCostPerMonthAmount', 'otherLoanAmount', 'otherLoanCostPerMonthAmount', 'creditCardAmount', 'creditCardCostPerMonthAmount', 'repaymentTimeInYears', 'amount'];
        var pulldownFieldNames = ['education', 'housing', 'employment', 'marriage'];
        var validMonthFieldNames = ['employedSinceMonth'];
        var phoneNrFieldNames = ['phone', 'employerPhone'];

        if (booleanFieldNames.some(x => x === initialData.name)) {
            $scope.editMode = 'boolean';
            $scope.edit.value = ($scope.edit.value === 'true' || $scope.edit.value === true)
        } else if (phoneNrFieldNames.some(x => x === initialData.name)) {
            $scope.editMode = 'phonenr';
        } else if (positiveIntFieldNames.some(x => x === initialData.name)) {
            $scope.editMode = 'positiveInt';
        } else if (validMonthFieldNames.some(x => x === initialData.name)) {
            $scope.editMode = 'validMonth';
        } else if (pulldownFieldNames.some(x => x === initialData.name)) {
            $scope.editMode = 'pulldown';
            if (initialData.name === 'education') {
                $scope.pulldownOptions = ['education_grundskola', 'education_yrkesskola', 'education_gymnasie', 'education_hogskola'];
            } else if (initialData.name === 'housing') {
                $scope.pulldownOptions = ['housing_egenbostad', 'housing_bostadsratt', 'housing_hyresbostad', 'housing_hosforaldrar', 'housing_tjanstebostad']
            } else if (initialData.name === 'employment') {
                $scope.pulldownOptions = ['employment_fastanstalld', 'employment_visstidsanstalld', 'employment_foretagare', 'employment_pensionar', 'employment_sjukpensionar', 'employment_studerande', 'employment_arbetslos']
            } else if (initialData.name === 'marriage') {
                $scope.pulldownOptions = ['marriage_gift', 'marriage_ogift', 'marriage_sambo']
            }
        } else {
            $scope.editMode = 'string';
        }
        $scope.isLoading = false;

        $scope.cancel = function () {
            $scope.onBack(null)
        }

        $scope.save = function () {
            if ($scope.removeMode) {
                $scope.remove();
            } else {
                var value = angular.copy($scope.edit.value)
                if ($scope.editMode === 'boolean') {
                    value = (value === true) ? 'true' : 'false'
                }
                $http({
                    method: 'POST',
                    url: $scope.actionUrl,
                    data: {
                        applicationNr: $scope.applicationNr,
                        name: $scope.name,
                        groupName: $scope.groupName,
                        value: value
                    }
                }).then(function successCallback(response) {
                    $scope.onBack(null)
                }, function errorCallback(response) {
                    location.reload()
                })
            }
        }

        $scope.startRemove = function () {
            $scope.removeMode = true;
        }

        $scope.startAdd = function () {
            $scope.removeMode = false;
        }

        $scope.remove = function () {
            $http({
                method: 'POST',
                url: initialData.removeValueUrl,
                data: {
                    applicationNr: $scope.applicationNr,
                    name: $scope.name,
                    groupName: $scope.groupName
                }
            }).then(function successCallback(response) {
                $scope.onBack(null)
            }, function errorCallback(response) {
                location.reload()
            })
        }

        $scope.isValidPositiveInt = function (value) {
            if (isNullOrWhitespace(value))
                return true;
            var v = value.toString()
            return (/^(\+)?([0-9]+)$/.test(v))
        }

        $scope.isValidMonth = function (value) {
            if (isNullOrWhitespace(value))
                return true;
            return moment(value + '-01', "YYYY-MM-DD", true).isValid()
        }

        $scope.isValidPhoneNr = function (value) {
            if (isNullOrWhitespace(value))
                return true
            return !(/[a-z]/i.test(value))
        }

        function isNullOrWhitespace(input) {
            if (typeof input === 'undefined' || input == null) return true;

            if ($.type(input) === 'string') {
                return $.trim(input).length < 1;
            } else {
                return false
            }
        }

        window.scope = $scope
    }
}

app.controller('ctr', CreditApplicationEditCtr);

module CreditApplicationNs {
}