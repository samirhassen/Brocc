import { Component, OnInit } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { Dictionary } from 'src/app/common.types';
import { CustomerPagesApiService } from '../../../common-services/customer-pages-api.service';
import { CustomerPagesConfigService } from '../../../common-services/customer-pages-config.service';
import { CustomerMessagesHelper, MessageChannelModel } from '../../../shared-components/customer-messages-helper';
import { SecureMessagesEditListComponentInitialData } from '../../../shared-components/secure-messages-edit-list/secure-messages-edit-list.component';
import {
    MyPagesMenuItemCode,
    MypagesShellComponentInitialData,
} from '../../components/mypages-shell/mypages-shell.component';

@Component({
    selector: 'np-my-messages',
    templateUrl: './my-messages.component.html',
    styleUrls: [],
})
export class MyMessagesComponent implements OnInit {
    constructor(
        private fb: UntypedFormBuilder,
        private config: CustomerPagesConfigService,
        private sharedApiService: CustomerPagesApiService
    ) {}

    public m: Model;
    public messageListInitialData: SecureMessagesEditListComponentInitialData;

    ngOnInit(): void {
        //Use getSecureMessages to just load channels
        //This could be optimized to split into its own method if this becomes a problem
        let h = new CustomerMessagesHelper(null, this.config, this.sharedApiService);
        h.getSecureMessages({ skip: 0, take: 0 }, false).then((x) => {
            this.messageListInitialData = new SecureMessagesEditListComponentInitialData(false, true);
            this.reset(x.CustomerChannels, null);
        });
    }

    private reset(channels: MessageChannelModel[], channelUiId: string) {
        let messageForm = new FormsHelper(
            this.fb.group({
                channelUiId: ['', Validators.required],
            })
        );
        let m = new Model(messageForm);

        for (let channel of channels) {
            m.addChannel(channel.ChannelType, channel.ChannelId);
        }

        messageForm.form.valueChanges.subscribe((x) => {
            let channelUiId = messageForm.getValue('channelUiId');
            let channel = m.findChannel(channelUiId);
            if (channel) {
                this.messageListInitialData.setActiveChannel(channel.channelType, channel.channelId);
            } else {
                this.messageListInitialData.removeActiveChannel();
            }
        });

        if (m.channels.length > 0) {
            messageForm.setValue('channelUiId', channelUiId ?? m.channels[0].uiId);
        }

        this.m = m;
    }
}

class Model {
    public shellInitialData: MypagesShellComponentInitialData;
    constructor(public messageForm: FormsHelper) {
        this.shellInitialData = {
            activeMenuItemCode: MyPagesMenuItemCode.SecureMessage,
        };
        this.channels = [];
    }
    public channels: ChannelModel[];

    public addChannel(channelType: string, channelId: string) {
        this.channels.push({
            channelId: channelId,
            channelType: channelType,
            uiText: `${seChannelNames[channelType] || channelType} ${
                channelId === generalName ? '' : channelId
            }`.trimEnd(),
            uiId: `${channelType}#${channelId}`,
        });
    }

    public findChannel(uiId: string): ChannelModel {
        return this.channels.find((x) => x.uiId === uiId);
    }
}

interface ChannelModel {
    channelId: string;
    channelType: string;
    uiId: string;
    uiText: string;
}

const generalName = 'General';

const seChannelNames: Dictionary<string> = {
    Credit_UnsecuredLoan: 'Lån',
    Credit_MortgageLoan: 'Lån',
    SavingsAccount_StandardAccount: 'Sparkonto',
    Application_UnsecuredLoan: 'Ansökan',
    [generalName]: 'Övrigt',
};
//@ts-ignore TODO remove unused locals
const pageSize = 15;
