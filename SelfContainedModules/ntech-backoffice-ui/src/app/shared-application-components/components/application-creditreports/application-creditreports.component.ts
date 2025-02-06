import { Component, Input, OnChanges, SimpleChanges, TemplateRef, ViewChild } from '@angular/core';
import { BsModalService } from 'ngx-bootstrap/modal';
import { ToastrService } from 'ngx-toastr';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { getNumberDictionaryValues, NumberDictionary } from 'src/app/common.types';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { SharedApplicationApiService } from '../../services/shared-loan-application-api.service';

@Component({
    selector: 'application-creditreports',
    templateUrl: './application-creditreports.component.html',
})
export class ApplicationCreditreportsComponent implements OnChanges {
    constructor(
        private generalApiService: NtechApiService,
        private toastr: ToastrService,
        private modalService: BsModalService,
        private validationService: NTechValidationService
    ) {}

    @Input() public initialData: ApplicationCreditReportsInitialData;

    public model: Model;
    @ViewChild('preview', { static: true }) previewModal: TemplateRef<any>;
    public modalRef: BsModalRef;

    ngOnChanges(changes: SimpleChanges) {
        if (this.initialData) {
            let applicants: CreditReportApplicantModel[] = [];
            let fieldsToFetch = ['firstName', 'birthDate', 'civicRegNr'];
            this.generalApiService.shared
                .fetchCustomerItemsBulk(getNumberDictionaryValues(this.initialData.customerIds), fieldsToFetch)
                .then((res) => {
                    for (let customerId of getNumberDictionaryValues(this.initialData.customerIds)) {
                        applicants.push({
                            customerId: customerId,
                            civicRegNr: res[customerId]['civicRegNr'],
                            headerText: `${this.getApplicantHeaderText(res[customerId]['firstName'], customerId)}, ${
                                res[customerId]['birthDate']
                            }`,
                            creditReports: [],
                        });
                    }

                    this.model = {
                        isActiveApplication: this.initialData.isActiveApplication,
                        applicants: applicants,
                    };

                    this.loadCreditReports(this.model.applicants);
                });
        } else {
            this.model = null;
            return;
        }
    }

    getApplicantHeaderText(firstName: string, customerId: number) {
        if (!firstName) {
            if (customerId)
                return `Applicant ${Object.keys(this.initialData.customerIds).find(
                    (key) => this.initialData.customerIds[parseInt(key)] === customerId
                )}`;
            else return '-';
        }

        return firstName;
    }

    buyNewReport(applicant: CreditReportApplicantModel) {
        this.initialData.applicationApiService
            .buyCreditReportForStandardApplication(this.initialData.applicationNr, applicant.customerId)
            .then(() => {
                this.loadCreditReports([applicant]);
            });
    }

    loadCreditReports(applicants: CreditReportApplicantModel[]) {
        if (applicants?.length > 0) {
            for (let applicant of applicants) {
                applicant.creditReports = [];
                this.initialData.applicationApiService
                    .findCreditReportsForPrivatePersonCustomer(applicant.customerId)
                    .then((result) => {
                        for (let report of result.CreditReportsBatch) {
                            let date = this.validationService.parseDatetimeOffset(report.RequestDate);
                            applicant.creditReports.push({ date: date.toDate(), creditReportId: report.Id, hasTableValuesPreview: report.HasTableValuesPreview });
                        }
                    });
            }
        }
    }

    showCreditReport(creditReport: Report, evt?: Event) {
        evt?.preventDefault();
        if(creditReport.hasTableValuesPreview) {
            this.initialData.applicationApiService.fetchCreditReportTabledValues(creditReport.creditReportId).then((x) => {
                this.model.preview = {
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
                this.modalRef = this.modalService.show(this.previewModal, {
                    class: 'modal-lg',
                    ignoreBackdropClick: true,
                });
            });
        } else {
            this.initialData.applicationApiService
                .getCreditReport(creditReport.creditReportId, ['htmlReportArchiveKey'])
                .then((x) => {
                    let htmlReportArchiveKey = x.Items?.find((x) => x.Name == 'htmlReportArchiveKey')?.Value;
                    if (htmlReportArchiveKey) {
                        this.generalApiService.shared.downloadArchiveDocumentData(htmlReportArchiveKey).then((blob) => {
                            blob.text().then((textData) => {
                                this.model.preview = {
                                    title: 'Html preview',
                                    html: textData,
                                };
                                this.modalRef = this.modalService.show(this.previewModal, {
                                    class: 'modal-lg',
                                    ignoreBackdropClick: true,
                                });
                            });
                        });
                    } else {
                        this.toastr.warning('Report has no preview');
                    }
                });
        }

    }
}

export class ApplicationCreditReportsInitialData {
    customerIds: NumberDictionary<number>;
    applicationNr: string;
    isActiveApplication: boolean;
    applicationApiService: SharedApplicationApiService;
}

class Model {
    isActiveApplication: boolean;
    applicants: CreditReportApplicantModel[];
    preview?: {
        title: string;
        html?: string;
        pre?: string;
        table?: { title: string; value: string, headerLevel: number }[];
    };
}

class CreditReportApplicantModel {
    headerText: string;
    customerId: number;
    civicRegNr: string;
    creditReports: Report[];
}
interface Report { date: Date; creditReportId: number, hasTableValuesPreview: boolean }