import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, TemplateRef } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import * as moment from 'moment';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { ToastrService } from 'ngx-toastr';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { generateUniqueId } from 'src/app/common.types';
import { getYearMonthDayDateValidator, parseYearMonthDayDate } from '../../services/shared-functions';
import {
    ActiveLoginMethodModel,
    AdministerUserGroupModel,
    AdministerUserLoginMethodModel,
    AdministerUserUserModel,
    CreateGroupmembershipRequest,
    UserManagementApiService,
} from '../../services/user-management-api.service';

@Component({
    selector: 'app-administer-user',
    templateUrl: './administer-user.component.html',
    styles: [],
})
export class AdministerUserComponent implements OnInit {
    constructor(
        private route: ActivatedRoute,
        private eventService: NtechEventService,
        private userApiService: UserManagementApiService,
        private configService: ConfigService,
        private toastr: ToastrService,
        private formBuilder: UntypedFormBuilder,
        private modalService: BsModalService,
        private validationService: NTechValidationService
    ) {}

    public m: Model = null;

    public modalRef?: BsModalRef;

    ngOnInit(): void {
        let userId: number = this.route.snapshot.params.userid;
        this.reload(userId);
    }

    private reload(userId: number) {
        if (this.modalRef) {
            this.modalRef.hide();
            this.modalRef = null;
        }
        this.userApiService.getActiveLoginMethods().then((x) => {
            this.userApiService.getDataForAdministerUser(userId).then((y) => {
                let isProvider = !!y.user.ProviderName;
                let isSystemUser = y.user.IsSystemUser;
                let isOnlyMortgageLoanEnabled =
                    this.configService.isFeatureEnabled('ntech.feature.mortgageloans') &&
                    !this.configService.isFeatureEnabled('ntech.feature.unsecuredloans');

                let m: Model = {
                    userId: userId,
                    groupMemberships: y.groups,
                    expiredGroupMemberships: y.expiredGroups,
                    user: y.user,
                    loginMethods: y.loginMethods,
                    isProvider: isProvider,
                    isSystemUser: isSystemUser,
                    isRegularUser: !isProvider && !isSystemUser,
                    activeLoginMethods: [],
                    isEditingSelf: y.user.UserId === this.configService.getCurrentUserId(),
                    isDeactivated: !!y.user.DeletionDate,
                    products: [
                        { n: isOnlyMortgageLoanEnabled ? 'Mortgage loan' : 'Consumer Credit', v: 'ConsumerCredit' },
                    ],
                    groups: [
                        { n: 'Admin', v: 'Admin' },
                        { n: 'Economy', v: 'Economy' },
                        { n: 'High', v: 'High' },
                        { n: 'Middle', v: 'Middle' },
                        { n: 'Low', v: 'Low' },
                    ],
                    minDate1: moment().startOf('day').format('YYYY-MM-DD'),
                    maxDate1: moment().startOf('day').add(90, 'days').format('YYYY-MM-DD'),
                    minDate2: moment().startOf('day').add(1, 'days').format('YYYY-MM-DD'),
                    maxDate2: moment().startOf('day').add(10, 'years').format('YYYY-MM-DD'),
                };

                //Not showing the full title in the browser to not leak the username unless that specific tab is open
                this.eventService.setCustomPageTitle('Administer user: ' + m.user.DisplayName, 'Administer user');

                for (let activeMethod of x) {
                    if (isProvider && activeMethod.IsAllowedForProvider === true) {
                        m.activeLoginMethods.push({ method: activeMethod, localId: generateUniqueId(10) });
                    } else if (isSystemUser && activeMethod.IsAllowedForSystemUser === true) {
                        m.activeLoginMethods.push({ method: activeMethod, localId: generateUniqueId(10) });
                    } else if (m.isRegularUser && activeMethod.IsAllowedForRegularUser === true) {
                        m.activeLoginMethods.push({ method: activeMethod, localId: generateUniqueId(10) });
                    }
                }
                this.m = m;
            });
        });
    }

    showAddGroupMembership(template: TemplateRef<any>, evt?: Event) {
        evt?.preventDefault();

        let form = new FormsHelper(
            this.formBuilder.group({
                startDate: [
                    moment().format('YYYY-MM-DD'),
                    [Validators.required, getYearMonthDayDateValidator(this.validationService, 'startDate')],
                ],
                endDate: ['', [Validators.required, getYearMonthDayDateValidator(this.validationService, 'endDate')]],
                product: ['', [Validators.required]],
                group: ['', [Validators.required]],
            })
        );

        form.setFormValidator((x) => {
            let startDate = parseYearMonthDayDate(x.get('startDate').value, true);
            let endDate = parseYearMonthDayDate(x.get('endDate').value, true);
            if (!startDate.isValid() || !endDate.isValid()) {
                return true; //Handled by the respective field validators
            }

            if (endDate.isBefore(startDate)) {
                return false;
            }
            if (endDate.isAfter(moment().add(10, 'years'))) {
                return false;
            }
            return true;
        });

        this.m.addGroupForm = form;
        this.modalRef = this.modalService.show(template, { class: 'modal-xs', ignoreBackdropClick: true });
    }

    addGroupMembership(evt?: Event) {
        evt?.preventDefault();
        let f = this.m.addGroupForm;
        let request: CreateGroupmembershipRequest = {
            userId: this.m.userId,
            group: f.getValue('group'),
            product: f.getValue('product'),
            startDate: f.getValue('startDate'),
            endDate: f.getValue('endDate'),
        };
        let result: Promise<{ Id: number }>;
        if (request.group === 'Admin') {
            result = this.userApiService.createAdminGroupMembership(request);
        } else {
            result = this.userApiService.createNonAdminGroupMembership(request);
        }
        result.then((_) => {
            this.reload(this.m.userId);
        });
    }

    showCancelGroupMembershipDialog(template: TemplateRef<any>, group: AdministerUserGroupModel, evt?: Event) {
        evt?.preventDefault();
        this.m.pendingCancelGroupModel = group;
        this.modalRef = this.modalService.show(template, { class: 'modal-xs', ignoreBackdropClick: true });
    }

    showAddLoginMethod(template: TemplateRef<any>, evt?: Event) {
        evt?.preventDefault();
        let f = new FormsHelper(
            this.formBuilder.group({
                loginMethodLocalId: ['', [Validators.required]],
            })
        );
        this.m.addLoginMethodForm = f;
        f.form.get('loginMethodLocalId').valueChanges.subscribe((_) => {
            f.ensureControlOnConditionOnly(
                this.isFederatitionUsingADUsernameMethod(),
                'adUsername',
                () => '',
                () => [Validators.required, Validators.minLength(2)]
            );
            f.ensureControlOnConditionOnly(
                this.isFederationUsingEmailMethod(),
                'providerEmail',
                () => '',
                () => [Validators.required, this.validationService.getEmailValidator()]
            );
            f.ensureControlOnConditionOnly(
                this.isFederationUsingObjectId(),
                'providerObjectId',
                () => '',
                () => [Validators.required, Validators.minLength(2)]
            );
            f.ensureControlOnConditionOnly(
                this.isLocalUserNameAndPasswordMethod(),
                'upwUsername',
                () => '',
                () => [Validators.required, Validators.minLength(2)]
            );
            f.ensureControlOnConditionOnly(
                this.isLocalUserNameAndPasswordMethod(),
                'upwPassword1',
                () => '',
                () => [Validators.required, Validators.minLength(2)]
            );
            f.ensureControlOnConditionOnly(
                this.isLocalUserNameAndPasswordMethod(),
                'upwPassword2',
                () => '',
                () => [
                    Validators.required,
                    this.validationService.getValidator(
                        'samePassword',
                        (pw2) => this.m.addLoginMethodForm.getValue('upwPassword1') === pw2
                    ),
                ]
            );
        });
        this.modalRef = this.modalService.show(template, { class: 'modal-xs', ignoreBackdropClick: true });
    }

    addLoginMethod(evt?: Event) {
        evt?.preventDefault();

        let f = this.m.addLoginMethodForm;
        let loginMethod = this.getSelectedLoginMethod();
        this.userApiService
            .createLoginMethod({
                userId: this.m.userId,
                adUsername: f.getValue('adUsername'),
                providerEmail: f.getValue('providerEmail'),
                upwUsername: f.getValue('upwUsername'),
                upwPassword: f.getValue('upwPassword1'),
                providerObjectId: f.getValue('providerObjectId'),
                authenticationType: loginMethod.AuthenticationType,
                providerName: loginMethod.ProviderName,
                userIdentityAndCredentialsType: loginMethod.UserIdentityAndCredentialsType,
            })
            .then((x) => {
                if (x.errorMessage) {
                    this.toastr.error(x.errorMessage);
                } else {
                    this.reload(this.m.userId);
                }
            });
    }

    getPasswordStrength() {
        let upwPassword1 = this.m?.addLoginMethodForm?.getValue('upwPassword1');
        if (!upwPassword1 || upwPassword1.length < 1 || this.m?.addLoginMethodForm?.hasError('upwPassword1')) {
            return null;
        }
        let result = {
            cssClass: 'text-danger',
            text: 'Very weak',
        };
        if (upwPassword1.length >= 5) {
            result.text = 'Weak';
        }
        if (upwPassword1.length >= 10) {
            result.text = 'Ok';
            result.cssClass = 'text-warning';
        }
        if (upwPassword1.length >= 20) {
            result.text = 'Good';
            result.cssClass = 'text-success';
        }
        return result;
    }

    removeLoginMethod(method: AdministerUserLoginMethodModel, evt?: Event) {
        evt?.preventDefault();
        if (!this.m.user.IsRemoveAuthenticationMechanismAllowed) {
            //Also blocked serverside so just for allowing a more helpful message
            this.toastr.error(
                'Authentication mechanism cannot be removed for users that have logged in using them. Create a new user instead.'
            );
            return;
        }
        this.userApiService.removeLoginMethod(method.Id).then((x) => {
            if (x.errorMessage) {
                this.toastr.error(x.errorMessage);
            } else {
                this.reload(this.m.userId);
            }
        });
    }

    showDeactivateDialog(template: TemplateRef<any>, evt?: Event) {
        evt?.preventDefault();
        this.modalRef = this.modalService.show(template, { class: 'modal-xs', ignoreBackdropClick: true });
    }

    deactivateUser(evt?: Event) {
        evt?.preventDefault();
        this.userApiService.deactivateUser(this.m.userId).then((x) => {
            this.modalRef?.hide();
            this.toastr.success('User has been deactivated', 'Deactivated');
            this.reload(this.m.userId);
        });
    }

    showReactivateDialog(template: TemplateRef<any>, evt?: Event) {
        evt?.preventDefault();
        this.modalRef = this.modalService.show(template, { class: 'modal-xs', ignoreBackdropClick: true });
    }

    reactivateUser(evt?: Event) {
        evt?.preventDefault();
        evt?.preventDefault();
        this.userApiService.reactivateUser(this.m.userId).then((x) => {
            this.modalRef?.hide();
            this.toastr.success('User has been reactivated. ', 'Reactivated');
            this.reload(this.m.userId);
        });
    }

    beginGroupmembershipCancellation(evt?: Event) {
        evt?.preventDefault();
        this.userApiService
            .beginGroupMembershipCancellation(this.m.pendingCancelGroupModel.Id)
            .then((_) => {
                this.reload(this.m.userId);
            })
            .catch((x) => {
                let httpErr = x as HttpErrorResponse;
                if (httpErr && httpErr.status == 400) {
                    this.toastr.warning(httpErr.statusText);
                } else if (httpErr) {
                    this.toastr.error(httpErr.statusText);
                } else {
                    this.toastr.error('Request failed');
                }
            });
    }

    getSelectedLoginMethod() {
        if (!this.m.addLoginMethodForm) return null;

        let loginMethodId = this.m.addLoginMethodForm.getValue('loginMethodLocalId');
        if (!loginMethodId) return null;

        return this.m.activeLoginMethods.find((x) => x.localId === loginMethodId)?.method;
    }

    isFederatitionUsingADUsernameMethod() {
        return this.getSelectedLoginMethod()?.UserIdentityAndCredentialsType == 'FederatitionUsingADUsername';
    }

    isFederationUsingEmailMethod() {
        return this.getSelectedLoginMethod()?.UserIdentityAndCredentialsType == 'FederationUsingEmail';
    }

    isLocalUserNameAndPasswordMethod() {
        return this.getSelectedLoginMethod()?.UserIdentityAndCredentialsType == 'LocalUserNameAndPassword';
    }

    isFederationUsingObjectId() {
        return this.getSelectedLoginMethod()?.UserIdentityAndCredentialsType == 'FederationUsingObjectId';
    }
}

export class Model {
    userId: number;
    groupMemberships: AdministerUserGroupModel[];
    expiredGroupMemberships: any[]; //TODO
    user: AdministerUserUserModel;
    loginMethods: AdministerUserLoginMethodModel[];
    isProvider: boolean;
    isSystemUser: boolean;
    isRegularUser: boolean;
    activeLoginMethods: { method: ActiveLoginMethodModel; localId: string }[];
    isEditingSelf: boolean;
    isDeactivated: boolean;
    products: NameValuePairShort[];
    groups: NameValuePairShort[];
    minDate1: string;
    maxDate1: string;
    minDate2: string;
    maxDate2: string;
    addGroupForm?: FormsHelper;
    addLoginMethodForm?: FormsHelper;
    pendingCancelGroupModel?: AdministerUserGroupModel;
}

export interface NameValuePairShort {
    n: string;
    v: string;
}
