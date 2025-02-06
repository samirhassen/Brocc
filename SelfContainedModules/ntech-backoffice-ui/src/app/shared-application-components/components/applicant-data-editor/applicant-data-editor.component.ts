import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { BehaviorSubject, Subscription } from 'rxjs';
import { ComplexApplicationListRow } from 'src/app/common-services/complex-application-list';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { Dictionary, getDictionaryValues } from 'src/app/common.types';
import { EditFormInitialData } from 'src/app/shared-application-components/components/edit-form/edit-form.component';
import { EditblockFormFieldModel } from 'src/app/shared-application-components/components/editblock-form-field/editblock-form-field.component';
import {
    ApplicantInfoModel,
    StandardApplicationModelBase,
} from 'src/app/shared-application-components/services/standard-application-base';
import { SharedApplicationApiService } from '../../services/shared-loan-application-api.service';

@Component({
    selector: 'applicant-data-editor',
    templateUrl: './applicant-data-editor.component.html',
    styles: [],
})
export class ApplicantDataEditorComponent implements OnInit {
    constructor(
        private fb: UntypedFormBuilder,
        private eventService: NtechEventService,
        private validationService: NTechValidationService
    ) {}

    ngOnInit(): void {}

    @Input()
    public initialData: ApplicantDataEditorInitialData;

    public m: Model;

    ngOnChanges(changes: SimpleChanges) {
        this.reset();
    }

    reset() {
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
        let applicantList = a.getComplexApplicationList('Applicant', true);
        let currentApplicantRow = applicantList.getRow(this.initialData.applicantNr, true);
        let originalEmploymentType = currentApplicantRow.getUniqueItem('employment');
        let isMainApplicant = this.initialData.applicantNr === 1;

        let form = new FormsHelper(this.fb.group({}));
        let editFields: Dictionary<EditblockFormFieldModel> = {};
        let yesNoBooleanDropdownOptions = [
            { Code: 'true', DisplayName: 'Yes' },
            { Code: 'false', DisplayName: 'No' },
        ];

        if (!isMainApplicant) {
            // Only show this field for coapplicant
            editFields['isPartOfTheHousehold'] = {
                getForm: () => form,
                formControlName: 'isPartOfTheHousehold',
                labelText: 'Is part of the household?',
                inEditMode: () => this.m.inEditMode.value,
                getOriginalValue: () => currentApplicantRow.getUniqueItem('isPartOfTheHousehold'),
                getValidators: () => [Validators.required],
                dropdownOptions: yesNoBooleanDropdownOptions,
            };
        }

        editFields['employment'] = {
            getForm: () => form,
            formControlName: 'employment',
            labelText: 'Employment',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => currentApplicantRow.getUniqueItem('employment'),
            getValidators: () => [],
            dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption(
                this.initialData.application.getEmploymentStatuses()
            ),
        };
        editFields['employer'] = {
            getForm: () => form,
            formControlName: 'employer',
            labelText: 'Employer',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () =>
                originalEmploymentType === form.getValue('employment')
                    ? currentApplicantRow.getUniqueItem('employer')
                    : '',
            getValidators: () => [],
            conditional: {
                shouldExist: () => StandardApplicationModelBase.isEmployerEmploymentCode(form.getValue('employment')),
            },
        };
        editFields['employerPhone'] = {
            getForm: () => form,
            formControlName: 'employerPhone',
            labelText: 'Employer phone',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () =>
                originalEmploymentType === form.getValue('employment')
                    ? currentApplicantRow.getUniqueItem('employerPhone')
                    : '',
            getValidators: () => [],
            conditional: {
                shouldExist: () => StandardApplicationModelBase.isEmployerEmploymentCode(form.getValue('employment')),
            },
        };
        editFields['employedSince'] = {
            getForm: () => form,
            formControlName: 'employedSince',
            labelText: 'Employed since',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () =>
                currentApplicantRow.getUniqueItem('employment') == form.getValue('employment')
                    ? currentApplicantRow.getUniqueItem('employedSince')
                    : '',
            getValidators: () => [this.validationService.getDateOnlyValidator()],
            conditional: {
                shouldExist: () =>
                    StandardApplicationModelBase.isEmployedSinceEmploymentCode(
                        form.getValue('employment'),
                        this.validationService
                    ),
            },
            placeholder: 'YYYY-MM-DD',
        };
        editFields['employedTo'] = {
            getForm: () => form,
            formControlName: 'employedTo',
            labelText: 'Employed to',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () =>
                currentApplicantRow.getUniqueItem('employment') == form.getValue('employment')
                    ? currentApplicantRow.getUniqueItem('employedTo')
                    : '',
            getValidators: () => [this.validationService.getDateOnlyValidator()],
            conditional: {
                shouldExist: () => StandardApplicationModelBase.isEmployedToEmploymentCode(form.getValue('employment')),
            },
            placeholder: 'YYYY-MM-DD',
        };
        editFields['claimsToBePep'] = {
            getForm: () => form,
            formControlName: 'claimsToBePep',
            labelText: 'Claims to be pep',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => currentApplicantRow.getUniqueItem('claimsToBePep'),
            getValidators: () => [],
            dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption(yesNoBooleanDropdownOptions),
        };
        editFields['marriage'] = {
            getForm: () => form,
            formControlName: 'marriage',
            labelText: 'Marital status',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => currentApplicantRow.getUniqueItem('marriage'),
            getValidators: () => [],
            dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption(
                this.initialData.application.getCivilStatuses()
            ),
        };
        editFields['incomePerMonthAmount'] = {
            getForm: () => form,
            formControlName: 'incomePerMonthAmount',
            labelText: 'Monthly income before tax',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => currentApplicantRow.getUniqueItem('incomePerMonthAmount'),
            getCustomDisplayValue: () =>
                this.validationService.formatIntegerForDisplay(form.getValue('incomePerMonthAmount')),
            getValidators: () => [this.validationService.getPositiveIntegerValidator()],
        };
        editFields['hasConsentedToShareBankAccountData'] = {
            getForm: () => form,
            formControlName: 'hasConsentedToShareBankAccountData',
            labelText: 'Has consented to share bank account data',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => currentApplicantRow.getUniqueItem('hasConsentedToShareBankAccountData'),
            getValidators: () => [],
            dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption(yesNoBooleanDropdownOptions),
        };
        editFields['hasConsentedToCreditReport'] = {
            getForm: () => form,
            formControlName: 'hasConsentedToCreditReport',
            labelText: 'Has consented to credit report',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => currentApplicantRow.getUniqueItem('hasConsentedToCreditReport'),
            getValidators: () => [],
            dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption(yesNoBooleanDropdownOptions),
        };
        editFields['hasLegalOrFinancialGuardian'] = {
            getForm: () => form,
            formControlName: 'hasLegalOrFinancialGuardian',
            labelText: 'Claims to have legal/financial guardian',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => currentApplicantRow.getUniqueItem('hasLegalOrFinancialGuardian'),
            getValidators: () => [],
            dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption(yesNoBooleanDropdownOptions),
        };
        editFields['claimsToBeGuarantor'] = {
            getForm: () => form,
            formControlName: 'claimsToBeGuarantor',
            labelText: 'Claims to be guarantor',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => currentApplicantRow.getUniqueItem('claimsToBeGuarantor'),
            getValidators: () => [],
            dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption(yesNoBooleanDropdownOptions),
        };
        editFields['claimsToHaveKfmDebt'] = {
            getForm: () => form,
            formControlName: 'claimsToHaveKfmDebt',
            labelText: 'Claims to have kfm debt',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => currentApplicantRow.getUniqueItem('claimsToHaveKfmDebt'),
            getValidators: () => [],
            dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption(yesNoBooleanDropdownOptions),
        };

        let inEditMode = new BehaviorSubject<boolean>(false);
        let isEditAllowed = a.applicationInfo.IsActive && !this.initialData.forceReadonly;

        this.m = {
            form: form,
            isMainApplicant: isMainApplicant,
            inEditMode: inEditMode,
            readonlyApplicant: currentApplicantRow,
            editFields: editFields,
            subscriptions: EditblockFormFieldModel.setupForm(getDictionaryValues(editFields), form),
            editFormInitialData: {
                onCancel: () => {
                    this.reset();
                },
                onSave: () => {
                    return this.onSave();
                },
                inEditMode: inEditMode,
                sharedIsEditing: this.initialData.sharedIsEditing,
                isEditAllowed: isEditAllowed,
                isInvalid: () => this.m.form.invalid(),
            },
        };
    }

    private onSave() {
        let applicant = this.m.readonlyApplicant;
        let f = this.m.form;
        let vs = this.validationService;

        let applicationNr = this.initialData.application.applicationNr;

        let employment = f.getValue('employment');

        return this.initialData.apiService
            .editApplicant({
                applicantNr: this.m.isMainApplicant ? 1 : 2,
                applicationNr: applicationNr,
                isPartOfTheHousehold: this.m.isMainApplicant
                    ? applicant.getUniqueItem('isPartOfTheHousehold')
                    : f.getValue('isPartOfTheHousehold'),
                employment: employment,
                employer: StandardApplicationModelBase.isEmployerEmploymentCode(employment)
                    ? f.getValue('employer')
                    : null,
                employerPhone: StandardApplicationModelBase.isEmployerEmploymentCode(employment)
                    ? f.getValue('employerPhone')
                    : null,
                employedSince: StandardApplicationModelBase.isEmployedSinceEmploymentCode(
                    employment,
                    this.validationService
                )
                    ? vs.parseDateOnlyOrNull(f.getValue('employedSince'))
                    : null,
                employedTo: StandardApplicationModelBase.isEmployedToEmploymentCode(employment)
                    ? vs.parseDateOnlyOrNull(f.getValue('employedTo'))
                    : null,
                claimsToBePep: f.getValue('claimsToBePep'),
                marriage: f.getValue('marriage'),
                incomePerMonthAmount: vs.parseIntegerOrNull(f.getValue('incomePerMonthAmount')),
                hasConsentedToShareBankAccountData: f.getValue('hasConsentedToShareBankAccountData'),
                hasConsentedToCreditReport: f.getValue('hasConsentedToCreditReport'),
                claimsToHaveKfmDebt: f.getValue('claimsToHaveKfmDebt'),
                hasLegalOrFinancialGuardian: f.getValue('hasLegalOrFinancialGuardian'),
                claimsToBeGuarantor: f.getValue('claimsToBeGuarantor')
            })
            .then(() => {
                this.eventService.signalReloadApplication(applicationNr);
                return { removeEditModeAfter: false };
            });
    }
}

class Model {
    editFields: Dictionary<EditblockFormFieldModel>;
    form: FormsHelper;
    inEditMode: BehaviorSubject<boolean>;
    isMainApplicant: boolean;
    readonlyApplicant: ComplexApplicationListRow;
    subscriptions: Subscription[];
    editFormInitialData: EditFormInitialData;
}

export class ApplicantDataEditorInitialData {
    application: StandardApplicationModelBase;
    applicantNr: number;
    applicantInfo: ApplicantInfoModel;
    forceReadonly: boolean;
    sharedIsEditing?: BehaviorSubject<boolean>;
    apiService: SharedApplicationApiService;
}
