import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap, Params, Router } from '@angular/router';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { StringDictionary, fromUrlSafeBase64String } from 'src/app/common.types';
import { CustomerSearchSourceService } from '../services/customer-search-sources.service';

@Component({
    selector: 'app-search',
    templateUrl: './search.component.html',
    styles: [],
})
export class SearchComponent implements OnInit {
    constructor(
        private eventService: NtechEventService,
        private route: ActivatedRoute,
        private apiService: NtechApiService,
        private customerSearchSourceService: CustomerSearchSourceService,
        private router: Router
    ) {}

    public m: SearchComponentModel;

    async ngOnInit(): Promise<void> {
        this.route.paramMap.subscribe((params: ParamMap) => {
            this.reload(params.get('query'));
        });
    }

    private async reload(queryRaw: string) {
        this.m = null;

        let query: string
        if((queryRaw ?? '').startsWith('eq__')) {
            query = fromUrlSafeBase64String(queryRaw.substring(4));
        } else {
            query = queryRaw;
        }

        if (!query) {
            return;
        }

        this.eventService.emitApplicationEvent('SetLayoutShellSearchQuery', query);
        let m = new SearchComponentModel();

        let customerIds = await this.customerSearchSourceService.findCustomers(query);
        let customerDataAll = await this.apiService.shared.fetchCustomerItemsBulk(customerIds, [
            'birthDate',
            'isCompany',
            'firstName',
            'companyName',
            'lastName',
            'addressStreet',
            'addressCity',
            'addressZipcode',
            'email',
        ]);

        let navigationTargetToHere = CrossModuleNavigationTarget.create('CustomerOverviewSearch', {
            searchQuery: query,
        });

        m.customers = [];
        if (customerIds.length === 1) {
            let customerLink = this.getCustomerLink(customerIds[0], navigationTargetToHere);
            this.router.navigate(customerLink.commands, {
                queryParams: customerLink.queryParams,
            });
            return;
        }

        for (let customerId of customerIds) {
            let customerData = customerDataAll[customerId];
            let isCompany = customerData['isCompany'] === 'true';
            let personOrCompanyText: string;
            if (isCompany) {
                personOrCompanyText = this.concatCustomerItems(customerData, 'companyName');
            } else {
                personOrCompanyText = `${
                    this.concatCustomerItems(customerData, 'firstName', 'lastName') ?? '<Unknown name>'
                }, ${customerData['birthDate']}`;
            }
            m.customers.push({
                customerId: customerId,
                personOrCompanyText: personOrCompanyText,
                addressText: this.concatCustomerItems(customerData, 'addressStreet', 'addressZipcode', 'addressCity'),
                email: customerData['email'],
                customerLink: this.getCustomerLink(customerId, navigationTargetToHere),
            });
        }

        this.m = m;
    }

    private concatCustomerItems(customerData: StringDictionary, ...names: string[]) {
        let values = names.map((x) => customerData[x]);
        let nonEmptyValues = values.filter((x) => (x ?? '').trim().length > 0);
        return nonEmptyValues.join(' ');
    }

    private getCustomerLink(
        customerId: number,
        targetToHere: CrossModuleNavigationTarget
    ): {
        commands: string[];
        queryParams: Params;
    } {
        return {
            commands: ['/customer-overview/customer/', customerId.toString()],
            queryParams: {
                backTarget: targetToHere.getCode(),
            },
        };
    }
}

class SearchComponentModel {
    customers: {
        customerId: number;
        personOrCompanyText: string;
        addressText: string;
        email: string;
        customerLink: {
            commands: string[];
            queryParams: Params;
        };
    }[];
}
