import { Component, Input, OnInit, SimpleChanges, TemplateRef } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { ToastrService } from 'ngx-toastr';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import {
    CreditReportModel,
    CreditReportProviderModel,
    CreditReportRequest,
    ManualCreditReportsApiService,
} from '../services/manual-creditreports-api.service';

const LocalReasonType = 'ManualCreditReport';

@Component({
    selector: 'app-list-creditreports',
    templateUrl: './list-creditreports.component.html',
    styles: [],
})
export class ListCreditreportsComponent implements OnInit {
    constructor(
        private creditReportService: ManualCreditReportsApiService,
        private apiService: NtechApiService,
        private validationService: NTechValidationService,
        private modalService: BsModalService,
        private toastr: ToastrService,
        private formBuilder: UntypedFormBuilder
    ) {}

    public m: Model;
    public modalRef: BsModalRef;

    @Input()
    public initialData: ListCreditreportsInitialData;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.reload();
    }

    reload() {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let customerId: Promise<{ CustomerId?: number }>;
        if (this.initialData.isCompany) {
            customerId = this.apiService.shared.fetchCustomerIdByOrgnr(this.initialData.civicRegNrOrOrgnr);
        } else {
            customerId = this.apiService.shared.fetchCustomerIdByCivicRegNr(this.initialData.civicRegNrOrOrgnr);
        }

        customerId.then((x) => {
            this.creditReportService.getCreditReports(x.CustomerId, this.initialData.isCompany, 0, 20).then((y) => {
                this.m = {
                    customerId: x.CustomerId,
                    civicRegNrOrOrgnr: this.initialData.civicRegNrOrOrgnr,
                    isCompany: this.initialData.isCompany,
                    creditReports: y.CreditReportsBatch,
                    remainingReportsCount: y.RemainingReportsCount,
                    buyForm: new FormsHelper(
                        this.formBuilder.group({
                            reason: ['', [Validators.required, Validators.maxLength(100)]],
                            providerName: ['', [Validators.required]],
                        })
                    ),
                    providers: this.validationService.clone(this.initialData.providers),
                };
            });
        });
    }

    reset(evt?: Event) {
        evt?.preventDefault();
        this.initialData?.onReset();
    }

    parseRequestDate(report: CreditReportModel): Date {
        return this.validationService.parseDatetimeOffset(report.RequestDate).toDate();
    }

    hasPreview(report: CreditReportModel) {
        return report.HtmlPreviewArchiveKey || report.PdfPreviewArchiveKey || report.HasTableValuesPreview;
    }

    showPreview(report: CreditReportModel, preview: TemplateRef<any>, evt?: Event) {
        evt?.preventDefault();

        if (report.HtmlPreviewArchiveKey) {
            this.apiService.shared.downloadArchiveDocumentData(report.HtmlPreviewArchiveKey).then((blob) => {
                blob.text().then((textData) => {
                    this.m.preview = {
                        title: 'Html preview',
                        html: textData,
                    };
                    this.showModal(preview);
                });
            });
        } else if (report.HasTableValuesPreview) {
            this.creditReportService.fetchTabledValues(report.Id).then((x) => {
                this.m.preview = {
                    title: 'Table preview',
                    table: x.map(y => ({
                        title: y.title,
                        value: y.value,
                        headerLevel: y.title.startsWith('--') && !y.value
                            ? 2
                            : (y.title.startsWith('-') && !y.value ? 1 : null)
                    })).map(y => ({
                        title: y.headerLevel ? y.title.substring(y.headerLevel) : y.title,
                        value: y.value,
                        headerLevel: y.headerLevel
                    })),
                };
                this.showModal(preview);
            });
        } else {
            this.toastr.warning('No preview exists');
        }
    }

    showRawXml(report: CreditReportModel, preview: TemplateRef<any>, evt?: Event) {
        evt?.preventDefault();
        this.apiService.shared.downloadArchiveDocumentData(report.RawXmlArchiveKey).then((blob) => {
            blob.text().then((textData) => {
                this.m.preview = {
                    title: 'Raw xml',
                    pre: textData,
                };
                this.showModal(preview);
            });
        });
    }

    showReason(report: CreditReportModelLocal, evt?: Event) {
        evt?.preventDefault();

        this.creditReportService.fetchReason(report.Id).then((x) => {
            if (!x.ReasonType) {
                report.LocalReasonText = '-';
            } else if (x.ReasonType === LocalReasonType) {
                report.LocalReasonText = x.ReasonData;
            } else {
                report.LocalReasonText = `${x.ReasonType}: ${x.ReasonData}`;
            }
        });
    }

    buyReport(evt?: Event) {
        evt?.preventDefault();

        let request: CreditReportRequest = {
            reasonType: LocalReasonType,
            reasonData: this.m.buyForm.getFormGroupValue(null, 'reason'),
            providerName: this.m.buyForm.getFormGroupValue(null, 'providerName'),
            customerId: this.m.customerId,
            returningItemNames: null,
            additionalParameters: null,
        };

        let result: Promise<boolean>;
        if (this.m.isCompany) {
            result = this.creditReportService
                .buyNewCompanyReport(this.m.civicRegNrOrOrgnr, request)
                .then((x) => x.Success);
        } else {
            result = this.creditReportService
                .buyNewPersonReport(this.m.civicRegNrOrOrgnr, request)
                .then((x) => x.Success);
        }
        result.then((x) => {
            if (x) {
                this.reload();
            } else {
                this.toastr.error('Failed to buy credit report!');
            }
        });
    }

    private showModal(preview: TemplateRef<any>) {
        this.modalRef = this.modalService.show(preview, { class: 'modal-lg', ignoreBackdropClick: true });
    }
}

export class ListCreditreportsInitialData {
    civicRegNrOrOrgnr: string;
    isCompany: boolean;
    providers: CreditReportProviderModel[];
    onReset: () => void;
}

class Model {
    customerId: number;
    civicRegNrOrOrgnr: string;
    isCompany: boolean;
    creditReports?: CreditReportModelLocal[];
    remainingReportsCount?: number;
    providers: CreditReportProviderModel[];
    preview?: {
        title: string;
        html?: string;
        pre?: string;
        table?: { title: string; value: string, headerLevel: number }[];
    };
    buyForm: FormsHelper;
}

interface CreditReportModelLocal extends CreditReportModel {
    LocalReasonText?: string;
}
