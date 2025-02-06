import { Component, ElementRef, Input, OnInit, SimpleChanges, ViewChild } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { Subscription } from 'rxjs';
import { splitIntoPages, TablePagerInitialData } from 'src/app/common-components/table-pager/table-pager.component';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { NtechEventService, ReloadCreditCommentsEventName } from 'src/app/common-services/ntech-event.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { FileInputEventTarget } from 'src/app/common.types';
import { CreditCommentModel, CreditService } from '../credit.service';

@Component({
    selector: 'credit-comments',
    templateUrl: './credit-comments.component.html',
    styles: [],
})
export class CreditCommentsComponent implements OnInit {
    constructor(
        private toastrService: ToastrService,
        private creditService: CreditService,
        private apiService: NtechApiService,
        private eventService: NtechEventService
    ) {}

    private eventSub: Subscription;

    ngOnInit(): void {
        this.gotoPage = this.gotoPage.bind(this);
        this.eventSub = this.eventService.applicationEvents.subscribe((x) => {
            if (x.eventCode === ReloadCreditCommentsEventName && x.customData === this.creditNr) {
                this.reload();
            }
        });
    }

    ngOnDestroy(): void {
        this.eventSub?.unsubscribe();
    }

    @Input()
    public creditNr: string;

    @ViewChild('fileInput')
    fileInput: ElementRef<HTMLInputElement>;

    @ViewChild('fileInputForm')
    fileInputForm: ElementRef<HTMLFormElement>;

    public m: Model;

    async reload() {
        this.m = null;

        if (!this.creditNr) {
            return;
        }

        let m: Model = {
            filterMode: 'all',
            newCommentText: '',
            isNewCommentTextAreaExpanded: false,
        };

        await this.reloadComments(m, m.filterMode);

        this.m = m;
    }

    private async reloadComments(m: Model, filterMode: string) {
        let excludeTheseEventTypes = null;
        let onlyTheseEventTypes = null;

        if (filterMode === 'user') {
            onlyTheseEventTypes = ['UserComment'];
        } else if (filterMode === 'system') {
            excludeTheseEventTypes = ['UserComment'];
        }
        m.filterMode = filterMode;
        m.allComments = [];
        let allComments = await this.creditService.loadCreditComments(
            this.creditNr,
            excludeTheseEventTypes,
            onlyTheseEventTypes
        );
        for (let comment of allComments) {
            m.allComments.push({ data: comment, showDetails: false });
        }
        this.gotoPage(m, 0);
    }

    private gotoPage(m: Model, pageNr: number) {
        m.allCommentsPages = splitIntoPages(m.allComments, 20);
        m.currentCommentsPage = m.allCommentsPages[pageNr];
        m.paging = {
            currentPageNr: pageNr,
            totalNrOfPages: m.allCommentsPages.length,
            onGotoPage: (x) => {
                this.gotoPage(m, x);
            },
        };
    }

    async ngOnChanges(changes: SimpleChanges) {
        await this.reload();
    }

    async addComment(evt?: Event) {
        evt?.preventDefault();

        if (!this.m?.newCommentText) {
            return;
        }

        let { comment } = await this.creditService.createCreditComment(
            this.creditNr,
            this.m.newCommentText,
            this.m.attachedFile?.dataUrl,
            this.m.attachedFile?.name
        );

        this.m.newCommentText = null;
        this.m.attachedFile = null;
        this.m.allComments.unshift({
            data: comment,
            showDetails: false,
        });

        this.gotoPage(this.m, 0);
    }

    selectFileToAttach(evt: Event) {
        evt?.preventDefault();
        this.fileInput.nativeElement.click();
    }

    async toggleCommentDetails(c: CommentUiModel, evt: Event) {
        if (evt) {
            evt.preventDefault();
        }
        if (!c.showDetails && c.data.CustomerSecureMessageId && !c.secureMessage) {
            let messageId = c.data.CustomerSecureMessageId;

            let x = await this.creditService.getSecureMessageTexts([messageId]);

            let attachmentArchiveKey = x.AttachedDocumentsByMessageId[messageId];
            c.secureMessage = {
                text: x.MessageTextByMessageId[messageId],
                format: x.MessageTextFormat[messageId],
                isByCustomer: x.IsFromCustomerByMessageId[messageId],
                attachedDocumentUrl: this.getArchiveDocumentUrl(attachmentArchiveKey),
                requestIpAddress: c.data.RequestIpAddress,
            };
            c.showDetails = true;
        } else {
            c.showDetails = !c.showDetails;
        }
    }

    async onFilterModeChanged(filterMode: string) {
        setTimeout(async () => {
            await this.reloadComments(this.m, filterMode);
        }, 0);
    }

    onFileAttached(evt: Event) {
        let target: FileInputEventTarget = (evt as any).target;
        FormsHelper.loadSingleAttachedFileAsDataUrl(target.files).then(
            (x) => {
                this.m.attachedFile = {
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

    getArchiveDocumentUrl(archiveKey: string) {
        return archiveKey ? this.apiService.getArchiveDocumentUrl(archiveKey, true) : null;
    }
}

interface CommentUiModel {
    data: CreditCommentModel;
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
    allComments?: CommentUiModel[];
    newCommentText: string;
    attachedFile?: {
        name: string;
        dataUrl: string;
    };
    isNewCommentTextAreaExpanded: boolean;
    filterMode: string;
    paging?: TablePagerInitialData;
    allCommentsPages?: CommentUiModel[][];
    currentCommentsPage?: CommentUiModel[];
}
