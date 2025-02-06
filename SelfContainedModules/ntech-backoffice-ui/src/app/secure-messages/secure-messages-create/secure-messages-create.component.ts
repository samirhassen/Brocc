import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import * as moment from 'moment';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { distinct } from 'src/app/common.types';
import { CustomerChannelModel, SecureMessagesApiService } from '../services/secure-messages-api.service';

@Component({
    selector: 'app-secure-messages-create',
    templateUrl: './secure-messages-create.component.html',
    styles: [],
})
export class SecureMessagesCreateComponent implements OnInit {
    public m?: Model;

    constructor(
        private messageApiService: SecureMessagesApiService,
        private apiService: NtechApiService,
        private activatedRoute: ActivatedRoute,
        private router: Router
    ) {}

    ngOnInit(): void {
        this.m = {
            omniSearchValue: '',
            channels: null,
            backTarget: CrossModuleNavigationTarget.parseBackTargetFromRoute(this.activatedRoute),
        };
    }

    clearSearch(evt: Event) {
        evt?.preventDefault();

        this.m.omniSearchValue = '';
    }

    omniSearch(evt: Event) {
        evt?.preventDefault();
        this.messageApiService.findCustomerChannels(this.m.omniSearchValue, 'Omni', false).then((x) => {
            let customerIds = distinct(x.CustomerChannels.map((y) => y.CustomerId));
            this.apiService.shared
                .fetchCustomerItemsBulk(customerIds, ['firstName', 'companyName', 'isCompany', 'birthDate'])
                .then((y) => {
                    let cs: ChannelModel[] = [];
                    for (let c of x.CustomerChannels) {
                        let birthDate: Date = null;
                        let firstName: string = '';
                        let customerInfo = y[c.CustomerId];
                        if (customerInfo) {
                            let isCompany = customerInfo['isCompany'] === 'true';
                            if (isCompany) {
                                firstName = customerInfo['companyName'];
                            } else {
                                firstName = customerInfo['firstName'];
                                let birthDateRaw = customerInfo['birthDate'];
                                if (birthDateRaw) {
                                    birthDate = moment(birthDateRaw, 'YYYY-MM-DD').toDate();
                                }
                            }
                        }

                        cs.push({
                            channel: c,
                            birthDate: birthDate,
                            firstName: firstName,
                        });
                    }
                    this.m.channels = cs;
                });
        });
    }

    openChannel(c: ChannelModel, evt?: Event) {
        evt?.preventDefault();

        this.router.navigate(['/secure-messages/channel/'], {
            queryParams: {
                channelId: c.channel.ChannelId,
                customerId: c.channel.CustomerId,
                channelType: c.channel.ChannelType,
                backTarget: CrossModuleNavigationTarget.create('SecureMessagesCreate', {}),
            },
        });
    }

    getProductName(c: ChannelModel) {
        return c.channel.ChannelId;
    }
}

class Model {
    backTarget: CrossModuleNavigationTarget;
    omniSearchValue: string;
    channels: ChannelModel[];
}

class ChannelModel {
    channel: CustomerChannelModel;
    firstName: string;
    birthDate: Date;
}
