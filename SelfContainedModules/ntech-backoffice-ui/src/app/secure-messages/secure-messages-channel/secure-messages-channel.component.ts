import { Component, ElementRef, Input, OnInit, SimpleChanges, ViewChild } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { CustomerInfoInitialData } from 'src/app/common-components/customer-info/customer-info.component';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { FileInputEventTarget } from 'src/app/common.types';
import {
    CustomerMessageAttachedDocumentModel,
    CustomerMessageModel,
    SecureMessagesApiService,
} from '../services/secure-messages-api.service';
import { sharedQuillSettings } from '../shared.quill.settings';

@Component({
    selector: 'secure-messages-channel',
    templateUrl: './secure-messages-channel.component.html',
    styles: [' .align-with-title { margin-top: -58px; }'],
})
export class SecureMessagesChannelComponent implements OnInit {
    constructor(
        private apiService: NtechApiService,
        private messageApiService: SecureMessagesApiService,
        private toastr: ToastrService,
        private sanitizer: DomSanitizer,
        private route: ActivatedRoute,
        private config: ConfigService
    ) {
        this.route.queryParams.subscribe((params) => {
            this.initialData = {
                channelId: params['channelId'],
                customerId: params['customerId'],
                channelType: params['channelType'],
            };
            this.reload();
        });
    }

    @Input()
    public initialData?: SecureMessageChannelInitialData;

    public quillEditorOptions = sharedQuillSettings.editorOptions;
    public quillEditorFormats = sharedQuillSettings.editorFormats;

    public showDiv = false;

    @ViewChild('fileInput')
    fileInput: ElementRef<HTMLInputElement>;

    @ViewChild('fileInputForm')
    fileInputForm: ElementRef<HTMLFormElement>;

    public m: Model | null = null;

    ngOnInit(): void {}

    public sanitizeHtml(text: string) {
        return this.sanitizer.bypassSecurityTrustHtml(text);
    }

    ngOnChanges(changes: SimpleChanges) {
        this.reload();
    }

    reload() {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let i = this!.initialData;

        this.getMessages(0).then((x) => {
            this.m = {
                channelId: i.channelId,
                channelType: i.channelType,
                customerId: i.customerId,
                customerInfoInitialData: {
                    backTarget: i.navigationTargetToHere,
                    customerId: i.customerId,
                },
                newMessageText: '',
                messages: {
                    list: x.Messages,
                    totalCount: x.TotalMessageCount,
                    hasUnhandled: x.Messages.some((x) => !x.HandledDate),
                },
            };
        });
    }

    handleUnhandledMessages(evt?: Event) {
        evt?.preventDefault();
        this.messageApiService
            .handleMessages({
                MessageIds: this.m.messages.list
                    .filter(
                        (x) =>
                            x.ChannelId == this.initialData.channelId &&
                            x.ChannelType == this.initialData.channelType &&
                            x.CustomerId == this.initialData.customerId &&
                            !x.HandledByUserId
                    )
                    .map((x) => x.Id),
            })
            .then((x) => {
                this.reload();
            });
    }

    hasMoreMessages(): boolean {
        if (!this.m) {
            return false;
        }
        let s = this.m.messages;
        return s.totalCount > s.list.length;
    }

    loadMoreMessages(evt: Event) {
        if (evt) {
            evt.preventDefault();
        }

        let m = this.m.messages;
        this.getMessages(m.list.length).then((x) => {
            m.totalCount = x.TotalMessageCount;
            for (let e of x.Messages) {
                m.list.push(e);
            }
            m.hasUnhandled = m.list.some((x) => !x.HandledDate);
        });
    }

    selectFileToAttach(evt: Event) {
        evt?.preventDefault();
        this.fileInput.nativeElement.click();
    }

    removeDocument(evt: Event) {
        evt?.preventDefault();
        this.m.attachedFile = null;
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
                this.toastr.error(x);
                this.fileInputForm.nativeElement.reset();
            }
        );
    }

    sendMessage(evt: Event) {
        evt?.preventDefault();

        let requestEmailNotification = this.config.hasEmailProvider();

        this.messageApiService
            .createSecureCustomerMessage({
                CustomerId: this.m.customerId,
                ChannelId: this.m.channelId,
                ChannelType: this.m.channelType,
                IsFromCustomer: false,
                Text: this.m.newMessageText,
                TextFormat: 'html',
                FlagPreviousMessagesAsHandled: true,
                NotifyCustomerByEmail: requestEmailNotification,
            })
            .then((x) => {
                if (requestEmailNotification && !x.WasNotificationEmailSent) {
                    this.toastr.warning('The message was sent but the email notification was not');
                }
                this.m.newMessageText = '';

                if (this.m.attachedFile) {
                    this.messageApiService
                        .attachMessageDocument({
                            MessageId: x.CreatedMessage.Id,
                            AttachedFileAsDataUrl: this.m.attachedFile.dataUrl,
                            AttachedFileName: this.m.attachedFile.name,
                        })
                        .then((y) => {
                            this.reload();
                        });
                } else {
                    //Mimic a page reload
                    for (let s of this.m.messages.list) {
                        (s.HandledByUserId = x.CreatedMessage.HandledByUserId),
                            (s.HandledDate = x.CreatedMessage.HandledDate);
                    }
                    this.m.messages.list.splice(0, 0, x.CreatedMessage);
                    this.m.messages.totalCount += 1;
                }
            })
            .catch((error) => {
                this.toastr.error(`Message could not be sent. (${error.status} ${error.statusText})`);
            });
    }

    private getMessages(skipCount: number) {
        let i = this.initialData;
        let request = {
            ChannelId: i?.channelId,
            ChannelType: i?.channelType,
            CustomerId: i?.customerId,
            IncludeChannels: false,
            IncludeMessageTexts: true,
            SkipCount: skipCount,
            TakeCount: PageSize,
        };
        return this.messageApiService.getMessages(request);
    }

    public getMessageClass(isFromCustomer: boolean, includeDirection?: boolean) {
        if (includeDirection)
            return isFromCustomer ? 'pull-left direction-sent-msg' : 'pull-right direction-recieved-msg';

        return isFromCustomer ? 'pull-left' : 'pull-right';
    }

    getProductUrl() {
        if (!this.m) {
            return null;
        }
        let ct = this.m.channelType;
        let ci = this.m.channelId;
        if (ct === 'Application_UnsecuredLoan') {
            return '/s/unsecured-loan-application/application/' + ci;
        } else if (ct === 'Application_MortgageLoan') {
            return '/s/mortgage-loan-application/application/' + ci;
        } else if (ct === 'Credit_UnsecuredLoan' || ct === 'Credit_MortgageLoan' || ct === 'Credit_CompanyLoan') {
            return this.apiService.getUiGatewayUrl('nCredit', 'Ui/Credit', [['creditNr', ci]]);
        } else if (ct === 'SavingsAccount_StandardAccount') {
            return this.apiService.getUiGatewayUrl('nSavings', 'Ui/SavingsAccounts/Goto', [['savingsAccountNr', ci]]);
        } else {
            return null;
        }
    }

    format(text: string) {
        return '';
    }

    getAttachedDocumentArchiveUrl(document: CustomerMessageAttachedDocumentModel) {
        return this.apiService.getArchiveDocumentUrl(document.ArchiveKey);
    }
}

const PageSize: number = 15;

export class Model {
    customerId?: number;
    channelType?: string;
    channelId?: string;
    customerInfoInitialData: CustomerInfoInitialData;
    newMessageText?: string;
    messages?: {
        list: CustomerMessageModel[];
        totalCount: number;
        hasUnhandled: boolean;
    };
    attachedFile?: {
        name: string;
        dataUrl: string;
    };
}

export interface SecureMessageChannelInitialData {
    customerId: number;
    channelType: string;
    channelId: string;
    navigationTargetToHere?: CrossModuleNavigationTarget;
}
