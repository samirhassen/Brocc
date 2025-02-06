var app = angular.module('app', []);

app.controller("ctr", ['$scope', '$http', '$window', '$timeout', function ($scope, $http, $window, $timeout) {
    window.scope = $scope;
    var currentBalanceHistoryStr = ''
   
    var lineGraph = null
    var barGraph = null
    var myDoughnutChart = null

    var nextRefreshAt = moment()

    function onTick() {
        try {
            var now = moment()
            if (now >= nextRefreshAt) {
                fetchLatestData(now, function () {
                    $scope.awaitingRefresh = false
                    nextRefreshAt = moment().add(5, 'minutes')
                    $timeout(function () { onTick() }, 1000)
                })
            } else {
                $timeout(function () { onTick() }, 1000)
            }
        } catch (e) {
            $scope.isBroken = true
            throw e;
        }
    }

    onTick()

    function fetchLatestData(now, afterUpdate) {        
        $http({
            method: 'POST',
            url: '/Dashboard-Data',
            data: {
                currentDate: initialData.currentDate
            }
        }).then(function successCallback(response) {
            var d = response.data

            $scope.dailyPaymentRecord = d.dailyPaymentRecord
            $scope.totalBalance = d.totalBalance
            $scope.totalNrOfLoans = d.totalNrOfLoans
            $scope.avgBalancePerLoan = d.avgBalancePerLoan
            $scope.avgInterestRatePerLoan = d.avgInterestRatePerLoan
            $scope.balanceHistory = d.balanceHistory

            if ($scope.dailyApprovedApplicationsAmount !== d.dailyApprovedApplicationsAmount) {
                $scope.dailyApprovedApplicationsAmount = d.dailyApprovedApplicationsAmount
            }

            $scope.budgetData = response.data.budget

            if (!initialData.chosenGraph) {
                if (response.data.budget && response.data.budget.budgets) {
                    $scope.chosenGraph = 'budget-vs-results'
                } else {
                    $scope.chosenGraph = 'accumulated-balance'
                }
            } else {
                $scope.chosenGraph = initialData.chosenGraph;
            }
            $scope.lastUpdateMoment = now

            afterUpdate()
        }, function errorCallback(response) {
            $scope.isBroken = true
        })
    }

    $scope.forceRefresh = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        nextRefreshAt = moment()
        $scope.awaitingRefresh = true
    }

    $scope.$watch(
        function () { return $scope.dailyApprovedApplicationsAmount },
        function () {
            var p = 0
            if ($scope.dailyPaymentRecord > 0 || $scope.dailyApprovedApplicationsAmount > 0) {
                if ($scope.dailyApprovedApplicationsAmount >= $scope.dailyPaymentRecord) {
                    p = 100
                } else {
                    p = Math.floor(100 * $scope.dailyApprovedApplicationsAmount / $scope.dailyPaymentRecord)
                }
            }
            updateApprovedApplicationsAmountCircleGraph(p)
        }
    )

    $scope.$watch(
        function () { return $scope.balanceHistory },
        function () {
            if ($scope.balanceHistory) {                
                updateBalancePerDayLineGraph($scope.balanceHistory)
            }
        }
    )

    $scope.$watch(
        function () { return $scope.budgetData },
        function () {
            updateBudgetVsActualBarGraph()
        }
    )
    
    function updateApprovedApplicationsAmountCircleGraph(percentage) {
        var dataCircle = {
            labels: [
                "Done",
                "Todo"
            ],
            datasets: [
                {
                    data: [percentage, 100 - percentage],
                    backgroundColor: [
                            "white",
                            'rgba(255, 255, 255,0.3)'
                    ]
                }]
        }
        if (myDoughnutChart) {
            myDoughnutChart.destroy()
            myDoughnutChart = null
        }
        myDoughnutChart = new Chart(document.getElementById("circleChart"), {
            type: 'doughnut',
            data: dataCircle,
            options: {
                elements: {
                    arc: {
                        borderWidth: 0
                    }
                },
                cutoutPercentage: 90,
                legend: {
                    display: false
                },
                tooltips: {
                    enabled: true
                }
            }
        });
    }

    function updateBudgetVsActualBarGraph() {
        if (barGraph) {
            barGraph.destroy()
            barGraph = null
        }
        if (!$scope.budgetData || !$scope.budgetData.budgets) {
            return
        }

        var getMonthLabels = function () {
            var labels = []
            var m = moment($scope.budgetData.startDate)
            for (var i = 0; i < $scope.budgetData.budgets.length; i++) {
                labels.push(m.format('MMM-YY'))
                m = m.add(1, 'month')
            }
            return labels
        }

        var getResultBackgroundColors = function () {
            var backgroundColors = []
            var m = moment($scope.budgetData.startDate)
            var currentDate = moment(initialData.currentDate)

            var isAtOrPastCurrentMonth = false
            for (var i = 0; i < $scope.budgetData.results.length; i++) {
                if (m.year() === currentDate.year() && m.month() === currentDate.month()) {
                    isAtOrPastCurrentMonth = true
                }
                if (isAtOrPastCurrentMonth) {
                    backgroundColors.push('rgba(99,148,206, 0.2)')
                } else {
                    backgroundColors.push('rgba(99,148,206, 1)')
                }
                m = m.add(1, 'month')
            }

            return backgroundColors
        }

        var dataSets = []
        dataSets.push({
            label: 'budget new lending (in euros)',
            data: $scope.budgetData.budgets,
            backgroundColor: 'rgba(60,87,143, 1)',
            borderColor: 'rgba(60,87,143, 1)',
            borderWidth: 1
        })
        dataSets.push({
            label: 'actual new lending (in euros)',
            data: $scope.budgetData.results,
            backgroundColor: getResultBackgroundColors()
        })
        barGraph = new Chart(document.getElementById("barChart").getContext('2d'), {
            type: 'bar',
            data: {
                labels: getMonthLabels(),
                datasets: dataSets
            },
            options: {
                scales: {
                    xAxes: [{
                        ticks: {
                            fontFamily: 'Arial',
                            fontColor: '#6394CE'
                        }
                    }],
                    yAxes: [{
                        ticks: {
                            beginAtZero: true,
                            fontFamily: 'Arial',
                            fontColor: '#6394CE',
                            callback: function (value, index, values) {
                                return value.toLocaleString();
                            }
                        }
                    }]
                },      
                legend: {
                    display: false
                }
            }
        });
    }

    function updateBalancePerDayLineGraph(h) {
        var dataLine = {
            labels: h.labelPerDay,
            datasets: [
                {
                    backgroundColor: 'rgb(75,105,173)',
                    borderColor: 'rgb(75,105,173)',
                    type: 'line',
                    fill: false,
                    data: h.balancePerDay
                }]
        }
        if (lineGraph) {
            lineGraph.destroy()
            lineGraph = null
        }
        lineGraph = new Chart(document.getElementById("lineChart").getContext('2d'), {
            type: 'line',
            data: dataLine,

            options: {
                elements: { point: { radius: 0 } },
                scales: {
                    xAxes: [{
                        display: false,
                        gridLines: {
                            display: false
                        }
                    }],
                    yAxes: [{
                        display: false,
                        gridLines: {
                            display: false
                        }
                    }]
                },
                legend: {
                    display: false
                },
                tooltips: {

                }
            }
        });
    }
}])