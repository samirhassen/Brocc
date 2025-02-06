import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { BehaviorSubject, Subscription } from 'rxjs';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { Dictionary, getDictionaryValues } from 'src/app/common.types';
import {
    MortgageApplicationEditGeneralData,
    MortgageLoanApplicationApiService,
} from 'src/app/mortgage-loan-application/services/mortgage-loan-application-api.service';
import { StandardMortgageLoanApplicationModel } from 'src/app/mortgage-loan-application/services/mortgage-loan-application-model';
import { EditFormInitialData } from 'src/app/shared-application-components/components/edit-form/edit-form.component';
import { EditblockFormFieldModel } from 'src/app/shared-application-components/components/editblock-form-field/editblock-form-field.component';

@Component({
    selector: 'ml-application-general-data-editor',
    templateUrl: './ml-application-general-data-editor.component.html',
    styles: [],
})
export class MlApplicationGeneralDataEditorComponent implements OnInit {
    constructor(
        private fb: UntypedFormBuilder,
        private apiService: MortgageLoanApplicationApiService,
        private eventService: NtechEventService,
        private validationService: NTechValidationService,
        private toastr: ToastrService
    ) {}

    public model: Model;

    @Input()
    public initialData: MlAppGeneralDataEditorInitialData;

    ngOnInit() {}

    ngOnChanges(changes: SimpleChanges) {
        this.reset();
    }

    private reset() {
        this.model = null;

        let form = new FormsHelper(this.fb.group({}));
        let inEditMode = new BehaviorSubject<boolean>(false);
        let editFields = this.setupEditFields(form);
        let isEditAllowed = this.initialData.application.applicationInfo.IsActive && !this.initialData.forceReadonly;

        this.model = {
            form: form,
            editFields: editFields,
            inEditMode: inEditMode,
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
                isInvalid: () => this.invalid(),
            },
            totalRequestedAmount: parseInt(this.getTotalRequestedLoanAmount(form)),
        };

        form.form.valueChanges.subscribe((_) => {
            this.model.totalRequestedAmount = parseInt(this.getTotalRequestedLoanAmount(form));
        });
    }

    private setupEditFields(form: FormsHelper): Dictionary<EditblockFormFieldModel> {
        let editFields: Dictionary<EditblockFormFieldModel> = {};
        let yesNoDropdownValues = [
            { Code: 'true', DisplayName: 'Yes' },
            { Code: 'false', DisplayName: 'No' },
        ];

        let app = this.initialData.application;
        let applicationRow = app.getComplexApplicationList('Application', true).getRow(1, true);

        let formatForDisplay = (fieldName: string) => {
            return this.validationService.formatIntegerForDisplay(form.getValue(fieldName));
        };

        editFields['isPurchase'] = {
            getForm: () => form,
            formControlName: 'isPurchase',
            labelText: 'New purchase?',
            inEditMode: () => this.model.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('isPurchase'),
            getValidators: () => [],
            dropdownOptions: yesNoDropdownValues,
        };

        editFields['settlementAmount'] = {
            getForm: () => form,
            formControlName: 'settlementAmount',
            labelText: 'Settlement amount',
            inEditMode: () => this.model.inEditMode.value,
            isReadonly: () => true,
            getOriginalValue: () => new Number(this.getSettlementAmount(form)).toString(),
            getCustomDisplayValue: () => formatForDisplay('settlementAmount'),
            getValidators: () => [],
            conditional: {
                shouldExist: () => form.getValue('isPurchase') === 'false',
            },
        };

        editFields['isAdditionalLoanRequested'] = {
            getForm: () => form,
            formControlName: 'isAdditionalLoanRequested',
            labelText: 'Additional loan requested',
            inEditMode: () => this.model.inEditMode.value,
            getOriginalValue: () =>
                applicationRow.getUniqueItem('paidToCustomerAmount') !== undefined ? 'true' : 'false',
            getValidators: () => [],
            dropdownOptions: yesNoDropdownValues,
            conditional: {
                shouldExist: () => form.getValue('isPurchase') === 'false',
            },
        };

        editFields['additionalLoanAmount'] = {
            getForm: () => form,
            formControlName: 'additionalLoanAmount',
            labelText: 'Additional loan amount',
            inEditMode: () => this.model.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('paidToCustomerAmount'),
            getCustomDisplayValue: () => formatForDisplay('additionalLoanAmount'),
            getValidators: () => [this.validationService.getPositiveIntegerValidator()],
            conditional: {
                shouldExist: () => form.getValue('isAdditionalLoanRequested') === 'true',
            },
        };

        editFields['ownSavingsAmount'] = {
            getForm: () => form,
            formControlName: 'ownSavingsAmount',
            labelText: 'Cash payment',
            inEditMode: () => this.model.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('ownSavingsAmount'),
            getCustomDisplayValue: () => formatForDisplay('ownSavingsAmount'),
            getValidators: () => [this.validationService.getPositiveIntegerValidator()],
            conditional: {
                shouldExist: () => form.getValue('isPurchase') === 'true',
            },
        };

        editFields['objectPriceAmount'] = {
            getForm: () => form,
            formControlName: 'objectPriceAmount',
            labelText: 'Purchase price',
            inEditMode: () => this.model.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('objectPriceAmount'),
            getCustomDisplayValue: () => formatForDisplay('objectPriceAmount'),
            getValidators: () => [this.validationService.getPositiveIntegerValidator()],
            conditional: {
                shouldExist: () => form.getValue('isPurchase') === 'true',
            },
        };

        editFields['objectValueAmount'] = {
            getForm: () => form,
            formControlName: 'objectValueAmount',
            labelText: 'Valuation',
            inEditMode: () => this.model.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('objectValueAmount'),
            getCustomDisplayValue: () => formatForDisplay('objectValueAmount'),
            getValidators: () => [this.validationService.getPositiveIntegerValidator()],
        };

        return editFields;
    }

    private getSettlementAmount(form: FormsHelper) {
        let app = this.initialData.application;
        let isPurchase = form.getValue('isPurchase') === 'true';

        if (isPurchase) return 0;

        let mortgageLoans = app.getMortgageLoans();

        return mortgageLoans.reduce((acc, loan) => {
            return acc + (loan.shouldBeSettled ? loan.currentDebtAmount : 0);
        }, 0);
    }

    private getTotalRequestedLoanAmount(form: FormsHelper) {
        let objectPriceAmount = form.getValue('objectPriceAmount') ?? 0;
        let ownSavingsAmount = form.getValue('ownSavingsAmount') ?? 0;
        let additionalLoanAmount = form.getValue('additionalLoanAmount') ?? 0;
        let sumMortgageLoans = this.getSettlementAmount(form);

        return new Number(
            +objectPriceAmount + +additionalLoanAmount + +sumMortgageLoans - +ownSavingsAmount
        ).toString();
    }

    private onSave() {
        let form = this.model?.form;
        let isPurchase = form.getValue('isPurchase') === 'true';
        let isAdditionalLoanRequested = form.getValue('isAdditionalLoanRequested') === 'true';

        if (isPurchase && this.initialData.application.hasAnyConnectedMortgageLoans()) {
            this.toastr.warning('You must remove all mortgage loans connected to the property to change purchase type');
            return new Promise((resolve) => resolve({ removeEditModeAfter: false }));
        }

        let request = {
            applicationNr: this.initialData.application.applicationNr,
            isPurchase: isPurchase,
            objectPriceAmount: isPurchase ? parseInt(form.getValue('objectPriceAmount')) : null,
            ownSavingsAmount: isPurchase ? parseInt(form.getValue('ownSavingsAmount')) : null,
            additionalLoanAmount: isAdditionalLoanRequested ? parseInt(form.getValue('additionalLoanAmount')) : null,
            objectValueAmount: parseInt(form.getValue('objectValueAmount')),
        } as MortgageApplicationEditGeneralData;

        return this.apiService.editGeneralData(request).then((x) => {
            this.eventService.signalReloadApplication(this.initialData.application.applicationNr);
            return { removeEditModeAfter: false };
        });
    }

    public invalid() {
        return !this.model || this.model.form.invalid();
    }
}

class Model {
    form: FormsHelper;
    editFields: Dictionary<EditblockFormFieldModel>;
    inEditMode: BehaviorSubject<boolean>;
    editFormInitialData: EditFormInitialData;
    subscriptions: Subscription[];
    totalRequestedAmount: number;
}

export class MlAppGeneralDataEditorInitialData {
    application: StandardMortgageLoanApplicationModel;
    forceReadonly: boolean;
    sharedIsEditing?: BehaviorSubject<boolean>;
}
