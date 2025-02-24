var SavingsAccountCommentsCtr = /** @class */ (function () {
    function SavingsAccountCommentsCtr($scope, $sce, $http, $q, savingsAccountCommentsService) {
        var apiClient = new NTechSavingsApi.ApiClient(function (errorMessage) {
            toastr.error(errorMessage);
        }, $http, $q);
        apiClient.loggingContext = 'savingsAccountCommentsService';
        $scope.addComment = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (!$scope.savingsAccountNr || !$scope.commentText) {
                toastr.warning('Failed!');
                return;
            }
            $scope.filterMode = null;
            var saveComment = function (attachedFileAsDataUrl, attachedFileName) {
                $http({
                    method: 'POST',
                    url: '/Api/SavingsAccountComment/Create',
                    data: {
                        savingsAccountNr: $scope.savingsAccountNr,
                        commentText: $scope.commentText,
                        attachedFileAsDataUrl: attachedFileAsDataUrl,
                        attachedFileName: attachedFileName
                    }
                }).then(function (response) {
                    $scope.comments.unshift(response.data.comment);
                    $scope.commentText = null;
                    $scope.attachedFileName = null;
                    savingsAccountCommentsService.isLoading = false;
                }, function (response) {
                    savingsAccountCommentsService.isLoading = false;
                    toastr.error('Failed!');
                });
            };
            savingsAccountCommentsService.isLoading = true;
            var attachedFiles = document.getElementById('commentAttachedFile').files;
            if (attachedFiles.length == 1) {
                var r = new FileReader();
                var f_1 = attachedFiles[0];
                if (f_1.size > (10 * 1024 * 1024)) {
                    toastr.warning('Attached file is too big!');
                    savingsAccountCommentsService.isLoading = false;
                    return;
                }
                r.onloadend = function (e) {
                    var dataUrl = e.target.result;
                    var filename = f_1.name;
                    //Reset the file input
                    document.getElementById('commentform').reset();
                    //Save the document
                    saveComment(dataUrl, filename);
                };
                r.readAsDataURL(f_1);
            }
            else if (attachedFiles.length == 0) {
                saveComment(null, null);
            }
            else {
                toastr.warning('Multiple files have been attached. Please reload the page and only attach a single file.');
            }
        };
        var reload = function (savingsAccountNr) {
            $scope.comments = null;
            $scope.savingsAccountNr = null;
            $scope.commentText = null;
            $scope.attachedFileName = null;
            document.getElementById('commentform').reset();
            savingsAccountCommentsService.isLoading = true;
            var excludeTheseEventTypes = null;
            var onlyTheseEventTypes = null;
            if ($scope.filterMode === 'user') {
                onlyTheseEventTypes = ['UserComment'];
            }
            else if ($scope.filterMode === 'system') {
                excludeTheseEventTypes = ['UserComment'];
            }
            $http({
                method: 'POST',
                url: '/Api/SavingsAccountComment/LoadForSavingsAccount',
                data: { savingsAccountNr: savingsAccountNr, excludeTheseEventTypes: excludeTheseEventTypes, onlyTheseEventTypes: onlyTheseEventTypes }
            }).then(function (response) {
                $scope.savingsAccountNr = savingsAccountNr;
                $scope.comments = response.data;
                savingsAccountCommentsService.isLoading = false;
            }, function (response) {
                savingsAccountCommentsService.isLoading = false;
                toastr.error('Failed!');
            });
        };
        $scope.$watch(function () { return savingsAccountCommentsService.forceReload; }, function () {
            if (savingsAccountCommentsService.forceReload === true) {
                savingsAccountCommentsService.forceReload = false;
                if (savingsAccountCommentsService.savingsAccountNr) {
                    var savingsAccountNr = savingsAccountCommentsService.savingsAccountNr;
                    reload(savingsAccountNr);
                }
            }
        });
        $scope.$watch(function () { return savingsAccountCommentsService.savingsAccountNr; }, function () {
            if (savingsAccountCommentsService.savingsAccountNr) {
                var savingsAccountNr = savingsAccountCommentsService.savingsAccountNr;
                if ($scope.savingsAccountNr != savingsAccountNr) {
                    if (savingsAccountCommentsService.forceReload == true) {
                        savingsAccountCommentsService.forceReload = false;
                    }
                    reload(savingsAccountNr);
                }
            }
            else {
                $scope.comments = null;
                $scope.savingsAccountNr = null;
                $scope.commentText = null;
                $scope.attachedFileName = null;
                document.getElementById('commentform').reset();
            }
        });
        $scope.selectCommentFileToAttach = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            $('#commentAttachedFile').click();
        };
        $scope.refreshAttachedCommentFile = function () {
            if (!document) {
                $scope.attachedFileName = null;
                return;
            }
            ;
            var f = document.getElementById('commentAttachedFile');
            if (!f) {
                $scope.attachedFileName = null;
                return;
            }
            var attachedFiles = f.files;
            if (!attachedFiles) {
                $scope.attachedFileName = null;
                return;
            }
            if (!attachedFiles || attachedFiles.length == 0) {
                $scope.attachedFileName = null;
            }
            else if (attachedFiles.length == 1) {
                $scope.attachedFileName = attachedFiles[0].name;
            }
            else {
                $scope.attachedFileName = 'Error - multiple files selected!';
            }
        };
        $scope.onFilterModeChanged = function () {
            if (!$scope.savingsAccountNr) {
                return;
            }
            reload($scope.savingsAccountNr);
        };
        $scope.toggleCommentDetails = function (c, evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (!c) {
                return;
            }
            if (c.commentDetails) {
                c.commentDetails = null;
                return;
            }
            if (c.CustomerSecureMessageId) {
                savingsAccountCommentsService.isLoading = true;
                apiClient.getCustomerMessagesTexts([c.CustomerSecureMessageId]).then(function (x) {
                    savingsAccountCommentsService.isLoading = false;
                    c.commentDetails = {
                        ArchiveLinks: null,
                        CommentByName: null,
                        CustomerSecureMessageText: x.MessageTextByMessageId[c.CustomerSecureMessageId],
                        CustomerSecureMessageTextFormat: x.MessageTextFormat[c.CustomerSecureMessageId],
                        CustomerSecureMessageBy: x.IsFromCustomerByMessageId[c.CustomerSecureMessageId] ? 'Customer' : 'System',
                        CustomerSecureMessageArchiveKey: x.AttachedDocumentsByMessageId ? x.AttachedDocumentsByMessageId[c.CustomerSecureMessageId] : null
                    };
                    if (c.commentDetails.CustomerSecureMessageTextFormat === 'html')
                        c.commentDetails.CustomerSecureMessageText = $sce.trustAsHtml(x.MessageTextByMessageId[c.CustomerSecureMessageId]);
                });
            }
            else {
                c.commentDetails = {
                    ArchiveLinks: c.ArchiveLinks,
                    CommentByName: c.DisplayUserName,
                    CustomerSecureMessageText: null,
                    CustomerSecureMessageTextFormat: null,
                    CustomerSecureMessageBy: null,
                    CustomerSecureMessageArchiveKey: null
                };
            }
        };
        window.commentsScope = $scope;
    }
    SavingsAccountCommentsCtr.$inject = ['$scope', '$sce', '$http', '$q', 'savingsAccountCommentsService'];
    return SavingsAccountCommentsCtr;
}());
app.controller('savingsAccountComments', SavingsAccountCommentsCtr);
var SavingsAccountCommentsNs;
(function (SavingsAccountCommentsNs) {
    var CommentDetails = /** @class */ (function () {
        function CommentDetails() {
        }
        return CommentDetails;
    }());
    SavingsAccountCommentsNs.CommentDetails = CommentDetails;
    var SavingsAccountCommentsService = /** @class */ (function () {
        function SavingsAccountCommentsService() {
        }
        return SavingsAccountCommentsService;
    }());
    SavingsAccountCommentsNs.SavingsAccountCommentsService = SavingsAccountCommentsService;
    function initSavingsAccountCommentsService($http) {
        var d = { savingsAccountNr: null, isLoading: false, forceReload: false };
        return d;
    }
    SavingsAccountCommentsNs.initSavingsAccountCommentsService = initSavingsAccountCommentsService;
})(SavingsAccountCommentsNs || (SavingsAccountCommentsNs = {}));
app.factory('savingsAccountCommentsService', ['$http', SavingsAccountCommentsNs.initSavingsAccountCommentsService]);
$('textarea.expand').focus(function () {
    $(this).animate({ height: "6em" }, 500);
});
$('#commentsContainer').on('change', '#commentAttachedFile', function () {
    var scope = angular.element($("#commentsContainer")).scope();
    scope.$apply(function () {
        scope.refreshAttachedCommentFile();
    });
});
