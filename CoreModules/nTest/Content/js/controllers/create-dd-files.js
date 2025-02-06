var app = angular.module('app', []);
app
    .config([
        '$compileProvider',
        function ($compileProvider) {
            $compileProvider.aHrefSanitizationWhitelist(/^\s*(https?|ftp|mailto|chrome-extension|data|http):/);
        }
    ])
    .controller('ctr', ['$scope', '$http', '$timeout', function ($scope, $http, $timeout) {
        $scope.m = {
            fileType: 'incomingStatusChange'
        }

        var resetIncomingStatusChangeModel = function () {
            var s = {
                CopyToImportFolder: true,
                BankGiroCustomerNr: initialData.bgcCustomerNr,
                ClientBankGiroNr: initialData.bankGiroNr,
                Items:[]
            }

            $scope.m.incomingStatusChange = s
            $scope.m.incomingStatusChangePatternCode = '0432'
            $scope.m.incomingStatusChangeItem = {
                InfoCode: '04',
                CommentCode: '32'
            }
        }
        resetIncomingStatusChangeModel()

        var resetIncomingStatusChangeItem = function (patternCode) {
            var p = patternCode
            if (p == 'custom') {
                $scope.m.incomingStatusChangeItem = {
                    CommentCode: '',
                    InfoCode: ''
                }
            } else {
                $scope.m.incomingStatusChangeItem = {
                    InfoCode: p.substr(0, 2),
                    CommentCode: p.substr(2, 2)
                }
            }
        }

        $scope.onincomingStatusChangePatternCodeChanged = function () {
            if ($scope.m && $scope.m.incomingStatusChangePatternCode) {
                resetIncomingStatusChangeItem($scope.m.incomingStatusChangePatternCode)
            }
        }

        $scope.addIncomingStatusChangeItem = function (evt) {
            if (evt) {
                evt.preventDefault()
            }
            $scope.m.incomingStatusChange.Items.push($scope.m.incomingStatusChangeItem)
            resetIncomingStatusChangeItem('0432')
        }

        $scope.createIncomingStatusChangeFile = function (evt) {
            if (evt) {
                evt.preventDefault()
            }
            $scope.isLoading = true

            $http({
                method: 'POST',
                url: '/Api/CreateIncomingDirectDebitStatusChangeFile',
                data: { request: $scope.m.incomingStatusChange }
            }).then(function successCallback(response) {
                $scope.m.incomingStatusChangeFile = {
                    fileAsDataUrl: response.data.fileAsDataUrl,
                    fileName: response.data.fileName
                }
                if (response.data.copyFailedMessage) {
                    toastr.warning('File created: ' + + response.fileName + ' but copy failed: ' + response.data.copyFailedMessage)
                } else {
                    toastr.info('File created and copied: ' + response.fileName)
                }
                resetIncomingStatusChangeModel()
                $scope.isLoading = false
            }, function errorCallback(response) {
                $scope.isLoading = false
                toastr.error(response.statusText, 'Failed!')
            })
        }

        $scope.resetIncomingStatusChangeFile = function (evt) {
            if (evt) {
                evt.preventDefault()
            }
            resetIncomingStatusChangeModel()
        }

        window.scope = $scope
    }])