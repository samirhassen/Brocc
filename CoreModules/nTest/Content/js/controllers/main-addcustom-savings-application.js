var app = angular.module('app', ['angular-jsoneditor']);
app
    .controller('ctr', ['$scope', '$http', '$timeout', function ($scope, $http, $timeout) {
        $scope.savingsAccountUrlPrefix = initialData.savingsAccountUrlPrefix
        $scope.backUrl = initialData.backUrl

        /*Make едц and such work*/
        function b64EncodeUnicode(str) {
            return btoa(encodeURIComponent(str).replace(/%([0-9A-F]{2})/g, function (match, p1) {
                return String.fromCharCode('0x' + p1);
            }));
        }

        var storedHistoryItems = localStorage.getItem('ntech_customsavingsapplicaions_historyitems_v2')
        if (storedHistoryItems) {
            $scope.history = { items: JSON.parse(storedHistoryItems) }
        } else {
            $scope.history = { items: [] }
        }

        function setInitial() {
            $scope.state = 'initial'
            $scope.initial = { applicant1: 'New', remarks: 'NoRemarks' }
            $scope.applicants = null
            $scope.application = null
        }

        $scope.submitInitial = function (evt) {
            evt.preventDefault()

            $scope.state = 'working'

            var nrOfTestPersonsToGenerate = 0
            if ($scope.initial.applicant1 === 'New') {
                nrOfTestPersonsToGenerate = nrOfTestPersonsToGenerate + 1
            }

            var afterGeneratePersons = function (data) {
                $scope.state = 'applicants'
                var applicants = {}

                if ($scope.initial.applicant1 === 'New') {
                    applicants.newApplicant1 = {
                        data: JSON.parse(data.applicants[0]),
                        options: { mode: 'tree' }
                    }
                } else if ($scope.initial.applicant1 === 'Existing') {
                    applicants.existingApplicant1 = {}
                }

                $scope.applicants = applicants
                $scope.isLoading = false
            }

            $scope.isLoading = true
            if (nrOfTestPersonsToGenerate > 0) {
                $http({
                    method: 'POST',
                    url: '/Api/TestPerson/Generate',
                    data: { isAccepted: $scope.initial.remarks === 'NoRemarks', count: nrOfTestPersonsToGenerate }
                }).then(function successCallback(response) {
                    afterGeneratePersons({ applicants: response.data.applicants })
                }, function errorCallback(response) {
                    $scope.isLoading = false
                    toastr.error('Failed!')
                })
            } else {
                afterGeneratePersons()
            }
        }

        $scope.submitApplicants = function (evt) {
            evt.preventDefault()

            var applicant1CivicRegNr = null
            var applicant2CivicRegNr = null

            var afterUpdateApplicants = function () {
                if ($scope.initial.applicant1 === 'New') {
                    applicant1CivicRegNr = $scope.applicants.newApplicant1.data.civicRegNr
                } else if ($scope.initial.applicant1 === 'Existing') {
                    applicant1CivicRegNr = $scope.applicants.existingApplicant1.civicRegNr
                }

                $scope.isLoading = true
                $http({
                    method: 'POST',
                    url: '/Api/TestDriver/Credit/CreateCustomSavingsApplicationJson',
                    data: {
                        hasRemarks: $scope.initial.remarks === 'WithRemarks',
                        applicant1CivicRegNr: applicant1CivicRegNr
                    }
                }).then(function successCallback(response) {
                    $scope.state = 'application'
                    $scope.application = {
                        application: {
                            data: JSON.parse(response.data.applicationJson),
                            options: { mode: 'tree' }
                        },
                        applicant1CivicRegNr: applicant1CivicRegNr
                    }
                    $scope.isLoading = false
                }, function errorCallback(response) {
                    $scope.isLoading = false
                    toastr.error('Failed!')
                })
            }

            if ($scope.initial.applicant1 === 'New') {
                var persons = []

                if ($scope.initial.applicant1 === 'New') {
                    persons.push(JSON.stringify($scope.applicants.newApplicant1.data))
                }

                $scope.isLoading = true
                $http({
                    method: 'POST',
                    url: '/Api/TestPerson/CreateOrUpdate',
                    data: { persons: persons }
                }).then(function successCallback(response) {
                    afterUpdateApplicants()
                }, function errorCallback(response) {
                    $scope.isLoading = false
                    toastr.error('Failed!')
                })
            } else {
                afterUpdateApplicants()
            }
        }

        $scope.submitApplication = function (evt) {
            $scope.isLoading = true

            var scenarioData = b64EncodeUnicode(JSON.stringify({
                applicationJson: JSON.stringify($scope.application.application.data)
            }))

            $http({
                method: 'POST',
                url: '/Api/TestDriver/Credit/Simulate',
                data: { scenario: 'AddCustomSavingsApplication', scenarioData: scenarioData, returnCallLog: true }
            }).then(function successCallback(response) {
                $scope.history.items.unshift({
                    date: moment().format('YYYY-MM-DD HH:mm'),
                    callLog: response.data.callLog,
                    applicant1CivicRegNr: $scope.application.applicant1CivicRegNr,
                    remarks: $scope.initial.remarks,
                    savingsAccountNr: response.data.outputDataContext ? response.data.outputDataContext.savingsAccountNr : null
                })
                if ($scope.history.items.length > 20) {
                    $scope.history.items.splice(-1, 1)
                }
                localStorage.setItem('ntech_customsavingsapplicaions_historyitems_v2', JSON.stringify($scope.history.items))
                $scope.isLoading = false
                setInitial()
            }, function errorCallback(response) {
                $scope.isLoading = false
                toastr.error('Failed!')
            })
        }

        setInitial()

        window.scope = $scope
    }])