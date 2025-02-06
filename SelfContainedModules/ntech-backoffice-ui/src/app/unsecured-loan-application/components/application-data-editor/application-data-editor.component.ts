import { ChangeDetectorRef, Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, ValidationErrors, Validators } from '@angular/forms';
import * as moment from 'moment';
import { ToastrService } from 'ngx-toastr';
import { BehaviorSubject, Observable, of, Subscription } from 'rxjs';
import { ComplexApplicationListRow } from 'src/app/common-services/complex-application-list';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { Dictionary, getDictionaryValues } from 'src/app/common.types';
import { EditblockFormFieldModel } from 'src/app/shared-application-components/components/editblock-form-field/editblock-form-field.component';
import { StandardCreditApplicationModel } from '../../services/standard-credit-application-model';
import { UnsecuredLoanApplicationApiService } from '../../services/unsecured-loan-application-api.service';

@Component({
    selector: 'application-data-editor',
    templateUrl: './application-data-editor.component.html',
    styles: [],
})
export class ApplicationDataEditorComponent implements OnInit {
    constructor(
        private fb: UntypedFormBuilder,
        private eventService: NtechEventService,
        private apiService: UnsecuredLoanApplicationApiService,
        private toastr: ToastrService,
        private validationService: NTechValidationService,
        private config: ConfigService,
        private changeDetector: ChangeDetectorRef
    ) {}

    ngOnInit(): void {}

    @Input()
    public initialData: ApplicationDataEditorInitialData;

    public m: Model;

    async ngOnChanges(changes: SimpleChanges) {
        await this.reset();
    }

    async reset() {
        if (this?.m?.subscriptions) {
            for (let s of this.m.subscriptions) {
                s.unsubscribe();
            }
        }

        this.m = null;

        if (!this.initialData) {
            return;
        }

        let a = this.initialData.application;
        let applicationRow = a.getComplexApplicationList('Application', true).getRow(1, true);

        let bankAccountValidatorResult = this.validationService.getBankAccountNrAsyncValidator(
            this.config,
            'bankAccountNr',
            (x, y) => this.apiService.validateBankAccountNr(x, y, true).then((x) => x.IsValid)
        );
        let bankAccountValidator = bankAccountValidatorResult.validator;
        let bankAccountType = bankAccountValidatorResult.defaultBankAccountType;

        let requestedRepaymentTimeInitialValue = this.validationService.parseRepaymentTimeWithPeriodMarker(applicationRow.getUniqueItem('requestedRepaymentTime'), true); 
        let form = new FormsHelper(this.fb.group({
            'requestedRepaymentTimeInPeriod': [requestedRepaymentTimeInitialValue.isMissingValue ? '' : requestedRepaymentTimeInitialValue.repaymentTime.toFixed(0), 
                [this.validationService.getPositiveIntegerValidator()]],
            'requestedRepaymentTimePeriod': [requestedRepaymentTimeInitialValue.isDays ? 'd' : 'm', [Validators.required]]
        }));
        let editFields: Dictionary<EditblockFormFieldModel> = {};
        editFields['requestedLoanAmount'] = {
            getForm: () => form,
            formControlName: 'requestedLoanAmount',
            labelText: 'Requested loan amount',
            inEditMode: () => this.m.inEditMode,
            getOriginalValue: () => applicationRow.getUniqueItem('requestedLoanAmount'),
            getCustomDisplayValue: () =>
                this.validationService.formatIntegerForDisplay(form.getValue('requestedLoanAmount')),
            getValidators: () => [this.validationService.getPositiveIntegerValidator()],
        };
        editFields['paidToCustomerBankAccountNr'] = {
            getForm: () => form,
            formControlName: 'paidToCustomerBankAccountNr',
            labelText: 'Customer bank account nr',
            inEditMode: () => this.m.inEditMode,
            getOriginalValue: () => applicationRow.getUniqueItem('paidToCustomerBankAccountNr'),
            getCustomDisplayValue: () => form.getValue('paidToCustomerBankAccountNr'),
            getValidators: () => [],
            getAsyncValidators: () => [bankAccountValidator],
        };
        editFields['directDebitBankAccountNr'] = {
            getForm: () => form,
            formControlName: 'directDebitBankAccountNr',
            labelText: 'Direct debit account',
            inEditMode: () => this.m.inEditMode,
            getOriginalValue: () => applicationRow.getUniqueItem('directDebitBankAccountNr'),
            getCustomDisplayValue: () => form.getValue('directDebitBankAccountNr'),
            getValidators: () => [],
            getAsyncValidators: () => [bankAccountValidator],
        };
        editFields['directDebitAccountOwnerApplicantNr'] = {
            getForm: () => form,
            formControlName: 'directDebitAccountOwnerApplicantNr',
            labelText: 'Direct debit account owner',
            inEditMode: () => this.m.inEditMode,
            getOriginalValue: () => applicationRow.getUniqueItem('directDebitAccountOwnerApplicantNr'),
            getCustomDisplayValue: () => form.getValue('directDebitAccountOwnerApplicantNr'),
            getValidators: () => [],
            dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption(
                this.getAccountOwnersApplicantNrDropdown()
            ),
        };
        
        let loanObjectives = await this.apiService.getAllLoanObjectives();
        if(loanObjectives?.length > 0) {
            editFields['loanObjective'] = {
                getForm: () => form,
                formControlName: 'loanObjective',
                labelText: 'Loan objective',
                inEditMode: () => this.m.inEditMode,
                getOriginalValue: () => applicationRow.getUniqueItem('loanObjective'),
                getCustomDisplayValue: () => form.getValue('loanObjective'),
                getValidators: () => [],
                dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption(
                    loanObjectives.map(x => ({ Code: x, DisplayName: x }))
                ),
            };            
        }

        let setup = (
            accountNrDisplay: { bankName: string; displayNr: string },
            directDebitAccountOwnerDisplayName: string
        ) => {
            this.m = {
                defaultBankAccountType: bankAccountType,
                accountNrDisplay: accountNrDisplay,
                directDebitAccountOwnerDisplayName: directDebitAccountOwnerDisplayName,
                form: form,
                isEditing: this.initialData.sharedIsEditing || new BehaviorSubject<boolean>(false),
                inEditMode: false,
                isEditAllowed: a.applicationInfo.IsActive && !this.initialData.forceReadonly,
                editFields: editFields,
                subscriptions: EditblockFormFieldModel.setupForm(getDictionaryValues(editFields), form),
                readonlyRow: applicationRow,
            };
        };

        let directDebitApplicantNr = parseInt(applicationRow.getUniqueItem('directDebitAccountOwnerApplicantNr'));
        let directDebitAccountOwnerDisplayName = this.getApplicantDisplayName(directDebitApplicantNr);

        let paidToCustomerBankAccountNr = applicationRow.getUniqueItem('paidToCustomerBankAccountNr');
        let paidToCustomerBankAccountNrType = applicationRow.getUniqueItem('paidToCustomerBankAccountNrType');
        if (paidToCustomerBankAccountNr && paidToCustomerBankAccountNrType) {
            this.apiService
                .validateBankAccountNr(paidToCustomerBankAccountNr, paidToCustomerBankAccountNrType, true)
                .then((x) => {
                    setup(
                        { bankName: x?.ValidAccount?.BankName, displayNr: x?.ValidAccount?.DisplayNr },
                        directDebitAccountOwnerDisplayName
                    );
                });
        } else {
            setup(null, directDebitAccountOwnerDisplayName);
        }

        form.form.asyncValidator = (x) => {
            let directDebitBankAccountNr = x?.get('directDebitBankAccountNr')?.value;
            let directDebitAccountOwnerApplicantNr = x?.get('directDebitAccountOwnerApplicantNr')?.value;
            if (
                this.validationService.isNullOrWhitespace(directDebitBankAccountNr) &&
                this.validationService.isNullOrWhitespace(directDebitAccountOwnerApplicantNr)
            ) {
                return of(null);
            }
            return new Observable<ValidationErrors>((resolver) => {
                if (
                    !this.validationService.isNullOrWhitespace(directDebitBankAccountNr) &&
                    !this.validationService.isNullOrWhitespace(directDebitAccountOwnerApplicantNr)
                ) {
                    resolver.next(null);
                } else {
                    let err: Dictionary<any> = {};
                    err['directDebitBankAccountNrAndAccountOwnerCombination'] = 'invalid';
                    resolver.next(err);
                }
                resolver.complete();
            });
        };
    }

    getAccountOwnersApplicantNrDropdown() {
        let applicantInfo = this.initialData.application.applicantInfoByApplicantNr;
        let arr: { Code: string; DisplayName: string }[] = [];

        for (let i = 1; i <= this.initialData.application.nrOfApplicants; i++) {
            if (!applicantInfo[i]) {
                this.toastr.warning('Could not fetch account owner details.');
                arr.push({ Code: i.toString(), DisplayName: `Applicant Nr ${i}` });
            }

            let applicantBirthDate = moment(applicantInfo[i].BirthDate).format('YYYYMMDD');
            arr.push({ Code: i.toString(), DisplayName: `${applicantInfo[i].FirstName}, ${applicantBirthDate}` });
        }

        return arr;
    }

    getApplicantDisplayName(applicantNr: number) {
        if (!applicantNr) return '';

        let applicantInfo = this.initialData.application.applicantInfoByApplicantNr;

        if (!applicantInfo[applicantNr]) {
            this.toastr.warning('Could not fetch applicant details.');
            return `Applicant Nr ${applicantNr}`;
        }

        let applicantBirthDate = moment(applicantInfo[applicantNr].BirthDate).format('YYYYMMDD');
        return `${applicantInfo[applicantNr].FirstName}, ${applicantBirthDate}`;
    }

    getRepaymentTimeDisplayText() {
        let value = this.m.readonlyRow.getUniqueItem('requestedRepaymentTime') ?? '';
        return value.length > 0 ? value.replace('d', ' days').replace('m', ' months') : '';
    }

    beginEdit(evt?: Event) {
        evt?.preventDefault();

        this.m.inEditMode = true;
        this.m.isEditing.next(true);

        //The async validator for bank accounts causes ExpressionChangedAfterItHasBeenCheckedError without this
        //Dont entirely understand why. Async validation is evil in general. Can we move bank account validation to ts?
        this.changeDetector.detectChanges();
    }

    async onCancel(evt?: Event) {
        evt?.preventDefault();

        this.m.isEditing.next(false);

        await this.reset();
    }

    onSave(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.initialData.application.applicationNr;
        let vs = this.validationService;
        let f = this.m.form;

        let requestedRepaymentTimeInPeriod = vs.parseIntegerOrNull(f.getValue('requestedRepaymentTimeInPeriod'));
        let isRequestedRepaymentTimePeriodDays = f.getValue('requestedRepaymentTimePeriod') === 'd';
        if(isRequestedRepaymentTimePeriodDays && requestedRepaymentTimeInPeriod !== null) {
            if(requestedRepaymentTimeInPeriod < 10) {
                this.toastr.warning('Requested repayment time in days must be >= 10');
                return;
            }
            if(requestedRepaymentTimeInPeriod > 30) {
                this.toastr.warning('Requested repayment time in days must be <= 30');
                return;
            }
        }

        let save = (paidToCustomerBankAccountNr: string, paidToCustomerBankAccountNrType: string) => {
            let request = {
                applicationNr: applicationNr,
                requestedLoanAmount: vs.parseIntegerOrNull(f.getValue('requestedLoanAmount')),
                requestedRepaymentTimeInMonths: isRequestedRepaymentTimePeriodDays ? null : requestedRepaymentTimeInPeriod,
                requestedRepaymentTimeInDays:  isRequestedRepaymentTimePeriodDays ? requestedRepaymentTimeInPeriod : null,
                paidToCustomerBankAccountNr: paidToCustomerBankAccountNr,
                paidToCustomerBankAccountNrType: paidToCustomerBankAccountNrType,
                directDebitBankAccountNr: f.getValue('directDebitBankAccountNr'),
                directDebitAccountOwnerApplicantNr: f.getValue('directDebitAccountOwnerApplicantNr'),
                loanObjective: f.getValue('loanObjective')
            };
            this.apiService.editApplication(request).then((x) => {
                this.eventService.signalReloadApplication(applicationNr);
            });
        };

        let paidToCustomerBankAccountNr = f.getValue('paidToCustomerBankAccountNr');
        if (paidToCustomerBankAccountNr) {
            this.apiService
                .validateBankAccountNr(paidToCustomerBankAccountNr, this.m.defaultBankAccountType, true)
                .then((x) => {
                    save(x.ValidAccount.NormalizedNr, x.ValidAccount.BankAccountNrType);
                });
        } else {
            save('', '');
        }
    }
}

class Model {
    defaultBankAccountType: string;
    accountNrDisplay: { bankName: string; displayNr: string };
    directDebitAccountOwnerDisplayName: string;
    editFields: Dictionary<EditblockFormFieldModel>;
    form: FormsHelper;
    inEditMode: boolean;
    isEditAllowed: boolean;
    isEditing: BehaviorSubject<boolean>;
    readonlyRow: ComplexApplicationListRow;
    subscriptions: Subscription[];
}

export class ApplicationDataEditorInitialData {
    application: StandardCreditApplicationModel;
    forceReadonly: boolean;
    sharedIsEditing?: BehaviorSubject<boolean>;
}
