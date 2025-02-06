import { Component } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { distinct } from 'src/app/common.types';
import { CreditService, MortgageLoanStandardParty } from '../credit.service';

@Component({
    selector: 'app-ml-collateral-page',
    templateUrl: './ml-collateral-page.component.html',
    styles: [],
})
export class MlCollateralPageComponent {
    constructor(
        private route: ActivatedRoute,
        private eventService: NtechEventService,
        private creditService: CreditService,
        private apiService: NtechApiService
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
            let title = 'Credit collateral';
            this.eventService.setCustomPageTitle(title, title);
            return;
        }
        this.eventService.setCustomPageTitle(`Credit ${creditNr}`, `Credit collateral ${creditNr}`);

        let targetToHere = CrossModuleNavigationTarget.create('CreditMortgageloanStandardCollateral', {
            creditNr: creditNr,
        });

        let collateralsResult = await this.creditService.fetchMortgageLoanStandardCollaterals([creditNr]);
        let collateral = collateralsResult.Collaterals[0];
        let ci = collateral.CollateralItems;
        let propertyItems: { name: string; value: string }[] = [];
        let add = (n: string, v: string) => propertyItems.push({ name: n, value: v });

        if (ci['objectTypeCode']) {
            let isBrf = ci['objectTypeCode']?.StringValue === 'seBrf';
            if (isBrf) {
                add('Property type', 'BRF');
                add('Housing cooperative name', ci['seBrfName']?.StringValue);
                add('Housing cooperative org. nr', ci['seBrfOrgNr']?.StringValue);
                add('Housing cooperative apartment nr', ci['seBrfApartmentNr']?.StringValue);
                add('Tax office apartment nr', ci['seTaxOfficeApartmentNr']?.StringValue);
            } else {
                add('Property type', 'Fastighet');
                add('Fastighetsbeteckning', ci['objectId']?.StringValue);
            }
            add('Street', ci['objectAddressStreet']?.StringValue);
            add('Zip code', ci['objectAddressZipcode']?.StringValue);
            add('City', ci['objectAddressCity']?.StringValue);
            add('Municipality', ci['objectAddressMunicipality']?.StringValue);
        }

        let creditCustomerData = await this.creditService.getCreditCustomersSimple(creditNr);
        let allCustomerIds = distinct(creditCustomerData.ListCustomers.map((x) => x.CustomerId));
        let customerData = await this.apiService.shared.fetchCustomerItemsBulk(allCustomerIds, [
            'firstName',
            'birthDate',
        ]);

        this.m = {
            creditNr: creditNr,
            mortgageLoanStandardParties: this.creditService.setupMortgageLoanStandardParties(
                creditCustomerData,
                customerData,
                targetToHere
            ),
            propertyItems: propertyItems,
            otherCreditNrsWithSameCollateral: collateral.CreditNrs.filter((x) => x != creditNr),
        };
    }
}

interface Model {
    creditNr: string;
    mortgageLoanStandardParties: MortgageLoanStandardParty[];
    propertyItems: { name: string; value: string }[];
    otherCreditNrsWithSameCollateral: string[];
}
