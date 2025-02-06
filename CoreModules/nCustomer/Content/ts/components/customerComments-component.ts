namespace CustomerCommentsComponentNs {

    export class CustomerCommentsController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData

        attachedFileName: string;
        newCommentText: string;
        commentFileUpload: NtechAngularFileUpload.FileUploadHelper
        comments: NTechCustomerApi.CustomerComment[]

        isExpanded: boolean = false

        static $inject = ['$http', '$q', 'ntechComponentService', '$scope']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private $scope: ng.IScope) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'customerComments'
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
            this.newCommentText = '';
            this.attachedFileName = null;            

            if (this.initialData) {
                this.apiClient.fetchCustomerComments(this.initialData.customerId).then(comments => {
                    this.comments = comments;
                });
            }
        }
        
        addComment(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.newCommentText) {
                return
            }

            var saveComment = (attachedFileAsDataUrl: string, attachedFileName: string) => {

                this.apiClient.addCustomerComment(this.initialData.customerId, this.newCommentText, attachedFileAsDataUrl, attachedFileName).then(result => {
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

        selectCommentFileToAttach(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.commentFileUpload.showFilePicker();
        }
    }

    export class CustomerCommentsComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CustomerCommentsController;
            this.templateUrl = 'customer-comments.html';
        }
    }

    export class InitialData {
        customerId: number
    }
}

angular.module('ntech.components').component('customerComments', new CustomerCommentsComponentNs.CustomerCommentsComponent())