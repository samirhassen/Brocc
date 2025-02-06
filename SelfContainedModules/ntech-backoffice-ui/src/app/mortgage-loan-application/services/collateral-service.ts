import { Injectable } from '@angular/core';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { Dictionary, distinct } from 'src/app/common.types';
import { MortgageLoanApplicationApiService } from './mortgage-loan-application-api.service';
import { StandardMortgageLoanApplicationModel } from './mortgage-loan-application-model';

@Injectable({
    providedIn: 'root',
})
export class CollateralService {
    constructor(private apiService: MortgageLoanApplicationApiService) {}

    async fetchCollaterals(
        filter: { customerIds?: number[]; collateralIds?: number[] },
        targetToHere: CrossModuleNavigationTarget
    ): Promise<CollateralServerModel[]> {
        let collateralsResult = await this.apiService.fetchMortageLoanCollaterals(filter);

        let allCreditCustomerIds = CollateralService.multiDistinct(
            collateralsResult.Credits.map((x) => x.Customers.map((y) => y.CustomerId))
        );

        let customerData = await this.apiService
            .shared()
            .fetchCustomerItemsBulk(allCreditCustomerIds, ['firstName', 'birthDate']);

        let collaterals: CollateralServerModel[] = [];
        for (let c of collateralsResult.Collaterals) {
            let collateral: CollateralServerModel = {
                collateralId: c.CollateralId,
                credits: [] as CollateralServerCreditModel[],
                collateralStringItems: {},
            };
            for (let key of Object.keys(c.CollateralItems)) {
                collateral.collateralStringItems[key] = c.CollateralItems[key].StringValue;
            }
            let collateralCredits = collateralsResult.Credits.filter((x) => x.CollateralId === c.CollateralId);
            let credits: CollateralServerCreditModel[] = [];
            for (let collateralCredit of collateralCredits) {
                let collateralCustomers = collateralCredit.Customers.map((x) => ({
                    customerId: x.CustomerId,
                    firstName: customerData[x.CustomerId]['firstName'],
                    birthDate: customerData[x.CustomerId]['birthDate'],
                    listNames: x.ListNames,
                    rolesText: CollateralService.getRolesText(x),
                    isOwner: (x.ListNames || []).includes('mortgageLoanPropertyOwner'),
                }));
                credits.push({
                    creditNr: collateralCredit.CreditNr,
                    capitalBalance: collateralCredit.CurrentCapitalBalance,
                    customers: collateralCustomers,
                    creditCollateralTabUrl: CrossModuleNavigationTarget.create('CreditOverview', {
                        creditNr: collateralCredit.CreditNr,
                        initialTab: 'mortgageloanStandardCollateral',
                    }).getCrossModuleNavigationUrl(targetToHere),
                });
            }
            collateral.credits = credits;
            collaterals.push(collateral);
        }

        return collaterals;
    }

    public getConnectedCollateralId(application: StandardMortgageLoanApplicationModel) {
        return application
            .getComplexApplicationList('Application', true)
            .getRow(1, true)
            .getUniqueItemInteger('creditCollateralId');
    }

    public static isOwnershipCheckApproved(application: StandardMortgageLoanApplicationModel) {
        return (
            application
                .getComplexApplicationList('Application', true)
                .getRow(1, true)
                .getUniqueItemBoolean('isOwnershipCheckApproved') === true
        );
    }

    public static multiDistinct<T>(input: T[][]) {
        let result = [] as T[];
        input.forEach((x) => result.push(...x));
        return distinct(result);
    }

    public static getRolesText(customer: { ApplicantNr?: number; ListNames: string[] }) {
        let parts: string[] = [];
        if (customer.ApplicantNr === 1) {
            parts.push('HL');
        }
        if (customer.ApplicantNr === 2) {
            parts.push('ML');
        }
        if (customer.ListNames.includes('mortgageLoanPropertyOwner')) {
            parts.push('propertyOwner');
        }
        if (customer.ListNames.includes('mortgageLoanConsentingParty')) {
            parts.push('consentingParty');
        }
        return parts.join(', ');
    }
}

export interface CollateralServerModel {
    collateralId: number;
    collateralStringItems: Dictionary<string>;
    credits: CollateralServerCreditModel[];
}

export interface CollateralServerCreditModel {
    creditNr: string;
    capitalBalance: number;
    customers: {
        customerId: number;
        firstName: string;
        birthDate: string;
        rolesText: string;
        listNames: string[];
        isOwner: boolean;
    }[];
    creditCollateralTabUrl: string;
}
