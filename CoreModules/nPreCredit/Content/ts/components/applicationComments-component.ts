namespace ApplicationCommentsComponentNs {

    export class ApplicationCommentsController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData

        attachedFileName: string;
        newCommentText: string;
        commentFileUpload: NtechAngularFileUpload.FileUploadHelper
        comments: NTechPreCreditApi.ApplicationComment[]
        affiliateReportingInitialData: AffiliateReportingLogComponentNs.InitialData
        filterMode: string

        isExpanded: boolean = false

        static $inject = ['$http', '$q', 'ntechComponentService', '$scope']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private $scope: ng.IScope) {
            super(ntechComponentService, $http, $q);

            ntechComponentService.subscribeToNTechEvents(evt => {
                if (evt.eventName !== 'reloadComments') {
                    return
                }
                if (!this.initialData) {
                    return
                }
                if (this.initialData.applicationInfo.ApplicationNr !== evt.eventData) {
                    return
                }
                this.onChanges()
            })
        }

        componentName(): string {
            return 'applicationComments'
        }

        onFocusGained() {            
            this.isExpanded = true
        }

        onFocusLost() {
            
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
            } else  {
                this.commentFileUpload.reset();
            }
            this.comments = [];
            this.filterMode = ''
            this.newCommentText = '';
            this.attachedFileName = null;       
            this.affiliateReportingInitialData = null

            if (this.initialData) {
                this.reloadComments()
            }
        }

        reloadComments() {
            let hideTheseEventTypes = this.initialData.hideTheseEventTypes ? angular.copy(this.initialData.hideTheseEventTypes) : []
            let showOnlyTheseEventTypes = this.initialData.showOnlyTheseEventTypes ? angular.copy(this.initialData.showOnlyTheseEventTypes) : []
            if (this.filterMode === 'user') {
                showOnlyTheseEventTypes.push('UserComment')
            } else if (this.filterMode === 'system') {
                hideTheseEventTypes.push('UserComment')
            }
            this.apiClient.fetchApplicationComments(this.initialData.applicationInfo.ApplicationNr, {
                hideTheseEventTypes: hideTheseEventTypes,
                showOnlyTheseEventTypes: showOnlyTheseEventTypes
            }).then(comments => {
                this.comments = comments;
                this.affiliateReportingInitialData = {
                    applicationNr: this.initialData.applicationInfo.ApplicationNr
                }
            });
        }

        onFilterModeChanged() {
            if (!this.initialData) {
                return
            }
            this.reloadComments()
        }
        
        addComment(evt) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.newCommentText) {
                return
            }

            var saveComment = (attachedFileAsDataUrl, attachedFileName) => {

                this.apiClient.addApplicationComment(this.initialData.applicationInfo.ApplicationNr, this.newCommentText, {
                    attachedFileAsDataUrl: attachedFileAsDataUrl,
                    attachedFileName: attachedFileName,
                    eventType: this.initialData.newCommentEventType
                }).then(result => {
                    this.newCommentText = null;
                    this.attachedFileName = null;
                    this.comments.unshift(result);
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

        toggleCommentDetails(c: NTechPreCreditApi.ApplicationComment, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            let cc = ((c as any) as IApplicationCommentWithDetails)
            if (cc.commentDetails) {
                cc.commentDetails = null
                return
            }            
            cc.commentDetails = {
                AttachmentFilename: c.AttachmentFilename,
                AttachmentUrl: c.AttachmentUrl,
                CommentByName: c.CommentByName,
                DirectUrl: c.DirectUrl,
                DirectUrlShortName: c.DirectUrlShortName,
                RequestIpAddress: c.RequestIpAddress,
                BankAccountPdfSummaryArchiveKey: c.BankAccountPdfSummaryArchiveKey,
                BankAccountRawJsonDataArchiveKey: c.BankAccountRawJsonDataArchiveKey
            }
        }

        selectCommentFileToAttach(evt) {
            if (evt) {
                evt.preventDefault()
            }
            this.commentFileUpload.showFilePicker();
        }
                
        toggleWaitingForInformation(evt) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.initialData.applicationInfo.IsActive) {
                return;
            }
            this.apiClient.setApplicationWaitingForAdditionalInformation(this.initialData.applicationInfo.ApplicationNr, !this.initialData.applicationInfo.IsWaitingForAdditionalInformation).then(result => {
                if (this.initialData.reloadPageOnWaitingForAdditionalInformation) {
                    document.location.reload(); //Temporary measure while refactoring
                } else {
                    this.signalReloadRequired();
                }                
            });
        }
    }

    export class ApplicationCommentsComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ApplicationCommentsController;
            this.templateUrl = 'application-comments.html';
        }
    }

    export class InitialData {
        applicationInfo: NTechPreCreditApi.ApplicationInfoModel
        reloadPageOnWaitingForAdditionalInformation?: boolean
        newCommentEventType?: string
        showOnlyTheseEventTypes?: string[]
        hideTheseEventTypes?: string[]
        alwaysShowAttachedFiles?: boolean
        hideAdditionalInfoToggle?: boolean
    }

    interface IApplicationCommentWithDetails {
        commentDetails: IApplicationCommentDetails
    }

    export interface IApplicationCommentDetails {
        AttachmentFilename: string;
        AttachmentUrl: string;
        CommentByName: string;
        DirectUrlShortName: string;
        DirectUrl: string;
        RequestIpAddress: string;
        BankAccountRawJsonDataArchiveKey: string;
        BankAccountPdfSummaryArchiveKey: string;
    }
}

angular.module('ntech.components').component('applicationComments', new ApplicationCommentsComponentNs.ApplicationCommentsComponent())