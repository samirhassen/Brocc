import { CustomerPagesApiService } from '../common-services/customer-pages-api.service';
import { CustomerPagesConfigService } from '../common-services/customer-pages-config.service';

/*
This class exists to isolate the tracking of unread count for customers
across the different contexts.
The issue is that a customer context has two different types:
- For applications it's just a single channel type + id
- For mypages though it's all loans, savings accounts and the general channel so more of a family of channel types
*/
export class CustomerMessagesHelper {
    constructor(
        private singleApplicationNr: string, //If this is missing it's interpreted as the my pages context
        private config: CustomerPagesConfigService,
        private apiService: CustomerPagesApiService
    ) {}

    public getSecureMessages(paging: { skip: number; take: number }, markAsRead: boolean) {
        let filter = this.getFilter();

        return this.apiService.postLocal<GetMessagesResponse>('api/embedded-customerpages/messages', {
            skipCount: paging?.skip,
            takeCount: paging?.take,
            onlyTheseChannelTypes: filter.onlyTheseChannelTypes,
            channelType: filter.onlyThisChannel?.channelType,
            channelId: filter.onlyThisChannel?.channelId,
            markAsReadByCustomerContext: markAsRead ? filter.markAsReadContextName : null,
        });
    }

    public getSecureMessagesUnreadByCustomerCount() {
        let filter = this.getFilter();

        return this.apiService.postLocal<{ UnreadCount: number }>('api/embedded-customerpages/messages-unread-count', {
            markAsReadByCustomerContext: filter.markAsReadContextName,
            onlyTheseChannelTypes: filter.onlyTheseChannelTypes,
            channelType: filter.onlyThisChannel?.channelType,
            channelId: filter.onlyThisChannel?.channelId,
        });
    }

    public sendSecureMessage(
        channelType: string,
        channelId: string,
        text: string,
        textFormat: string,
        attachedFile?: {
            dataUrl: string;
            name: string;
        }
    ) {
        return this.apiService.postLocal<CreateMessageResponse>('api/embedded-customerpages/send-secure-message', {
            channelType,
            channelId,
            text,
            textFormat,
            attachedFileAsDataUrl: attachedFile?.dataUrl,
            attachedFileName: attachedFile?.name,
        });
    }

    private getFilter() {
        return {
            markAsReadContextName: this.getMarkAsReadContextName(),
            onlyTheseChannelTypes: this.singleApplicationNr ? null : this.getMyPagesChannelTypes(),
            onlyThisChannel: this.singleApplicationNr
                ? { channelType: this.getApplicationChannelType(), channelId: this.singleApplicationNr }
                : null,
        };
    }

    private getApplicationChannelType() {
        if (this.config.isFeatureEnabled('ntech.feature.unsecuredloans.standard')) {
            return 'Application_UnsecuredLoan';
        } else if (this.config.isFeatureEnabled('ntech.feature.mortgageloans.standard')) {
            return 'Application_MortgageLoan';
        } else {
            throw new Error('Not implemented');
        }
    }

    private getMyPagesChannelTypes() {
        let h: string[] = [];

        if (this.config.isFeatureEnabled('ntech.feature.unsecuredloans.standard')) h.push('Credit_UnsecuredLoan');

        if (this.config.isFeatureEnabled('ntech.feature.mortgageloans.standard')) h.push('Credit_MortgageLoan');

        if (this.config.isFeatureEnabled('ntech.feature.savingsstandard')) h.push('SavingsAccount_StandardAccount');

        h.push('General');

        return h;
    }

    private getMarkAsReadContextName() {
        return this.singleApplicationNr ? `Application_${this.singleApplicationNr}` : 'MyPages';
    }
}

export interface GetMessagesResponse {
    TotalMessageCount: number;
    AareMessageTextsIncluded: boolean;
    Messages: MessageServerModel[];
    CustomerChannels: MessageChannelModel[];
}

export interface CustomerMessageAttachedDocumentModel {
    Id: number;
    FileName: string;
    ArchiveKey: string;
    ContentTypeMimetype: string;
}

export interface MessageServerModel {
    Id: number;
    Text: string;
    TextFormat: string;
    CustomerId: number;
    IsFromCustomer: boolean;
    CreationDate: string;
    CreatedByUserId: number;
    HandledDate: string;
    HandledByUserId: number;
    ChannelType: string;
    ChannelId: string;
    CustomerMessageAttachedDocuments: CustomerMessageAttachedDocumentModel[];
}

export interface MessageChannelModel {
    CustomerId: number;
    ChannelType: string;
    ChannelId: string;
    IsRelation: boolean;
    RelationStartDate: string;
    RelationEndDate: string;
}

export interface CreateMessageResponse {
    WasNotificationEmailSent: boolean;
    CreatedMessage: SecureMessage;
}

export interface SecureMessage {
    Id: number;
    Text: string;
    TextFormat: string;
    CustomerId: number;
    IsFromCustomer: boolean;
    ChannelType: string;
    ChannelId: string;
}
