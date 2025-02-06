import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import {
    AgreementTaskModel,
    CustomerPagesApplicationsApiService,
} from '../../services/customer-pages-applications-api.service';

@Component({
    selector: 'customer-pages-agreement',
    templateUrl: './customer-pages-agreement.component.html',
    styles: [],
})
export class CustomerPagesAgreementComponent implements OnInit {
    constructor(private apiService: CustomerPagesApplicationsApiService) {}

    @Input()
    public initialData: CustomerPagesAgreementInitialData;

    public m: Model;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let t = this.initialData.task;

        let m: Model = {
            signedAgreementPdfArchiveKey: t.SignedAgreementArchiveKey,
            unsignedAgreementPdfArchiveKey: t.UnsignedAgreementArchiveKey,
            applicants: [],
        };

        let nrOfApplicants = Object.keys(t.ApplicantStatusByApplicantNr).length;
        for (var applicantNr = 1; applicantNr <= nrOfApplicants; applicantNr++) {
            let applicantStatus = t.ApplicantStatusByApplicantNr[applicantNr];
            let applicantDisplayName =
                (applicantStatus.CustomerShortName || `Sökande ${applicantNr}`) +
                (applicantStatus.CustomerBirthDate ? `, ${applicantStatus.CustomerBirthDate}` : '');
            m.applicants.push({
                applicantDisplayName: applicantDisplayName,
                applicantNr: applicantNr,
                hasSigned: applicantStatus.HasSigned,
                signatureUrl: applicantStatus.IsPossibleToSign ? applicantStatus.SignatureUrl : null,
            });
        }
        this.m = m;
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

    getDocumentUrl(archiveKey: string) {
        return this.apiService.getArchiveDocumentUrl(archiveKey, true);
    }
}

class Model {
    unsignedAgreementPdfArchiveKey: string;
    signedAgreementPdfArchiveKey: string;
    applicants: { applicantNr: number; applicantDisplayName: string; hasSigned: boolean; signatureUrl: string }[];
}

export class CustomerPagesAgreementInitialData {
    applicationNr: string;
    task: AgreementTaskModel;
}
