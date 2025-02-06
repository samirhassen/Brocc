import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import * as moment from 'moment';
import { ToastrService } from 'ngx-toastr';
import { ConfigService } from 'src/app/common-services/config.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { parseYearMonthDayDate } from '../../services/shared-functions';
import { UserManagementApiService, UserModel } from '../../services/user-management-api.service';

@Component({
    selector: 'app-create-admin',
    templateUrl: './create-admin.component.html',
    styles: [],
})
export class CreateAdminComponent implements OnInit {
    constructor(
        private configService: ConfigService,
        private userApiService: UserManagementApiService,
        private toastr: ToastrService,
        private formBuilder: UntypedFormBuilder,
        private validationService: NTechValidationService
    ) {}

    public m: Model;

    ngOnInit(): void {
        this.userApiService.getAllUsers().then((allUsers) => {
            let startDateValidator = this.validationService.getValidator('startDate', (x) => {
                let startDate = parseYearMonthDayDate(x, true);
                if (!startDate.isValid()) {
                    return false;
                }
                if (startDate < moment().startOf('day') || startDate > moment().startOf('day').add(6, 'years')) {
                    return false;
                }
                return true;
            });
            let endDateValidator = this.validationService.getValidator('endDate', (x) => {
                let endDate = parseYearMonthDayDate(x, true);
                if (!endDate.isValid()) {
                    return false;
                }
                if (
                    endDate < moment().startOf('day').add(1, 'days') ||
                    endDate > moment().startOf('day').add(10, 'years')
                ) {
                    return false;
                }
                return true;
            });

            let m: Model = {
                products: [{ n: 'Consumer Credit', v: 'ConsumerCredit' }],
                candidateUsers: [],
                form: new FormsHelper(
                    this.formBuilder.group({
                        userId: ['', [Validators.required]],
                        startDate: [
                            moment().startOf('day').format('YYYY-MM-DD'),
                            [Validators.required, startDateValidator],
                        ],
                        endDate: [
                            moment().startOf('day').add(1, 'days').format('YYYY-MM-DD'),
                            [Validators.required, endDateValidator],
                        ],
                        product: ['', [Validators.required]],
                    })
                ),
            };
            m.form.setFormValidator((x) => {
                let startDate = parseYearMonthDayDate(x.get('startDate').value, true);
                let endDate = parseYearMonthDayDate(x.get('endDate').value, true);
                if (!startDate.isValid() || !endDate.isValid()) {
                    return true; //Validated by the respective fields
                }
                if (startDate > endDate) {
                    return false;
                }
                return true;
            }, 'invalidDateCombination');

            let currentUserId = this.configService.getCurrentUserId();
            for (let user of allUsers) {
                if (user.Id !== currentUserId) {
                    m.candidateUsers.push(user);
                }
            }
            this.m = m;
        });
    }

    addAdminMembership(evt?: Event) {
        this.userApiService
            .createAdminGroupMembership({
                userId: parseInt(this.m.form.getValue('userId')),
                startDate: this.m.form.getValue('startDate'),
                endDate: this.m.form.getValue('endDate'),
                group: 'Admin',
                product: this.m.form.getValue('product'),
            })
            .then(() => {
                this.toastr.info('New admin added');
                this.m.form.form.reset();
            })
            .catch((x) => {
                let httpError = x as HttpErrorResponse;
                if (httpError?.status === 400) {
                    this.toastr.error(httpError.statusText);
                } else {
                    this.toastr.error('Failed for unknown reason');
                }
            });
    }
}

class Model {
    products: { n: string; v: string }[];
    candidateUsers: UserModel[];
    form: FormsHelper;
}
