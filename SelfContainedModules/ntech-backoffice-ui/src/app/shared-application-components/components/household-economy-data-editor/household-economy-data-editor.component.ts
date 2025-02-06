import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, ValidationErrors } from '@angular/forms';
import { BehaviorSubject, Observable, of, Subscription } from 'rxjs';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { Dictionary, getBankAccountTypeDropdownOptions, getDictionaryValues } from 'src/app/common.types';
import { EditblockFormFieldModel } from 'src/app/shared-application-components/components/editblock-form-field/editblock-form-field.component';
import {
    BankAccountNrValidationRequest,
    SharedApplicationApiService,
} from 'src/app/shared-application-components/services/shared-loan-application-api.service';
import {
    StandardApplicationModelBase,
    StandardLoanApplicationChildModel,
    StandardLoanApplicationOtherLoanModel,
} from '../../services/standard-application-base';
import { EditFormInitialData } from '../edit-form/edit-form.component';

@Component({
    selector: 'household-economy-data-editor',
    templateUrl: './household-economy-data-editor.component.html',
    styles: [],
})
export class HouseholdEconomyDataEditorComponent implements OnInit {
    constructor(
        private fb: UntypedFormBuilder,
        private eventService: NtechEventService,
        private validationService: NTechValidationService,
        private config: ConfigService
    ) {}

    ngOnInit(): void {}

    @Input()
    public initialData: HouseholdEconomyDataEditorInitialData;

    public m: Model;

    ngOnChanges(changes: SimpleChanges) {
        this.reset();
    }

    public isUl() {
        return this.config.isFeatureEnabled('ntech.feature.unsecuredloans.standard');
    }

    private isMl() {
        return this.config.isFeatureEnabled('ntech.feature.mortgageloans.standard');
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
        let applicationRow = a.getComplexApplicationList('Application', true).getRow(1, true);

        let householdInfoForm = new FormsHelper(this.fb.group({}));
        let editFields: Dictionary<EditblockFormFieldModel> = {};

        editFields['outgoingChildSupportAmount'] = {
            getForm: () => householdInfoForm,
            formControlName: 'outgoingChildSupportAmount',
            labelText: 'Pays in child support',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('outgoingChildSupportAmount'),
            getCustomDisplayValue: () =>
                this.validationService.formatIntegerForDisplay(
                    householdInfoForm.getValue('outgoingChildSupportAmount')
                ),
            getValidators: () => [this.validationService.getPositiveIntegerValidator()],
        };
        editFields['incomingChildSupportAmount'] = {
            getForm: () => householdInfoForm,
            formControlName: 'incomingChildSupportAmount',
            labelText: 'Receives in child support',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('incomingChildSupportAmount'),
            getCustomDisplayValue: () =>
                this.validationService.formatIntegerForDisplay(
                    householdInfoForm.getValue('incomingChildSupportAmount')
                ),
            getValidators: () => [this.validationService.getPositiveIntegerValidator()],
        };
        editFields['childBenefitAmount'] = {
            getForm: () => householdInfoForm,
            formControlName: 'childBenefitAmount',
            labelText: 'Child benefit amount',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('childBenefitAmount'),
            getCustomDisplayValue: () =>
                this.validationService.formatIntegerForDisplay(householdInfoForm.getValue('childBenefitAmount')),
            getValidators: () => [this.validationService.getPositiveIntegerValidator()],
        };
        if (!this.isMl()) {
            editFields['housing'] = {
                getForm: () => householdInfoForm,
                formControlName: 'housing',
                labelText: 'Housing type',
                inEditMode: () => this.m.inEditMode.value,
                getOriginalValue: () => applicationRow.getUniqueItem('housing'),
                getValidators: () => [],
                dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption(
                    this.initialData.application.getHousingTypes()
                ),
            };
            editFields['housingCostPerMonthAmount'] = {
                getForm: () => householdInfoForm,
                formControlName: 'housingCostPerMonthAmount',
                labelText: 'Housing cost',
                inEditMode: () => this.m.inEditMode.value,
                getOriginalValue: () => applicationRow.getUniqueItem('housingCostPerMonthAmount'),
                getCustomDisplayValue: () =>
                    this.validationService.formatIntegerForDisplay(
                        householdInfoForm.getValue('housingCostPerMonthAmount')
                    ),
                getValidators: () => [this.validationService.getPositiveIntegerValidator()],
            };
            editFields['otherHouseholdFixedCostsAmount'] = {
                getForm: () => householdInfoForm,
                formControlName: 'otherHouseholdFixedCostsAmount',
                labelText: 'Other household fixed costs',
                inEditMode: () => this.m.inEditMode.value,
                getOriginalValue: () => applicationRow.getUniqueItem('otherHouseholdFixedCostsAmount'),
                getCustomDisplayValue: () =>
                    this.validationService.formatIntegerForDisplay(
                        householdInfoForm.getValue('otherHouseholdFixedCostsAmount')
                    ),
                getValidators: () => [this.validationService.getPositiveIntegerValidator()],
            };
        }
        editFields['otherHouseholdFinancialAssetsAmount'] = {
            getForm: () => householdInfoForm,
            formControlName: 'otherHouseholdFinancialAssetsAmount',
            labelText: 'Other household financial assets',
            inEditMode: () => this.m.inEditMode.value,
            getOriginalValue: () => applicationRow.getUniqueItem('otherHouseholdFinancialAssetsAmount'),
            getCustomDisplayValue: () =>
                this.validationService.formatIntegerForDisplay(
                    householdInfoForm.getValue('otherHouseholdFinancialAssetsAmount')
                ),
            getValidators: () => [this.validationService.getPositiveIntegerValidator()],
        };

        let childrenForm = new FormsHelper(this.fb.group({}));
        let originalChildren = a.getHouseholdChildren();
        let childrenFormGroupNames: string[] = [];
        for (var childIndex = 0; childIndex < originalChildren.length; childIndex++) {
            childrenFormGroupNames.push(this.addChildFormGroup(childrenForm, childIndex, originalChildren[childIndex]));
        }

        let otherLoansForm = new FormsHelper(this.fb.group({}));
        let originalLoans = a.getOtherLoans();
        let loansGroupNames: string[] = [];
        for (var loanIndex = 0; loanIndex < originalLoans.length; loanIndex++) {
            loansGroupNames.push(this.addOtherLoanGroup(otherLoansForm, loanIndex, originalLoans[loanIndex]));
        }

        let inEditMode = new BehaviorSubject<boolean>(false);
        let isEditAllowed = a.applicationInfo.IsActive && !this.initialData.forceReadonly;

        this.augmentOtherLoansWithBankAccountDisplayData(originalLoans).then((x) => {
            this.m = {
                householdInfo: {
                    form: householdInfoForm,
                    editFields: editFields,
                },
                children: {
                    form: childrenForm,
                    original: originalChildren,
                    groupNames: childrenFormGroupNames,
                },
                otherLoans: {
                    form: otherLoansForm,
                    view: x,
                    groupNames: loansGroupNames,
                    loanTypes: EditblockFormFieldModel.includeEmptyDropdownOption(a.getOtherLoanTypes()),
                    bankAccountTypes: this.getPossibleBankAccountTypes(),
                },
                subscriptions: EditblockFormFieldModel.setupForm(getDictionaryValues(editFields), householdInfoForm),
                inEditMode: inEditMode,
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
            };
        });
    }

    private addChildFormGroup(
        form: FormsHelper,
        index: number,
        originalValue?: StandardLoanApplicationChildModel
    ): string {
        let formGroupName = (index + 1).toString();
        form.addGroupIfNotExists(formGroupName, [
            {
                controlName: 'ageInYears',
                initialValue: originalValue?.ageInYears?.toString(),
                validators: [this.validationService.getPositiveIntegerWithBoundsValidator(0, 200)],
            },
            {
                controlName: 'sharedCustody',
                initialValue:
                    originalValue?.sharedCustody === true
                        ? 'true'
                        : originalValue?.sharedCustody === false
                        ? 'false'
                        : '',
                validators: [],
            },
        ]);
        return formGroupName;
    }

    private addOtherLoanGroup(
        form: FormsHelper,
        index: number,
        originalValue?: StandardLoanApplicationOtherLoanModel
    ): string {
        let formGroupName = (index + 1).toString();

        let newGroup = form.addGroupIfNotExists(formGroupName, [
            {
                controlName: 'loanType',
                initialValue: originalValue?.loanType,
                validators: [],
            },
            {
                controlName: 'currentDebtAmount',
                initialValue: originalValue?.currentDebtAmount?.toString(),
                validators: [this.validationService.getPositiveIntegerValidator()],
            },
            {
                controlName: 'monthlyCostAmount',
                initialValue: originalValue?.monthlyCostAmount?.toString(),
                validators: [this.validationService.getPositiveIntegerValidator()],
            },
            {
                controlName: 'currentInterestRatePercent',
                initialValue: originalValue?.currentInterestRatePercent?.toString(),
                validators: [this.validationService.getPositiveDecimalValidator()],
            },
            this.isUl()
                ? {
                      controlName: 'shouldBeSettled',
                      initialValue:
                          originalValue?.shouldBeSettled === true
                              ? 'true'
                              : originalValue?.shouldBeSettled === false
                              ? 'false'
                              : '',
                      validators: [],
                  }
                : null,
        ]);

        let synchSettlementFields = () => {
            let shouldBeSettledControl = newGroup.get('shouldBeSettled');
            if (!shouldBeSettledControl) {
                return;
            }
            let shouldBeSettled = shouldBeSettledControl?.value === 'true';
            let currentBankAccountNrType = newGroup.contains('bankAccountNrType')
                ? newGroup.get('bankAccountNrType').value
                : originalValue?.bankAccountNrType;
            let isRegularBankAccount = this.validationService.isRegularBankAccount(currentBankAccountNrType);

            form.ensureGroupedControlOnConditionOnly(
                shouldBeSettled,
                formGroupName,
                'bankAccountNrType',
                () => originalValue?.bankAccountNrType,
                () => []
            );

            form.ensureGroupedControlOnConditionOnly(
                shouldBeSettled,
                formGroupName,
                'bankAccountNr',
                () => originalValue?.bankAccountNr,
                () => []
            );

            form.ensureGroupedControlOnConditionOnly(
                shouldBeSettled && !isRegularBankAccount,
                formGroupName,
                'settlementPaymentReference',
                () => originalValue?.settlementPaymentReference,
                () => []
            );

            form.ensureGroupedControlOnConditionOnly(
                shouldBeSettled && !isRegularBankAccount,
                formGroupName,
                'settlementPaymentMessage',
                () => originalValue?.settlementPaymentMessage,
                () => []
            );
        };

        synchSettlementFields();

        newGroup.valueChanges.subscribe((_) => {
            synchSettlementFields();
        });

        newGroup.setAsyncValidators([
            (x) => {
                let bankAccountNrTypeControl = x.get('bankAccountNrType');
                if (!bankAccountNrTypeControl) {
                    return of(null);
                }
                let bankAccountNrType = bankAccountNrTypeControl?.value;
                let bankAccountNr = x.get('bankAccountNr')?.value;
                if (this.validationService.isNullOrWhitespace(bankAccountNr)) {
                    return of(null);
                }
                return new Observable<ValidationErrors>((resolver) => {
                    this.initialData.apiService
                        .validateBankAccountNr(bankAccountNr, bankAccountNrType, true)
                        .then((x) => {
                            if (x.IsValid) {
                                resolver.next(null);
                            } else {
                                let err: Dictionary<any> = {};
                                err['bankAccountNrAndTypeCombination'] = 'invalid';
                                resolver.next(err);
                            }
                            resolver.complete();
                        });
                });
            },
            (x) => {
                let bankAccountNrTypeControl = x.get('bankAccountNrType');
                if (!bankAccountNrTypeControl) {
                    return of(null);
                }
                let errors: Dictionary<any> = {};
                let bankAccountNrType = x.get('bankAccountNrType')?.value;
                if (this.validationService.isRegularBankAccount(bankAccountNrType)) {
                    return of(null);
                }
                let settlementPaymentReference = x.get('settlementPaymentReference')?.value;
                if (
                    settlementPaymentReference &&
                    !this.validationService.perCountry.isValidPaymentFileReferenceNr(
                        bankAccountNrType,
                        settlementPaymentReference
                    )
                ) {
                    errors['invalidPaymentReference'] = 'invalid';
                }
                return of(errors);
            },
        ]);

        return formGroupName;
    }

    private onSave() {
        let applicationNr = this.initialData.application.applicationNr;
        let vs = this.validationService;
        let householdInfoForm = this.m.householdInfo.form;
        let childrenForm = this.m.children.form;
        let loansForm = this.m.otherLoans.form;

        let request = {
            applicationNr: applicationNr,
            housing: householdInfoForm.getValue('housing'),
            housingCostPerMonthAmount: vs.parseIntegerOrNull(householdInfoForm.getValue('housingCostPerMonthAmount')),
            otherHouseholdFixedCostsAmount: vs.parseIntegerOrNull(
                householdInfoForm.getValue('otherHouseholdFixedCostsAmount')
            ),
            otherHouseholdFinancialAssetsAmount: vs.parseIntegerOrNull(
                householdInfoForm.getValue('otherHouseholdFinancialAssetsAmount')
            ),
            outgoingChildSupportAmount: vs.parseIntegerOrNull(householdInfoForm.getValue('outgoingChildSupportAmount')),
            incomingChildSupportAmount: vs.parseIntegerOrNull(householdInfoForm.getValue('incomingChildSupportAmount')),
            childBenefitAmount: vs.parseIntegerOrNull(householdInfoForm.getValue('childBenefitAmount')),
            children: this.m.children.groupNames.map((childGroupName) => {
                let sharedCustody = childrenForm.getFormGroupValue(childGroupName, 'sharedCustody');
                return {
                    ageInYears: vs.parseIntegerOrNull(childrenForm.getFormGroupValue(childGroupName, 'ageInYears')),
                    sharedCustody: sharedCustody === 'true' ? true : sharedCustody === 'false' ? false : null,
                };
            }),
            otherLoans: this.m.otherLoans.groupNames.map((otherLoansGroupName) => {
                let shouldBeSettled = this.isUl()
                    ? loansForm.getFormGroupValue(otherLoansGroupName, 'shouldBeSettled')
                    : null;
                return {
                    loanType: loansForm.getFormGroupValue(otherLoansGroupName, 'loanType'),
                    currentDebtAmount: vs.parseIntegerOrNull(
                        loansForm.getFormGroupValue(otherLoansGroupName, 'currentDebtAmount')
                    ),
                    currentInterestRatePercent: vs.parseDecimalOrNull(
                        loansForm.getFormGroupValue(otherLoansGroupName, 'currentInterestRatePercent'),
                        false
                    ),
                    monthlyCostAmount: vs.parseIntegerOrNull(
                        loansForm.getFormGroupValue(otherLoansGroupName, 'monthlyCostAmount')
                    ),
                    shouldBeSettled: shouldBeSettled === 'true' ? true : shouldBeSettled === 'false' ? false : null,
                    bankAccountNrType: loansForm.getFormGroupValue(otherLoansGroupName, 'bankAccountNrType'),
                    bankAccountNr: loansForm.getFormGroupValue(otherLoansGroupName, 'bankAccountNr'),
                    settlementPaymentReference: loansForm.getFormGroupValue(
                        otherLoansGroupName,
                        'settlementPaymentReference'
                    ),
                    settlementPaymentMessage: loansForm.getFormGroupValue(
                        otherLoansGroupName,
                        'settlementPaymentMessage'
                    ),
                };
            }),
        };
        return this.initialData.apiService.editHouseholdEconomy(request).then((x) => {
            this.eventService.signalReloadApplication(applicationNr);
            return { removeEditModeAfter: false };
        });
    }

    invalid() {
        if (!this.m) {
            return true;
        }
        return (
            this.m.householdInfo.form.invalid() || this.m.children.form.invalid() || this.m.otherLoans.form.invalid()
        );
    }

    removeChild(evt?: Event) {
        evt?.preventDefault();

        let c = this.m.children;
        let indexToRemove = c.groupNames.length - 1;
        let groupNameToRemove = c.groupNames[indexToRemove];
        c.groupNames.splice(indexToRemove, 1);
        this.m.children.form.form.removeControl(groupNameToRemove);
    }

    addChild(evt?: Event) {
        evt?.preventDefault();

        let currentCount = this.m.children.groupNames.length;
        this.m.children.groupNames.push(this.addChildFormGroup(this.m.children.form, currentCount, null));
    }

    addOtherLoan(evt?: Event) {
        evt?.preventDefault();

        this.m.otherLoans.groupNames.push(
            this.addOtherLoanGroup(this.m.otherLoans.form, this.m.otherLoans.groupNames.length, {
                loanType: 'unknown',
            })
        );
    }

    private getPossibleBankAccountTypes() {
        return getBankAccountTypeDropdownOptions(this.config.getClient().BaseCountry, true, 'en');
    }

    removeOtherLoan(groupName: string, evt?: Event) {
        evt?.preventDefault();

        let lm = this.m.otherLoans;
        let indexToRemove = lm.groupNames.indexOf(groupName);
        if (indexToRemove < 0) {
            return;
        }
        let groupNameToRemove = lm.groupNames[indexToRemove];
        lm.groupNames.splice(indexToRemove, 1);
        this.m.otherLoans.form.form.removeControl(groupNameToRemove);
    }

    hasBankAccountNrAndTypeCombinationError(loanGroupName: string) {
        let f = this.m.otherLoans.form.form;
        let group = f.get(loanGroupName);
        if (!group) {
            return false;
        }
        return group.hasError('bankAccountNrAndTypeCombination');
    }

    hasInvalidPaymentReferenceError(loanGroupName: string) {
        let f = this.m.otherLoans.form.form;
        let group = f.get(loanGroupName);
        if (!group) {
            return false;
        }
        return group.hasError('invalidPaymentReference');
    }

    private augmentOtherLoansWithBankAccountDisplayData(
        originalLoans: StandardLoanApplicationOtherLoanModel[]
    ): Promise<
        {
            original: StandardLoanApplicationOtherLoanModel;
            accountNrDisplay?: BankAccountDisplay;
            usesReference: boolean;
        }[]
    > {
        let request: Dictionary<BankAccountNrValidationRequest> = {};
        for (var i = 0; i < originalLoans.length; i++) {
            let loan = originalLoans[i];
            if (loan.bankAccountNr) {
                request[i.toString()] = {
                    bankAccountNr: loan.bankAccountNr,
                    bankAccountNrType: loan.bankAccountNrType,
                };
            }
        }
        return this.initialData.apiService.validateBankAccountNrsBatch(request, false).then((x) => {
            let result: {
                original: StandardLoanApplicationOtherLoanModel;
                accountNrDisplay?: BankAccountDisplay;
                usesReference: boolean;
            }[] = [];
            for (var i = 0; i < originalLoans.length; i++) {
                let loan = originalLoans[i];
                let validAccount = x.ValidatedAccountsByKey[i.toString()]?.ValidAccount;
                result.push({
                    original: loan,
                    accountNrDisplay: validAccount
                        ? {
                              displayType: validAccount.BankAccountNrType,
                              bankName: validAccount.BankName,
                              displayNr: validAccount.DisplayNr,
                          }
                        : null,
                    usesReference:
                        loan.shouldBeSettled === true &&
                        !this.validationService.isRegularBankAccount(loan.bankAccountNrType),
                });
            }
            return result;
        });
    }
}

class Model {
    householdInfo: {
        form: FormsHelper;
        editFields: Dictionary<EditblockFormFieldModel>;
    };
    children: {
        form: FormsHelper;
        groupNames: string[];
        original: StandardLoanApplicationChildModel[];
    };
    otherLoans: {
        form: FormsHelper;
        groupNames: string[];
        loanTypes: { Code: string; DisplayName: string }[];
        bankAccountTypes: { Code: string; DisplayName: string }[];
        view: {
            original: StandardLoanApplicationOtherLoanModel;
            accountNrDisplay?: BankAccountDisplay;
            usesReference: boolean;
        }[];
    };
    inEditMode: BehaviorSubject<boolean>;
    editFormInitialData: EditFormInitialData;
    subscriptions: Subscription[];
}

class BankAccountDisplay {
    displayType: string;
    bankName: string;
    displayNr: string;
}

export class HouseholdEconomyDataEditorInitialData {
    application: StandardApplicationModelBase;
    forceReadonly: boolean;
    sharedIsEditing?: BehaviorSubject<boolean>;
    apiService: SharedApplicationApiService;
}
