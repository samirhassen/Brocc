import { Component, Input, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { CustomerInfoInitialData } from 'src/app/common-components/customer-info/customer-info.component';
import { CustomerInfoService } from 'src/app/common-components/customer-info/customer-info.service';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { distinct } from 'src/app/common.types';
import { CreditService } from '../../credit.service';

@Component({
    selector: 'company-connections',
    templateUrl: './company-connections.component.html',
    styles: [],
})
export class CompanyConnectionsComponent {
    constructor(
        private toastr: ToastrService,
        private apiService: NtechApiService,
        private fb: UntypedFormBuilder,
        private validationService: NTechValidationService,
        private creditService: CreditService,
        private customerInfoService: CustomerInfoService
    ) {}

    public m: Model;

    @Input()
    public creditNr: string;

    async ngOnChanges(changes: SimpleChanges) {
        this.reload(this.creditNr);
    }

    private async reload(creditNr: string) {
        this.m = null;

        if (!creditNr) {
            return;
        }

        let searchCivicRegNrForm = new FormsHelper(
            this.fb.group({
                searchCivicRegNr: ['', [Validators.required, this.validationService.getCivicRegNrValidator()]],
            })
        );

        let addCompanyConnectionDetailsForm = new FormsHelper(
            this.fb.group({
                firstName: ['', [Validators.required]],
                lastName: ['', [Validators.required]],
                email: ['', [Validators.required, this.validationService.getEmailValidator()]],
                phone: ['', [Validators.required, this.validationService.getPhoneValidator()]],
                addressStreet: ['', []],
                addressZipcode: ['', []],
                addressCity: ['', []],
                addressCountry: ['', []],
                isBeneficialOwner: [false, []],
                isAuthorizedSignatory: [false, []],
            })
        );

        let creditCustomerData = await this.creditService.getCreditCustomersSimple(this.creditNr);

        let targetToHere = CrossModuleNavigationTarget.create('CreditOverviewCustomer', { creditNr: this.creditNr });
        let allCustomerIds = distinct(creditCustomerData.ListCustomers.map((x) => x.CustomerId));

        let allCustomerData = await this.customerInfoService.fetchCustomerComponentInitialDataByCustomerIdBulk(
            allCustomerIds,
            targetToHere.getCode()
        );

        let createList = (listName: string, displayName: string, minCount: number): CustomerList => {
            let members = creditCustomerData.ListCustomers.filter((x) => x.ListName === listName);
            let result: CustomerList = {
                listDisplayName: displayName,
                minCount: minCount,
                customers: members.map((x) => ({
                    customerId: x.CustomerId,
                    listName: listName,
                    customerInfoInitialData: {
                        customerId: x.CustomerId,
                        linksOnTheLeft: true,
                        showFatcaCrsLink: true,
                        showPepSanctionLink: true,
                        backTarget: targetToHere,
                        preLoadedCustomer: allCustomerData[x.CustomerId],
                    },
                })),
            };

            return result;
        };

        this.m = {
            creditNr: creditNr,
            searchCivicRegNrForm: searchCivicRegNrForm,
            addCompanyConnectionDetailsForm: addCompanyConnectionDetailsForm,
            lists: [
                createList('companyLoanBeneficialOwner', 'Beneficial owners', 0),
                createList('companyLoanAuthorizedSignatory', 'Authorized signatory', 1),
                createList('companyLoanCollateral', 'Collateral', null), //null meaning you can not delete these at all
            ],
        };
    }

    async searchCivicRegNr(evt?: Event) {
        evt?.preventDefault();

        let propertiesToFetch = [
            'firstName',
            'lastName',
            'email',
            'phone',
            'addressStreet',
            'addressZipcode',
            'addressCity',
            'addressCountry',
        ];

        let civicRegNr = this.m.searchCivicRegNrForm.getValue('searchCivicRegNr');
        let customerId = (await this.apiService.shared.fetchCustomerIdByCivicRegNr(civicRegNr)).CustomerId;
        let customer = await this.apiService.shared.fetchCustomerItems(customerId, propertiesToFetch);

        let f = this.m.addCompanyConnectionDetailsForm;

        f.setValue('firstName', customer['firstName']);
        f.setValue('lastName', customer['lastName']);
        f.setValue('email', customer['email']);
        f.setValue('phone', customer['phone']);
        f.setValue('addressStreet', customer['addressStreet']);
        f.setValue('addressZipcode', customer['addressZipcode']);
        f.setValue('addressCity', customer['addressCity']);
        f.setValue('isBeneficialOwner', false);
        f.setValue('isAuthorizedSignatory', false);

        this.m.newCompanyConnectionCustomer = {
            customerId: customerId,
            civicRegNr: civicRegNr,
        };
    }

    async addCompanyConnection(evt?: Event) {
        evt?.preventDefault();

        let f = this.m.addCompanyConnectionDetailsForm;

        let isBeneficialOwner: boolean = f.getValue('isBeneficialOwner');
        let isAuthorizedSignatory: boolean = f.getValue('isAuthorizedSignatory');

        let addToLists: string[] = [];
        if (isBeneficialOwner === true) addToLists.push('companyLoanBeneficialOwner');
        if (isAuthorizedSignatory === true) addToLists.push('companyLoanAuthorizedSignatory');

        if (addToLists.length === 0) {
            this.toastr.error('Customer must be at least one of Beneficial Owner and Authorized Signatory.');
            return;
        }

        let propArray: { Name: string; Value: string; ForceUpdate: boolean }[] = [];
        let addProperty = (name: string, value: string) => {
            if (value) {
                propArray.push({ Name: name, Value: value, ForceUpdate: true });
            }
        };

        addProperty('firstName', f.getValue('firstName'));
        addProperty('lastName', f.getValue('lastName'));
        addProperty('email', f.getValue('email'));
        addProperty('phone', f.getValue('phone'));
        if (f.getValue('addressStreet')) addProperty('addressStreet', f.getValue('addressStreet'));
        if (f.getValue('addressZipcode')) addProperty('addressZipcode', f.getValue('addressZipcode'));
        if (f.getValue('addressCity')) addProperty('addressCity', f.getValue('addressCity'));
        if (f.getValue('addressCountry')) addProperty('addressCountry', f.getValue('addressCountry'));

        let customerId = this.m.newCompanyConnectionCustomer.customerId;

        await this.creditService.createOrUpdatePersonCustomer({
            CivicRegNr: this.m.newCompanyConnectionCustomer.civicRegNr,
            ExpectedCustomerId: customerId,
            Properties: propArray,
        });

        await this.creditService.addCompanyConnections(customerId, this.m.creditNr, addToLists);

        this.reload(this.m.creditNr);
    }

    async cancelAddCompanyConnection(evt?: Event) {
        evt?.preventDefault();

        this.m.newCompanyConnectionCustomer = null;
    }

    async removeCustomerFromList(customerId: number, listName: string, evt?: Event) {
        evt?.preventDefault();

        await this.creditService.removeCompanyConnection(customerId, this.m.creditNr, listName);

        this.reload(this.m.creditNr);
    }
}

class Model {
    creditNr: string;
    searchCivicRegNrForm: FormsHelper;
    addCompanyConnectionDetailsForm: FormsHelper;
    newCompanyConnectionCustomer?: {
        civicRegNr: string;
        customerId: number;
    };
    lists: CustomerList[];
}

interface CustomerList {
    listDisplayName: string;
    minCount: number;
    customers: {
        customerId: number;
        listName: string;
        customerInfoInitialData: CustomerInfoInitialData;
    }[];
}
