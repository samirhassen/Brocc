import { Component, ElementRef, EventEmitter, Input, OnInit, Output, SimpleChanges, ViewChild } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { DomSanitizer } from '@angular/platform-browser';
import { ToastrService } from 'ngx-toastr';
import { BehaviorSubject } from 'rxjs';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { FileInputEventTarget } from 'src/app/common.types';
import { sharedQuillSettings } from 'src/app/secure-messages/shared.quill.settings';
import { CustomerPagesApiService } from '../../common-services/customer-pages-api.service';
import { CustomerPagesConfigService } from '../../common-services/customer-pages-config.service';
import {
    CustomerMessageAttachedDocumentModel,
    CustomerMessagesHelper,
    GetMessagesResponse,
    MessageServerModel,
} from '../customer-messages-helper';

@Component({
    selector: 'secure-messages-edit-list',
    templateUrl: './secure-messages-edit-list.component.html',
    styleUrls: ['./secure-messages-edit-list.component.scss'],
})
export class SecureMessagesEditListComponent implements OnInit {
    constructor(
        private fb: UntypedFormBuilder,
        private sanitizer: DomSanitizer,
        private toastr: ToastrService,
        private config: CustomerPagesConfigService,
        private sharedApiService: CustomerPagesApiService
    ) {}

    @Input()
    public initialData: SecureMessagesEditListComponentInitialData;

    @Output()
    public messagesLoaded: EventEmitter<GetMessagesResponse> = new EventEmitter();

    public m: Model;

    @ViewChild('fileInput')
    fileInput: ElementRef<HTMLInputElement>;

    @ViewChild('fileInputForm')
    fileInputForm: ElementRef<HTMLFormElement>;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        this.loadMessages(0).then((x) => {
            this.reset(x);
        });
    }

    private createMessageHelper() {
        return new CustomerMessagesHelper(
            this.initialData.getSingleApplicationNr(),
            this.config,
            this.sharedApiService
        );
    }

    private reset(messagesResponse: GetMessagesResponse) {
        let messageForm = new FormsHelper(
            this.fb.group({
                message: ['', [Validators.required]],
            })
        );
        let m = new Model(messageForm);

        m.addMessages(messagesResponse.Messages, messagesResponse.TotalMessageCount);

        this.m = m;
    }

    private loadMessages(skipCount: number) {
        return this.createMessageHelper()
            .getSecureMessages({ skip: skipCount, take: pageSize }, true)
            .then((x) => {
                this.messagesLoaded.emit(x);
                return x;
            });
    }

    public sendMessage(evt?: Event) {
        evt?.preventDefault();

        let m = this.m;
        let channel = this.initialData.sendChannel.value;
        let messagesHelper = this.createMessageHelper();

        messagesHelper
            .sendSecureMessage(
                channel.channelType,
                channel.channelId,
                m.messageForm.getValue('message'),
                'html',
                m.attachedFile
            )
            .then((x) => {
                this.loadMessages(0).then((x) => {
                    this.reset(x);
                });
            });
    }

    public invalid() {
        return this.m.messageForm.invalid() || !this.initialData.sendChannel.value;
    }

    public sanitizeHtml(text: string) {
        return this.sanitizer.bypassSecurityTrustHtml(text);
    }

    public isHtml(message: MessageServerModel) {
        return message.TextFormat == 'html';
    }

    public getAttachmentUrl(a: CustomerMessageAttachedDocumentModel) {
        return this.sharedApiService.getArchiveDocumentUrl(a.ArchiveKey, true);
    }

    public selectFileToAttach(evt: Event) {
        evt?.preventDefault();
        this.fileInput.nativeElement.click();
    }

    public removeDocument(evt: Event) {
        evt?.preventDefault();
        this.m.attachedFile = null;
    }

    public getMessageClass(isFromCustomer: boolean, includeDirection?: boolean) {
        if (includeDirection)
            return isFromCustomer ? 'pull-right direction-recieved-msg' : 'pull-left direction-sent-msg';

        return isFromCustomer ? 'pull-right' : 'pull-left';
    }

    public showSendMessageForm(): boolean {
        return this.initialData.showSendMessage;
    }

    public onFileAttached(evt: Event) {
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
                this.toastr.error(x);
                this.fileInputForm.nativeElement.reset();
            }
        );
    }

    public loadMoreMessages(evt?: Event) {
        evt.preventDefault();

        this.loadMessages(this.m.messages.length).then((x) => {
            this.m.addMessages(x.Messages, x.TotalMessageCount);
        });
    }

    public quillEditorOptions = sharedQuillSettings.editorOptions;
    public quillEditorFormats = sharedQuillSettings.editorFormats;
}

class Model {
    constructor(public messageForm: FormsHelper) {
        this.messages = [];
    }

    public addMessages(messages: MessageServerModel[], totalCount: number) {
        for (let m of messages) {
            this.messages.push(m);
        }
        this.hasMoreMessages = totalCount > this.messages.length;
    }

    public messages: MessageServerModel[];
    public hasMoreMessages: boolean;

    public attachedFile?: {
        name: string;
        dataUrl: string;
    };
}

const pageSize = 15;

export class SecureMessagesEditListComponentInitialData {
    constructor(public isSingleApplication: boolean, public showSendMessage: boolean) {
        this.sendChannel = new BehaviorSubject<{ channelType: string; channelId: string }>(null);
        this.showSendMessageForm = showSendMessage;
    }
    public sendChannel: BehaviorSubject<{ channelType: string; channelId: string }>;

    public showSendMessageForm: boolean;

    public setActiveChannel(channelType: string, channelId: string) {
        this.sendChannel.next({ channelType, channelId });
    }

    public removeActiveChannel() {
        this.sendChannel.next(null);
    }

    public getSingleApplicationNr() {
        if (this.isSingleApplication) {
            let channelId = this.sendChannel.value?.channelId;
            if (!channelId) {
                throw new Error('In this context the caller is expected to have set this always');
            }
            return channelId;
        } else {
            return null;
        }
    }
}
