import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { CustomerPagesApiService } from '../../common-services/customer-pages-api.service';
import { CustomerPagesConfigService } from '../../common-services/customer-pages-config.service';
import { CustomerMessagesHelper, GetMessagesResponse } from '../customer-messages-helper';
import { SecureMessagesEditListComponentInitialData } from '../secure-messages-edit-list/secure-messages-edit-list.component';

@Component({
    selector: 'customer-pages-application-messages',
    templateUrl: './customer-pages-application-messages.component.html',
    styleUrls: [],
})
export class CustomerPagesApplicationMessagesComponent implements OnInit {
    constructor(private config: CustomerPagesConfigService, private apiService: CustomerPagesApiService) {}

    @Input()
    public initialData: CustomerPagesApplicationMessagesInitialData;
    public m: Model;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let messageListInitialData = new SecureMessagesEditListComponentInitialData(
            true,
            this.initialData.isApplicationActive || this.initialData.isInactiveMessagingAllowed
        );
        let channelType = this.initialData.isMortgagaeLoan ? 'Application_MortgageLoan' : 'Application_UnsecuredLoan';
        messageListInitialData.setActiveChannel(channelType, this.initialData.applicationNr);
        let h = new CustomerMessagesHelper(this.initialData.applicationNr, this.config, this.apiService);
        h.getSecureMessagesUnreadByCustomerCount().then((x) => {
            this.m = {
                isExpanded: this.initialData.isInitiallyExpanded,
                nrOfUnreadMessages: x.UnreadCount,
                messageListInitialData: messageListInitialData,
            };
        });
    }

    toggleExpanded(evt: Event) {
        if (evt) {
            evt.preventDefault();
        }
        this.m.isExpanded = !this.m.isExpanded;
    }

    onMessagesLoaded(_: GetMessagesResponse) {
        this.m.nrOfUnreadMessages = 0;
    }
}

class Model {
    isExpanded: boolean;
    nrOfUnreadMessages: number;
    messageListInitialData: SecureMessagesEditListComponentInitialData;
}

export class CustomerPagesApplicationMessagesInitialData {
    applicationNr: string;
    isApplicationActive: boolean;
    isInitiallyExpanded?: boolean;
    isMortgagaeLoan: boolean;
    isInactiveMessagingAllowed: boolean;
}
