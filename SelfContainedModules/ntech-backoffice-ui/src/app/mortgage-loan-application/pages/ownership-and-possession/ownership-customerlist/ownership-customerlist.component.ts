import { Component, Input, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, UntypedFormGroup, Validators } from '@angular/forms';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { ConfigService } from 'src/app/common-services/config.service';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { MortgageLoanApplicationApiService } from 'src/app/mortgage-loan-application/services/mortgage-loan-application-api.service';

@Component({
    selector: 'ownership-customerlist',
    templateUrl: './ownership-customerlist.component.html',
    styles: [],
})
export class OwnershipCustomerlistComponent {
    constructor(
        private config: ConfigService,
        private validationService: NTechValidationService,
        private fb: UntypedFormBuilder,
        private apiService: MortgageLoanApplicationApiService
    ) {}

    @Input()
    public initialData: OwnershipCustomerlistComponentInitialData;

    public m: Model;

    async ngOnInit() {}

    async ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let m: Model = {
            newForm: this.fb.group({
                civicNr: ['', [Validators.required, this.validationService.getCivicRegNrValidator()]],
            }),
            addForm: this.fb.group({
                firstName: ['', [Validators.required]],
                lastName: ['', [Validators.required]],
                addressStreet: ['', [Validators.required]],
                addressZipcode: ['', [Validators.required]],
                addressCity: ['', [Validators.required]],
                addressCountry: ['', []],
                email: ['', [Validators.required, this.validationService.getEmailValidator()]],
                phone: ['', [Validators.required, this.validationService.getPhoneValidator()]],
            }),
            form: null,
            isAdding: false,
        };
        m.form = this.fb.group({
            new: m.newForm,
            add: m.addForm,
        });

        this.m = m;
    }

    public getCustomerCardUrl(customerId: number) {
        let targetToHere = CrossModuleNavigationTarget.create('MortgageLoanAppOwnerShipAndPossession', {
            applicationNr: this.initialData.applicationNr,
        });

        return this.config.getServiceRegistry().createUrl('nCustomer', 'Customer/CustomerCard', [
            ['customerId', customerId.toString()],
            ['backTarget', targetToHere.getCode()],
        ]);
    }

    private itemNames = [
        'firstName',
        'lastName',
        'addressStreet',
        'addressZipcode',
        'addressZipcode',
        'addressCity',
        'addressCountry',
        'email',
        'phone',
    ];

    async search(evt?: Event) {
        evt.preventDefault();

        let newForm = this.m.form.controls['new'] as UntypedFormGroup;
        let civicRegNr = newForm.controls['civicNr'].value;

        let addForm = this.m.form.controls['add'] as UntypedFormGroup;

        this.m.isAdding = true;
        let customerId = (await this.apiService.shared().fetchCustomerIdByCivicRegNr(civicRegNr)).CustomerId;

        let customerData = await this.apiService.shared().fetchCustomerItems(customerId, this.itemNames);

        for (let itemName of this.itemNames) {
            let initialValue = customerData[itemName] || '';
            if (itemName === 'addressCountry' && !initialValue) {
                initialValue = this.config.getClient().BaseCountry;
            }
            addForm.controls[itemName].setValue(initialValue);
        }
    }

    async addCustomer(evt?: Event) {
        evt.preventDefault();

        let newForm = this.m.form.controls['new'] as UntypedFormGroup;
        let civicRegNr = newForm.controls['civicNr'].value;

        let customerId = (await this.apiService.shared().fetchCustomerIdByCivicRegNr(civicRegNr)).CustomerId;

        let addForm = this.m.form.controls['add'] as UntypedFormGroup;

        let result = await this.apiService.addCustomerToCustomerApplicationList(
            this.initialData.applicationNr,
            this.initialData.listName,
            customerId,
            {
                CivicRegNr: civicRegNr,
                FirstName: addForm.controls['firstName'].value,
                LastName: addForm.controls['lastName'].value,
                Email: addForm.controls['email'].value,
                Phone: addForm.controls['phone'].value,
                AddressStreet: addForm.controls['addressStreet'].value,
                AddressCountry: addForm.controls['addressCountry'].value,
                AddressCity: addForm.controls['addressCity'].value,
                AddressZipcode: addForm.controls['addressZipcode'].value,
            }
        );

        this.initialData.onAddOrRemoveCustomer({
            CustomerId: result.CustomerId,
            IsAdd: true,
            ListName: this.initialData.listName,
        });
    }

    async removeCustomer(customer: MemberCustomer, evt?: Event) {
        evt?.preventDefault();

        await this.apiService.removeCustomerFromCustomerApplicationList(
            this.initialData.applicationNr,
            this.initialData.listName,
            customer.customerId
        );

        this.initialData.onAddOrRemoveCustomer({
            CustomerId: customer.customerId,
            IsAdd: false,
            ListName: this.initialData.listName,
        });
    }

    isCivicNrValid(civicNr: string) {
        return civicNr && this.validationService.isValidCivicNr(civicNr);
    }
}

interface Model {
    form: UntypedFormGroup;
    newForm: UntypedFormGroup;
    addForm: UntypedFormGroup;
    isAdding: boolean;
}

export interface MemberCustomer {
    customerId: number;
    firstName: string;
    birthDate: string;
}

export interface OwnershipCustomerlistComponentInitialData {
    applicationNr: string;
    memberCustomers: MemberCustomer[];
    title: string;
    helpText: string;
    noMembersText: string;
    listName: string;
    onAddOrRemoveCustomer: (evt: { CustomerId: number; IsAdd: boolean; ListName: string }) => void;
    isReadonly: boolean;
}
