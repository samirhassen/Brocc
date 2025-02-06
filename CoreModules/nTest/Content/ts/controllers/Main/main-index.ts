var app = angular.module('app', ['angular-jsoneditor']);
app
    .controller('ctr', ['$scope', '$http', '$timeout', ($scope: MainIndexNs.Scope, $http: ng.IHttpService, $timeout: ng.ITimeoutService) => {
        function postLocal<TResult>(url: string, postBody: object, skipLoadingIndicator?: boolean): angular.IPromise<{ isError: boolean, errorMessage: string, successResult: TResult }> {
            if (!skipLoadingIndicator) {
                $scope.isLoading = true
            }            
            return $http({
                method: 'POST',
                url: url,
                data: postBody === null ? undefined : postBody
            }).then((response: angular.IHttpResponse<TResult>) => {
                if (!skipLoadingIndicator) {
                    $scope.isLoading = false
                }
                return { isError: false, errorMessage: null, successResult: response.data }
            }, (reason: any) => {
                if (!skipLoadingIndicator) {
                    $scope.isLoading = false
                }                
                let errorMessage: string
                if (reason?.status === 400 && reason?.data?.errorCode) {
                    errorMessage = reason.data.errorMessage
                } else {
                    errorMessage = JSON.stringify(reason)
                }
                return { isError: true, errorMessage: errorMessage, successResult: null as TResult }
            })
        }

        $scope.p = {}
        $scope.isCompanyLoansEnabled = initialData.isCompanyLoansEnabled
        $scope.isMortgageLoansEnabled = initialData.isMortgageLoansEnabled
        $scope.isUnsecuredLoansEnabled = initialData.isUnsecuredLoansEnabled
        $scope.customApplicationUrl = initialData.customApplicationUrl
        $scope.customUnsecuredLoansStandardApplicationUrl = initialData.customUnsecuredLoansStandardApplicationUrl
        $scope.customMortgageLoansStandardApplicationUrl = initialData.customMortgageLoansStandardApplicationUrl
        $scope.customMortgageLoansStandardLoanUrl = initialData.newBackofficeTestBaseUrl
            + (initialData.clientCountry === 'SE' ? '/mortgage-standard/createloan-se' : '/mortgage-standard/createloan')
        $scope.createPaymentFileUrl = initialData.createPaymentFileUrl
        $scope.urlLoginToCustomerPages = initialData.urlLoginToCustomerPages
        $scope.urlLoginToCustomerPagesApplications = initialData.urlLoginToCustomerPagesApplications
        $scope.urlLoginToCustomerPagesOverview = initialData.urlLoginToCustomerPagesOverview
        $scope.urlApplyForSavingsAccountInCustomerPages = initialData.urlApplyForSavingsAccountInCustomerPages
        $scope.urlToGccCustomerApplication = initialData.urlToGccCustomerApplication
        $scope.urlToBackoffice = initialData.urlToBackoffice
        $scope.urlLoginToCustomerPagesMortgageLoanApplication = initialData.urlLoginToCustomerPagesMortgageLoanApplication
        $scope.urlToCustomerPagesMortageLoanCalculator = initialData.urlToCustomerPagesMortageLoanCalculator
        $scope.apiDocumentationUrl = initialData.apiDocumentationUrl
        $scope.urlToHere = initialData.urlToHere
        $scope.urlToUlStandardWebApplication = initialData.urlToUlStandardWebApplication
        $scope.currentTime = initialData.currentTime
        $scope.canResetEnvironment = initialData.environmentRestoreJobs.length > 0
        $scope.resetEnvironmentOptions = initialData.environmentRestoreJobs
        $scope.resetOptionJobName = initialData.environmentRestoreJobs.length > 0 ? initialData.environmentRestoreJobs[0].JobName : null
        $scope.updateTime = (currentTimeTmp: string) => {
            postLocal<any>('/Api/TimeMachine/SetCurrentTime', { currentTime: currentTimeTmp }).then(x => {
                if (x.isError === false) {
                    $scope.currentTime = currentTimeTmp
                    $scope.isTimeEditMode = false
                } else {
                    $scope.isLoading = false
                    toastr.error('Failed!')
                }
            })
        }
        $scope.addDaysToTime = (nrOfDays: number) => {
            let newCurrentTime = moment($scope.currentTime).add(nrOfDays, 'days').format('YYYY-MM-DDTHH:mm:ssZ')
            $scope.updateTime(newCurrentTime)
        }
        $scope.isEmailProviderDown = initialData.isEmailProviderDown;
        $scope.hasEmailProvider = initialData.hasEmailProvider;
        $scope.toggleEmailProviderDown = (evt ?: Event) => {
            evt?.preventDefault();
            let newIsDown = $scope.isEmailProviderDown = !$scope.isEmailProviderDown;
            postLocal<any>('/Api/EmailProvider-Set-Down', { isDown: newIsDown }).then(x => {
                if (x.isError === false) {
                    $scope.isEmailProviderDown = newIsDown;
                } else {
                    $scope.isLoading = false
                    toastr.error('Failed!')
                }
            })            
        }
        function isValidDate(dateString: string) { //
            var regEx = /^\d{4}-\d{2}-\d{2}$/;
            if (!dateString.match(regEx)) return false;  // Invalid format
            var d = new Date(dateString);
            var dNum = d.getTime();
            if (!dNum && dNum !== 0) return false; // NaN value, Invalid date
            return d.toISOString().slice(0, 10) === dateString;
        }

        function refreshCurrentJob() {
            postLocal<any>('/Api/TestDriver/Credit/PollSimulate', { jobId: $scope.jobId, normalResponseOnError: true }, true).then(x => {
                if (x.isError === false) {
                    $scope.currentTime = x.successResult.currentTime
                    if (x.successResult.newEvents) {
                        angular.forEach(x.successResult.newEvents, function (v) {
                            $scope.simulateOneMonthLogItems.push(v)
                        })
                    }
                    if (x.successResult.isComplete) {
                        $scope.jobId = null
                        $scope.simulateOneMonthLogItems.push("----------DONE---------------")
                    } else {
                        $timeout(refreshCurrentJob, 5000)
                    }
                } else {
                    toastr.error('Failed!')
                    $scope.jobId = null
                }
            })
        }
        $scope.simulate = () => {
            let stopAtDate: string;
            if ($scope.stopAtDate) {
                if (!isValidDate($scope.stopAtDate)) {
                    toastr.warning('Invalid stop at date');
                    return;
                }
                stopAtDate = $scope.stopAtDate;
            }
            if (!$scope.scenario) {
                toastr.warning('Pick a scenario');
                return;
            }
            postLocal<any>('/Api/TestDriver/Credit/BeginSimulate', { scenario: $scope.scenario, scenarioData: $scope.scenarioData, stopAtDate }).then(x => {
                if (x.isError === false) {
                    $scope.currentTime = x.successResult.currentTime
                    $scope.jobId = x.successResult.jobId
                    $scope.simulateOneMonthLogItems = []
                    $scope.stopAtDate = '';
                    $timeout(refreshCurrentJob, 5000)
                } else {
                    toastr.error('Failed!')
                }
            })
        }
        $scope.scenario = ''

        function updateResetState(state: any, other?: { errorMessage?: string, currentResetId ?: string }) {
            $scope.resetting.state = state;
            $scope.resetting.updatedEpoch = Date.now()
            if (other?.errorMessage) {
                $scope.resetting.errorMessage = other.errorMessage
            }
            if (other?.currentResetId) {
                $scope.resetting.currentResetId = other.currentResetId;
            }
        }

        function startResetEnvironment() {
            let jobName = $scope.resetOptionJobName;
            let timeTakenKey = 'ntech_testreset_last_success_ms_v1' + '_' + jobName;
            $scope.resetting = {
                state: 'startingJob',
                startedEpoch: Date.now(),
                lastSuccessTotalMs: localStorage.getItem(timeTakenKey) ? parseInt(localStorage.getItem(timeTakenKey)) : undefined
            };            
            postLocal<{ currentResetId: string }>('/Api/ResetEnvironment/Start', { shouldStartProcess: true, jobName: jobName }).then(x => {
                if (x.isError) {
                    updateResetState('failed', { errorMessage: x.errorMessage });
                } else {
                    updateResetState('waitingForAppPoolsToStop', { currentResetId: x.successResult.currentResetId });
                    let intervalId = setInterval(() => {
                        postLocal<{ currentResetId: string }>('/Api/ResetEnvironment/Get-Id', {}, true).then(x => {
                            if ($scope.resetting.state === 'success') {
                                //Dont need to do anything here. The page will be reloaded shortly
                            } else if (x.isError && $scope.resetting.state === 'waitingForAppPoolsToStop') {
                                //Service is down which should mean the job stopped the app pools. Waiting for them to come back up which is the last step.
                                updateResetState('waitingForAppPoolsToRestart')
                            } else if (!x.isError && $scope.resetting.state === 'waitingForAppPoolsToRestart') {
                                clearInterval(intervalId);
                                //App pool back up
                                if (x.successResult.currentResetId) {
                                    updateResetState('failed', { errorMessage: 'Something went wrong - a reset is pending but not the same one that was started from here' })
                                } else {
                                    updateResetState('success');
                                    let totalTimeInMs = Date.now() - $scope.resetting.startedEpoch;
                                    localStorage.setItem(timeTakenKey, totalTimeInMs.toString());
                                    $scope.isLoading = true
                                    $timeout(() => { document.location.reload(); }, 500)
                                }
                            } else if (!x.isError && $scope.resetting.state === 'waitingForAppPoolsToStop') {
                                $scope.resetting.updatedEpoch = Date.now() //Still waiting
                            } else if (x.isError && $scope.resetting.state === 'waitingForAppPoolsToRestart') {
                                $scope.resetting.updatedEpoch = Date.now()//Still waiting
                            } else {
                                updateResetState('failed', { errorMessage: 'Something went wrong' });
                                clearInterval(intervalId);
                            }
                        });
                    }, 2000);
                }
            })
        }

        $scope.startResetEnvironment = () => {
            startResetEnvironment();
        }
        $scope.getResetTimeText = () => {
            if (!$scope?.resetting?.startedEpoch) {
                return ''
            }
            let currentElapsedTimeMs = Math.round(($scope.resetting.updatedEpoch - $scope.resetting.startedEpoch) / 1000).toFixed(0);
            let text = currentElapsedTimeMs + " seconds"
            if ($scope.resetting.lastSuccessTotalMs) {
                text += ' (last time took ' + Math.round($scope.resetting.lastSuccessTotalMs / 1000).toFixed(0) + ' seconds)'
            }
            return text
        }

        window.scope = $scope
    }])

module MainIndexNs {
    export interface Scope extends ng.IScope {
        p: any
        isCompanyLoansEnabled: boolean
        isMortgageLoansEnabled: boolean
        isUnsecuredLoansEnabled: boolean
        customApplicationUrl: string
        customUnsecuredLoansStandardApplicationUrl: string
        customMortgageLoansStandardApplicationUrl: string
        customMortgageLoansStandardLoanUrl: string
        createPaymentFileUrl: string
        urlLoginToCustomerPages: string
        urlLoginToCustomerPagesApplications: string
        urlLoginToCustomerPagesOverview: string
        urlApplyForSavingsAccountInCustomerPages: string
        urlToGccCustomerApplication: string
        urlToBackoffice: string
        urlLoginToCustomerPagesMortgageLoanApplication: string
        urlToCustomerPagesMortageLoanCalculator: string
        apiDocumentationUrl: string
        urlToHere: string
        urlToUlStandardWebApplication: string
        currentTime: string //TODO: Server is date
        stopAtDate: string
        updateTime: (currentTimeTmp: string) => void
        isLoading: boolean
        isTimeEditMode: boolean
        jobId: number
        simulateOneMonthLogItems: string[]
        simulate: () => void
        startResetEnvironment: () => void
        canResetEnvironment: boolean
        resetOptionJobName: string
        resetEnvironmentOptions: {
            JobName: string
            DisplayName: string
        }[]
        scenario: string
        scenarioData: string
        addDaysToTime: (nrOfDays: number) => void
        getResetTimeText: () => string
        resetting?: {
            currentResetId?: string
            startedEpoch: number
            state: 'startingJob' | 'waitingForAppPoolsToStop' | 'waitingForAppPoolsToRestart' | 'success' | 'failed',
            errorMessage?: string
            updatedEpoch?: number
            lastSuccessTotalMs ?: number
        }
        isEmailProviderDown: boolean
        hasEmailProvider: boolean
        toggleEmailProviderDown: (evt ?: Event) => void
    }
}