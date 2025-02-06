import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { CustomerInfoInitialData } from 'src/app/common-components/customer-info/customer-info.component';
import { CustomerInfoService } from 'src/app/common-components/customer-info/customer-info.service';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { distinct } from 'src/app/common.types';

@Component({
    selector: 'app-legacy-ml-collateral-page',
    templateUrl: './legacy-ml-collateral-page.component.html',
    styles: [],
})
export class LegacyMlCollateralPageComponent implements OnInit {
    constructor(
        private route: ActivatedRoute,
        private eventService: NtechEventService,
        private apiService: NtechApiService,
        private customerInfoService: CustomerInfoService
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

        let targetToHere = CrossModuleNavigationTarget.create('CreditMortgageloanLegacyCollateral', {
            creditNr: creditNr,
        });

        let collaterals = (await this.getMortgageLoanCollaterals(creditNr))?.Collaterals;

        let m: Model = {
            creditNr: creditNr,
            collaterals: [],
        };

        let allCustomerIds: number[] = [];
        collaterals.forEach((x) => allCustomerIds.push(...x.CustomerIds));
        allCustomerIds = distinct(allCustomerIds);

        let customerData = await this.customerInfoService.fetchCustomerComponentInitialDataByCustomerIdBulk(
            allCustomerIds,
            targetToHere.getCode()
        );

        for (let collateral of collaterals) {
            m.collaterals.push({
                c: collateral,
                customers: collateral.CustomerIds.map((x) => ({
                    customerId: x,
                    preLoadedCustomer: customerData[x],
                    linksOnTheLeft: true,
                    showAmlRisk: true,
                })),
            });
        }

        this.m = m;
    }

    //NOTE: We keep this out of credit-service as we dont want to promote the use of legacy things
    async getMortgageLoanCollaterals(creditNr: string) {
        let collateralsModelRaw = (
            await this.apiService.post<{ Value: string }>('nCredit', 'Api/KeyValueStore/Get', {
                Key: creditNr,
                KeySpace: 'MortgageLoanCollateralsV1',
            })
        )?.Value;

        if (!collateralsModelRaw) {
            return null;
        } else {
            return JSON.parse(collateralsModelRaw) as { Collaterals: CollateralModel[] };
        }
    }
}

interface Model {
    creditNr: string;
    collaterals: {
        c: CollateralModel;
        customers: CustomerInfoInitialData[];
    }[];
}

interface CollateralModel {
    IsMain: boolean;
    Properties: PropertyModel[];
    Valuations: ValuationModel[];
    CustomerIds: number[];
}

interface PropertyModel {
    CodeName: string;
    DisplayName: string;
    TypeCode: string;
    CodeValue: string;
    DisplayValue: string;
}

interface ValuationModel {
    ValuationDate: Date;
    Amount: number;
    TypeCode: string;
    SourceDescription: string;
}
