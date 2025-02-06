import { Component, OnInit, TemplateRef } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { ToastrService } from 'ngx-toastr';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { getYearMonthDayDateValidator } from '../../services/shared-functions';
import { UserManagementApiService, UserModel } from '../../services/user-management-api.service';

@Component({
    selector: 'app-administer-users',
    templateUrl: './administer-users.component.html',
    styles: [],
})
export class AdministerUsersComponent implements OnInit {
    constructor(
        private userApiService: UserManagementApiService,
        private modalService: BsModalService,
        private formBuilder: UntypedFormBuilder,
        private validationService: NTechValidationService,
        private toastr: ToastrService,
        private router: Router
    ) {}

    public m: Model = null;

    public createUserModalRef?: BsModalRef;

    ngOnInit(): void {
        this.reload(false);
    }

    reload(showDeleted: boolean) {
        this.userApiService.getAllUsers(showDeleted).then((x) => {
            this.m = {
                users: x,
                areDeactivatedIncluded: showDeleted,
            };
        });
    }

    onCreateUserStarted(template: TemplateRef<any>, evt?: Event) {
        evt?.preventDefault();

        let currentDisplayNames = this.m.users.map((x) => x.Name.toLowerCase());
        let displayNameValidator = this.validationService.getValidator('duplicateDisplayName', (input) => {
            if (!input) {
                return true; //handled by required
            }
            return currentDisplayNames.indexOf(input.toLowerCase()) < 0;
        });

        let createUserForm = new FormsHelper(
            this.formBuilder.group({
                userType: ['', [Validators.required]],
                displayName: ['', [Validators.required, displayNameValidator]],
            })
        );
        createUserForm.form.get('userType').valueChanges.subscribe((x) => {
            let isAdmin = createUserForm.getValue('userType') === 'admin';
            createUserForm.ensureControlOnConditionOnly(
                isAdmin,
                'adminStartDate',
                () => '',
                () => [Validators.required, getYearMonthDayDateValidator(this.validationService)]
            );
            createUserForm.ensureControlOnConditionOnly(
                isAdmin,
                'adminEndDate',
                () => '',
                () => [Validators.required, getYearMonthDayDateValidator(this.validationService)]
            );
        });

        this.m.createUserForm = createUserForm;

        this.createUserModalRef = this.modalService.show(template, { class: 'modal-xs', ignoreBackdropClick: true });
    }

    navigateToUser(id: number, evt?: Event) {
        evt?.preventDefault();

        this.router.navigate(['/user-management/administer-user/', id]);
    }

    reactivateUser(id: number, evt?: Event) {
        evt?.preventDefault();
        this.userApiService.reactivateUser(id).then((x) => {
            this.toastr.success('User has been reactivated. ', 'Reactivated');
            this.reload(this.m.areDeactivatedIncluded);
        });
    }

    createUser(evt?: Event) {
        evt?.preventDefault();
        this.createUserModalRef.hide();
        let f = this.m.createUserForm;
        this.m = null;
        this.userApiService
            .createUser(
                f.getValue('displayName'),
                f.getValue('userType'),
                f.getValue('adminStartDate'),
                f.getValue('adminEndDate')
            )
            .then((x) => {
                if (x.errorMessage) {
                    this.toastr.error(x.errorMessage);
                } else {
                    this.navigateToUser(x.createdUserId, null);
                }
            });
    }

    onShowDeactivatedToggled(evt?: Event) {
        let isChecked: boolean = (evt.target as any).checked;
        this.reload(isChecked);
    }
}

export class Model {
    users: UserModel[];
    createUserForm?: FormsHelper;
    areDeactivatedIncluded: boolean;
}
