app.controller('creditComments', ['$scope', '$http', '$q', 'creditCommentsService', function ($scope, $http, $q, creditCommentsService) {
    $scope.addComment = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        if (!$scope.creditNr || !$scope.commentText) {
            toastr.warning('Failed!')
            return
        }

        var saveComment = function (attachedFileAsDataUrl, attachedFileName) {
            $http({
                method: 'POST',
                url: '/Api/CreditComment/Create',
                data: { creditNr: $scope.creditNr, 
                    commentText: $scope.commentText,
                    attachedFileAsDataUrl: attachedFileAsDataUrl,
                    attachedFileName: attachedFileName 
                }
            }).then(function successCallback(response) {
                $scope.comments.unshift(response.data.comment)
                $scope.commentText = null
                $scope.attachedFileName = null
                creditCommentsService.isLoading = false
            }, function errorCallback(response) {
                creditCommentsService.isLoading = false
                toastr.error('Failed!')
            })
        }

        creditCommentsService.isLoading = true

        var attachedFiles = document.getElementById('commentAttachedFile').files
        if (attachedFiles.length == 1) {
            var r = new FileReader();
            var f = attachedFiles[0]
            if (f.size > (10 * 1024 * 1024)) {
                toastr.warning('Attached file is too big!')
                creditCommentsService.isLoading = false
                return
            }
            r.onloadend = function (e) {
                var dataUrl = e.target.result
                var filename = f.name

                //Reset the file input
                document.getElementById('commentform').reset()

                //Save the document
                saveComment(dataUrl, filename)
            }
            r.readAsDataURL(f)
        } else if (attachedFiles.length == 0) {
            saveComment()
        } else {
            toastr.warning('Multiple files have been attached. Please reload the page and only attach a single file.')
        }
    }

    $scope.$watch(
        function () { return creditCommentsService.creditNr },
        function () {
            if (creditCommentsService.creditNr) {
                var creditNr = creditCommentsService.creditNr
                if ($scope.creditNr != creditNr) {
                    $scope.comments = null
                    $scope.creditNr = null
                    $scope.commentText = null
                    $scope.attachedFileName = null
                    document.getElementById('commentform').reset()
                    creditCommentsService.isLoading = true
                    $http({
                        method: 'POST',
                        url: '/Api/CreditComment/LoadForCredit',
                        data: { creditNr : creditNr }
                    }).then(function successCallback(response) {
                        $scope.creditNr = creditNr
                        $scope.comments = response.data
                        creditCommentsService.isLoading = false
                    }, function errorCallback(response) {
                        creditCommentsService.isLoading = false
                        toastr.error('Failed!')
                    })
                }
            } else {                
                $scope.comments = null
                $scope.creditNr = null
                $scope.commentText = null
                $scope.attachedFileName = null
                document.getElementById('commentform').reset()
            }
        }
    )

    $scope.selectCommentFileToAttach = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        $('#commentAttachedFile').click()
    }

    $scope.refreshAttachedCommentFile = function () {
        if (!document) {
            $scope.attachedFileName = null
            return
        }

        var f = document.getElementById('commentAttachedFile')
        if (!f) {
            $scope.attachedFileName = null
            return
        }

        var attachedFiles = f.files
        if (!attachedFiles) {
            $scope.attachedFileName = null
            return
        }

        if (!attachedFiles || attachedFiles.length == 0) {
            $scope.attachedFileName = null
        } else if (attachedFiles.length == 1) {
            $scope.attachedFileName = attachedFiles[0].name
        } else {
            $scope.attachedFileName = 'Error - multiple files selected!'
        }
    }

    window.commentsScope = $scope
}])
.factory('creditCommentsService', ['$http', function ($http) {
    var d = { creditNr : null, isLoading : false }
    return d
}])

$('textarea.expand').focus(function () {
    $(this).animate({ height: "6em" }, 500)
})

$('#commentsContainer').on('change', '#commentAttachedFile', function () {
    var scope = angular.element($("#commentsContainer")).scope();
    scope.$apply(function () {
        scope.refreshAttachedCommentFile()
    })
})