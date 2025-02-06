import { Component, Input, SimpleChanges } from '@angular/core';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { distinct } from 'src/app/common.types';
import { CreditService } from '../../credit.service';

@Component({
    selector: 'ml-std-connections',
    templateUrl: './ml-std-connections.component.html',
    styles: [],
})
export class MlStdConnectionsComponent {
    constructor(private creditService: CreditService, private apiService: NtechApiService) {}

    @Input()
    public creditNr: string;

    public m: Model;

    async ngOnChanges(changes: SimpleChanges) {
        this.reload(this.creditNr);
    }

    private async reload(creditNr: string) {
        this.m = null;

        if (!creditNr) {
            return;
        }

        let creditCustomerData = await this.creditService.getCreditCustomersSimple(this.creditNr);

        let targetToHere = CrossModuleNavigationTarget.create('CreditOverviewCustomer', { creditNr: this.creditNr });

        let allCustomerIds = distinct(creditCustomerData.ListCustomers.map((x) => x.CustomerId));
        let customerData = await this.apiService.shared.fetchCustomerItemsBulk(allCustomerIds, [
            'firstName',
            'birthDate',
        ]);

        let mortgageLoanStandardParties = this.creditService.setupMortgageLoanStandardParties(
            creditCustomerData,
            customerData,
            targetToHere
        );

        this.m = {
            mortgageLoanStandardParties: mortgageLoanStandardParties,
        };
    }
}

interface Model {
    mortgageLoanStandardParties: MortgageLoanStandardParty[];
}

interface MortgageLoanStandardParty {
    partyTypeDisplayName: string;
    customers: {
        customerCardUrl: string;
        firstName: string;
        birthDate: string;
    }[];
}
