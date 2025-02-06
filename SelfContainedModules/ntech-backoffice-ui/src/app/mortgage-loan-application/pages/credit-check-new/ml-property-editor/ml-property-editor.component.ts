import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, ValidatorFn } from '@angular/forms';
import { BehaviorSubject, Subscription } from 'rxjs';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { Dictionary, getDictionaryValues } from 'src/app/common.types';
import { MortgageLoanApplicationApiService } from 'src/app/mortgage-loan-application/services/mortgage-loan-application-api.service';
import {
    MortgageLoanApplicationLoanModel,
    StandardMortgageLoanApplicationModel,
} from 'src/app/mortgage-loan-application/services/mortgage-loan-application-model';
import { EditFormInitialData } from 'src/app/shared-application-components/components/edit-form/edit-form.component';
import { EditblockFormFieldModel } from 'src/app/shared-application-components/components/editblock-form-field/editblock-form-field.component';
import { SynchronizedCreditApplicationItemNames } from '../property-loans/property-loans.component';

@Component({
    selector: 'ml-property-editor',
    templateUrl: './ml-property-editor.component.html',
    styles: [],
})
export class MlPropertyEditorComponent implements OnInit {
    constructor(
        private fb: UntypedFormBuilder,
        private eventService: NtechEventService,
        private apiService: MortgageLoanApplicationApiService,
        private validationService: NTechValidationService
    ) {}

    @Input()
    public initialData: MlPropertyDataEditorInitialData;

    public m: Model;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.reset();
    }

    private reset() {
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

        let form = new FormsHelper(this.fb.group({}));
        let editFields: Dictionary<EditblockFormFieldModel> = {};

        let formatInteger = (fieldName: string) => {
            return this.validationService.formatIntegerForDisplay(form.getValue(fieldName));
        };

        editFields['objectTypeCode'] = {
            getForm: () => form,
            formControlName: 'objectTypeCode',
            labelText: 'Property type',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('objectTypeCode'),
            getValidators: () => [],
            dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption([
                { Code: 'seBrf', DisplayName: 'Apartment' },
                { Code: 'seFastighet', DisplayName: 'Other' },
            ]),
        };
        editFields['seBrfName'] = {
            getForm: () => form,
            formControlName: 'seBrfName',
            labelText: 'Housing cooperative name',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('seBrfName'),
            getValidators: () => [],
            conditional: {
                shouldExist: () => form.getValue('objectTypeCode') === 'seBrf',
            },
        };
        editFields['seBrfOrgNr'] = {
            getForm: () => form,
            formControlName: 'seBrfOrgNr',
            labelText: 'Housing cooperative orgnr',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('seBrfOrgNr'),
            getValidators: () => [this.validationService.getOrgNrValidator()],
            conditional: {
                shouldExist: () => form.getValue('objectTypeCode') === 'seBrf',
            },
        };
        editFields['seBrfApartmentNr'] = {
            getForm: () => form,
            formControlName: 'seBrfApartmentNr',
            labelText: 'Housing cooperative apartment nr',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('seBrfApartmentNr'),
            getValidators: () => [],
            conditional: {
                shouldExist: () => form.getValue('objectTypeCode') === 'seBrf',
            },
        };
        editFields['seTaxOfficeApartmentNr'] = {
            getForm: () => form,
            formControlName: 'seTaxOfficeApartmentNr',
            labelText: 'Tax office apartment number (LGH XXXX)',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('seTaxOfficeApartmentNr'),
            //NOTE: We validate as int so we dont have to think about everywhere else if
            //      the LGH prefix is present or not
            getValidators: () => [this.validationService.getPositiveIntegerValidator()],
            placeholder: 'XXXX',
            conditional: {
                shouldExist: () => form.getValue('objectTypeCode') === 'seBrf',
            },
        };
        editFields['objectLivingArea'] = {
            getForm: () => form,
            formControlName: 'objectLivingArea',
            labelText: 'Living area',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('objectLivingArea'),
            getCustomDisplayValue: () => formatInteger('objectLivingArea'),
            getValidators: () => [this.validationService.getPositiveIntegerValidator()],
        };
        editFields['objectMonthlyFeeAmount'] = {
            getForm: () => form,
            formControlName: 'objectMonthlyFeeAmount',
            labelText: 'Monthly fee',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('objectMonthlyFeeAmount'),
            getCustomDisplayValue: () => formatInteger('objectMonthlyFeeAmount'),
            getValidators: () => [this.validationService.getPositiveIntegerValidator()],
            conditional: {
                shouldExist: () => form.getValue('objectTypeCode') === 'seBrf',
            },
        };
        editFields['objectOtherMonthlyCostsAmount'] = {
            getForm: () => form,
            formControlName: 'objectOtherMonthlyCostsAmount',
            labelText: 'Other monthly costs',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('objectOtherMonthlyCostsAmount'),
            getCustomDisplayValue: () => formatInteger('objectOtherMonthlyCostsAmount'),
            getValidators: () => [this.validationService.getPositiveIntegerValidator()],
        };

        editFields['objectAddressStreet'] = {
            getForm: () => form,
            formControlName: 'objectAddressStreet',
            labelText: 'Street',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('objectAddressStreet'),
            getValidators: () => [],
        };
        editFields['objectAddressZipcode'] = {
            getForm: () => form,
            formControlName: 'objectAddressZipcode',
            labelText: 'Zip code',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('objectAddressZipcode'),
            getValidators: () => [],
        };
        editFields['objectAddressCity'] = {
            getForm: () => form,
            formControlName: 'objectAddressCity',
            labelText: 'City',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('objectAddressCity'),
            getValidators: () => [],
        };
        editFields['objectAddressMunicipality'] = {
            getForm: () => form,
            formControlName: 'objectAddressMunicipality',
            labelText: 'Municipality',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('objectAddressMunicipality'),
            getValidators: () => [],
        };

        if (applicationRow.getUniqueItemInteger('creditCollateralId')) {
            //Cant edit items connected to a collateral. These are edited in nCredit
            for (let f of getDictionaryValues(editFields)) {
                if (SynchronizedCreditApplicationItemNames.includes(f.formControlName)) {
                    f.isReadonly = () => true;
                }
            }
        }

        let inEditMode = new BehaviorSubject<boolean>(false);
        let isEditAllowed = a.applicationInfo.IsActive && !this.initialData.forceReadonly;
        let m: Model = {
            form: form,
            editFields: editFields,
            inEditMode: inEditMode,
            subscriptions: EditblockFormFieldModel.setupForm(getDictionaryValues(editFields), form),
            editFormInitialData: {
                onCancel: () => {},
                onSave: () => {
                    return this.onSave();
                },
                inEditMode: inEditMode,
                sharedIsEditing: this.initialData.sharedIsEditing,
                isEditAllowed: isEditAllowed,
                isInvalid: () => this.invalid(),
            },
            mortgageLoanNrs: [],
            isNewPurchase: applicationRow.getUniqueItem('isPurchase') === 'true',
        };

        let originalLoans = a.getMortgageLoans();
        for (let loan of originalLoans) {
            this.addMortgageLoanToModel(m, loan);
        }
        this.m = m;
    }

    private onSave() {
        let applicationNr = this.initialData.application.applicationNr;
        let f = this.m.form;
        let objectTypeCode = f.getValue('objectTypeCode');

        let isSeBrf = objectTypeCode === 'seBrf';

        let loans: MortgageLoanApplicationLoanModel[] = [];
        for (let nr of this.m.mortgageLoanNrs) {
            loans.push({
                bankName: f.getValue(`bankName${nr}`),
                currentDebtAmount: this.validationService.parseIntegerOrNull(f.getValue(`currentDebtAmount${nr}`)),
                currentMonthlyAmortizationAmount: this.validationService.parseIntegerOrNull(
                    f.getValue(`currentMonthlyAmortizationAmount${nr}`)
                ),
                interestRatePercent: this.validationService.parseDecimalOrNull(
                    f.getValue(`interestRatePercent${nr}`),
                    false
                ),
                shouldBeSettled:
                    f.getValue(`shouldBeSettled${nr}`) === 'true'
                        ? true
                        : f.getValue(`shouldBeSettled${nr}`) === 'false'
                        ? false
                        : null,
                loanNumber: f.getValue(`loanNumber${nr}`),
            });
        }

        let request = {
            applicationNr: applicationNr,
            objectTypeCode: objectTypeCode,
            seBrfName: isSeBrf ? f.getValue('seBrfName') : null,
            seBrfOrgNr: isSeBrf ? f.getValue('seBrfOrgNr') : null,
            seBrfApartmentNr: isSeBrf ? f.getValue('seBrfApartmentNr') : null,
            seTaxOfficeApartmentNr: isSeBrf ? f.getValue('seTaxOfficeApartmentNr') : null,
            objectLivingArea: f.getValue('objectLivingArea'),
            objectMonthlyFeeAmount: isSeBrf ? f.getValue('objectMonthlyFeeAmount') : null,
            objectOtherMonthlyCostsAmount: f.getValue('objectOtherMonthlyCostsAmount'),
            objectAddressStreet: f.getValue('objectAddressStreet'),
            objectAddressZipcode: f.getValue('objectAddressZipcode'),
            objectAddressCity: f.getValue('objectAddressCity'),
            objectAddressMunicipality: f.getValue('objectAddressMunicipality'),
            mortgageLoansToSettle: loans,
        };

        return this.apiService.editProperty(request).then((x) => {
            this.eventService.signalReloadApplication(applicationNr);
            return { removeEditModeAfter: false };
        });
    }

    public invalid() {
        return !this.m || this.m.form.invalid();
    }

    addMortgageLoan(evt?: Event) {
        this.addMortgageLoanToModel(this.m, null);
    }

    removeMortgageLoan(nr: number, evt?: Event) {
        let indexToRemove = this.m.mortgageLoanNrs.indexOf(nr);
        if (indexToRemove < 0) {
            return;
        }
        this.m.mortgageLoanNrs.splice(indexToRemove, 1);
        let remove = (name: string) => {
            this.m.editFields[`${name}${nr}`] = null;
            this.m.form.form.removeControl(`${name}${nr}`);
        };
        for (let fieldName of Object.keys(this.mortgageLoanFieldNames)) {
            remove(fieldName);
        }
    }

    private mortgageLoanFieldNames: Dictionary<string> = {};
    private addMortgageLoanToModel(m: Model, initialValue: MortgageLoanApplicationLoanModel) {
        let nextNr = initialValue?.complexListRowNr || Math.max(0, ...m.mortgageLoanNrs) + 1;
        let name = (s: string) => `${s}${nextNr}`;

        let addField = (
            fieldName: string,
            initialValue: string,
            validators: ValidatorFn[],
            createField: (
                localFieldName: string,
                localInitialValue: string,
                localValidators: ValidatorFn[]
            ) => EditblockFormFieldModel
        ) => {
            let localFieldName = name(fieldName);
            let field = createField(localFieldName, initialValue, validators);
            m.editFields[localFieldName] = field;
            this.mortgageLoanFieldNames[fieldName] = ''; //Value doesnt matter. Used as a set<string> basically
            m.form.addControlIfNotExists(localFieldName, initialValue, validators);
        };

        let formatInteger = (value: string) => {
            return this.validationService.formatIntegerForDisplay(value);
        };

        addField('bankName', initialValue?.bankName, [], (localFieldName, localInitialValue, localValidators) => {
            return {
                getForm: () => m.form,
                formControlName: localFieldName,
                labelText: 'Bank',
                inEditMode: () => m.inEditMode.value,
                getOriginalValue: () => localInitialValue,
                getValidators: () => localValidators,
            };
        });

        addField(
            'currentDebtAmount',
            this.validationService.formatIntegerForEdit(initialValue?.currentDebtAmount),
            [this.validationService.getPositiveIntegerValidator()],
            (localFieldName, localInitialValue, localValidators) => {
                return {
                    getForm: () => m.form,
                    formControlName: localFieldName,
                    labelText: 'Current debt',
                    inEditMode: () => m.inEditMode.value,
                    getOriginalValue: () => localInitialValue,
                    getCustomDisplayValue: () => formatInteger(localInitialValue),
                    getValidators: () => localValidators,
                };
            }
        );

        addField(
            'currentMonthlyAmortizationAmount',
            this.validationService.formatIntegerForEdit(initialValue?.currentMonthlyAmortizationAmount),
            [this.validationService.getPositiveIntegerValidator()],
            (localFieldName, localInitialValue, localValidators) => {
                return {
                    getForm: () => m.form,
                    formControlName: localFieldName,
                    labelText: 'Current amortization',
                    inEditMode: () => m.inEditMode.value,
                    getOriginalValue: () => localInitialValue,
                    getCustomDisplayValue: () => formatInteger(localInitialValue),
                    getValidators: () => localValidators,
                };
            }
        );

        addField(
            'interestRatePercent',
            this.validationService.formatPositiveDecimalForEdit(initialValue?.interestRatePercent),
            [this.validationService.getPositiveDecimalValidator()],
            (localFieldName, localInitialValue, localValidators) => {
                return {
                    getForm: () => m.form,
                    formControlName: localFieldName,
                    labelText: 'Current interest',
                    inEditMode: () => m.inEditMode.value,
                    getOriginalValue: () => localInitialValue,
                    getValidators: () => localValidators,
                };
            }
        );

        addField(
            'shouldBeSettled',
            initialValue?.shouldBeSettled === true ? 'true' : initialValue?.shouldBeSettled === false ? 'false' : null,
            [],
            (localFieldName, localInitialValue, localValidators) => {
                return {
                    getForm: () => m.form,
                    formControlName: localFieldName,
                    labelText: 'Should be settled',
                    inEditMode: () => m.inEditMode.value,
                    getOriginalValue: () => localInitialValue,
                    getValidators: () => localValidators,
                    dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption([
                        { Code: 'true', DisplayName: 'Yes' },
                        { Code: 'false', DisplayName: 'No' },
                    ]),
                };
            }
        );

        addField('loanNumber', initialValue?.loanNumber, [], (localFieldName, localInitialValue, localValidators) => {
            return {
                getForm: () => m.form,
                formControlName: localFieldName,
                labelText: 'Loan number',
                inEditMode: () => m.inEditMode.value,
                getOriginalValue: () => localInitialValue,
                getValidators: () => localValidators,
            };
        });

        m.mortgageLoanNrs.push(nextNr);
        return nextNr;
    }
}

class Model {
    form: FormsHelper;
    editFields: Dictionary<EditblockFormFieldModel>;
    inEditMode: BehaviorSubject<boolean>;
    editFormInitialData: EditFormInitialData;
    subscriptions: Subscription[];
    mortgageLoanNrs: number[];
    isNewPurchase: boolean;
}

export class MlPropertyDataEditorInitialData {
    application: StandardMortgageLoanApplicationModel;
    forceReadonly: boolean;
    sharedIsEditing?: BehaviorSubject<boolean>;
}
