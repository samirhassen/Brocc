import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { ConfigService } from 'src/app/common-services/config.service';
import { getNumberDictionaryKeys } from 'src/app/common.types';
import { WorkflowStepHelper } from 'src/app/shared-application-components/services/workflow-helper';
import { CollateralServerCreditModel, CollateralService } from '../../services/collateral-service';
import { MortgageLoanApplicationApiService } from '../../services/mortgage-loan-application-api.service';
import {
    MemberCustomer,
    OwnershipCustomerlistComponentInitialData,
} from './ownership-customerlist/ownership-customerlist.component';

const OwnerListName = 'mortgageLoanPropertyOwner';

@Component({
    selector: 'app-ownership-and-possession',
    templateUrl: './ownership-and-possession.component.html',
    styleUrls: ['./ownership-and-possession.component.scss'],
})
export class OwnershipAndPossessionComponent implements OnInit {
    constructor(
        private apiService: MortgageLoanApplicationApiService,
        private route: ActivatedRoute,
        private config: ConfigService,
        private collateralService: CollateralService
    ) {}

    public m: Model;

    async ngOnInit() {
        this.reload(this.route.snapshot.params['applicationNr']);
    }

    private async reload(applicationNr: string) {
        this.m = null;
        let application = await this.apiService.fetchApplicationInitialData(applicationNr);
        if (application === 'noSuchApplicationExists') {
            return;
        }

        let connectedPropertyLoans: CollateralServerCreditModel[] = [];
        let creditCollateralId = this.collateralService.getConnectedCollateralId(application);
        if (creditCollateralId) {
            let collaterals = await this.collateralService.fetchCollaterals(
                { collateralIds: [creditCollateralId] },
                this.getTargetToHere(application.applicationNr)
            );
            if (collaterals.length > 0) {
                connectedPropertyLoans = collaterals[0].credits;
            }
        }

        let step = new WorkflowStepHelper(
            application.workflow.Model,
            application.workflow.Model.Steps.find((x) => x.Name === 'Collateral'),
            application.applicationInfo
        );
        let isCollateralActiveAndCurrent =
            application.applicationInfo.IsActive && step.areAllStepBeforeThisAccepted() && step.isStatusInitial();

        let isOwnershipCheckApproved = CollateralService.isOwnershipCheckApproved(application);
        let m: Model = {
            applicationNr: application.applicationNr,
            propertyFields: [],
            applicantPropertyOwners: [],
            nonApplicantPropertyOwners: [],
            nonApplicantConsentingParties: [],
            ownerListInitialData: null,
            consentingPartyListInitialData: null,
            connectedPropertyLoans: connectedPropertyLoans,
            isPossibleToApprove: isCollateralActiveAndCurrent && !isOwnershipCheckApproved,
            isPossibleToRevert: isCollateralActiveAndCurrent && isOwnershipCheckApproved,
            isReadonly: isOwnershipCheckApproved || !isCollateralActiveAndCurrent,
        };

        let allConnectedCustomerIdsWithRoles = application.getAllConnectedCustomerIdsWithRoles();
        let customerIdByApplicantNr = application.customerIdByApplicantNr;
        let allCustomerIds = getNumberDictionaryKeys(allConnectedCustomerIdsWithRoles);
        let customerData = await this.apiService
            .shared()
            .fetchCustomerItemsBulk(allCustomerIds, ['firstName', 'birthDate']);

        for (let customerId of getNumberDictionaryKeys(allConnectedCustomerIdsWithRoles)) {
            let roles = allConnectedCustomerIdsWithRoles[customerId];
            let applicantNr =
                customerIdByApplicantNr[1] === customerId ? 1 : customerIdByApplicantNr[2] === customerId ? 2 : null;
            if (applicantNr) {
                m.applicantPropertyOwners.push({
                    applicantNr: applicantNr,
                    customerId: customerId,
                    firstName: customerData[customerId]['firstName'],
                    birthDate: customerData[customerId]['birthDate'],
                    isOwner: roles.includes(OwnerListName),
                });
            } else {
                if (roles.includes(OwnerListName)) {
                    m.nonApplicantPropertyOwners.push({
                        customerId: customerId,
                        firstName: customerData[customerId]['firstName'],
                        birthDate: customerData[customerId]['birthDate'],
                    });
                }
                if (roles.includes('mortgageLoanConsentingParty')) {
                    m.nonApplicantConsentingParties.push({
                        customerId: customerId,
                        firstName: customerData[customerId]['firstName'],
                        birthDate: customerData[customerId]['birthDate'],
                    });
                }
            }
        }

        let app = application.getComplexApplicationList('Application', true).getRow(1, true);

        let addField = (n: string, v: string) => m.propertyFields.push({ label: n, value: v });
        if (app.getUniqueItem('objectTypeCode') === 'seBrf') {
            addField('Property type', 'Brf');
            addField('Housing cooperative name', app.getUniqueItem('seBrfName'));
            addField('Housing cooperative orgnr', app.getUniqueItem('seBrfOrgNr'));
            addField('Housing cooperative apartment nr', app.getUniqueItem('seBrfApartmentNr'));
        } else {
            addField('Property type', 'Fastighet');
            addField('Fastighetsbeteckning', app.getUniqueItem('objectId'));
        }
        addField('Street', app.getUniqueItem('objectAddressStreet'));
        addField('Zip code', app.getUniqueItem('objectAddressZipcode'));
        addField('City', app.getUniqueItem('objectAddressCity'));
        addField('Municipality', app.getUniqueItem('objectAddressMunicipality'));

        m.ownerListInitialData = {
            applicationNr: applicationNr,
            title: 'Property owners',
            helpText: 'Lägg till ägare/besittningsrätt som inte är sökande.',
            noMembersText: 'There is no property owner',
            listName: 'mortgageLoanPropertyOwner',
            memberCustomers: m.nonApplicantPropertyOwners,
            onAddOrRemoveCustomer: (x) => this.onCustomerAddedOrRemoved(x),
            isReadonly: m.isReadonly,
        };

        m.consentingPartyListInitialData = {
            applicationNr: applicationNr,
            title: 'Consenting parties',
            helpText: 'Lägg till maka/e eller sambo som behöver ge sitt medgivande som inte är sökande.',
            noMembersText: 'There is no consenting party',
            listName: 'mortgageLoanConsentingParty',
            memberCustomers: m.nonApplicantConsentingParties,
            onAddOrRemoveCustomer: (x) => this.onCustomerAddedOrRemoved(x),
            isReadonly: m.isReadonly,
        };

        this.m = m;
    }

    private getTargetToHere(applicationNr: string) {
        return CrossModuleNavigationTarget.create('MortgageLoanAppOwnerShipAndPossession', {
            applicationNr: applicationNr,
        });
    }

    public getCustomerCardUrl(customerId: number) {
        return this.config.getServiceRegistry().createUrl('nCustomer', 'Customer/CustomerCard', [
            ['customerId', customerId.toString()],
            ['backTarget', this.getTargetToHere(this.m.applicationNr).getCode()],
        ]);
    }

    public onApplicantOwnerChanged(applicant: { customerId: number; isOwner: boolean }, evt: Event) {
        evt.preventDefault();
        let isOwner = (evt.target as any).value === 'true';
        if (!(isOwner !== applicant.isOwner)) {
            return;
        }

        let applicationNr = this.m.applicationNr;
        if (isOwner) {
            this.apiService
                .addCustomerToCustomerApplicationList(applicationNr, OwnerListName, applicant.customerId)
                .then((x) => {
                    if (x.WasAdded) {
                        this.reload(applicationNr);
                    }
                });
        } else {
            this.apiService
                .removeCustomerFromCustomerApplicationList(applicationNr, OwnerListName, applicant.customerId)
                .then((x) => {
                    if (x.WasRemoved) {
                        this.reload(applicationNr);
                    }
                });
        }
    }

    onCustomerAddedOrRemoved(event: { CustomerId: number; IsAdd: boolean; ListName: string }) {
        this.reload(this.m.applicationNr);
    }

    approve(evt?: Event) {
        evt?.preventDefault();
        this.setIsApproved(true).then(() => {
            this.reload(this.m.applicationNr);
        });
    }

    revert(evt?: Event) {
        evt?.preventDefault();

        this.setIsApproved(false).then(() => {
            this.reload(this.m.applicationNr);
        });
    }

    private setIsApproved(isApproved: boolean) {
        return this.apiService.api().post('nPreCredit', 'api/MortgageLoanStandard/OwnershipCheck/Set-Approved', {
            applicationNr: this.m.applicationNr,
            isApproved,
        });
    }
}

interface Model {
    applicationNr: string;
    propertyFields: { label: string; value: string }[];
    applicantPropertyOwners: {
        firstName: string;
        birthDate: string;
        customerId: number;
        isOwner: boolean;
        applicantNr: number;
    }[];
    nonApplicantPropertyOwners: MemberCustomer[];
    nonApplicantConsentingParties: MemberCustomer[];
    ownerListInitialData: OwnershipCustomerlistComponentInitialData;
    consentingPartyListInitialData: OwnershipCustomerlistComponentInitialData;
    connectedPropertyLoans: CollateralServerCreditModel[];
    isPossibleToApprove: boolean;
    isPossibleToRevert: boolean;
    isReadonly: boolean;
}
