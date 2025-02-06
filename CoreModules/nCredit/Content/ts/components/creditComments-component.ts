namespace CreditCommentsComponentNs {
    export class CreditCommentsController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData

        attachedFileName: string;
        newCommentText: string;
        commentFileUpload: NtechAngularFileUpload.FileUploadHelper
        comments: NTechCreditApi.CreditCommentModel[]
        filterMode: string

        isExpanded: boolean = false

        pagingHelper: NTechTables.PagingHelper
        commentsPaging: NTechTables.PagingObject

        static $inject = ['$http', '$q', 'ntechComponentService', '$scope', '$sce']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private $scope: ng.IScope, private $sce: ng.ISCEService) {
            super(ntechComponentService, $http, $q);

            this.pagingHelper = new NTechTables.PagingHelper($q, $http)

            ntechComponentService.subscribeToNTechEvents(evt => {
                if (evt.eventName !== 'reloadComments') {
                    return
                }
                if (!this.initialData) {
                    return
                }
                if (this.initialData.CreditNr !== evt.eventData) {
                    return
                }
                this.onChanges()
            })
        }

        componentName(): string {
            return 'creditComments'
        }

        onChanges() {
            if (!this.commentFileUpload) {
                this.commentFileUpload = new NtechAngularFileUpload.FileUploadHelper((<HTMLInputElement>document.getElementById('file')),
                    (<HTMLFormElement>document.getElementById('commentform')),
                    this.$scope, this.$q);
                this.commentFileUpload.addFileAttachedListener(filenames => {
                    if (filenames.length == 0) {
                        this.attachedFileName = null
                    } else if (filenames.length == 1) {
                        this.attachedFileName = filenames[0];
                    } else {
                        this.attachedFileName = 'Error - multiple files selected!'
                    }
                });
            } else {
                this.commentFileUpload.reset();
            }
            this.filterMode = null;
            this.comments = [];
            this.commentsPaging = null;
            this.newCommentText = '';
            this.attachedFileName = null;

            if (this.initialData) {
                this.reloadComments();
            }
        }

        reloadComments() {
            let excludeTheseEventTypes = null
            let onlyTheseEventTypes = null
            if (this.filterMode === 'user') {
                onlyTheseEventTypes = ['UserComment']
            } else if (this.filterMode === 'system') {
                excludeTheseEventTypes = ['UserComment']
            }
            this.apiClient.loadCreditComments(this.initialData.CreditNr, excludeTheseEventTypes, onlyTheseEventTypes).then(comments => {
                this.comments = comments;
                this.commentsPaging = this.createPagingModel(this.comments, 0)
            });
        }

        onFilterModeChanged() {
            this.reloadComments()
        }

        onFocusGained() {
            this.isExpanded = true
        }

        onFocusLost() {
        }

        addComment(evt) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.newCommentText) {
                return
            }

            var saveComment = (attachedFileAsDataUrl, attachedFileName) => {
                this.apiClient.createCreditComment(this.initialData.CreditNr, this.newCommentText, attachedFileAsDataUrl, attachedFileName).then(result => {
                    this.newCommentText = null;
                    this.attachedFileName = null;
                    this.comments.unshift(result.comment);
                    this.commentsPaging = this.createPagingModel(this.comments, 0)
                });
            }

            if (this.commentFileUpload.hasAttachedFiles()) {
                this.commentFileUpload.loadSingleAttachedFileAsDataUrl().then(result => {
                    saveComment(result.dataUrl, result.filename)
                }, err => {
                    toastr.warning(err)
                })
            } else {
                saveComment(null, null)
            }
        }

        toggleCommentDetails(c: NTechCreditApi.CreditCommentModel, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            let cc = ((c as any) as ICreditCommentWithDetails)
            if (cc.commentDetails) {
                cc.commentDetails = null
                return
            }

            if (c.CustomerSecureMessageId) {
                this.apiClient.getCustomerMessagesTexts([c.CustomerSecureMessageId]).then(x => {
                    cc.commentDetails = {
                        ArchiveLinks: null,
                        CommentByName: null,
                        CustomerSecureMessageText: x.MessageTextByMessageId[c.CustomerSecureMessageId],
                        CustomerSecureMessageTextFormat: x.MessageTextFormat[c.CustomerSecureMessageId],
                        CustomerSecureMessageBy: x.IsFromCustomerByMessageId[c.CustomerSecureMessageId] ? 'Customer' : 'System',
                        CustomerSecureMessageArchiveKey: x.AttachedDocumentsByMessageId ? x.AttachedDocumentsByMessageId[c.CustomerSecureMessageId] : null
                    }
                    if (cc.commentDetails.CustomerSecureMessageTextFormat === 'html')
                        cc.commentDetails.CustomerSecureMessageText =this.$sce.trustAsHtml(x.MessageTextByMessageId[c.CustomerSecureMessageId])
                })
            } else {
                cc.commentDetails = {
                    ArchiveLinks: c.ArchiveLinks,
                    CommentByName: c.DisplayUserName,
                    CustomerSecureMessageText: null,
                    CustomerSecureMessageTextFormat: null, 
                    CustomerSecureMessageBy: null,
                    CustomerSecureMessageArchiveKey: null
                }
            }
        }

        selectCommentFileToAttach(evt) {
            if (evt) {
                evt.preventDefault()
            }
            this.commentFileUpload.showFilePicker();
        }

        COMMENTS_PAGE_SIZE = 20

        createPagingModel(comments: NTechCreditApi.CreditCommentModel[], currentPageNr: number) {
            var p = this.pagingHelper.createPagingObjectFromPageResult({
                CurrentPageNr: currentPageNr,
                TotalNrOfPages: Math.ceil(comments.length / this.COMMENTS_PAGE_SIZE)
            })
            return p
        }

        gotoCommentsPage(pageNr: number, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.comments) {
                return
            }
            this.commentsPaging = this.createPagingModel(this.comments, pageNr)
        }

        getCommentsOnCurrentPage = (evt: Event) => {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.comments || this.comments.length === 0) {
                return []
            }
            if (!this.commentsPaging) {
                return this.comments
            } else {
                var currentPageNr = _.findWhere(this.commentsPaging.pages, { isCurrentPage: true }).pageNr
                return _.first(_.rest(this.comments, this.COMMENTS_PAGE_SIZE * currentPageNr), this.COMMENTS_PAGE_SIZE)
            }
        }
    }

    export class CreditCommentsComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CreditCommentsController;
            this.templateUrl = 'credit-comments.html';
        }
    }

    export class InitialData {
        CreditNr: string
    }

    interface ICreditCommentWithDetails {
        commentDetails: ICreditCommentDetails
    }

    export interface ICreditCommentDetails {
        ArchiveLinks: string[]
        CommentByName: string
        CustomerSecureMessageText: string
        CustomerSecureMessageTextFormat: string
        CustomerSecureMessageBy: string
        CustomerSecureMessageArchiveKey: string
    }
}

angular.module('ntech.components').component('creditComments', new CreditCommentsComponentNs.CreditCommentsComponent())