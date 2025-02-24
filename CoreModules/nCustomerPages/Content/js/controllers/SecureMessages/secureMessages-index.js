var app = angular.module('app', ['ngSanitize', 'pascalprecht.translate', 'ngCookies', 'ntech.forms']);
ntech.angular.setupTranslation(app);
var SecureMessagesCtr = /** @class */ (function () {
    function SecureMessagesCtr($scope, $http, $q, $translate, $timeout, $filter) {
        window.scope = $scope;
        var id = initialData;
        var apiClient = new NTechCustomerPagesApi.ApiClient(function (x) { toastr.error(x); $scope.isLoading = false; }, $http, $q);
        $scope.ci = id.customerSecureMessages;
        if ($scope.ci && $scope.ci.CustomerChannels) {
            $scope.ci.CustomerChannels = $scope.ci.CustomerChannels.filter(function (item) { return item.IsRelation === true; });
        }
        $scope.productOverviewUrl = initialData.productOverviewUrl;
        $scope.isLoading = false;
        function resetAttachedFile() {
            if (!$scope) {
                return;
            }
            $scope.attachedFileName = null;
            if ($scope.fileUpload) {
                $scope.fileUpload.reset();
            }
        }
        function init() {
            $scope.showMessageInput = false;
            $scope.selectedContext = '';
            $scope.messageText = '';
            //If only one context then dont show dd
            if (!$scope.selectedContext && $scope.ci.CustomerChannels.length === 1) {
                $scope.selectedContext = $scope.ci.CustomerChannels[0].ChannelType + '|' + $scope.ci.CustomerChannels[0].ChannelId;
                $scope.showContext = false;
            }
            else
                $scope.showContext = true;
            resetAttachedFile();
        }
        init();
        var pageSize = 15;
        $scope.areMoreTransactions = function () {
            return $scope.ci.TotalMessageCount > $scope.ci.Messages.length;
        };
        function appendLoadedTransactions(newTransactions, newTotalCount) {
            angular.forEach(newTransactions, function (tr) {
                $scope.ci.Messages.push(tr);
            });
            $scope.ci.TotalMessageCount = newTotalCount;
        }
        $scope.reload = function () {
            $scope.isLoading = true;
            apiClient.getSecureMessages({
                IncludeChannels: true, IncludeMessageTexts: true, SkipCount: 0, TakeCount: pageSize
            }).then(function (x) {
                $scope.isLoading = false;
                $scope.ci.Messages = [];
                appendLoadedTransactions(x.Messages, x.TotalMessageCount);
            });
        };
        $scope.loadMoreTransactions = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.isLoading = true;
            apiClient.getSecureMessages({
                IncludeChannels: true, IncludeMessageTexts: true, SkipCount: $scope.ci.Messages.length, TakeCount: pageSize
            }).then(function (x) {
                $scope.isLoading = false;
                appendLoadedTransactions(x.Messages, x.TotalMessageCount);
            });
        };
        $scope.sendMessage = function () {
            if (!$scope.selectedContext) {
                return;
            }
            $scope.isLoading = true;
            var channelType = $scope.selectedContext.split('|')[0];
            var channelId = $scope.selectedContext.split('|')[1];
            var saveMessage = function (attachedFileAsDataUrl, attachedFileName) {
                apiClient.createSecureMessage({
                    ChannelType: channelType, ChannelId: channelId, Text: $scope.messageText, TextFormat: null
                }).then(function (x) {
                    $scope.messageText = null;
                    //Attach file to ndocument and nCustomer
                    if (attachedFileAsDataUrl) {
                        apiClient.attachMessageDocument({
                            MessageId: x.CreatedMessage.Id,
                            AttachedFileAsDataUrl: attachedFileAsDataUrl,
                            AttachedFileName: attachedFileName
                        }).then(function (y) {
                            $scope.isLoading = false;
                            init();
                            $scope.reload();
                        });
                    }
                    else {
                        $scope.isLoading = false;
                        $scope.ci.Messages.unshift(x.CreatedMessage);
                        init();
                    }
                });
            };
            if ($scope.fileUpload) {
                if ($scope.fileUpload.hasAttachedFiles()) {
                    $scope.fileUpload.loadSingleAttachedFileAsDataUrl().then(function (result) {
                        saveMessage(result.dataUrl, result.filename);
                    }, function (err) {
                        toastr.warning(err);
                    });
                }
                else {
                    saveMessage(null, null);
                }
            }
            else {
                saveMessage(null, null);
            }
        };
        $scope.isSendMessageAllowed = function () {
            return (!$scope.messageText || !$scope.selectedContext);
        };
        function getTimeAgo(date) {
            return moment.duration(moment(initialData.today).diff(moment(date)));
        }
        $scope.beginSendMessage = function () {
            $scope.showMessageInput = true;
        };
        $scope.selectFileToAttach = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.setupFiles();
            $scope.fileUpload.showFilePicker();
        };
        $scope.setupFiles = function () {
            if (!$scope.fileUpload) {
                $scope.fileUpload = new NtechAngularFileUpload.FileUploadHelper(document.getElementById('file'), document.getElementById('secureform'), $scope, $q);
                $scope.fileUpload.addFileAttachedListener(function (filenames) {
                    if (filenames.length == 0) {
                        $scope.attachedFileName = null;
                    }
                    else if (filenames.length == 1) {
                        $scope.attachedFileName = filenames[0];
                    }
                    else {
                        $scope.attachedFileName = 'Error - multiple files selected!';
                    }
                });
            }
            else {
                resetAttachedFile();
            }
        };
        $scope.removeDocument = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            resetAttachedFile();
        };
        $scope.formatDate = function (date) {
            var dayTranslationKeys = ['coi_sunday', 'coi_monday', 'coi_tuesday', 'coi_wednesday', 'coi_thursday', 'coi_friday', 'coi_saturday'];
            var nowMoment = moment(initialData.today);
            var dateMoment = moment(date);
            var timeString = dateMoment.format('HH:mm');
            if (dateMoment.clone().startOf('day') > nowMoment.clone().startOf('day')) { //Should not be possible
                return dateMoment.clone().format($translate.use() == 'fi' ? 'DD-MM-YYYY' : 'YYYY-MM-DD') + ' ' + timeString + ' (f)';
            }
            else if (dateMoment.isSame(nowMoment, 'date')) { //today
                return $translate.instant('coi_today', { time: timeString });
            }
            else if (dateMoment > nowMoment.clone().startOf('day').subtract(1, 'day')) { //yesterday
                return $translate.instant('coi_yesterday', { time: timeString });
            }
            else if (dateMoment > nowMoment.clone().subtract(1, 'weeks').startOf('day').add(1, 'day')) { //less than one week ago (stop before the same weekday repeats)
                return $translate.instant(dayTranslationKeys[dateMoment.clone().toDate().getDay()]) + ' ' + timeString;
            }
            else {
                return dateMoment.clone().format($translate.use() == 'fi' ? 'DD-MM-YYYY' : 'YYYY-MM-DD') + ' ' + timeString;
            }
        };
    }
    SecureMessagesCtr.$inject = ['$scope', '$http', '$q', '$translate', '$timeout', '$filter'];
    return SecureMessagesCtr;
}());
app.controller('ctr', SecureMessagesCtr);
//# sourceMappingURL=secureMessages-index.js.map