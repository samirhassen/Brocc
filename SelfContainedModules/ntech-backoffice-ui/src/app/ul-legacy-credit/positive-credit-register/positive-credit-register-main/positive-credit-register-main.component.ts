import { Component, OnInit, Renderer2 } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { PositiveCreditRegisterService } from '../positive-credit-register.service';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { ToastrService } from 'ngx-toastr';
import * as moment from 'moment';
import { ConfigService } from 'src/app/common-services/config.service';

@Component({
    selector: 'app-positive-credit-register-main',
    templateUrl: './positive-credit-register-main.component.html',
    styles: [],
})
export class PositiveCreditRegisterMainComponent implements OnInit {
    constructor(
        private formBuilder: UntypedFormBuilder,
        private positiveCreditRegisterService: PositiveCreditRegisterService,
        private validationService: NTechValidationService,
        private toastrService: ToastrService,
        private configService: ConfigService,
        private renderer : Renderer2
    ) {}

    public m: Model;
    public nrOfLogsToFetch: number = 50;

    async ngOnInit() {
        const logs = await this.positiveCreditRegisterService.fetchLogs(this.nrOfLogsToFetch);
        const m: Model = {
            fetchGetLoanModel: {
                searchForm: new FormsHelper(
                    this.formBuilder.group({
                        searchText: ['', [Validators.required]],
                    })
                ),
                getLoanRawResponse: null,
            },
            exportBatchModel: {
                batchDatesForm: new FormsHelper(
                    this.formBuilder.group({
                        fromDate: [null, [Validators.required, this.validationService.getDateOnlyValidator()]],
                        toDate: [null, [Validators.required, this.validationService.getDateOnlyValidator()]],
                        isFirstTimeExport: [false],
                    })
                ),
            },
            batchStatusLogs: logs.statusLogs,
            batchExportLogs: logs.exportLogs.map(x => ({ batchReference: x })),
            exportRawResponse: null,
        };

        this.m = m;
    }

    async onSearch(evt?: Event) {
        evt?.preventDefault();
        if (this.m.fetchGetLoanModel.searchForm.invalid()) {
            return;
        }
        const loanNr = this.m.fetchGetLoanModel.searchForm.getValue('searchText');
        const loan = await this.positiveCreditRegisterService.fetchGetLoan(loanNr);
        this.m.fetchGetLoanModel.getLoanRawResponse = JSON.parse(loan.rawResponse);
    }

    resetGetLoanSearch(evt?: Event) {
        evt?.preventDefault();
        this.m.fetchGetLoanModel.getLoanRawResponse = null;
        this.m.fetchGetLoanModel.searchForm.form.reset({
            searchText: '',
        });
    }

    resetBatchExportFields(evt?: Event) {
        evt?.preventDefault();
        this.m.exportBatchModel.batchDatesForm.form.reset();
        this.m.exportRawResponse = null;
    }

    async onExportBatch(evt?: Event) {
        evt?.preventDefault();

        if (this.m.exportBatchModel.batchDatesForm.invalid() || !this.isValidExportDates()) {
            this.toastrService.error('Invalid dates. Dates have to be after yesterday.');
            return;
        }

        const fromDate = this.m.exportBatchModel.batchDatesForm.getValue('fromDate');
        const toDate = this.m.exportBatchModel.batchDatesForm.getValue('toDate');

        const isFirstExportChecked = this.m.exportBatchModel.batchDatesForm.getValue('isFirstTimeExport');

        await this.positiveCreditRegisterService
            .exportBatch(fromDate, toDate, isFirstExportChecked)
            .then((x) => {
                this.toastrService.info(`SuccessCount: ${x.successCount}`);
                this.m.exportRawResponse = x.rawResponse;
            })
            .catch((_) => this.toastrService.error('Something went wrong. Contact support.'));
    }

    isValidExportDates(): boolean {
        const fromDate = moment(this.m.exportBatchModel.batchDatesForm.getValue('fromDate'));
        const toDate = moment(this.m.exportBatchModel.batchDatesForm.getValue('toDate'));

        const yesterday = moment(this.configService.getCurrentDateAndTime()).subtract(1, 'days');

        if (fromDate.isAfter(yesterday) || toDate.isAfter(yesterday)) {
            return false;
        }

        return true;
    }

    async toggleExportLogItemDetails(logItem: ExportLogItem, evt ?: Event) {
        evt?.preventDefault();

        if(!logItem.details) {
            let result = await this.positiveCreditRegisterService.fetchSingleBatchLogs(logItem.batchReference);
            logItem.details = {
                logFiles: result.logFiles.map(x => ({
                    logDate: x.logDate,
                    logFilename: x.logFileName
                }))
            }
        }
        logItem.isExpanded = !logItem.isExpanded;
    }

    async downloadLogFile(logItem: ExportLogItem,  logFilename: string, evt ?: Event) {
        evt?.preventDefault();
        let fileContent = await this.positiveCreditRegisterService.fetchSingleBatchLogFileContent(logItem.batchReference, logFilename);

        const url = (window.URL || window.webkitURL).createObjectURL(fileContent);
        const link = this.renderer.createElement('a');
        this.renderer.setAttribute(link, 'download', logFilename);
        this.renderer.setAttribute(link, 'href', url);
        this.renderer.setAttribute(link, 'target', '_blank');
        this.renderer.appendChild(document.body, link);
        link.click();
        this.renderer.removeChild(document.body, link);
    }
}

class Model {
    fetchGetLoanModel: FetchGetLoanModel;
    exportBatchModel: ExportBatchModel;
    batchStatusLogs: string[];
    batchExportLogs: ExportLogItem[];
    exportRawResponse: string;
}

class FetchGetLoanModel {
    searchForm: FormsHelper;
    getLoanRawResponse: string;
}

class ExportBatchModel {
    batchDatesForm: FormsHelper;
}

interface ExportLogItem {
    batchReference: string
    isExpanded?: boolean
    details ?: {
        logFiles : {
            logDate: string
            logFilename: string
            url ?: string
        }[]
    }
}
