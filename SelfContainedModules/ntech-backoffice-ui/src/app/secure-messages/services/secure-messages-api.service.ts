import { Injectable } from '@angular/core';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';

@Injectable({
    providedIn: 'root',
})
export class SecureMessagesApiService {
    constructor(private apiService: NtechApiService) {}

    handleMessages(request: { MessageIds: number[] }): Promise<{ Status: string }> {
        return this.apiService.post('nCustomer', '/api/CustomerMessage/HandleMessages', request);
    }

    createSecureCustomerMessage(request: {
        CustomerId: number;
        ChannelType: string;
        ChannelId: string;
        Text: string;
        TextFormat: string;
        IsFromCustomer: boolean;
        FlagPreviousMessagesAsHandled?: boolean;
        NotifyCustomerByEmail?: boolean;
    }): Promise<{ CreatedMessage: CustomerMessageModel; WasNotificationEmailSent: boolean }> {
        return this.apiService.post('nCustomer', '/api/CustomerMessage/CreateMessage', request);
    }

    attachMessageDocument(request: {
        MessageId: number;
        AttachedFileAsDataUrl: string;
        AttachedFileName: string;
    }): Promise<{ Id: number }> {
        return this.apiService.post('nCustomer', '/api/CustomerMessage/attachMessageDocument', request);
    }

    findCustomerChannels(
        searchText: string,
        searchType: string,
        includeGeneralChannel: boolean
    ): Promise<{ CustomerChannels: CustomerChannelModel[] }> {
        return this.apiService.post('nCustomer', '/api/CustomerMessage/FindCustomerChannels', {
            SearchText: searchText,
            SearchType: searchType,
            includeGeneralChannel: includeGeneralChannel,
        });
    }

    getMessages(request: {
        ChannelId: string;
        ChannelType: string;
        CustomerId: number;
        IncludeChannels: boolean;
        IncludeMessageTexts: boolean;
        SkipCount: number;
        TakeCount: number;
    }) {
        return this.apiService.post<GetMessagesResponse>('nCustomer', '/api/CustomerMessage/GetMessages', request);
    }

    getCustomerMessagesByChannel(request: {
        CustomerId?: number;
        ChannelType?: string;
        ChannelId?: string;
        IncludeMessageTexts?: boolean;
        SkipCount?: number;
        TakeCount?: number;
        IncludeChannels?: boolean;
        IsHandled?: boolean;
        IsFromCustomer?: boolean;
    }): Promise<{
        TotalMessageCount: number;
        AreMessageTextsIncluded: boolean;
        GroupedMessages: CustomerMessagesGroupedByChannelTypeChannelId[];
        Channels: CustomerChannelModel[];
    }> {
        return this.apiService.post('nCustomer', '/api/CustomerMessage/GetCustomerMessagesByChannel', request);
    }
}

export interface CustomerMessageModel {
    Id: number;
    Text: string;
    TextFormat: string;
    CustomerId: number;
    IsFromCustomer: boolean;
    CreationDate: Date;
    CreatedByUserId: number;
    HandledDate: Date;
    HandledByUserId: number;
    ChannelType: string;
    ChannelId: string;
    CustomerMessageAttachedDocuments: CustomerMessageAttachedDocumentModel[];
}
export interface CustomerMessageAttachedDocumentModel {
    Id: number;
    CustomerMessageId: number;
    FileName: string;
    ArchiveKey: string;
    ContentTypeMimetype: string;
}
export interface GetMessagesResponse {
    TotalMessageCount: number;
    AreMessageTextsIncluded: boolean;
    Messages: CustomerMessageModel[];
    Channels: CustomerChannelModel[];
}

export interface CustomerChannelModel {
    CustomerId: number;
    ChannelType: string;
    ChannelId: string;
    IsRelation: boolean;
    RelationStartDate: Date;
    RelationEndDate: Date;
}

export interface CustomerMessagesGroupedByChannelTypeChannelId {
    CreationDate: Date;
    ChannelType: string;
    ChannelId: string;
    CustomerId: number;
}
