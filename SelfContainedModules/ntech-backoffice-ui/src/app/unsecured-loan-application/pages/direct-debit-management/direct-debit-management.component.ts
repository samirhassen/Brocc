import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { ConfigService } from 'src/app/common-services/config.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { Dictionary, getDictionaryValues } from 'src/app/common.types';
import { EditblockFormFieldModel } from 'src/app/shared-application-components/components/editblock-form-field/editblock-form-field.component';
import { StandardCreditApplicationModel } from '../../services/standard-credit-application-model';
import { UnsecuredLoanApplicationApiService } from '../../services/unsecured-loan-application-api.service';

@Component({
    selector: 'app-direct-debit-management',
    templateUrl: './direct-debit-management.component.html',
    styleUrls: ['./direct-debit-management.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DirectDebitManagementComponent implements OnInit {
    constructor(
        private apiService: UnsecuredLoanApplicationApiService,
        private route: ActivatedRoute,
        private fb: UntypedFormBuilder,
        public validationService: NTechValidationService,
        private config: ConfigService,
        private changeDetector: ChangeDetectorRef
    ) {}

    public m: Model = null;

    ngOnInit(): void {
        this.reload(this.route.snapshot.params['applicationNr']);
    }

    ngAfterContentChecked(): void {
        //The async validator of bankAccountNr causes https://angular.io/errors/NG0100
        //Cant find why but this known workaround fixes it at the cost of validation running twice
        this.changeDetector.detectChanges();
    }

    public initiateChange(evt?: Event) {
        evt?.preventDefault();

        let f = this.m.initial.form;

        let isActive = f.getValue('isActive') === 'true';
        let activeAccount = isActive
            ? {
                  ensureCreditNrExists: true,
                  bankAccountNr: f.getValue('bankAccountNr'),
                  bankAccountNrType: '', //NOTE: Assume the default for the country
                  bankAccountNrOwnerApplicantNr: parseInt(f.getValue('accountOwnerApplicantNr')),
                  directDebitConsentArchiveKey: this.includeConsentPdf()
                      ? this.m.initial.signedConsentPdf.archiveKey
                      : null,
              }
            : null;

        let applicationNr = this.m.applicationNr;
        this.apiService.setDirectDebitLoanTerms(applicationNr, true, activeAccount).then((x) => {
            this.reload(applicationNr);
        });
    }

    public approveChange(evt?: Event) {
        evt?.preventDefault();

        let directDebitTerms = this.getDirectDebitTermsValues(this.m.application);

        let isActive = directDebitTerms.isActive === 'true';
        let activeAccount = isActive
            ? {
                  ensureCreditNrExists: true,
                  bankAccountNr: directDebitTerms.bankAccountNr,
                  bankAccountNrType: directDebitTerms.bankAccountNrType,
                  bankAccountNrOwnerApplicantNr: parseInt(directDebitTerms.accountOwnerApplicantNr),
                  directDebitConsentArchiveKey: directDebitTerms.signedConsentPdfArchiveKey,
              }
            : null;

        let applicationNr = this.m.applicationNr;
        this.apiService.setDirectDebitLoanTerms(applicationNr, false, activeAccount).then((x) => {
            this.reload(applicationNr);
        });
    }

    public cancel(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.m.applicationNr;
        this.apiService.cancelDirectDebitLoanTerms(applicationNr).then((x) => {
            this.reload(applicationNr);
        });
    }

    onPendingConfirmedToggled(evt: Event) {
        let isChecked = (evt.target as any).checked;
        this.m.terms.isPendingConfirmed = isChecked;
    }

    getArchiveUrl(archiveKey: string) {
        if (!archiveKey) {
            return null;
        }
        return this.apiService.api().getArchiveDocumentUrl(archiveKey);
    }

    private reload(applicationNr: string) {
        this.m = null;
        this.apiService.fetchApplicationInitialData(applicationNr).then((x) => {
            if (x === 'noSuchApplicationExists') {
                return;
            }

            let m: Model = {
                application: x,
                applicationNr: applicationNr,
                initial: null,
                terms: null,
            };

            let directDebitLoanTerms = this.getDirectDebitTermsValues(x);

            if (directDebitLoanTerms) {
                m.terms = {
                    isPending: directDebitLoanTerms.isPending === 'true',
                    isActive: directDebitLoanTerms.isActive === 'true',
                    isCancelAllowed: x.applicationInfo.IsActive,
                };
                if (m.terms.isActive) {
                    let creditNr = x
                        .getComplexApplicationList('Application', true)
                        .getRow(1, true)
                        .getUniqueItem('creditNr');
                    let accountOwnerApplicantNr = parseInt(directDebitLoanTerms.accountOwnerApplicantNr);
                    let applicantInfo = x.applicantInfoByApplicantNr[accountOwnerApplicantNr];
                    this.apiService
                        .shared()
                        .fetchCustomerItems(applicantInfo.CustomerId, ['civicRegNr'])
                        .then((customerResult) => {
                            let civicRegNr = customerResult['civicRegNr'];
                            this.apiService
                                .generateDirectDebitPayerNumber(accountOwnerApplicantNr, creditNr)
                                .then((paymentNrResult) => {
                                    m.terms.activeAccount = {
                                        bankAccountNr: directDebitLoanTerms.bankAccountNr,
                                        paymentNr: paymentNrResult.PayerNr,
                                        accountOwnerName: `${applicantInfo.FirstName} ${applicantInfo.LastName}`,
                                        accountOwnerCivicRegNr: civicRegNr,
                                        signedConsentPdfArchiveKey: directDebitLoanTerms.signedConsentPdfArchiveKey,
                                    };
                                    this.m = m;
                                });
                        });
                } else {
                    this.m = m;
                }
            } else {
                let i = this.getInitialApplicationValues(x);

                m.initial = {
                    form: new FormsHelper(this.fb.group({})),
                    editFields: {},
                    formSubs: null,
                    isEditAllowed: true,
                    inEditMode: true,
                    signedConsentPdf: i.signedConsentPdfArchiveKey
                        ? {
                              archiveKey: i.signedConsentPdfArchiveKey,
                              applicantNr: i.accountOwnerApplicantNr,
                              bankAccountNr: i.bankAccountNr,
                          }
                        : null,
                };

                let bankAccountValidatorResult = this.validationService.getBankAccountNrAsyncValidator(
                    this.config,
                    'bankAccountNr',
                    (x, y) => this.apiService.validateBankAccountNr(x, y, true).then((x) => x.IsValid)
                );

                m.initial.editFields['isActive'] = {
                    getForm: () => m.initial.form,
                    formControlName: 'isActive',
                    labelText: 'Direct debit',
                    inEditMode: () => this.m.initial.inEditMode,
                    getOriginalValue: () => (i.bankAccountNr ? 'true' : 'false'),
                    getValidators: () => [Validators.required],
                    dropdownOptions: EditblockFormFieldModel.includeEmptyDropdownOption([
                        { Code: 'true', DisplayName: 'Active' },
                        { Code: 'false', DisplayName: 'Not active' },
                    ]),
                };

                m.initial.editFields['accountOwnerApplicantNr'] = {
                    getForm: () => m.initial.form,
                    formControlName: 'accountOwnerApplicantNr',
                    labelText: 'Account owner',
                    inEditMode: () => this.m.initial.inEditMode,
                    getOriginalValue: () => i.accountOwnerApplicantNr?.toString(),
                    getValidators: () => [Validators.required],
                    dropdownOptions: this.getAccountOwnerOptions(x),
                    conditional: {
                        shouldExist: () => m.initial.form.getValue('isActive') === 'true',
                    },
                };

                m.initial.editFields['bankAccountNr'] = {
                    getForm: () => m.initial.form,
                    formControlName: 'bankAccountNr',
                    labelText: 'Direct debit account',
                    inEditMode: () => this.m.initial.inEditMode,
                    getOriginalValue: () => i.bankAccountNr,
                    getValidators: () => [Validators.required],
                    getAsyncValidators: () => [bankAccountValidatorResult.validator],
                    conditional: {
                        shouldExist: () => m.initial.form.getValue('isActive') === 'true',
                    },
                };

                m.initial.formSubs = EditblockFormFieldModel.setupForm(
                    getDictionaryValues(m.initial.editFields),
                    m.initial.form
                );
                this.m = m;
            }
        });
    }

    public includeConsentPdf() {
        let i = this.m?.initial;
        return (
            i.signedConsentPdf &&
            i.form.getValue('isActive') === 'true' &&
            i.signedConsentPdf.applicantNr === parseInt(i.form.getValue('accountOwnerApplicantNr')) &&
            i.signedConsentPdf.bankAccountNr === i.form.getValue('bankAccountNr')
        );
    }

    private getDirectDebitTermsValues(application: StandardCreditApplicationModel) {
        let directDebitLoanTermsList = application.getComplexApplicationList('DirectDebitLoanTerms', false);
        if (!directDebitLoanTermsList) {
            return null;
        }

        let row = directDebitLoanTermsList.getRow(1, true);
        return {
            isPending: row.getUniqueItem('isPending'),
            isActive: row.getUniqueItem('isActive'),
            bankAccountNr: row.getUniqueItem('bankAccountNr'),
            bankAccountNrType: row.getUniqueItem('bankAccountNrType'),
            accountOwnerApplicantNr: row.getUniqueItem('accountOwnerApplicantNr'),
            signedConsentPdfArchiveKey: row.getUniqueItem('signedConsentPdfArchiveKey'),
        };
    }

    private getInitialApplicationValues(application: StandardCreditApplicationModel) {
        let applicationList = application.getComplexApplicationList('Application', true).getRow(1, true);
        return {
            accountOwnerApplicantNr: applicationList.getUniqueItemInteger('directDebitAccountOwnerApplicantNr'),
            bankAccountNr: applicationList.getUniqueItem('directDebitBankAccountNr'),
            signedConsentPdfArchiveKey: application
                .getComplexApplicationList('DirectDebitSigningSession', true)
                .getRow(1, true)
                .getUniqueItem('SignedDirectDebitConsentFilePdfArchiveKey'),
        };
    }

    private getAccountOwnerOptions(
        application: StandardCreditApplicationModel
    ): { Code: string; DisplayName: string }[] {
        let result: { Code: string; DisplayName: string }[] = [];
        let a = application.applicantInfoByApplicantNr;
        for (var applicantNr = 1; applicantNr <= application.nrOfApplicants; applicantNr++) {
            result.push({
                Code: applicantNr.toString(),
                DisplayName: `${a[applicantNr]?.FirstName}, ${a[applicantNr]?.BirthDate}`,
            });
        }
        return EditblockFormFieldModel.includeEmptyDropdownOption(result);
    }
}

class Model {
    applicationNr: string;
    application: StandardCreditApplicationModel;
    initial?: {
        form: FormsHelper;
        editFields: Dictionary<EditblockFormFieldModel>;
        formSubs: Subscription[];
        isEditAllowed: boolean;
        inEditMode: boolean;
        signedConsentPdf: {
            archiveKey: string;
            applicantNr: number;
            bankAccountNr: string;
        };
    };
    terms?: {
        isCancelAllowed: boolean;
        isPending: boolean;
        isPendingConfirmed?: boolean;
        isActive: boolean;
        activeAccount?: {
            bankAccountNr: string;
            paymentNr: string;
            accountOwnerName: string;
            accountOwnerCivicRegNr: string;
            signedConsentPdfArchiveKey: string;
        };
    };
}
