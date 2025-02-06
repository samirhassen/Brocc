import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import {
    CustomerPagesApplicationsApiService,
    DirectDebitTaskModel,
} from '../../services/customer-pages-applications-api.service';
import { from, of } from 'rxjs';
import { CustomerPagesEventService } from '../../../common-services/customerpages-event.service';

@Component({
    selector: 'customer-pages-direct-debit',
    templateUrl: './customer-pages-direct-debit.component.html',
})
export class CustomerPagesDirectDebitComponent implements OnInit {
    constructor(
        private fb: UntypedFormBuilder,
        private apiService: CustomerPagesApplicationsApiService,
        private eventService: CustomerPagesEventService
    ) {}

    public model: Model;

    @Input()
    public initialData: DirectDebitInitialData;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        if (!this.initialData || !this.initialData.task) return;

        this.model = null;
        let task = this.initialData.task;

        let applicants: { Code: string; DisplayName: string }[] = [];
        for (var applicantNr = 1; applicantNr <= Object.keys(task.CustomerInfoByApplicantNr).length; applicantNr++) {
            let app = task.CustomerInfoByApplicantNr[applicantNr];
            applicants.push({ Code: applicantNr.toString(), DisplayName: `${app.FirstName}, ${app.BirthDate}` });
        }
        let initialAccount = this.getInitialDirectDebitBankAccountNr(task, applicants);

        let selectedAccountOwnerName = initialAccount.AccountOwnerApplicantNr
            ? applicants.find((a) => a.Code === initialAccount.AccountOwnerApplicantNr.toString())?.DisplayName ?? ''
            : '';

        let model: Model = {
            view: {
                selectedAccountNrDisplay: initialAccount.DirectDebitBankAccountNr,
                selectedAccountOwnerDisplayName: selectedAccountOwnerName,
                preSelected:
                    initialAccount.AccountOwnerApplicantNr && initialAccount.DirectDebitBankAccountNr
                        ? {
                              bankAccountNr: initialAccount.DirectDebitBankAccountNr,
                              accountOwnerNr: initialAccount.AccountOwnerApplicantNr,
                          }
                        : null,
            },
            isPossibleToConfirmAccount:
                !!initialAccount.DirectDebitBankAccountNr && !!initialAccount.AccountOwnerApplicantNr,
            isDirectDebitAccountConfirmed: task.HasConfirmedAccountInfo,
            unsignedDirectDebitConsentFileArchiveKey: task.UnsignedDirectDebitConsentFileArchiveKey,
            signedDirectDebitConsentFileArchiveKey: task.SignedDirectDebitConsentFileArchiveKey,
            applicants: applicants,
            accountOwner: selectedAccountOwnerName,
            signatureSessionUrl: task.SignatureSessionUrl,
        };

        this.model = model;
    }

    private getInitialDirectDebitBankAccountNr(
        task: DirectDebitTaskModel,
        applicants: { Code: string; DisplayName: string }[]
    ): {
        AccountOwnerApplicantNr: number;
        DirectDebitBankAccountNr: string;
    } {
        if (task.DirectDebitBankAccountNr) {
            return {
                AccountOwnerApplicantNr: task.AccountOwnerApplicantNr,
                DirectDebitBankAccountNr: task.DirectDebitBankAccountNr,
            };
        } else if (task.PaidToCustomerBankAccountNr) {
            return {
                AccountOwnerApplicantNr: applicants.length === 1 ? 1 : null, //Dont default an owner when two and prefilled since it could be actively wrong
                DirectDebitBankAccountNr: task.PaidToCustomerBankAccountNr,
            };
        } else {
            return {
                AccountOwnerApplicantNr: 1,
                DirectDebitBankAccountNr: null,
            };
        }
    }

    private setupAccountForm(): FormsHelper {
        let initialAccount = this.getInitialDirectDebitBankAccountNr(this.initialData.task, this.model.applicants);
        let selectedBankAccount = initialAccount.DirectDebitBankAccountNr;
        let selectedAccountOwner = initialAccount.AccountOwnerApplicantNr;

        let form = new FormsHelper(this.fb.group({}));
        let bankAccountValidator = FormsHelper.createValidatorAsync('bankAccountNr', (res) => {
            if (!res) {
                return of(true); //Handled by required
            }
            return from(
                this.apiService
                    .validateBankAccountNrsBatch({ b: { bankAccountNr: res, bankAccountNrType: '' } }, true)
                    .then((y) => {
                        let result = y.ValidatedAccountsByKey['b'];
                        return result?.IsValid === true;
                    })
            );
        });
        form.addControlIfNotExists('bankAccountNr', selectedBankAccount, [Validators.required], [bankAccountValidator]);
        form.addControlIfNotExists('accountOwner', selectedAccountOwner?.toString(), [Validators.required]);
        return form;
    }

    beginEditAccount(evt?: Event) {
        this.model.editForm = this.setupAccountForm();
    }

    onCancelAccount(evt?: Event) {
        this.resetEditForm();
    }

    private resetEditForm() {
        this.model.editForm = null;
    }

    onSaveAccount(evt?: Event) {
        let selectedAccountNr = this.model.editForm.getValue('bankAccountNr');
        let selectedAccountOwner = this.model.editForm.getValue('accountOwner');

        this.apiService
            .saveDirectDebitAccountInfo(this.initialData.applicationNr, selectedAccountNr, selectedAccountOwner)
            .then((onSuccess) => {
                this.eventService.signalReloadApplication(this.initialData.applicationNr);
                this.resetEditForm();
            });
    }

    async confirmDirectDebitAccountAndOwner() {
        let task = this.initialData.task;
        if (!task.DirectDebitBankAccountNr && this.model.view.preSelected) {
            //Meaning there is an autofilled suggestion but the user did not edit or save it so we simulate that.
            let p = this.model.view.preSelected;
            await this.apiService.saveDirectDebitAccountInfo(
                this.initialData.applicationNr,
                p.bankAccountNr,
                p.accountOwnerNr
            );
        }

        this.apiService.confirmDirectDebitAccountInfo(this.initialData.applicationNr).then((onSuccess) => {
            this.model.isDirectDebitAccountConfirmed = true;
            this.eventService.signalReloadApplication(this.initialData.applicationNr);
        });
    }

    getDocumentUrl(archiveKey: string) {
        return this.apiService.getArchiveDocumentUrl(archiveKey, true);
    }

    getIconClass(isAccepted: boolean, isRejected: boolean) {
        let isOther = !isAccepted && !isRejected;
        return {
            'glyphicon-ok': isAccepted,
            'glyphicon-remove': isRejected,
            'glyphicon-minus': isOther,
            'glyphicon': true,
            'text-success': isAccepted,
            'text-danger': isRejected,
        };
    }
}

export class Model {
    view: {
        selectedAccountOwnerDisplayName: string;
        selectedAccountNrDisplay: string;
        preSelected: {
            bankAccountNr: string;
            accountOwnerNr: number;
        };
    };
    isPossibleToConfirmAccount: boolean;
    isDirectDebitAccountConfirmed: boolean;
    unsignedDirectDebitConsentFileArchiveKey: string;
    signedDirectDebitConsentFileArchiveKey: string;
    applicants: { Code: string; DisplayName: string }[];
    editForm?: FormsHelper;
    accountOwner: string;
    signatureSessionUrl: string;
}

export class DirectDebitInitialData {
    applicationNr: string;
    task: DirectDebitTaskModel;
}
