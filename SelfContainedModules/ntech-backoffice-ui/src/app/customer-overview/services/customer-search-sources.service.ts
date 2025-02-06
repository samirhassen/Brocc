import { Injectable } from '@angular/core';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { Dictionary, distinct, StringDictionary } from 'src/app/common.types';

const sourcesBySourceName: Dictionary<CustomerSearchSource> = {
    nCredit: {
        sourceName: 'nCredit',
        isLoan: true,
        isSavingsAccount: false,
        isLoanApplication: false,
        apiHostModuleName: 'nCredit',
        getCustomerEntitiesModuleRelativeUrl: 'Api/Credit/CustomerSearch/Get-Customer-Entities',
        findCustomersModuleRelativeUrl: 'Api/Credit/CustomerSearch/Find-Customers-Omni',
    },
    nSavings: {
        sourceName: 'nSavings',
        isLoan: false,
        isSavingsAccount: true,
        isLoanApplication: false,
        apiHostModuleName: 'nSavings',
        getCustomerEntitiesModuleRelativeUrl: 'Api/Savings/CustomerSearch/Get-Customer-Entities',
        findCustomersModuleRelativeUrl: 'Api/Savings/CustomerSearch/Find-Customers-Omni',
    },
    nPreCredit: {
        sourceName: 'nPreCredit',
        isLoan: false,
        isSavingsAccount: false,
        isLoanApplication: true,
        apiHostModuleName: 'nPreCredit',
        getCustomerEntitiesModuleRelativeUrl: 'Api/PreCredit/CustomerSearch/Get-Customer-Entities',
        findCustomersModuleRelativeUrl: 'Api/PreCredit/CustomerSearch/Find-Customers-Omni',
    },
    nCustomer: {
        sourceName: 'nCustomer',
        isLoan: false,
        isSavingsAccount: false,
        isLoanApplication: false,
        apiHostModuleName: 'nCustomer',
        getCustomerEntitiesModuleRelativeUrl: 'Api/Customer/CustomerSearch/Get-Customer-Entities',
        findCustomersModuleRelativeUrl: 'Api/Customer/CustomerSearch/Find-Customers-Omni',
    },
};
let sources = Object.keys(sourcesBySourceName).map((x) => sourcesBySourceName[x]);

export interface CustomerSearchSource {
    sourceName: string;
    isLoan: boolean;
    isSavingsAccount: boolean;
    isLoanApplication: boolean;
    apiHostModuleName: string;
    getCustomerEntitiesModuleRelativeUrl: string;
    findCustomersModuleRelativeUrl: string;
}

@Injectable({
    providedIn: 'root',
})
export class CustomerSearchSourceService {
    constructor(private apiService: NtechApiService, private configService: ConfigService) {}

    private getActiveSources(): CustomerSearchSource[] {
        let sr = this.configService.getServiceRegistry();
        return sources.filter((x) => sr.containsService(x.apiHostModuleName));
    }

    public async getCustomerEntities(customerId: number): Promise<CustomerSearchEntity[]> {
        let entites: CustomerSearchEntity[] = [];
        for (let source of this.getActiveSources()) {
            let sourceEntities = await this.getCustomerEntitiesPerModule(
                source.apiHostModuleName,
                source.getCustomerEntitiesModuleRelativeUrl,
                customerId
            );
            entites.push(...(sourceEntities || []));
        }
        return entites;
    }

    private async getCustomerEntitiesPerModule(
        moduleName: string,
        relativeUrl: string,
        customerId: number
    ): Promise<CustomerSearchEntity[]> {
        return await this.apiService.post(moduleName, relativeUrl, { customerId }, { forceCamelCase: true });
    }

    public async findCustomers(searchQuery: string): Promise<number[]> {
        if (!searchQuery) {
            return [];
        }
        let customerIds: number[] = [];
        for (let source of this.getActiveSources()) {
            let sourceCustomerIds = await this.findCustomersPerModule(
                source.apiHostModuleName,
                source.findCustomersModuleRelativeUrl,
                searchQuery
            );
            customerIds.push(...(sourceCustomerIds || []));
        }
        return distinct(customerIds);
    }

    public getSource(sourceName: string) {
        return sourcesBySourceName[sourceName] || null;
    }

    public isLoan(entity: CustomerSearchEntity) {
        return this.getSource(entity.source)?.isLoan === true;
    }

    public isSavingsAccount(entity: CustomerSearchEntity) {
        return this.getSource(entity.source)?.isSavingsAccount === true;
    }

    public isLoanApplication(entity: CustomerSearchEntity) {
        return this.getSource(entity.source)?.isLoanApplication === true;
    }

    public hasLoanProducts() {
        let serviceRegistry = this.configService.getServiceRegistry();
        return sources.some((x) => x.isLoan && serviceRegistry.containsService(x.sourceName));
    }

    public hasSavingsAccountProducts() {
        let serviceRegistry = this.configService.getServiceRegistry();
        return sources.some((x) => x.isSavingsAccount && serviceRegistry.containsService(x.sourceName));
    }

    public hasLoanApplicationProducts() {
        let serviceRegistry = this.configService.getServiceRegistry();
        return sources.some((x) => x.isLoanApplication && serviceRegistry.containsService(x.sourceName));
    }

    public getRoleDisplayName(entity: CustomerSearchEntity, roleName: string) {
        if (entity.source === 'nCredit') {
            return nCreditRoleNames[roleName] || roleName;
        } else if (entity.source === 'nPreCredit') {
            return nPreCreditRoleNames[roleName] || roleName;
        }
        return roleName;
    }

    public getEntityCrossModuleNavigationTarget(entity: CustomerSearchEntity) {
        if (entity.source === 'nCredit') {
            return CrossModuleNavigationTarget.create('CreditOverview', { creditNr: entity.entityId });
        } else if (entity.source === 'nSavings') {
            return CrossModuleNavigationTarget.create('SavingsAccountOverviewSpecificTab', {
                savingsAccountNr: entity.entityId,
                tab: 'Details',
            });
        } else if (entity.source === 'nPreCredit') {
            return CrossModuleNavigationTarget.create('CreditApplicationLink', { applicationNr: entity.entityId });
        }
        return null;
    }

    private async findCustomersPerModule(
        moduleName: string,
        relativeUrl: string,
        searchQuery: string
    ): Promise<number[]> {
        return await this.apiService.post(moduleName, relativeUrl, { searchQuery }, { forceCamelCase: true });
    }
}

export interface CustomerSearchEntity {
    source: string;
    entityType: string;
    entityId: string;
    statusCode: string;
    statusDisplayText: string;
    isActive: boolean;
    creationDate: string;
    endDate: string;
    currentBalance: number;
    customers: CustomerSearchEntityCustomer[];
    groupId: string
    groupType: string
}

export interface CustomerSearchEntityCustomer {
    customerId: number;
    roles: string[];
}

const nCreditRoleNames: StringDictionary = {
    creditCustomer: 'Customer',
    mortgageLoanApplicant: 'Applicant',
    mortgageLoanConsentingParty: 'Consenting party',
    mortgageLoanPropertyOwner: 'Property owner',
    companyLoanApplicant: 'Applicant',
    companyLoanAuthorizedSignatory: 'Authorized signatory',
    companyLoanBeneficialOwner: 'Beneficial owner',
    companyLoanCollateral: 'Collateral',
};

const nPreCreditRoleNames: StringDictionary = {
    mortgageLoanApplicant: 'Applicant',
    mortgageLoanConsentingParty: 'Consenting party',
    mortgageLoanPropertyOwner: 'Property owner',
    companyLoanApplicant: 'Applicant',
    companyLoanAuthorizedSignatory: 'Authorized signatory',
    companyLoanBeneficialOwner: 'Beneficial owner',
    companyLoanCollateral: 'Collateral',
};
