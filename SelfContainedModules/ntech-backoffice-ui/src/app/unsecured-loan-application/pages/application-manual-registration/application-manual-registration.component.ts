import { Component, OnInit } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { TestFunctionsModel } from 'src/app/common-components/test-functions-popup/test-functions-popup.component';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import {
    NTechLocalStorageContainer,
    NTechLocalStorageService,
} from 'src/app/common-services/ntech-localstorage.service';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { Dictionary, getDictionaryValues } from 'src/app/common.types';
import { EditblockFormFieldModel } from 'src/app/shared-application-components/components/editblock-form-field/editblock-form-field.component';
import {
    StandardApplicationModelBase,
    StandardLoanApplicationEnumsModel,
} from 'src/app/shared-application-components/services/standard-application-base';
import { UnsecuredLoanApplicationApiService } from '../../services/unsecured-loan-application-api.service';
import { fillInRandomTestApplication, ITestFunctionsSupport } from './application-manual-registration-test-generator';

let yesNoBooleanDropdownOptions = [
    { Code: 'true', DisplayName: 'Yes' },
    { Code: 'false', DisplayName: 'No' },
];

@Component({
    selector: 'app-application-manual-registration',
    templateUrl: './application-manual-registration.component.html',
    styles: [],
})
export class ApplicationManualRegistrationComponent implements OnInit, ITestFunctionsSupport {
    constructor(
        private fb: UntypedFormBuilder,
        public validationService: NTechValidationService,
        private apiService: UnsecuredLoanApplicationApiService,
        public config: ConfigService,
        public baseApiService: NtechApiService,
        localStorageService: NTechLocalStorageService
    ) {
        this.localStorage = localStorageService.getSharedContainer(
            'ApplicationManualRegistrationComponent',
            '2021101201'
        );
    }

    public m: Model;
    public r: RegisteredModel;
    public staticData: { Enums: StandardLoanApplicationEnumsModel; ProviderDisplayNameByName: Dictionary<string> };
    private localStorage: NTechLocalStorageContainer;

    public mainApplicantPrefix = 'mainApplicant';
    public coApplicantPrefix = 'coApplicant';

    ngOnInit(): void {
        this.apiService.fetchRegisterApplicationInitialData().then((x) => {
            this.staticData = x;
            this.reset();
        });
    }

    private unsubFromForm() {
        let subs = this.m?.formSubs;
        if (subs) {
            for (let s of subs) {
                s.unsubscribe();
            }
        }
    }

    reset(evt?: Event) {
        evt?.preventDefault();
        this.unsubFromForm();
        this.m = null;
        this.r = null;

        let form = new FormsHelper(this.fb.group({}));
        let editFields: Dictionary<EditblockFormFieldModel> = {};
        //Used to draw the ui to lessen the duplication a bit
        let editFieldsOrdered: { controlName: string; displayGroupName: string }[] = [];

        let addEditFields = (displayGroupName: string, editFieldList: EditblockFormFieldModel[]) => {
            for (let editField of editFieldList) {
                editFields[editField.formControlName] = editField;
                editFieldsOrdered.push({ displayGroupName, controlName: editField.formControlName });
            }
        };

        addEditFields('applicationLeft', [
            {
                getForm: () => form,
                formControlName: 'providerName',
                labelText: 'Provider',
                inEditMode: () => true,
                getOriginalValue: () => this.localStorage.get('providerName'),
                getValidators: () => [Validators.required],
                dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption(
                    Object.keys(this.staticData.ProviderDisplayNameByName).map((x) => {
                        return {
                            Code: x,
                            DisplayName: this.staticData.ProviderDisplayNameByName[x],
                        };
                    })
                ),
            },
        ]);

        addEditFields('applicationRight', [
            {
                getForm: () => form,
                formControlName: 'requestedLoanAmount',
                labelText: 'Requested loan amount',
                inEditMode: () => true,
                getOriginalValue: () => '',
                getValidators: () => [Validators.required, this.validationService.getPositiveIntegerValidator()],
            },
            {
                getForm: () => form,
                formControlName: 'requestedRepaymentTimeInMonths',
                labelText: 'Requested repayment time',
                inEditMode: () => true,
                getOriginalValue: () => '',
                getValidators: () => [Validators.required, this.validationService.getPositiveIntegerValidator()],
            },
        ]);

        editFields['hasCoApplicant'] = {
            getForm: () => form,
            formControlName: 'hasCoApplicant',
            labelText: 'Co applicant',
            inEditMode: () => true,
            getOriginalValue: () => 'false',
            getValidators: () => [Validators.required],
            dropdownOptions: yesNoBooleanDropdownOptions,
        };

        let createConditional = (shouldExist: () => boolean) => {
            return {
                shouldExist: shouldExist,
            };
        };

        for (let applicantPrefix of [this.mainApplicantPrefix, this.coApplicantPrefix]) {
            addEditFields(`${applicantPrefix}Left`, [
                {
                    getForm: () => form,
                    formControlName: `${applicantPrefix}CivicRegNr`,
                    labelText: 'Civic nr',
                    inEditMode: () => true,
                    getOriginalValue: () => '',
                    getValidators: () => [Validators.required, this.validationService.getCivicRegNrValidator()],
                },
                {
                    getForm: () => form,
                    formControlName: `${applicantPrefix}FirstName`,
                    labelText: 'First name',
                    inEditMode: () => true,
                    getOriginalValue: () => '',
                    getValidators: () => [],
                },
                {
                    getForm: () => form,
                    formControlName: `${applicantPrefix}LastName`,
                    labelText: 'Last name',
                    inEditMode: () => true,
                    getOriginalValue: () => '',
                    getValidators: () => [],
                },
                {
                    getForm: () => form,
                    formControlName: `${applicantPrefix}Employment`,
                    labelText: 'Employment',
                    inEditMode: () => true,
                    getOriginalValue: () => '',
                    getValidators: () => [],
                    dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption(
                        this.staticData.Enums.EmploymentStatuses
                    ),
                },
                {
                    getForm: () => form,
                    formControlName: `${applicantPrefix}Employer`,
                    labelText: 'Employer',
                    inEditMode: () => true,
                    getOriginalValue: () => '',
                    conditional: createConditional(() =>
                        StandardApplicationModelBase.isEmployerEmploymentCode(
                            form.getValue(`${applicantPrefix}Employment`)
                        )
                    ),
                    getValidators: () => [],
                },
                {
                    getForm: () => form,
                    formControlName: `${applicantPrefix}EmployerPhone`,
                    labelText: 'Employer phone',
                    inEditMode: () => true,
                    getOriginalValue: () => '',
                    conditional: createConditional(() =>
                        StandardApplicationModelBase.isEmployerEmploymentCode(
                            form.getValue(`${applicantPrefix}Employment`)
                        )
                    ),
                    getValidators: () => [],
                },
                {
                    getForm: () => form,
                    formControlName: `${applicantPrefix}EmployedSince`,
                    labelText: 'Employed since',
                    inEditMode: () => true,
                    getOriginalValue: () => '',
                    conditional: createConditional(() =>
                        StandardApplicationModelBase.isEmployedSinceEmploymentCode(
                            form.getValue(`${applicantPrefix}Employment`),
                            this.validationService
                        )
                    ),
                    getValidators: () => [this.validationService.getDateOnlyValidator()],
                    placeholder: 'YYYY-MM-DD',
                },
                {
                    getForm: () => form,
                    formControlName: `${applicantPrefix}EmployedTo`,
                    labelText: 'Employed to',
                    inEditMode: () => true,
                    getOriginalValue: () => '',
                    getValidators: () => [this.validationService.getDateOnlyValidator()],
                    conditional: createConditional(() =>
                        StandardApplicationModelBase.isEmployedToEmploymentCode(
                            form.getValue(`${applicantPrefix}Employment`)
                        )
                    ),
                    placeholder: 'YYYY-MM-DD',
                },
                {
                    getForm: () => form,
                    formControlName: `${applicantPrefix}Marriage`,
                    labelText: 'Marital status',
                    inEditMode: () => true,
                    getOriginalValue: () => '',
                    getValidators: () => [],
                    dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption(
                        this.staticData.Enums.CivilStatuses
                    ),
                },
                {
                    getForm: () => form,
                    formControlName: `${applicantPrefix}IncomePerMonthAmount`,
                    labelText: 'Monthly income before tax',
                    inEditMode: () => true,
                    getOriginalValue: () => '',
                    getValidators: () => [this.validationService.getPositiveIntegerValidator()],
                },
            ]);

            addEditFields(`${applicantPrefix}Right`, [
                {
                    getForm: () => form,
                    formControlName: `${applicantPrefix}Email`,
                    labelText: 'Email',
                    inEditMode: () => true,
                    getOriginalValue: () => '',
                    getValidators: () => [Validators.required, this.validationService.getEmailValidator()],
                },
                {
                    getForm: () => form,
                    formControlName: `${applicantPrefix}Phone`,
                    labelText: 'Phone nr',
                    inEditMode: () => true,
                    getOriginalValue: () => '',
                    getValidators: () => [Validators.required, this.validationService.getPhoneValidator()],
                },
                {
                    getForm: () => form,
                    formControlName: `${applicantPrefix}AddressStreet`,
                    labelText: 'Address street',
                    inEditMode: () => true,
                    getOriginalValue: () => '',
                    getValidators: () => [],
                },
                {
                    getForm: () => form,
                    formControlName: `${applicantPrefix}AddressZipcode`,
                    labelText: 'Address zipcode',
                    inEditMode: () => true,
                    getOriginalValue: () => '',
                    getValidators: () => [],
                },
                {
                    getForm: () => form,
                    formControlName: `${applicantPrefix}AddressCity`,
                    labelText: 'Address city',
                    inEditMode: () => true,
                    getOriginalValue: () => '',
                    getValidators: () => [],
                },
                {
                    getForm: () => form,
                    formControlName: `${applicantPrefix}HasConsentedToShareBankAccountData`,
                    labelText: 'Has consented to share bank account data',
                    inEditMode: () => true,
                    getOriginalValue: () => '',
                    getValidators: () => [],
                    dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption(yesNoBooleanDropdownOptions),
                },
                {
                    getForm: () => form,
                    formControlName: `${applicantPrefix}HasConsentedToCreditReport`,
                    labelText: 'Has consented to credit report',
                    inEditMode: () => true,
                    getOriginalValue: () => '',
                    getValidators: () => [],
                    dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption(yesNoBooleanDropdownOptions),
                },
                {
                    getForm: () => form,
                    formControlName: `${applicantPrefix}ClaimsToBePep`,
                    labelText: 'Claims to be pep',
                    inEditMode: () => true,
                    getOriginalValue: () => '',
                    getValidators: () => [],
                    dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption(yesNoBooleanDropdownOptions),
                },
            ]);
        }

        for (let editField of editFieldsOrdered.filter((x) => x.controlName.startsWith(this.coApplicantPrefix))) {
            let field = editFields[editField.controlName];
            let existingConditional = field.conditional;
            field.conditional = createConditional(
                () =>
                    form.getValue('hasCoApplicant') === 'true' &&
                    (!existingConditional || existingConditional.shouldExist())
            );
        }

        addEditFields('economyLeft', [
            {
                getForm: () => form,
                formControlName: 'nrOfChildren',
                labelText: 'Nr of children',
                inEditMode: () => true,
                getOriginalValue: () => '',
                getValidators: () => [this.validationService.getIntegerWithBoundsValidator(0, 99)],
            },
            {
                getForm: () => form,
                formControlName: 'housingType',
                labelText: 'Housing type',
                inEditMode: () => true,
                getOriginalValue: () => '',
                getValidators: () => [],
                dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption(this.staticData.Enums.HousingTypes),
            },
            {
                getForm: () => form,
                formControlName: 'housingCostPerMonthAmount',
                labelText: 'Housing cost',
                inEditMode: () => true,
                getOriginalValue: () => '',
                getValidators: () => [this.validationService.getPositiveIntegerValidator()],
            },
        ]);

        addEditFields('economyRight', []);

        form.setFormValidator((x) => {
            if (form.getValue('hasCoApplicant') !== 'true') {
                return true;
            }
            if (
                form.getValue(`${this.mainApplicantPrefix}CivicRegNr`) ===
                form.getValue(`${this.coApplicantPrefix}CivicRegNr`)
            ) {
                return false;
            }
            return true;
        });

        this.m = {
            form: form,
            editFields: editFields,
            formSubs: EditblockFormFieldModel.setupForm(getDictionaryValues(editFields), form),
            testFunctions: this.createTestFunctions(),
            editFieldsOrdered: editFieldsOrdered,
            nextOtherLoanPrefix: 1,
            otherLoanPrefixes: [],
        };
    }

    registerApplication(evt?: Event) {
        evt?.preventDefault();

        let f = this.m.form;
        let applicants: any[] = [];
        let hasCoApplicant = f.getValue('hasCoApplicant') === 'true';
        let parseTriStateBool = (x: string) => (x === 'true' ? true : x === 'false' ? false : null);

        for (let applicantPrefix of hasCoApplicant
            ? [this.mainApplicantPrefix, this.coApplicantPrefix]
            : [this.mainApplicantPrefix]) {
            let employment = f.getValue(`${applicantPrefix}Employment`);
            let isMainApplicant = applicantPrefix === this.mainApplicantPrefix;

            applicants.push({
                CivicRegNr: f.getValue(`${applicantPrefix}CivicRegNr`),
                FirstName: f.getValue(`${applicantPrefix}FirstName`),
                LastName: f.getValue(`${applicantPrefix}LastName`),
                addressStreet: f.getValue(`${applicantPrefix}AddressStreet`),
                AddressZipcode: f.getValue(`${applicantPrefix}AddressZipcode`),
                AddressCity: f.getValue(`${applicantPrefix}AddressCity`),
                Email: f.getValue(`${applicantPrefix}Email`),
                Phone: f.getValue(`${applicantPrefix}Phone`),
                ClaimsToBePep: parseTriStateBool(f.getValue(`${applicantPrefix}ClaimsToBePep`)),
                HasConsentedToShareBankAccountData: parseTriStateBool(
                    f.getValue(`${applicantPrefix}HasConsentedToShareBankAccountData`)
                ),
                HasConsentedToCreditReport: parseTriStateBool(
                    f.getValue(`${applicantPrefix}HasConsentedToCreditReport`)
                ),
                EmploymentStatus: employment,
                EmployerName: StandardApplicationModelBase.isEmployerEmploymentCode(employment)
                    ? f.getValue(`${applicantPrefix}Employer`)
                    : null,
                EmployerPhone: StandardApplicationModelBase.isEmployerEmploymentCode(employment)
                    ? f.getValue(`${applicantPrefix}EmployerPhone`)
                    : null,
                EmployedSince: StandardApplicationModelBase.isEmployedSinceEmploymentCode(
                    employment,
                    this.validationService
                )
                    ? this.validationService.parseDateOnlyOrNull(f.getValue(`${applicantPrefix}EmployedSince`))
                    : null,
                EmployedTo: StandardApplicationModelBase.isEmployedToEmploymentCode(employment)
                    ? this.validationService.parseDateOnlyOrNull(f.getValue(`${applicantPrefix}EmployedTo`))
                    : null,
                CivilStatus: f.getValue(`${applicantPrefix}Marriage`),
                MonthlyIncomeAmount: this.validationService.parseIntegerOrNull(
                    f.getValue(`${applicantPrefix}IncomePerMonthAmount`)
                ),
                NrOfChildren: isMainApplicant
                    ? this.validationService.parseIntegerOrNull(f.getValue('nrOfChildren'))
                    : null,
                HousingType: isMainApplicant ? f.getValue('housingType') : null,
                HousingCostPerMonthAmount: isMainApplicant ? f.getValue('housingCostPerMonthAmount') : null,
            });
        }
        let otherLoans = this.m.otherLoanPrefixes.map((x) => {
            return {
                LoanType: f.getValue(`${x}LoanType`),
                CurrentDebtAmount: this.validationService.parseIntegerOrNull(f.getValue(`${x}CurrentDebtAmount`)),
                MonthlyCostAmount: this.validationService.parseIntegerOrNull(f.getValue(`${x}MonthlyCostAmount`)),
                ShouldBeSettled: parseTriStateBool(f.getValue(`${x}ShouldBeSettled`)),
            };
        });

        let providerName = f.getValue('providerName');
        let request: any = {
            RequestedAmount: this.validationService.parseInteger(f.getValue('requestedLoanAmount')),
            RequestedRepaymentTimeInMonths: this.validationService.parseInteger(
                f.getValue('requestedRepaymentTimeInMonths')
            ),
            Applicants: applicants,
            Meta: {
                ProviderName: providerName,
            },
            HouseholdOtherLoans: otherLoans,
        };

        this.apiService.createApplication(request).then((x) => {
            this.localStorage.set('providerName', providerName, 8 * 60); //Basically remember that day. To simplify repeated registration of many applications from the same provider
            this.unsubFromForm();
            this.m = null;
            this.r = {
                applicationNr: x.ApplicationNr,
                //NOTE: This currently does not work since the layout shell seems to handle target priorty wrong
                //      so the application page ignores the target. It just happens to work from applications since that is the default anyway
                targetToHere: CrossModuleNavigationTarget.create('UnsecuredLoanStandardRegisterApplication', {}),
            };
        });
    }

    public displayGroupFieldNames(displayGroupName: string) {
        return this.m.editFieldsOrdered
            .filter((x) => x.displayGroupName === displayGroupName)
            .map((x) => x.controlName);
    }

    private createTestFunctions() {
        if (!this.config.isNTechTest()) {
            return null;
        }
        let t = new TestFunctionsModel();
        if (this.config.baseCountry() === 'SE') {
            let randomize = (useCoApplicant: boolean) => {
                return fillInRandomTestApplication(useCoApplicant, t, this.m.form, this);
            };
            t.addFunctionCall('Randomize', () => {
                randomize(t.getRandomInt(1, 3) === 1); //One in three will have a co applicant
            });
            t.addFunctionCall('Randomize (force co applicant)', () => {
                randomize(true);
            });
        }

        return t;
    }

    public addOtherLoanRowReturningNewPrefix(): string {
        let f = this.m.form;
        let nextPrefix = this.m.nextOtherLoanPrefix;
        this.m.nextOtherLoanPrefix = this.m.nextOtherLoanPrefix + 1;
        let prefix = `otherLoan${nextPrefix}`;

        let addEditFields = (editFieldList: EditblockFormFieldModel[]) => {
            for (let editField of editFieldList) {
                this.m.editFields[editField.formControlName] = editField;
                this.m.editFieldsOrdered.push({ displayGroupName: prefix, controlName: editField.formControlName });
            }
        };

        addEditFields([
            {
                getForm: () => f,
                formControlName: `${prefix}LoanType`,
                labelText: 'Loan type',
                inEditMode: () => true,
                getOriginalValue: () => '',
                getValidators: () => [],
                dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption(
                    this.staticData.Enums.OtherLoanTypes
                ),
            },
            {
                getForm: () => f,
                formControlName: `${prefix}CurrentDebtAmount`,
                labelText: 'Current debt',
                inEditMode: () => true,
                getOriginalValue: () => '',
                getValidators: () => [this.validationService.getPositiveIntegerValidator()],
            },
            {
                getForm: () => f,
                formControlName: `${prefix}MonthlyCostAmount`,
                labelText: 'Monthly cost',
                inEditMode: () => true,
                getOriginalValue: () => '',
                getValidators: () => [this.validationService.getPositiveIntegerValidator()],
            },
            {
                getForm: () => f,
                formControlName: `${prefix}ShouldBeSettled`,
                labelText: 'Should be settled',
                inEditMode: () => true,
                getOriginalValue: () => '',
                getValidators: () => [],
                dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption(yesNoBooleanDropdownOptions),
            },
        ]);

        this.unsubFromForm();
        this.m.formSubs = EditblockFormFieldModel.setupForm(getDictionaryValues(this.m.editFields), f);
        this.m.otherLoanPrefixes.push(prefix);

        return prefix;
    }

    addOtherLoan(evt?: Event) {
        evt?.preventDefault();
        this.addOtherLoanRowReturningNewPrefix();
    }

    clearOtherLoans() {
        for (let prefix of this.m.otherLoanPrefixes) {
            this.removeOtherLoan(prefix, null);
        }
    }

    removeOtherLoan(otherLoanPrefix: string, evt?: Event) {
        evt?.preventDefault();

        let index = this.m.otherLoanPrefixes.findIndex((x) => x == otherLoanPrefix);
        if (index < 0) {
            return;
        }
        this.m.editFieldsOrdered = this.m.editFieldsOrdered.filter((x) => x.displayGroupName !== otherLoanPrefix);
        this.m.otherLoanPrefixes.splice(index, 1);
        for (let fieldName of Object.keys(this.m.editFields).filter((x) => x.startsWith(otherLoanPrefix))) {
            delete this.m.editFields[fieldName];
        }
        this.unsubFromForm();
        this.m.formSubs = EditblockFormFieldModel.setupForm(getDictionaryValues(this.m.editFields), this.m.form);
    }
}

class Model {
    form: FormsHelper;
    editFields: Dictionary<EditblockFormFieldModel>;
    formSubs: Subscription[];
    testFunctions: TestFunctionsModel;
    editFieldsOrdered: { controlName: string; displayGroupName: string }[];
    otherLoanPrefixes: string[];
    nextOtherLoanPrefix: number;
}

class RegisteredModel {
    applicationNr: string;
    targetToHere: CrossModuleNavigationTarget;
}
