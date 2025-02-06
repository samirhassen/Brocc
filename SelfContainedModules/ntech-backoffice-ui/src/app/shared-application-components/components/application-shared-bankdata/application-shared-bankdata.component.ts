import { Component, Input, SimpleChanges, TemplateRef, ViewChild } from '@angular/core';
import { StandardApplicationModelBase } from '../../services/standard-application-base';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';

@Component({
    selector: 'application-shared-bankdata',
    templateUrl: './application-shared-bankdata.component.html',
    styles: [
    ]
})
export class ApplicationSharedBankdataComponent {
    constructor(private modalService: BsModalService, private apiService: NtechApiService, private validationService: NTechValidationService) { }

    public m: Model;

    @Input()
    public initialData: ApplicationSharedBankdataComponentInitialData

    @ViewChild('preview', { static: true }) previewModal: TemplateRef<any>;
    public modalRef: BsModalRef;

    async ngOnChanges(_: SimpleChanges) {
        this.m = null;
        if(!this.initialData) {
            return;
        }

        let m: Model = {
            applicants: []
        }

        let a = this.initialData.application;

        for(let applicant of a.getApplicantNrs().map(applicantNr => a.getComplexApplicationList('Applicant', true).getRow(applicantNr, true))) {
            let applicantData : ApplicantModel = {
                headerText: `Applicant ${applicant.nr}`,
                dataSets: []
            };
            let dataShareProviderName = applicant.getUniqueItem('dataShareProviderName');
            if(dataShareProviderName) {
                let dataSet: DataSet = {
                    date: applicant.getUniqueItem('dataShareDate'),
                    archiveKey: applicant.getUniqueItem('dataShareArchiveKey'),
                    providerName: dataShareProviderName,
                    tableItems: []
                };
                dataSet.tableItems.push({
                    title: `Session id from ${dataShareProviderName}`,
                    value: applicant.getUniqueItem('dataShareSessionId')
                });
                dataSet.tableItems.push({
                    title: 'Left to live on',
                    value: applicant.getUniqueItem('dataShareLtlAmount')
                });
                dataSet.tableItems.push({
                    title: 'Income',
                    value: applicant.getUniqueItem('dataShareIncomeAmount')
                });
                dataSet.tableItems = dataSet.tableItems.filter(x => !!x.value);
                applicantData.dataSets.push(dataSet);
            }
            m.applicants.push(applicantData)
        }

        this.m = m;
    }

    public async showDataSet(dataSet: DataSet, evt ?: Event) {
        evt?.preventDefault();
        let preData: string = undefined;
        let scoringData : string = undefined;
        if(dataSet.archiveKey) {
            let archiveData = await this.apiService.shared.downloadArchiveDocumentData(dataSet.archiveKey);
            let rawData = await archiveData.text();
            preData = this.validationService.formatJsonForDisplay(rawData);
            scoringData = this.parseScoringData(dataSet, preData);
        }
        this.m.preview = {
            title: 'Preview',
            table: dataSet.tableItems,
            rawData: dataSet.archiveKey ? {
                downloadUrl: this.apiService.getArchiveDocumentUrl(dataSet.archiveKey),
                data: preData
            } : null,
            scoringData: scoringData ? {
                data: scoringData
            } : null
        }
        this.modalRef = this.modalService.show(this.previewModal, {
            class: 'modal-lg',
            ignoreBackdropClick: true,
        });
    }

    private parseScoringData(dataSet: DataSet, rawData: string) {
        if(dataSet.providerName !== 'kreditz') {
            return undefined;
        }
        try {
            return this.validationService.formatJsonForDisplay(JSON.stringify(JSON.parse(rawData).alta));
        } catch {
            return undefined;
        }
    }
}

interface Model {
    applicants: ApplicantModel[];
    preview?: {
        title: string;
        html?: string;
        scoringData?: {
            data: string
        };
        rawData?: {
            downloadUrl ?: string
            data: string
        };
        table?: { title: string; value: string }[];
    };
}

export interface ApplicationSharedBankdataComponentInitialData {
    application: StandardApplicationModelBase
}

interface ApplicantModel {
    headerText: string;
    dataSets: DataSet[];
}

interface DataSet {
    date: string;
    archiveKey: string,
    providerName: string,
    tableItems: { title: string, value: string }[]
 }
