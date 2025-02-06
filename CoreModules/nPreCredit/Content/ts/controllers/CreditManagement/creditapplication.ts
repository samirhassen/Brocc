var app = angular.module('app', ['ntech.forms', 'ntech.components'])

class CreditApplicationCtr {
    static $inject = ['$scope', '$http', '$q', '$timeout', 'trafficCop', 'ntechComponentService']
    constructor(
        $scope: CreditApplicationNs.ICreditApplicationScope,
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $timeout: ng.ITimeoutService,
        trafficCop: NTechComponents.NTechHttpTrafficCopService,
        ntechComponentService: NTechComponents.NTechComponentService
    ) {
        $scope.applicationInfo = initialData.ApplicationInfo
        let ai = $scope.applicationInfo
        $scope.isApplicationActive = () => $scope.applicationInfo.IsActive === true
        $scope.isApplicationInactive = () => $scope.applicationInfo.IsActive === false

        $scope.app = initialData
        $scope.applicationCheckpointsInitialData = { applicationNr: initialData.ApplicationNr, applicationType: 'unsecuredLoan' }

        $scope.m = {
            additionalQuestionsInitialData: { applicationInfo: $scope.applicationInfo },
            customerCheckInitialData: {
                applicationNr: initialData.ApplicationNr,
            },
            fraudCheckInitialData: { applicationInfo: $scope.applicationInfo },
            creditCheckStatusInitialData: { applicationInfo: $scope.applicationInfo }
        }

        $scope.creditCheckBlockInitialData = {
            isInitiallyExpanded: ai.CreditCheckStatus !== 'Accepted',
            title: 'Credit',
            status: ai.CreditCheckStatus,
            isActive: ai.IsActive
        }
        $scope.additionalQuestionsBlockInitialData = {
            isInitiallyExpanded: ai.IsActive && ai.CreditCheckStatus === 'Accepted' && ai.AgreementStatus !== 'Accepted', //TODO: block logic
            isActive: ai.IsActive,
            status: ai.AgreementStatus,
            title: 'Agreement'
        }
        $scope.customerCheckBlockInitialData = {
            isInitiallyExpanded: ai.IsActive && ai.CustomerCheckStatus !== 'Accepted' && ai.AgreementStatus === 'Accepted', //TODO: Use top block that is not done when all use application status block
            title: 'Customer',
            status: ai.CustomerCheckStatus,
            isActive: ai.IsActive
        }
        $scope.documentCheckBlockInitialData = {
            isInitiallyExpanded: ai.IsActive && ai.AgreementStatus === 'Accepted' && $scope.app.DocumentCheckStatus !== 'Accepted',
            title: 'Proof of income',
            status: $scope.app.DocumentCheckStatus,
            isActive: ai.IsActive
        }
        $scope.fraudCheckBlockInitialData = {
            isInitiallyExpanded: ai.IsActive && ai.AgreementStatus === 'Accepted' && $scope.app.DocumentCheckStatus === 'Accepted' && ai.FraudCheckStatus !== 'Accepted',
            title: 'Fraud',
            status: ai.FraudCheckStatus,
            isActive: ai.IsActive
        }

        window.scope = $scope; //for console debugging
        $scope.isLoading = false
        $scope.isUsingDirectLinkFlow = initialData.Provider.IsUsingDirectLinkFlow

        $scope.onKycScreenDone = customerId => {
            location.reload()
            return false
        }

        let preCreditApiClient = new NTechPreCreditApi.ApiClient(msg => toastr.error(msg), $http, $q);
        $scope.isLoading = trafficCop.pending.all > 0;
        trafficCop.addStateChangeListener(() => {
            $scope.isLoading = trafficCop.pending.all > 0
        })

        $scope.onBack = (evt) => {
            if (evt) {
                evt.preventDefault()
            }
            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, preCreditApiClient, $q, { applicationNr: initialData.ApplicationNr }, NavigationTargetHelper.NavigationTargetCode.UnsecuredLoanApplications)
        }

        $scope.apiClient = preCreditApiClient
        let reloadComments = () => {
            $scope.commentsInitialData = {
                applicationInfo: $scope.applicationInfo,
                reloadPageOnWaitingForAdditionalInformation: true
            }
        }
        reloadComments()

        ntechComponentService.subscribeToReloadRequired(x => {
            ntechComponentService.ntechLog.logDebug('Reloading due to ' + x.sourceComponentName)
            location.reload()
        })

        $scope.customerInitialData1 = {
            applicantNr: 1,
            applicationNr: initialData.ApplicationNr,
            backTarget: initialData.NavigationTargetToHere,
            showKycBlock: false,
            onkycscreendone: $scope.onKycScreenDone,
            customerIdCompoundItemName: null
        }
        if ($scope.applicationInfo.NrOfApplicants > 1) {
            $scope.customerInitialData2 = {
                applicantNr: 2,
                applicationNr: initialData.ApplicationNr,
                backTarget: initialData.NavigationTargetToHere,
                showKycBlock: false,
                onkycscreendone: $scope.onKycScreenDone,
                customerIdCompoundItemName: null
            }
        }

        if (initialData.WarningMessage) {
            toastr.warning(initialData.WarningMessage)
        }

        $scope.isReactivateApplicationAllowed = function (evt) {
            if (evt) {
                evt.preventDefault()
            }
            //NOTE: isNewCreditCheckAllowed is intended to exactly match when the new credit check button is allowed.
            let isNewCreditCheckAllowed = !($scope.applicationInfo.IsFinalDecisionMade === true || $scope.applicationInfo.AgreementStatus === 'Accepted');
            let isAllowedDueToCancelled = $scope.applicationInfo.IsCancelled === true && $scope.applicationInfo.IsWaitingForAdditionalInformation === false;
            return $scope.applicationInfo.IsActive === false && (isNewCreditCheckAllowed || isAllowedDueToCancelled);
        }

        $scope.reactivateApplication = function (evt) {
            if (evt) { evt.preventDefault() }
            if (!$scope.isReactivateApplicationAllowed(null)) {
                return;
            }
            $scope.isLoading = true
            $http({
                method: 'POST',
                url: $scope.app.ReactivateApplicationUrl,
                data: {}
            }).then(function successCallback(response) {
                location.reload()
            }, function errorCallback(response) {
                $scope.isLoading = false
                location.reload()
            })
        }

        $scope.headerClassFromStatus = function (status) {
            var isAccepted = status === 'Accepted'
            var isRejected = status === 'Rejected'
            var isOther = !isAccepted && !isRejected
            return { 'text-success': isAccepted, 'text-danger': isRejected }
        }

        $scope.iconClassFromStatus = function (status) {
            var isAccepted = status === 'Accepted'
            var isRejected = status === 'Rejected'
            var isOther = !isAccepted && !isRejected
            return { 'glyphicon-ok': isAccepted, 'glyphicon-remove': isRejected, 'glyphicon-minus': isOther, 'glyphicon': true, 'text-success': isAccepted, 'text-danger': isRejected }
        }

        $scope.isRejectAllowed = function (evt) {
            if (evt) { evt.preventDefault() }
            return $scope.applicationInfo.IsActive === true && $scope.applicationInfo.IsWaitingForAdditionalInformation === false && ($scope.applicationInfo.CreditCheckStatus === 'Rejected' || $scope.applicationInfo.CustomerCheckStatus === 'Rejected' || $scope.applicationInfo.FraudCheckStatus === 'Rejected' || $scope.app.DocumentCheckStatus === 'Rejected')
        }

        $scope.rejectApplication = function (evt) {
            if (evt) { evt.preventDefault() }
            if (!$scope.isRejectAllowed(null)) {
                return;
            }
            $scope.isLoading = true
            $http({
                method: 'POST',
                url: $scope.app.RejectApplicationUrl,
                data: {}
            }).then((response: ng.IHttpResponse<CreditApplicationNs.IRejectApplicationResult>) => {
                if (response.data.userWarningMessage && response.data.redirectToUrl) {
                    //Give the user a chance to actually read the message before we move on
                    toastr.warning(response.data.userWarningMessage)
                    $timeout(() => {
                        document.location.href = response.data.redirectToUrl    
                    }, 3000);                    
                } else if (response.data.userWarningMessage) {
                    $scope.isLoading = false
                    toastr.warning(response.data.userWarningMessage)                    
                } else if (response.data.redirectToUrl) {
                    document.location.href = response.data.redirectToUrl
                }

                if (!$scope.isLoading) {
                    //Reload comments if we intend to remain on this page
                    reloadComments()
                }
            }, (response) => {
                if (response.data?.errorCode && response.data?.errorMessage) {
                    toastr.warning(response.data?.errorMessage);
                    reloadComments();
                } else {
                    location.reload()
                }                
            })
        }

        $scope.isApproveApplicationAllowed = function (evt) {
            if (evt) { evt.preventDefault() }

            return $scope.applicationInfo.IsActive === true && $scope.applicationInfo.IsWaitingForAdditionalInformation === false && $scope.applicationInfo.IsPartiallyApproved !== true && $scope.applicationInfo.CreditCheckStatus === 'Accepted' && $scope.applicationInfo.CustomerCheckStatus === 'Accepted' && $scope.app.DocumentCheckStatus === 'Accepted' && $scope.applicationInfo.FraudCheckStatus === 'Accepted' && $scope.applicationInfo.AgreementStatus === 'Accepted'
        }

        $scope.approveApplication = function (evt) {
            if (evt) { evt.preventDefault() }
            if (!$scope.isApproveApplicationAllowed(null)) {
                return;
            }
            $scope.isLoading = true
            if ($scope.app.ApplicationType === 'mortgageLoan') {
                toastr.error('Mortage loans not supported')
            } else {
                preCreditApiClient.approveApplication($scope.app.ApproveApplicationUrl).then(result => {
                    if (result.userWarningMessage && (result.redirectToUrl || result.reloadPage)) {
                        //Give the user a chance to actually read the message before we move on
                        toastr.warning(result.userWarningMessage)
                        $timeout(() => {
                            if (result.redirectToUrl) {
                                document.location.href = result.redirectToUrl;
                            } else {
                                location.reload();
                            }
                        }, 3000);
                    } else if (result.userWarningMessage) {
                        $scope.isLoading = false
                        toastr.warning(result.userWarningMessage)
                    } else if (result.redirectToUrl) {
                        document.location.href = result.redirectToUrl
                    } else if (result.reloadPage) {
                        location.reload();
                    }

                    if (!$scope.isLoading) {
                        //Reload comments if we intend to remain on this page
                        reloadComments()
                    }
                }, errorMessage => {
                    $scope.isLoading = false
                });
            }
        }

        $scope.isCancelApplicationAllowed = function (evt) {
            if (evt) { evt.preventDefault() }

            return $scope.applicationInfo.IsActive === true && $scope.applicationInfo.IsFinalDecisionMade === false && $scope.applicationInfo.IsWaitingForAdditionalInformation === false
        }

        $scope.cancelApplication = function (evt) {
            if (evt) { evt.preventDefault() }
            if (!$scope.isCancelApplicationAllowed(null)) {
                return;
            }
            $scope.isLoading = true
            $http({
                method: 'POST',
                url: $scope.app.CancelApplicationUrl,
                data: {}
            }).then(function successCallback(response) {
                $scope.isLoading = false
                location.reload()
            }, function errorCallback(response) {
                $scope.isLoading = false
                location.reload()
            })
        }

        $scope.flagCustomersAsExternallyOnboarded = function (evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            $scope.isLoading = true
            $http({
                method: 'POST',
                url: '/CreditManagement/FlagCustomersAsExternallyOnboarded',
                data: { applicationNr: $scope.applicationInfo.ApplicationNr }
            }).then(function successCallback(response: any) {
                location.reload()//To refresh customer status
            }, function errorCallback(response) {
                $scope.isLoading = false
                toastr.error(response.statusText)
            })
        }

        if (initialData.IsTest) {
            $scope.onArchive = evt => {
                if (evt) {
                    evt.preventDefault()
                }
                preCreditApiClient.archiveSingleApplication(ai.ApplicationNr).then(() => {
                    document.location.reload()
                })                                
            }
        }
        window.scope = $scope
    }
}

app.controller('ctr', CreditApplicationCtr)

module CreditApplicationNs {
    export interface ICreditApplicationScope extends ng.IScope {
        app: any,
        isLoading: boolean,
        isUsingDirectLinkFlow: boolean,
        attachedFileName: string,
        onKycScreenDone: (customerId: number) => boolean,
        isReactivateApplicationAllowed: (evt: Event) => boolean,
        reactivateApplication: (evt: Event) => void,
        headerClassFromStatus: (status: string) => any,
        iconClassFromStatus: (status: string) => any,
        isRejectAllowed: (evt: Event) => boolean,
        rejectApplication: (evt: Event) => void,
        isApproveApplicationAllowed: (evt: Event) => boolean,
        approveApplication: (evt: Event) => void,
        isCancelApplicationAllowed: (evt: Event) => boolean,
        cancelApplication: (evt: Event) => void,
        flagCustomersAsExternallyOnboarded: (evt: Event) => void,
        commentsInitialData: ApplicationCommentsComponentNs.InitialData
        applicationCheckpointsInitialData: ApplicationCheckpointsComponentNs.InitialData
        apiClient: NTechPreCreditApi.ApiClient
        applicationInfo: NTechPreCreditApi.ApplicationInfoModel
        isApplicationActive: () => boolean
        isApplicationInactive: () => boolean
        customerInitialData1: ApplicationCustomerInfoComponentNs.InitialData
        customerInitialData2: ApplicationCustomerInfoComponentNs.InitialData

        customerCheckBlockInitialData: ApplicationStatusBlockComponentNs.InitialData
        additionalQuestionsBlockInitialData: ApplicationStatusBlockComponentNs.InitialData
        documentCheckBlockInitialData: ApplicationStatusBlockComponentNs.InitialData
        fraudCheckBlockInitialData: ApplicationStatusBlockComponentNs.InitialData
        creditCheckBlockInitialData: ApplicationStatusBlockComponentNs.InitialData

        m: HandleNgIfModel
        onBack: (evt: Event) => void
        onArchive?: (evt: Event) => void
    }

    export interface HandleNgIfModel {
        additionalQuestionsInitialData: UnsecuredApplicationAdditionalQuestionsComponentNs.InitialData
        customerCheckInitialData: UnsecuredApplicationCustomerCheckComponentNs.InitialData
        fraudCheckInitialData: UnsecuredApplicationFraudCheckComponentNs.InitialData
        creditCheckStatusInitialData: UnsecuredApplicationCreditCheckStatusComponentNs.InitialData
    }

    export interface IRejectApplicationResult {
        redirectToUrl: string,
        userWarningMessage: string,
        newComment: any
    }
}