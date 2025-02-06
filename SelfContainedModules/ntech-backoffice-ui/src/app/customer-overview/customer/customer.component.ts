import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { distinct, groupByString, NumberDictionary } from 'src/app/common.types';
import {
    CustomerSearchEntity,
    CustomerSearchEntityCustomer,
    CustomerSearchSourceService,
} from '../services/customer-search-sources.service';

@Component({
    selector: 'app-customer',
    templateUrl: './customer.component.html',
    styleUrls: ['./customer.component.scss'],
})
export class CustomerComponent implements OnInit {
    constructor(private route: ActivatedRoute, private customerSearchSourceService: CustomerSearchSourceService, 
        private config: ConfigService, private apiService: NtechApiService) {}

    public m: CustomerComponentModel;

    async ngOnInit(): Promise<void> {
        this.route.paramMap.subscribe((params: ParamMap) => {
            this.reload(parseInt(params.get('customerId')));
        });
    }

    async reload(customerId: number) {
        this.m = null;

        let m: CustomerComponentModel = {
            customerId: customerId,
            loanGroups: null,
            savingsAccounts: null,
            loanApplications: null,
            navigationTargetToHere: CrossModuleNavigationTarget.create('CustomerOverview', {
                customerId: customerId.toString(),
            }),
        };

        let entities = await this.customerSearchSourceService.getCustomerEntities(customerId);

        let mapRoles = (entity: CustomerSearchEntity, customer: CustomerSearchEntityCustomer) =>
            customer.roles.map((x) => this.customerSearchSourceService.getRoleDisplayName(entity, x)).join(', ');

        let getUrl = (entity: CustomerSearchEntity) =>
            this.customerSearchSourceService
                .getEntityCrossModuleNavigationTarget(entity)
                ?.getCrossModuleNavigationUrl(m.navigationTargetToHere);

        if (this.customerSearchSourceService.hasLoanProducts()) {
            let loans = entities.filter((x) => this.customerSearchSourceService.isLoan(x));
            m.loanGroups = await this.createLoanGroups(loans, customerId, mapRoles, getUrl);
        }

        if (this.customerSearchSourceService.hasSavingsAccountProducts()) {
            m.savingsAccounts = [];
            for (let account of entities.filter((x) => this.customerSearchSourceService.isSavingsAccount(x))) {
                let customer = account.customers.find((x) => x.customerId === customerId);
                m.savingsAccounts.push({
                    accountNr: account.entityId,
                    statusText: account.statusDisplayText,
                    roleText: mapRoles(account, customer),
                    accountBalance: account.currentBalance,
                    isActive: account.isActive,
                    url: getUrl(account),
                });
            }
        }

        if (this.customerSearchSourceService.hasLoanApplicationProducts()) {
            m.loanApplications = [];
            for (let application of entities.filter((x) => this.customerSearchSourceService.isLoanApplication(x))) {
                let customer = application.customers.find((x) => x.customerId === customerId);
                m.loanApplications.push({
                    applicationNr: application.entityId,
                    statusText: application.statusDisplayText,
                    roleText: mapRoles(application, customer),
                    isActive: application.isActive,
                    url: getUrl(application),
                    creationDate: application.creationDate,
                });
            }
        }

        this.m = m;
    }

    private async createLoanGroups(loans: CustomerSearchEntity[], customerId: number,
        mapRoles: (x: CustomerSearchEntity, y: CustomerSearchEntityCustomer) => string,
        getUrl: (x: CustomerSearchEntity) => string) : Promise<LoanUiGroupModel[]> {
        let toLoans = (loanGroup: CustomerSearchEntity[]) => {
            let loans: LoanUiModel[] = [];
            for(let loan of loanGroup) {
                let customer = loan.customers.find((x) => x.customerId === customerId);
                loans.push({
                    loanNr: loan.entityId,
                    statusText: loan.statusDisplayText,
                    roleText: mapRoles(loan, customer),
                    capitalDebt: loan.currentBalance,
                    isActive: loan.isActive,
                    url: getUrl(loan),
                }); 
            }
            return loans;                   
        };
        if(this.config.baseCountry() === 'SE') {
            const MlCollateralGroup = 'MortgageLoan_Collateral';
            let nonGroupedLoans = loans.filter((x) => !x.groupId || x.groupType !== MlCollateralGroup);
            let mortageCollateralLoans = loans.filter((x) => x.groupType === MlCollateralGroup);
            
            let groups : LoanUiGroupModel[] = [];
            
            if(nonGroupedLoans.length > 0) {
                groups.push({
                    groupDisplayText: null,
                    loans: toLoans(nonGroupedLoans)
                })
            }
            
            if(mortageCollateralLoans.length > 0) {
                let collateralIds = distinct(mortageCollateralLoans.map(x => parseInt(x.groupId)));
                let propertyIdByCollateralId = (await this.apiService.post<NumberDictionary<string>>('NTechHost', 
                    'Api/Credit/MortgageLoan/Property-Id-By-CollateralId', { collateralIds })) ?? {};
                let loanByCollateralId = groupByString(mortageCollateralLoans, x => x.groupId);
                for(let collateralId of Object.keys(loanByCollateralId)) {
                    groups.push({
                        groupDisplayText: propertyIdByCollateralId[parseInt(collateralId)],
                        loans: toLoans(loanByCollateralId[collateralId])
                    });
                }
            }
            return groups;
        } else {
            return [{
                groupDisplayText: null,
                loans: toLoans(loans)
            }]
        }
    }
}

class CustomerComponentModel {
    customerId: number;
    navigationTargetToHere: CrossModuleNavigationTarget;
    loanGroups: LoanUiGroupModel[];
    savingsAccounts: {
        accountNr: string;
        url: string;
        statusText: string;
        roleText: string;
        accountBalance: number;
        isActive: boolean;
    }[];
    loanApplications: {
        applicationNr: string;
        url: string;
        statusText: string;
        roleText: string;
        creationDate: string;
        isActive: boolean;
    }[];
}

interface LoanUiModel {
    loanNr: string;
    url: string;
    statusText: string;
    roleText: string;
    capitalDebt: number;
    isActive: boolean;
}

interface LoanUiGroupModel {
    groupDisplayText: string
    loans: LoanUiModel[];
}