import { Component, ElementRef, Input, OnChanges, SimpleChanges, ViewChild } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { FileInputEventTarget } from 'src/app/common.types';
import {
    ApplicationComment,
    ApplicationInfoModel,
    SharedApplicationApiService,
} from 'src/app/shared-application-components/services/shared-loan-application-api.service';

@Component({
    selector: 'application-comments',
    templateUrl: './application-comments.component.html',
    styles: [],
})
export class ApplicationCommentsComponent implements OnChanges {
    @Input() public initialData: ApplicationCommentsInitialData;

    @ViewChild('fileInput')
    fileInput: ElementRef<HTMLInputElement>;

    @ViewChild('fileInputForm')
    fileInputForm: ElementRef<HTMLFormElement>;

    public model: Model;

    constructor(private toastrService: ToastrService) {}

    reload() {
        this.model = null;

        if (!this.initialData) {
            return;
        }

        this.initialData.applicationApiService
            .fetchApplicationComments(this.initialData.applicationInfo.ApplicationNr)
            .then((x) => {
                let model: Model = {
                    comments: [],
                    newCommentText: '',
                    isNewCommentTextAreaExpanded: false,
                };
                for (let comment of x) {
                    model.comments.push({ data: comment, showDetails: false });
                }
                this.model = model;
            });
    }

    ngOnChanges(changes: SimpleChanges) {
        this.reload();
    }

    addComment(evt?: Event) {
        evt?.preventDefault();

        if (!this.model?.newCommentText) {
            return;
        }

        this.initialData.applicationApiService
            .addApplicationComment(this.initialData.applicationInfo.ApplicationNr, this.model.newCommentText, {
                attachedFileAsDataUrl: this.model.attachedFile?.dataUrl,
                attachedFileName: this.model.attachedFile?.name,
                eventType: this.initialData.newCommentEventType,
            })
            .then((result) => {
                this.model.newCommentText = null;
                this.model.attachedFile = null;
                this.model.comments.unshift({
                    data: result,
                    showDetails: false,
                });
            });
    }

    selectFileToAttach(evt: Event) {
        evt?.preventDefault();
        this.fileInput.nativeElement.click();
    }

    toggleCommentDetails(c: CommentUiModel, evt: Event) {
        if (evt) {
            evt.preventDefault();
        }
        if (!c.showDetails && c.data.CustomerSecureMessageId && !c.secureMessage) {
            let messageId = c.data.CustomerSecureMessageId;
            let apiService = this.initialData.applicationApiService;
            apiService.getSecureMessageTexts([messageId]).then((x) => {
                let attachmentArchiveKey = x.AttachedDocumentsByMessageId[messageId];
                c.secureMessage = {
                    text: x.MessageTextByMessageId[messageId],
                    format: x.MessageTextFormat[messageId],
                    isByCustomer: x.IsFromCustomerByMessageId[messageId],
                    attachedDocumentUrl: attachmentArchiveKey
                        ? apiService.api().getArchiveDocumentUrl(attachmentArchiveKey)
                        : null,
                    requestIpAddress: c.data.RequestIpAddress,
                };
                c.showDetails = true;
            });
        } else {
            c.showDetails = !c.showDetails;
        }
    }

    onFileAttached(evt: Event) {
        let target: FileInputEventTarget = (evt as any).target;
        FormsHelper.loadSingleAttachedFileAsDataUrl(target.files).then(
            (x) => {
                this.model.attachedFile = {
                    name: x.filename,
                    dataUrl: x.dataUrl,
                };
                this.fileInputForm.nativeElement.reset();
            },
            (x) => {
                this.toastrService.error(x);
                this.fileInputForm.nativeElement.reset();
            }
        );
    }
}

export class ApplicationCommentsInitialData {
    applicationInfo: ApplicationInfoModel;
    applicationApiService: SharedApplicationApiService;
    newCommentEventType?: string;
}

interface CommentUiModel {
    data: ApplicationComment;
    showDetails: boolean;
    secureMessage?: {
        text: string;
        format: string;
        isByCustomer: boolean;
        requestIpAddress: string;
        attachedDocumentUrl: string;
    };
}

class Model {
    comments: CommentUiModel[];
    newCommentText: string;
    attachedFile?: {
        name: string;
        dataUrl: string;
    };
    isNewCommentTextAreaExpanded: boolean;
}
