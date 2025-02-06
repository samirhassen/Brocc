import { Component } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { CustomerInfoInitialData } from 'src/app/common-components/customer-info/customer-info.component';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { CreditService } from '../credit.service';

@Component({
    selector: 'app-customer-page',
    templateUrl: './customer-page.component.html',
    styles: [],
})
export class CustomerPageComponent {
    constructor(
        private route: ActivatedRoute,
        private eventService: NtechEventService,
        private creditService: CreditService,
        private configService: ConfigService
    ) {}

    async ngOnInit(): Promise<void> {
        this.route.paramMap.subscribe((params: ParamMap) => {
            this.reload(params.get('creditNr'));
        });
    }

    public m: Model;

    private async reload(creditNr: string) {
        this.m = null;

        if (!creditNr) {
            let title = 'Credit customer';
            this.eventService.setCustomPageTitle(title, title);
            return;
        }
        this.eventService.setCustomPageTitle(`Credit ${creditNr}`, `Credit customer ${creditNr}`);

        let result = await this.creditService.getCreditCustomersSimple(creditNr);

        let targetToHere = CrossModuleNavigationTarget.create('CreditOverviewCustomer', { creditNr: creditNr });

        const isStandardMortgageLoansEnabled = this.configService.isFeatureEnabled(
            'ntech.feature.mortgageloans.standard'
        );

        const isCompanyLoansEnabled = this.configService.isFeatureEnabled('ntech.feature.companyloans');

        this.m = {
            creditNr: creditNr,
            customers: result.CreditCustomers.map((x) => ({
                customerId: x.CustomerId,
                linksOnTheLeft: true,
                showPepSanctionLink: true,
                showFatcaCrsLink: true,
                showKycQuestionsLink: !isCompanyLoansEnabled,
                showAmlRisk: true,
                backTarget: targetToHere,
            })),
            isStandardMortgageLoansEnabled: isStandardMortgageLoansEnabled,
            isCompanyLoansEnabled: isCompanyLoansEnabled,
        };
    }
}

interface Model {
    creditNr: string;
    customers: CustomerInfoInitialData[];
    isStandardMortgageLoansEnabled: boolean;
    isCompanyLoansEnabled: boolean;
}
