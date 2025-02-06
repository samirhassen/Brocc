import { Component, Input, SimpleChanges } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { createMortgagePropertyIdFromCollateralItems } from 'src/app/common-services/ml-standard-functions';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { Dictionary, distinct, getDictionaryValues } from 'src/app/common.types';
import { CollateralService } from 'src/app/mortgage-loan-application/services/collateral-service';
import { StandardMortgageLoanApplicationModel } from 'src/app/mortgage-loan-application/services/mortgage-loan-application-model';

@Component({
    selector: 'property-loans',
    templateUrl: './property-loans.component.html',
    styleUrls: ['./property-loans.component.scss'],
})
export class PropertyLoansComponent {
    constructor(
        private apiService: NtechApiService,
        private eventService: NtechEventService,
        private toastr: ToastrService,
        private collateralService: CollateralService
    ) {}

    @Input()
    public initialData: PropertyLoansInitialData;

    async ngOnChanges(changes: SimpleChanges) {
        if (!this.initialData) {
            return;
        }

        let targetToHere = CrossModuleNavigationTarget.create('MortgageLoanStandardNewCreditCheck', {
            applicationNr: this.initialData.application.applicationNr,
        });

        let creditCollaterals = await this.collateralService.fetchCollaterals(
            { customerIds: getDictionaryValues(this.initialData.application.customerIdByApplicantNr) },
            targetToHere
        );

        let m: Model = {
            applicantCustomerIds: getDictionaryValues(this.initialData.application.customerIdByApplicantNr),
            collaterals: [],
        };

        let creditCollateralId = this.collateralService.getConnectedCollateralId(this.initialData.application);

        for (let c of creditCollaterals) {
            let collateral = {
                id: c.collateralId,
                name: this.getPropertyHeaderText(c.collateralStringItems),
                isConnected: creditCollateralId && c.collateralId === creditCollateralId,
                loans: c.credits.map((credit) => ({
                    creditNr: credit.creditNr,
                    capitalBalance: credit.capitalBalance,
                    url: credit.creditCollateralTabUrl,
                    customers: credit.customers.map((customer) => ({
                        customerId: customer.customerId,
                        firstName: customer.firstName,
                        birthDate: customer.birthDate,
                        roles: customer.rolesText,
                        isOwner: customer.isOwner,
                    })),
                })),
            };

            m.collaterals.push(collateral);
        }

        this.m = m;
    }

    private toggleConnectedToCollateral(applicationNr: string, connect: boolean, collateralId: number | null) {
        let ownerCustomerIds: number[] = null;
        if (connect) {
            ownerCustomerIds = [];
            let collateral = this.m.collaterals.find((x) => x.id === collateralId);
            let isAnyApplicantOwner = false;
            for (let loan of collateral.loans) {
                for (let customer of loan.customers) {
                    if (customer.isOwner) {
                        ownerCustomerIds.push(customer.customerId);
                        if (this.m.applicantCustomerIds.includes(customer.customerId)) {
                            isAnyApplicantOwner = true;
                        }
                    }
                }
            }
            if (!isAnyApplicantOwner) {
                this.toastr.warning('At least one applicant has to be property owner to connect the application');
                return new Promise<{ reload: boolean }>((resolve) => resolve({ reload: false }));
            }
            ownerCustomerIds = distinct(ownerCustomerIds);
        }
        return this.apiService
            .post('nPreCredit', 'Api/MortgageLoanStandard/Application/Connect-Credit-Collateral', {
                ApplicationNr: applicationNr,
                IsDisconnect: connect ? false : true,
                CreditCollateralId: connect ? collateralId : null,
                ownerCustomerIds: connect ? ownerCustomerIds : null,
            })
            .then((x) => ({ reload: true }));
    }

    private getPropertyHeaderText(collateralItems: Dictionary<string>) {
        return createMortgagePropertyIdFromCollateralItems((x) => collateralItems[x], true);
    }

    public m: Model;

    toggleCollateralConnected(collateral: any, evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.initialData.application.applicationNr;
        let connect = !collateral.isConnected;
        let id = connect ? collateral.id : null;
        this.toggleConnectedToCollateral(applicationNr, connect, id).then((x) => {
            if (x.reload) {
                this.eventService.signalReloadApplication(applicationNr);
            }
        });
    }

    isAnyOtherCollateralConnected(collateral: any) {
        return !!this.m.collaterals.find((x) => x.isConnected && x.id !== collateral.id);
    }
}

interface Model {
    applicantCustomerIds: number[];
    collaterals: CollateralModel[];
}

interface CollateralModel {
    id: number;
    isConnected: boolean;
    name: string;
    loans: {
        creditNr: string;
        capitalBalance: number;
        customers: {
            customerId: number;
            firstName: string;
            birthDate: string;
            roles: string;
            isOwner: boolean;
        }[];
        url: string;
    }[];
}

export interface PropertyLoansInitialData {
    application: StandardMortgageLoanApplicationModel;
    isReadOnly: boolean;
}

export const SynchronizedCreditApplicationItemNames: string[] = [
    'objectTypeCode',
    'seBrfOrgNr',
    'seBrfName',
    'seBrfApartmentNr',
    'seTaxOfficeApartmentNr',
    'objectId',
    'objectAddressStreet',
    'objectAddressZipcode',
    'objectAddressCity',
    'objectAddressMunicipality',
];
