import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { FileInputEventTarget } from 'src/app/common.types';
import {
    MortgageLoanApplicationApiService,
    ServerDocumentModel,
} from '../../services/mortgage-loan-application-api.service';
import {
    getPossibleDocumentsForApplication,
    MortageLoanApplicationDocumentType,
    MortgageApplicationDocumentType,
} from './application-documents-model';

@Component({
    selector: 'app-application-documents',
    templateUrl: './application-documents.component.html',
    styleUrls: ['./application-documents.component.scss'],
})
export class ApplicationDocumentsComponent implements OnInit {
    constructor(
        private route: ActivatedRoute,
        private apiService: MortgageLoanApplicationApiService,
        private toastr: ToastrService
    ) {}

    public m: Model;

    @ViewChild('fileInput')
    fileInput: ElementRef<HTMLInputElement>;

    @ViewChild('fileInputForm')
    fileInputForm: ElementRef<HTMLFormElement>;

    public async removeDocument(document: DocumentModel, evt?: Event) {
        evt?.preventDefault();
        await this.apiService.removeApplicationDocument(this.m.applicationNr, document.serverDocument.DocumentId);
        document.serverDocument = null;
        document.serverDocumentArchiveUrl = null;
    }

    public toggleVerified(document: DocumentModel) {
        this.apiService
            .setApplicationDocumentVerified(
                this.m.applicationNr,
                document.serverDocument.DocumentId,
                !document.serverDocument.VerifiedDate
            )
            .then((x) => {
                document.serverDocument = x;
            });
    }

    public selectDocumentToAttach(document: DocumentModel, evt?: Event) {
        evt?.preventDefault();

        this.m.attach = {
            document: document,
        };
        this.fileInput.nativeElement.click();
    }

    public onDocumentAttached(evt?: Event) {
        let target: FileInputEventTarget = (evt as any).target;
        FormsHelper.loadSingleAttachedFileAsDataUrl(target.files).then(
            (x) => {
                this.fileInputForm.nativeElement.reset();

                this.apiService
                    .addApplicationDocument(
                        this.m.applicationNr,
                        MortageLoanApplicationDocumentType,
                        this.m.attach.document.documentType.displayName,
                        x.dataUrl,
                        x.filename
                    )
                    .then((serverDocument) => {
                        let document = this.m.attach.document;
                        this.m.attach = null;
                        document.serverDocumentArchiveUrl = this.apiService
                            .api()
                            .getArchiveDocumentUrl(serverDocument.DocumentArchiveKey, true);
                        document.serverDocument = serverDocument;
                    });
            },
            (x) => {
                this.toastr.error(x);
                this.m.attach = null;
                this.fileInputForm.nativeElement.reset();
            }
        );
    }

    async ngOnInit() {
        let applicationNr = this.route.snapshot.params['applicationNr'] as string;
        let applicationModel = await this.apiService.fetchApplicationInitialData(applicationNr);
        if (applicationModel === 'noSuchApplicationExists') {
            return;
        }

        let m: Model = {
            applicationNr: applicationNr,
            documents: [],
            isReadOnly: !applicationModel.applicationInfo.IsActive,
        };

        let documents = await this.apiService.fetchApplicationDocuments(applicationModel.applicationNr, [
            MortageLoanApplicationDocumentType,
        ]);

        let sortedDocuments = documents.sort((x) => -x.DocumentId);
        for (let documentType of getPossibleDocumentsForApplication(applicationModel)) {
            let serverDocument = sortedDocuments.find((x) => x.DocumentSubType === documentType.displayName);
            m.documents.push({
                documentType: documentType,
                serverDocument: serverDocument,
                serverDocumentArchiveUrl: serverDocument
                    ? this.apiService.api().getArchiveDocumentUrl(serverDocument.DocumentArchiveKey, true)
                    : null,
            });
        }

        this.m = m;
    }
}

class Model {
    applicationNr: string;
    documents: DocumentModel[];
    isReadOnly: boolean;
    attach?: {
        document: DocumentModel;
        file?: {
            name: string;
            dataUrl: string;
        };
    };
}

interface DocumentModel {
    documentType: MortgageApplicationDocumentType;
    serverDocument: ServerDocumentModel;
    serverDocumentArchiveUrl: string;
}
