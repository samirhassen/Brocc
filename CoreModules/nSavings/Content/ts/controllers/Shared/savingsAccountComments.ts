class SavingsAccountCommentsCtr {
    static $inject = ['$scope', '$sce', '$http', '$q', 'savingsAccountCommentsService']
    constructor(
        $scope: SavingsAccountCommentsNs.Scope, $sce: ng.ISCEService, $http: ng.IHttpService,
        $q: ng.IQService, savingsAccountCommentsService: SavingsAccountCommentsNs.SavingsAccountCommentsService) {
        let apiClient = new NTechSavingsApi.ApiClient(errorMessage => {
            toastr.error(errorMessage);
        }, $http, $q);

        apiClient.loggingContext = 'savingsAccountCommentsService';

        $scope.addComment = (evt?: Event) => {
            if (evt) {
                evt.preventDefault()
            }
            if (!$scope.savingsAccountNr || !$scope.commentText) {
                toastr.warning('Failed!')
                return
            }
            $scope.filterMode = null

            let saveComment = (attachedFileAsDataUrl, attachedFileName) => {
                $http({
                    method: 'POST',
                    url: '/Api/SavingsAccountComment/Create',
                    data: {
                        savingsAccountNr: $scope.savingsAccountNr,
                        commentText: $scope.commentText,
                        attachedFileAsDataUrl: attachedFileAsDataUrl,
                        attachedFileName: attachedFileName
                    }
                }).then((response: ng.IHttpResponse<{ comment: SavingsAccountCommentsNs.Comment }>) => {
                    $scope.comments.unshift(response.data.comment)
                    $scope.commentText = null
                    $scope.attachedFileName = null
                    savingsAccountCommentsService.isLoading = false
                }, (response) => {
                    savingsAccountCommentsService.isLoading = false
                    toastr.error('Failed!')
                })
            }

            savingsAccountCommentsService.isLoading = true;

            let attachedFiles = (document.getElementById('commentAttachedFile') as HTMLInputElement).files
            if (attachedFiles.length == 1) {
                let r = new FileReader();
                let f = attachedFiles[0]
                if (f.size > (10 * 1024 * 1024)) {
                    toastr.warning('Attached file is too big!')
                    savingsAccountCommentsService.isLoading = false
                    return
                }
                r.onloadend = (e: any) => {
                    let dataUrl = e.target.result
                    let filename = f.name;

                    //Reset the file input
                    (document.getElementById('commentform') as HTMLFormElement).reset();

                    //Save the document
                    saveComment(dataUrl, filename)
                }
                r.readAsDataURL(f)
            } else if (attachedFiles.length == 0) {
                saveComment(null, null)
            } else {
                toastr.warning('Multiple files have been attached. Please reload the page and only attach a single file.')
            }
        }

        let reload = (savingsAccountNr: string) => {
            $scope.comments = null
            $scope.savingsAccountNr = null
            $scope.commentText = null
            $scope.attachedFileName = null;
            (document.getElementById('commentform') as HTMLFormElement).reset();
            savingsAccountCommentsService.isLoading = true

            let excludeTheseEventTypes = null
            let onlyTheseEventTypes = null
            if ($scope.filterMode === 'user') {
                onlyTheseEventTypes = ['UserComment']
            } else if ($scope.filterMode === 'system') {
                excludeTheseEventTypes = ['UserComment']
            }

            $http({
                method: 'POST',
                url: '/Api/SavingsAccountComment/LoadForSavingsAccount',
                data: { savingsAccountNr: savingsAccountNr, excludeTheseEventTypes: excludeTheseEventTypes, onlyTheseEventTypes: onlyTheseEventTypes }
            }).then((response: ng.IHttpResponse<SavingsAccountCommentsNs.Comment[]>) => {
                $scope.savingsAccountNr = savingsAccountNr
                $scope.comments = response.data
                savingsAccountCommentsService.isLoading = false
            }, (response) => {
                savingsAccountCommentsService.isLoading = false
                toastr.error('Failed!')
            })
        }
        $scope.$watch(
            () => savingsAccountCommentsService.forceReload,
            () => {
                if (savingsAccountCommentsService.forceReload === true) {
                    savingsAccountCommentsService.forceReload = false
                    if (savingsAccountCommentsService.savingsAccountNr) {
                        let savingsAccountNr = savingsAccountCommentsService.savingsAccountNr
                        reload(savingsAccountNr)
                    }
                }
            }
        )

        $scope.$watch(
            () => savingsAccountCommentsService.savingsAccountNr,
            () => {
                if (savingsAccountCommentsService.savingsAccountNr) {
                    let savingsAccountNr = savingsAccountCommentsService.savingsAccountNr
                    if ($scope.savingsAccountNr != savingsAccountNr) {
                        if (savingsAccountCommentsService.forceReload == true) {
                            savingsAccountCommentsService.forceReload = false
                        }
                        reload(savingsAccountNr)
                    }
                } else {
                    $scope.comments = null
                    $scope.savingsAccountNr = null
                    $scope.commentText = null
                    $scope.attachedFileName = null;
                    (document.getElementById('commentform') as HTMLFormElement).reset();
                }
            }
        )

        $scope.selectCommentFileToAttach = (evt: Event) => {
            if (evt) {
                evt.preventDefault()
            }
            $('#commentAttachedFile').click()
        }

        $scope.refreshAttachedCommentFile = () => {
            if (!document) {
                $scope.attachedFileName = null
                return
            };

            let f = (document.getElementById('commentAttachedFile') as HTMLInputElement);
            if (!f) {
                $scope.attachedFileName = null
                return
            }

            let attachedFiles = f.files
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

        $scope.onFilterModeChanged = () => {
            if (!$scope.savingsAccountNr) {
                return
            }
            reload($scope.savingsAccountNr)
        }

        $scope.toggleCommentDetails = (c: SavingsAccountCommentsNs.Comment, evt?: Event) => {
            if (evt) {
                evt.preventDefault()
            }
            if (!c) {
                return
            }
            if (c.commentDetails) {
                c.commentDetails = null
                return
            }

            if (c.CustomerSecureMessageId) {
                savingsAccountCommentsService.isLoading = true
                apiClient.getCustomerMessagesTexts([c.CustomerSecureMessageId]).then(x => {
                    savingsAccountCommentsService.isLoading = false
                    c.commentDetails = {
                        ArchiveLinks: null,
                        CommentByName: null,
                        CustomerSecureMessageText: x.MessageTextByMessageId[c.CustomerSecureMessageId],
                        CustomerSecureMessageTextFormat: x.MessageTextFormat[c.CustomerSecureMessageId],
                        CustomerSecureMessageBy: x.IsFromCustomerByMessageId[c.CustomerSecureMessageId] ? 'Customer' : 'System',
                        CustomerSecureMessageArchiveKey: x.AttachedDocumentsByMessageId ? x.AttachedDocumentsByMessageId[c.CustomerSecureMessageId] : null
                    }
                    if (c.commentDetails.CustomerSecureMessageTextFormat === 'html')
                        c.commentDetails.CustomerSecureMessageText = $sce.trustAsHtml(x.MessageTextByMessageId[c.CustomerSecureMessageId])
                })
            } else {
                c.commentDetails = {
                    ArchiveLinks: c.ArchiveLinks,
                    CommentByName: c.DisplayUserName,
                    CustomerSecureMessageText: null,
                    CustomerSecureMessageTextFormat: null, 
                    CustomerSecureMessageBy: null,
                    CustomerSecureMessageArchiveKey: null
                }
            }
        }

        window.commentsScope = $scope
    }
}

app.controller('savingsAccountComments', SavingsAccountCommentsCtr)

module SavingsAccountCommentsNs {
    export interface Scope extends ng.IScope {
        savingsAccountNr: string
        commentText: string
        attachedFileName: string
        filterMode: string
        addComment: (evt?: Event) => void
        comments: Comment[]
        selectCommentFileToAttach: (evt?: Event) => void
        refreshAttachedCommentFile: () => void
        onFilterModeChanged: () => void
        toggleCommentDetails: (c: Comment, evt?: Event) => void
    }

    export interface Comment {
        CommentDate: Date
        CommentText: string
        ArchiveLinks: string[]
        DisplayUserName: string
        CustomerSecureMessageId: number
        commentDetails?: CommentDetails
    }

    export class CommentDetails {
        CommentByName: string
        ArchiveLinks: string[]
        CustomerSecureMessageText: string
        CustomerSecureMessageTextFormat: string
        CustomerSecureMessageBy: string
        CustomerSecureMessageArchiveKey: string
    }

    export class SavingsAccountCommentsService {
        savingsAccountNr: string
        isLoading: boolean
        forceReload: boolean
    }

    export function initSavingsAccountCommentsService($http: ng.IHttpService): SavingsAccountCommentsService {
        let d: SavingsAccountCommentsService = { savingsAccountNr: null, isLoading: false, forceReload: false }
        return d
    }
}

app.factory('savingsAccountCommentsService', ['$http', SavingsAccountCommentsNs.initSavingsAccountCommentsService])

$('textarea.expand').focus(function () {
    $(this).animate({ height: "6em" }, 500)
})

$('#commentsContainer').on('change', '#commentAttachedFile', function () {
    let scope: SavingsAccountCommentsNs.Scope = angular.element($("#commentsContainer")).scope();
    scope.$apply(() => {
        scope.refreshAttachedCommentFile()
    })
})