import { Component, OnInit } from '@angular/core';
import { UntypedFormBuilder, UntypedFormGroup, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { CustomerPagesValidationService } from 'projects/customerpages/src/app/common-services/customer-pages-validation.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import {
    MyPagesMenuItemCode,
    MypagesShellComponentInitialData,
} from '../../../components/mypages-shell/mypages-shell.component';
import { CustomerInfo, MyPagesApiService } from '../../../services/mypages-api.service';

@Component({
    selector: 'my-contactinfo',
    templateUrl: './my-contactinfo.component.html',
    styles: [
        `
            .right-5 {
                margin-right: 5px;
            }
        `,
    ],
})
export class MyContactinfoComponent implements OnInit {
    constructor(
        private apiService: MyPagesApiService,
        private fb: UntypedFormBuilder,
        private validationService: CustomerPagesValidationService,
        private toastr: ToastrService
    ) {}

    public shellInitialData: MypagesShellComponentInitialData;
    public customer: CustomerInfo;
    public editForm: FormsHelper;
    public dummyViewForm: UntypedFormGroup; // Otherwise throws error when editForm.form does not exist (when in view mode)

    ngOnInit() {
        this.shellInitialData = {
            activeMenuItemCode: MyPagesMenuItemCode.MyData,
        };

        this.fetchCustomerInfo();
        this.dummyViewForm = this.fb.group({});
    }

    private fetchCustomerInfo() {
        this.apiService.fetchCustomerInfo().then((result) => {
            // Force null instead of empty object.
            if (!result.Address?.Street && !result.Address?.Zipcode) {
                result.Address = null;
            }
            this.customer = result;
        });
    }

    public onCancelEdit(evt?: Event) {
        evt?.preventDefault();

        this.editForm = null;
    }

    public beginEdit(evt?: Event) {
        evt?.preventDefault();

        let form = new FormsHelper(this.fb.group({}));
        form.addControlIfNotExists('emailAddress', this.customer.Email, [
            Validators.required,
            this.validationService.getEmailValidator(),
        ]);
        form.addControlIfNotExists('phoneNumber', this.customer.Phone, [
            Validators.required,
            this.validationService.getPhoneValidator(),
        ]);

        this.editForm = null; // Ensure empty.
        this.editForm = form;
    }

    public onSave(evt?: Event) {
        evt?.preventDefault();

        let newEmail: string = this.editForm.getValue('emailAddress');
        let newPhone: string = this.editForm.getValue('phoneNumber');

        let properties = [
            { Name: 'email', Value: newEmail },
            { Name: 'phone', Value: newPhone },
        ];
        this.apiService.updateCustomerValues(properties).then(
            (success) => {
                this.fetchCustomerInfo();
                this.editForm = null;
            },
            (error) => this.toastr.error('Något gick fel, vänligen kontrollera uppgifterna och försök igen. ')
        );
    }
}
