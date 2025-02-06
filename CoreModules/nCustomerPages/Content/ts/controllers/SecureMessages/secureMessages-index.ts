var app = angular.module('app', ['ngSanitize', 'pascalprecht.translate', 'ngCookies', 'ntech.forms'])

ntech.angular.setupTranslation(app)

class SecureMessagesCtr {
    static $inject = ['$scope', '$http', '$q', '$translate', '$timeout', '$filter']
    constructor($scope: SecureMessagesNs.Scope, $http: ng.IHttpService, $q: ng.IQService, $translate: any, $timeout: ng.ITimeoutService, $filter: ng.IFilterService) {
        window.scope = $scope
        let id: SecureMessagesNs.InitialData = initialData

        let apiClient = new NTechCustomerPagesApi.ApiClient(x => { toastr.error(x); $scope.isLoading = false }, $http, $q)

        $scope.ci = id.customerSecureMessages
        if ($scope.ci && $scope.ci.CustomerChannels) {
            $scope.ci.CustomerChannels = $scope.ci.CustomerChannels.filter(item => item.IsRelation === true)
        }

        $scope.productOverviewUrl = initialData.productOverviewUrl
        $scope.isLoading = false

        function resetAttachedFile() {
            if (!$scope) {
                return
            }

            $scope.attachedFileName = null;
            if ($scope.fileUpload) {
                $scope.fileUpload.reset();
            }
        }

        function init() {
            $scope.showMessageInput = false
            $scope.selectedContext = ''
            $scope.messageText = ''
            //If only one context then dont show dd
            if (!$scope.selectedContext && $scope.ci.CustomerChannels.length === 1) {
                $scope.selectedContext = $scope.ci.CustomerChannels[0].ChannelType + '|' + $scope.ci.CustomerChannels[0].ChannelId
                $scope.showContext = false
            }
            else
                $scope.showContext = true

            resetAttachedFile()
        }
        init()

        var pageSize = 15

        $scope.areMoreTransactions = () => {
            return $scope.ci.TotalMessageCount > $scope.ci.Messages.length
        }

        function appendLoadedTransactions(newTransactions: NTechCustomerPagesApi.GetMessagesResponseMessage[], newTotalCount: number) {
            angular.forEach(newTransactions, (tr) => {
                $scope.ci.Messages.push(tr)
            })
            $scope.ci.TotalMessageCount = newTotalCount
        }

        $scope.reload = () => {
            $scope.isLoading = true

            apiClient.getSecureMessages({
                IncludeChannels: true, IncludeMessageTexts: true, SkipCount: 0, TakeCount: pageSize
            }).then(x => {
                $scope.isLoading = false
                $scope.ci.Messages = []
                appendLoadedTransactions(x.Messages, x.TotalMessageCount)
            })
        }

        $scope.loadMoreTransactions = (evt) => {
            if (evt) {
                evt.preventDefault()
            }
            $scope.isLoading = true

            apiClient.getSecureMessages({
                IncludeChannels: true, IncludeMessageTexts: true, SkipCount: $scope.ci.Messages.length, TakeCount: pageSize
            }).then(x => {
                $scope.isLoading = false
                appendLoadedTransactions(x.Messages, x.TotalMessageCount)
            })
        }

        $scope.sendMessage = () => {
            if (!$scope.selectedContext) {
                return
            }

            $scope.isLoading = true

            var channelType = $scope.selectedContext.split('|')[0]
            var channelId = $scope.selectedContext.split('|')[1]

            var saveMessage = (attachedFileAsDataUrl: string, attachedFileName: string) => {
                apiClient.createSecureMessage({
                    ChannelType: channelType, ChannelId: channelId, Text: $scope.messageText, TextFormat: null
                }).then(x => {
                    $scope.messageText = null
                    //Attach file to ndocument and nCustomer
                    if (attachedFileAsDataUrl) {
                        apiClient.attachMessageDocument({
                            MessageId: x.CreatedMessage.Id,
                            AttachedFileAsDataUrl: attachedFileAsDataUrl,
                            AttachedFileName: attachedFileName
                        }).then(y => {
                            $scope.isLoading = false
                            init();
                            $scope.reload();
                        })
                    } else {
                        $scope.isLoading = false
                        $scope.ci.Messages.unshift(x.CreatedMessage)
                        init()
                    }
                })
            }
            if ($scope.fileUpload) {
                if ($scope.fileUpload.hasAttachedFiles()) {
                    $scope.fileUpload.loadSingleAttachedFileAsDataUrl().then(result => {
                        saveMessage(result.dataUrl, result.filename)
                    }, err => {
                        toastr.warning(err)
                    })
                } else {
                    saveMessage(null, null)
                }
            } else {
                saveMessage(null, null)
            }
        }

        $scope.isSendMessageAllowed = () => {
            return (!$scope.messageText || !$scope.selectedContext)
        }

        function getTimeAgo(date: Date): moment.Duration {
            return moment.duration(moment(initialData.today).diff(moment(date)))
        }

        $scope.beginSendMessage = () => {
            $scope.showMessageInput = true;
        }

        $scope.selectFileToAttach = (evt: Event) => {
            if (evt) {
                evt.preventDefault()
            }

            $scope.setupFiles();
            $scope.fileUpload.showFilePicker();
        }

        $scope.setupFiles = () => {
            if (!$scope.fileUpload) {
                $scope.fileUpload = new NtechAngularFileUpload.FileUploadHelper((<HTMLInputElement>document.getElementById('file')),
                    (<HTMLFormElement>document.getElementById('secureform')),
                    $scope, $q);
                $scope.fileUpload.addFileAttachedListener((filenames: string[]) => {
                    if (filenames.length == 0) {
                        $scope.attachedFileName = null
                    } else if (filenames.length == 1) {
                        $scope.attachedFileName = filenames[0];
                    } else {
                        $scope.attachedFileName = 'Error - multiple files selected!'
                    }
                });
            } else {
                resetAttachedFile()
            }
        }

        $scope.removeDocument = (evt: Event) => {
            if (evt) {
                evt.preventDefault()
            }
            resetAttachedFile()
        }

        $scope.formatDate = (date: Date) => {
            let dayTranslationKeys = ['coi_sunday', 'coi_monday', 'coi_tuesday', 'coi_wednesday', 'coi_thursday', 'coi_friday', 'coi_saturday']
            let nowMoment = moment(initialData.today)
            let dateMoment = moment(date)
            let timeString = dateMoment.format('HH:mm')

            if (dateMoment.clone().startOf('day') > nowMoment.clone().startOf('day')) { //Should not be possible
                return dateMoment.clone().format($translate.use() == 'fi' ? 'DD-MM-YYYY' : 'YYYY-MM-DD') + ' ' + timeString + ' (f)'
            } else if (dateMoment.isSame(nowMoment, 'date')) { //today
                return $translate.instant('coi_today', { time: timeString })
            }
            else if (dateMoment > nowMoment.clone().startOf('day').subtract(1, 'day')) { //yesterday
                return $translate.instant('coi_yesterday', { time: timeString })
            }
            else if (dateMoment > nowMoment.clone().subtract(1, 'weeks').startOf('day').add(1, 'day')) { //less than one week ago (stop before the same weekday repeats)
                return $translate.instant(dayTranslationKeys[dateMoment.clone().toDate().getDay()]) + ' ' + timeString
            }
            else {
                return dateMoment.clone().format($translate.use() == 'fi' ? 'DD-MM-YYYY' : 'YYYY-MM-DD') + ' ' + timeString
            }
        }
    }
}

app.controller('ctr', SecureMessagesCtr)

module SecureMessagesNs {
    export interface InitialData {
        translation: any
        customerSecureMessages: NTechCustomerPagesApi.GetMessagesResponse
        today: Date
        productOverviewUrl: string
    }

    export interface Scope extends ng.IScope {
        ci: NTechCustomerPagesApi.GetMessagesResponse
        productOverviewUrl: string
        showMessageInput: boolean
        isLoading: boolean
        showContext: boolean
        areMoreTransactions: () => boolean
        loadMoreTransactions: (evt: Event) => void
        isSendMessageAllowed: () => boolean
        beginSendMessage: () => void
        sendMessage: () => void
        formatDate: (date: Date) => string
        selectedContext: string
        messageText: string
        removeDocument: (evt: Event) => void
        selectFileToAttach: (evt: Event) => void
        setupFiles: () => void
        onDocumentsAddedOrRemoved: (() => void)
        attachedFileName: string
        fileUpload: NtechAngularFileUpload.FileUploadHelper
        reload: () => void
    }
}