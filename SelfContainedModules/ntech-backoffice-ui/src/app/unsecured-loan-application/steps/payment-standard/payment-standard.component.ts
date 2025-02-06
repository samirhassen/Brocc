import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { ComplexApplicationListRow } from 'src/app/common-services/complex-application-list';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { StandardCreditApplicationModel } from '../../services/standard-credit-application-model';
import { UnsecuredLoanApplicationApiService } from '../../services/unsecured-loan-application-api.service';
import { WorkflowStepInitialData } from '../workflow-step';

@Component({
    selector: 'payment-standard',
    templateUrl: './payment-standard.component.html',
    styles: [],
})
export class PaymentStandardComponent implements OnInit {
    constructor(
        private apiService: UnsecuredLoanApplicationApiService,
        private toastr: ToastrService,
        private router: Router,
        private eventService: NtechEventService,
        private config: ConfigService,
        private generalApiService: NtechApiService
    ) {}

    @Input()
    public initialData: WorkflowStepInitialData;

    public m: Model;

    ngOnInit(): void {}

    async ngOnChanges(changes: SimpleChanges): Promise<void> {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let isDirectDebitEnabled = this.config.isFeatureEnabled('ntech.feature.directdebitpaymentsenabled');

        let isDirectDebitApproved: boolean;
        if(isDirectDebitEnabled) {
            let directDebitLoanTerms = this.initialData.application
                .getComplexApplicationList('DirectDebitLoanTerms', true)
                .getRow(1, true);
            isDirectDebitApproved = directDebitLoanTerms.getUniqueItem('isPending') === 'false';
        } else {
            isDirectDebitApproved = false;
        }

        let applicationRow = this.initialData.application
            .getComplexApplicationList('Application', true)
            .getRow(1, true);

        let ai = this.initialData.application.applicationInfo;
        let isStepActiveAndCurrent =
            ai.IsActive &&
            this.initialData.workflow.step.areAllStepBeforeThisAccepted() &&
            this.initialData.workflow.step.isStatusInitial();

        this.m = {
            isDirectDebitEnabled: isDirectDebitEnabled,
            isCreateLoanAllowed: isStepActiveAndCurrent && (!isDirectDebitEnabled || isDirectDebitApproved),
            areAllStepBeforeThisAccepted: this.initialData.workflow.step.areAllStepBeforeThisAccepted(),
            application: this.initialData.application,
            createdLoan: ai.IsFinalDecisionMade
                ? {
                      creditNr: applicationRow.getUniqueItem('creditNr'),
                      url: this.config.getServiceRegistry().createUrl('nCredit', 'Ui/Credit', [
                          ['creditNr', applicationRow.getUniqueItem('creditNr')],
                          ['backTarget', this.initialData.applicationNavigationTarget.getCode()],
                      ]),
                  }
                : null,
            signatureSession: null,
            isDirectDebitApproved: isDirectDebitApproved,
        };

        if(isDirectDebitEnabled) {
            let directDebitSession = this.initialData.application
                .getComplexApplicationList('DirectDebitSigningSession', true)
                .getRow(1, false);
            if (directDebitSession !== null) {
                this.populateDirectDebitModel(directDebitSession, applicationRow);
            }
        }
    }

    private populateDirectDebitModel(
        directDebitSession: ComplexApplicationListRow,
        applicationRow: ComplexApplicationListRow
    ) {
        let applicantInfoByApplicantNr = this.initialData.application.applicantInfoByApplicantNr;
        let unsignedDocumentKey = directDebitSession.getUniqueItem('UnsignedDirectDebitConsentFilePdfArchiveKey');
        let signedDocumentKey = directDebitSession.getUniqueItem('SignedDirectDebitConsentFilePdfArchiveKey');
        let accountOwnerApplicantNr = applicationRow.getUniqueItemInteger('directDebitAccountOwnerApplicantNr');

        this.m.signatureSession = {
            isCancelAllowed: this.initialData.application.applicationInfo.IsActive,
            hasSignatureSessionFailed: false,
            unsignedAgreementPdfArchiveKey: unsignedDocumentKey,
            signedAgreementPdfArchiveKey: signedDocumentKey,
            applicant: {
                applicantNr: accountOwnerApplicantNr,
                firstName: applicantInfoByApplicantNr[accountOwnerApplicantNr].FirstName,
                birthDate: applicantInfoByApplicantNr[accountOwnerApplicantNr].BirthDate,
                hasSigned: !!signedDocumentKey,
            },
        };
    }

    getIconClass(isAccepted: boolean, isRejected?: boolean) {
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

    async createLoan(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.m.application.applicationNr;
        try {
            await this.apiService.createLoan(applicationNr);
            this.eventService.signalReloadApplication(applicationNr);
        } catch(error: any) {
            let errorMessage : string = error?.error?.errorMessage;
            let errorCode : string = error?.error?.errorCode;
            if(errorCode === 'missingRequiredItem' && (errorMessage??'').toLowerCase().includes('bankaccountnr')) {
                this.toastr.warning('Missing bank account for outgoing payment. This can be added in the application basis.');
            } else if(errorCode === 'loanToSettleMissingPaymentReference') {
                this.toastr.warning('Missing payment message or reference for a loan settlement payment. This can be added in the application basis.');
            } else if(errorMessage) {
                this.toastr.error(errorMessage);
            } else {
                this.toastr.error('Failed to create loan');
            }
        }
    }

    viewDirectDebitDetails(evt?: Event) {
        evt?.preventDefault();

        this.router.navigate(['/unsecured-loan-application/direct-debit', this.initialData.application.applicationNr]);
    }

    getDocumentUrl(archiveKey: string) {
        return this.generalApiService.getArchiveDocumentUrl(archiveKey);
    }

    cancelDirectDebitSignatureSession(evt?: Event) {
        evt?.preventDefault();

        let applicationnr = this.initialData.application.applicationNr;
        this.apiService.cancelDirectDebitSignatureSession(applicationnr).then(
            (onSuccess) => {
                this.eventService.signalReloadApplication(applicationnr);
            },
            (onError) => {
                this.toastr.error('Could not cancel direct debit signature session. ', 'Error');
            }
        );
    }
}

class Model {
    isDirectDebitEnabled: boolean
    application: StandardCreditApplicationModel;
    areAllStepBeforeThisAccepted: boolean;
    isCreateLoanAllowed: boolean;
    createdLoan?: {
        url: string;
        creditNr: string;
    };
    signatureSession: {
        isCancelAllowed: boolean;
        hasSignatureSessionFailed: boolean;
        unsignedAgreementPdfArchiveKey: string;
        signedAgreementPdfArchiveKey: string;
        applicant: {
            applicantNr: number;
            firstName: string;
            birthDate: string;
            hasSigned: boolean;
        };
    };
    isDirectDebitApproved: boolean;
}
