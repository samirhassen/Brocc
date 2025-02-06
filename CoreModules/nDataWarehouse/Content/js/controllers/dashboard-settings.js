var app = angular.module('app', ['ntech.forms']);

app.controller("ctr", ['$scope', '$http', '$window', function ($scope, $http, $window) {
    window.scope = $scope
    $scope.backUrl = initialData.backUrl
    $scope.chosenGraph = initialData.chosenGraph

    $scope.graphs = ['accumulated-balance', 'budget-vs-results']
    var graphNames = ['Accumulated Balance', 'Budget vs Results']
    $scope.graphName = function (label) {
        for (var i = 0; i < graphNames.length; ++i) {
            if (label === $scope.graphs[i]) {
                return graphNames[i]
            }
        }
        return 'Select graph'
    }

    $scope.allMonths = []
    for (var i = 0; i < 12; ++i) {
        $scope.allMonths.push(i + 1)
    }
    function isNullOrWhitespace(input) {
        if (typeof input === 'undefined' || input == null) return true;

        if ($.type(input) === 'string') {
            return $.trim(input).length < 1;
        } else {
            return false
        }
    }

    $scope.isValidPositiveInt = function (value) {
        if (isNullOrWhitespace(value))
            return true;
        var v = value.toString()
        return (/^(\+)?([0-9]+)$/.test(v))
    }

    /////////////////////////////////////
    //////// BUDGET GRAPH ///////////////
    /////////////////////////////////////
    var chosenYear = initialData.budgetGraph.chosenYear
    var currentYear = parseInt(moment(initialData.currentDate).format('YYYY'))

    $scope.budgetGraphYears = [currentYear - 3, currentYear - 2, currentYear - 1, currentYear, currentYear + 1]
    if (chosenYear) {
        if (chosenYear < $scope.budgetGraphYears[0]) {
            $scope.budgetGraphYears.unshift(chosenYear)
        } else if (chosenYear > $scope.budgetGraphYears[$scope.budgetGraphYears.length - 1]) {
            $scope.budgetGraphYears.push(chosenYear)
        }
    }

    $scope.budgetGraph = {
        startYear: initialData.budgetGraph.chosenYear ? initialData.budgetGraph.chosenYear.toString() : null,
        startMonth: initialData.budgetGraph.chosenMonth ? initialData.budgetGraph.chosenMonth.toString() : null
    }

    function padMonth(s) {
        return (s < 10 ? '0' : '') + s.toString()
    }
    $scope.monthDisplayName = function (m) {
        if (!m) {
            return ''
        }
        return moment('2017-' + padMonth(m) + '-01').format('MMMM')
    }
    $scope.budgetGraph.budgetMonths = []
    for (var i = 0; i < 12; ++i) {
        $scope.budgetGraph.budgetMonths.push({
            budgetIndex: i,
            budgetAmount: initialData.budgetGraph.budgets && initialData.budgetGraph.budgets.length === 12 ? initialData.budgetGraph.budgets[i] : 0
        })
    }
    $scope.budgetGraphStartDate = function () {
        if ($scope.budgetGraph.startYear && $scope.budgetGraph.startMonth) {
            return moment($scope.budgetGraph.startYear.toString() + '-' + padMonth($scope.budgetGraph.startMonth) + '-01')
        } else {
            return null
        }
    }
    $scope.beginEdit = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        $scope.budgetGraphEditCopy = angular.copy($scope.budgetGraph)
        $scope.isEditMode = true
    }

    $scope.commitEdit = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        if ($scope.choosegraphform.$invalid) {
            return
        }
        if ($scope.budgetgraphform.$invalid) {
            return
        }
        $scope.isLoading = true

        var updateSelectedGraph = function (onSuccess) {
            $http({
                method: 'POST',
                url: initialData.updateChosenGraphUrl,
                data: {
                    name: $scope.chosenGraph
                }
            }).then(function successCallback(response) {
                if (onSuccess) {
                    onSuccess()
                } else {
                    $scope.isLoading = false
                    $scope.isEditMode = false
                }                
            }, function errorCallback(response) {
                $scope.isBroken = true
                $scope.isLoading = false
            })
        }

        var updateBudgets = function (onSuccess) {
            var budgetAmounts = []
            angular.forEach($scope.budgetGraph.budgetMonths, function (v) {
                budgetAmounts.push(v.budgetAmount)
            })
            $scope.budgetGraphEditCopy = null
            $http({
                method: 'POST',
                url: initialData.budgetGraph.updateBudgetVsResultStartUrl,
                data: {
                    startMonth: $scope.budgetGraph.startMonth,
                    startYear: $scope.budgetGraph.startYear,
                    budgets: budgetAmounts
                }
            }).then(function successCallback(response) {
                if (onSuccess) {
                    onSuccess()
                } else {
                    $scope.isLoading = false
                    $scope.isEditMode = false
                }
            }, function errorCallback(response) {
                $scope.isBroken = true
                $scope.isLoading = false
            })
        }

        if ($scope.chosenGraph == 'budget-vs-results') {
            updateBudgets(updateSelectedGraph)
        } else {
            updateSelectedGraph()
        }
    }

    $scope.cancelEdit = function (evt) {
        if (evt) {
            evt.preventDefault()
        }

        $scope.isEditMode = false
    }

    $scope.cancelEditBudgetGraph = function (evt) {
        if (evt) {
            evt.preventDefault()
        }

        for (var i = 0; i < $scope.budgetGraph.budgetMonths.length; i++) {
            if ($scope.budgetGraph.budgetMonths[i].budgetAmount !== $scope.budgetGraphEditCopy.budgetMonths[i].budgetAmount) {
                $scope.budgetGraph.budgetMonths[i].budgetAmount = $scope.budgetGraphEditCopy.budgetMonths[i].budgetAmount
            }
        }
        if ($scope.budgetGraph.startYear !== $scope.budgetGraphEditCopy.startYear) {
            $scope.budgetGraph.startYear = $scope.budgetGraphEditCopy.startYear
        }
        if ($scope.budgetGraph.startMonth !== $scope.budgetGraphEditCopy.startMonth) {
            $scope.budgetGraph.startMonth = $scope.budgetGraphEditCopy.startMonth
        }
        $scope.isEditMode = false
        $scope.budgetGraphEditCopy = null
    }

    $scope.budgetMonthDisplayText = function (budgetIndex) {
        var sd = $scope.budgetGraphStartDate()
        if (sd) {
            return sd.add(budgetIndex, 'month').format('MMMM')
        } else {
            return null
        }
    }
    /////////////////////////////////////
    //////// END BUDGET GRAPH ///////////
    /////////////////////////////////////
}])