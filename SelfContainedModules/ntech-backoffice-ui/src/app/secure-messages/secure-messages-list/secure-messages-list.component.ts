import { Component, OnInit, SimpleChanges } from '@angular/core';
import { ConfigService } from 'src/app/common-services/config.service';
import {
    CustomerMessagesGroupedByChannelTypeChannelId,
    SecureMessagesApiService,
} from '../services/secure-messages-api.service';
import { ActivatedRoute, Router } from '@angular/router';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';

@Component({
    selector: 'app-secure-messages-list',
    templateUrl: './secure-messages-list.component.html',
    styleUrls: [],
})
export class SecureMessagesListComponent implements OnInit {
    public m?: Model;

    constructor(
        public config: ConfigService,
        private messageApiService: SecureMessagesApiService,
        private activatedRoute: ActivatedRoute,
        private router: Router
    ) {}

    ngOnInit(): void {
        this.reload();
    }

    reload() {
        this.m = null;
        var request = this.createGetMessagesRequest(0);
        this.messageApiService.getCustomerMessagesByChannel(request).then((x) => {
            this.m = {
                messages: {
                    list: x.GroupedMessages,
                    totalCount: x.TotalMessageCount,
                },
                backTarget: CrossModuleNavigationTarget.parseBackTargetFromRoute(this.activatedRoute),
            };
        });
    }

    ngOnChanges(changes: SimpleChanges) {}

    openChannel(channel: CustomerMessagesGroupedByChannelTypeChannelId, event?: Event) {
        event?.preventDefault();

        this.router.navigate(['/secure-messages/channel/'], {
            queryParams: {
                channelId: channel.ChannelId,
                customerId: channel.CustomerId,
                channelType: channel.ChannelType,
                backTarget: CrossModuleNavigationTarget.create('SecureMessagesList', {}),
            },
        });
    }

    getProductName(c: CustomerMessagesGroupedByChannelTypeChannelId) {
        return c.ChannelId;
    }

    private createGetMessagesRequest(skipCount: number) {
        return {
            IncludeChannels: false,
            IncludeMessageTexts: false,
            SkipCount: skipCount,
            TakeCount: 500,
            IsHandled: false,
        };
    }
}

export class Model {
    backTarget?: CrossModuleNavigationTarget;
    channelDialogId?: string;
    messages?: {
        list: CustomerMessagesGroupedByChannelTypeChannelId[];
        totalCount: number;
    };
}
