import { Component, ElementRef, Input, OnInit, SimpleChanges, ViewChild } from '@angular/core';
import * as moment from 'moment';
import { ToastrService } from 'ngx-toastr';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { FileInputEventTarget, generateUniqueId } from 'src/app/common.types';
import { StandardLoanApplicationDocumentModel } from 'src/app/shared-application-components/services/standard-application-base';
import { StandardCreditApplicationModel } from '../../services/standard-credit-application-model';
import { UnsecuredLoanApplicationApiService } from '../../services/unsecured-loan-application-api.service';
import { WorkflowStepInitialData } from '../workflow-step';

@Component({
    selector: 'agreement-standard',
    templateUrl: './agreement-standard.component.html',
    styles: [],
})
export class AgreementStandardComponent implements OnInit {
    constructor(
        private apiService: UnsecuredLoanApplicationApiService,
        private generalApiService: NtechApiService,
        private eventService: NtechEventService,
        private toastr: ToastrService
    ) {}

    @Input()
    public initialData: WorkflowStepInitialData;

    @ViewChild('fileInput')
    fileInput: ElementRef<HTMLInputElement>;

    @ViewChild('fileInputForm')
    fileInputForm: ElementRef<HTMLFormElement>;

    public m: Model;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let application = this.initialData.application;
        let ai = application.applicationInfo;

        let isActiveAndCurrent =
            ai.IsActive &&
            this.initialData.workflow.step.areAllStepBeforeThisAccepted() &&
            this.initialData.workflow.step.isStatusInitial();

        let applicantInfoByApplicantNr = application.applicantInfoByApplicantNr;

        let signatureSessionRow = application
            .getComplexApplicationList('AgreementSignatureSession', true)
            .getRow(1, true);

        let hasSignatureSessionFailed = signatureSessionRow.getUniqueItem('IsSessionFailed') === 'true';
        let signedByApplicantNrs = signatureSessionRow.getRepeatedItems('SignedByApplicantNr').map((x) => parseInt(x));
        let signedAgreement = application.getApplicationDocuments().find((x) => x.DocumentType === 'SignedAgreement');
        let haveAllApplicantsSigned = signedByApplicantNrs.length === application.nrOfApplicants;

        let m: Model = {
            application: application,
            signatureSession: ai.HasLockedAgreement
                ? {
                      isCancelAllowed: ai.HasLockedAgreement && isActiveAndCurrent,
                      haveAllApplicantsSigned: haveAllApplicantsSigned,
                      hasSignatureSessionFailed: hasSignatureSessionFailed,
                      unsignedAgreementPdfArchiveKey: signatureSessionRow.getUniqueItem(
                          'UnsignedAgreementPdfArchiveKey'
                      ),
                      signedAgreementPdfArchiveKey: signatureSessionRow.getUniqueItem('SignedAgreementPdfArchiveKey'),
                      applicants: application.getApplicantNrs().map((applicantNr) => {
                          return {
                              applicantNr: applicantNr,
                              customerId: applicantInfoByApplicantNr[applicantNr].CustomerId,
                              firstName: applicantInfoByApplicantNr[applicantNr].FirstName,
                              birthDate: applicantInfoByApplicantNr[applicantNr].BirthDate,
                              hasSigned: !hasSignatureSessionFailed && signedByApplicantNrs.indexOf(applicantNr) >= 0,
                          };
                      }),
                  }
                : null,
            sessionCreation: !ai.HasLockedAgreement && isActiveAndCurrent && !signedAgreement ? {} : null,
            isPossibleToApprove: isActiveAndCurrent && !!signedAgreement,
            isPossibleToRevert: this.initialData.workflow.step.isRevertable(),
            documents: ai.HasLockedAgreement
                ? null
                : {
                      isEditAllowed: isActiveAndCurrent,
                      isEditing: false,
                      signedAgreement: signedAgreement,
                  },
            unsignedAgreementCreationLink:
                !ai.HasLockedAgreement && isActiveAndCurrent
                    ? this.generalApiService.getUiGatewayUrl(
                          'nPreCredit',
                          'api/UnsecuredLoanStandard/Create-Agreement-Pdf',
                          [
                              ['ApplicationNr', ai.ApplicationNr],
                              ['DownloadFilename', `credit-agreement-${application.applicationNr}.pdf`],
                              ['RedirectToAuthorize', 'True'],
                          ]
                      )
                    : null,
        };

        this.m = m;

        this.addTestFunctions();
    }

    addTestFunctions() {
        let tf = this.initialData.testFunctions;
        if (this.m.sessionCreation) {
            tf.addFunctionCall('Attach test agreement', () => {
                let dataUrl = tf.generateTestPdfDataUrl('Test agreement: ' + moment().toISOString());
                this.m.sessionCreation.attachedFile = {
                    dataUrl: dataUrl,
                    name: 'testagreement-' + generateUniqueId(10) + '.pdf',
                };
            });
        }
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

    cancel(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.m.application.applicationNr;
        this.apiService.cancelAgreementSignatureSession(applicationNr).then(
            (onSuccess) => {
                this.eventService.signalReloadApplication(applicationNr);
            },
            (onError) => {
                this.toastr.error('Could not cancel agreement signature session', 'Error');
            }
        );
    }

    getDocumentUrl(archiveKey: string) {
        return this.generalApiService.getArchiveDocumentUrl(archiveKey);
    }

    selectFileToAttach(evt: Event) {
        evt?.preventDefault();

        this.m.attachContext = 'createLink';
        this.fileInput.nativeElement.click();
    }

    onFileAttached(evt: Event) {
        let target: FileInputEventTarget = (evt as any).target;
        FormsHelper.loadSingleAttachedFileAsDataUrl(target.files).then(
            (x) => {
                let attachedFile = {
                    name: x.filename,
                    dataUrl: x.dataUrl,
                };
                if (this.m.attachContext === 'createLink') {
                    this.m.sessionCreation.attachedFile = attachedFile;
                } else if (this.m.attachContext === 'attachSignedAgreement') {
                    this.m.documents.attachedFile = attachedFile;
                }

                this.m.attachContext = null;
                this.fileInputForm.nativeElement.reset();
            },
            (x) => {
                this.toastr.error(x);
                this.m.attachContext = null;
                this.fileInputForm.nativeElement.reset();
            }
        );
    }

    removeDocument(evt?: Event) {
        evt?.preventDefault();
        this.m.sessionCreation.attachedFile = null;
    }

    createLink(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.m.application.applicationNr;
        this.apiService
            .createAgreementSignatureSessionWithDataUrl(
                applicationNr,
                this.m.sessionCreation.attachedFile.dataUrl,
                this.m.sessionCreation.attachedFile.name
            )
            .then((x) => {
                this.eventService.signalReloadApplication(applicationNr);
            })
            .catch((_) => this.toastr.error('Error: Could not create signature link for the attached file.'));
    }

    approve(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.m.application.applicationNr;
        this.apiService.setIsApprovedAgreementStep(applicationNr, true).then((x) => {
            this.eventService.signalReloadApplication(applicationNr);
        });
    }

    revert(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.m.application.applicationNr;
        this.apiService.setIsApprovedAgreementStep(applicationNr, false).then((x) => {
            this.eventService.signalReloadApplication(applicationNr);
        });
    }

    beginEditDocuments(evt?: Event) {
        evt?.preventDefault();

        this.m.documents.isEditing = true;
    }

    cancelEditDocuments(evt?: Event) {
        evt?.preventDefault();

        this.m.documents.isEditing = false;
        this.m.documents.attachedFile = null;
        this.m.documents.isPendingRemoval = false;
    }

    commitEditDocuments(evt?: Event) {
        evt?.preventDefault();

        let applicationNr = this.m.application.applicationNr;
        if (this.m.documents.isPendingRemoval) {
            if (this.m?.signatureSession?.isCancelAllowed && this.m.signatureSession.haveAllApplicantsSigned) {
                //The user has no longer actually signed this document
                //Its fine to leave it if not everyone has signed since when the last user signs the agreement will be overwritten
                this.apiService.cancelAgreementSignatureSession(applicationNr).then(
                    (onSuccess) => {
                        this.eventService.signalReloadApplication(applicationNr);
                    },
                    (onError) => {
                        this.toastr.error('Could not cancel direct debit signature session. ', 'Error');
                    }
                );
            } else {
                this.apiService.removeSignedAgreementManually(applicationNr).then((x) => {
                    this.eventService.signalReloadApplication(applicationNr);
                });
            }
        } else if (this.m.documents.attachedFile) {
            let d = this.m.documents.attachedFile;
            this.apiService.attachSignedAgreementManually(applicationNr, d.dataUrl, d.name).then((x) => {
                this.eventService.signalReloadApplication(applicationNr);
            });
        }
    }

    removeEditDocument(evt?: Event) {
        evt?.preventDefault();

        this.m.documents.isPendingRemoval = true;
    }

    attachEditDocument(evt?: Event) {
        evt?.preventDefault();

        this.m.attachContext = 'attachSignedAgreement';
        this.fileInput.nativeElement.click();
    }
}

class Model {
    application: StandardCreditApplicationModel;
    signatureSession: {
        isCancelAllowed: boolean;
        hasSignatureSessionFailed: boolean;
        haveAllApplicantsSigned: boolean;
        unsignedAgreementPdfArchiveKey: string;
        signedAgreementPdfArchiveKey: string;
        applicants: {
            applicantNr: number;
            customerId: number;
            firstName: string;
            birthDate: string;
            hasSigned: boolean;
        }[];
    };
    sessionCreation: {
        attachedFile?: {
            name: string;
            dataUrl: string;
        };
    };
    documents: {
        isEditAllowed: boolean;
        isEditing: boolean;
        signedAgreement: StandardLoanApplicationDocumentModel;
        attachedFile?: {
            name: string;
            dataUrl: string;
        };
        isPendingRemoval?: boolean;
    };
    isPossibleToApprove: boolean;
    isPossibleToRevert: boolean;
    attachContext?: string;
    unsignedAgreementCreationLink: string;
}
