import { Component } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { TablePagerInitialData } from 'src/app/common-components/table-pager/table-pager.component';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';

@Component({
    selector: 'app-sat-export',
    templateUrl: './sat-export.component.html',
    styles: ``
})
export class SatExportComponent {
    constructor(private apiService: NtechApiService, private config: ConfigService, private formBuilder: UntypedFormBuilder,
        private validationService: NTechValidationService, private toastr: ToastrService) {

    }

    public m: Model

    public getArchiveDocumentUrl(archiveKey: string) {
        return this.apiService.getArchiveDocumentUrl(archiveKey)
    }

    public async search(evt ?: Event)  {
        evt?.preventDefault();

        await this.gotoPage(this.m, 0, this.m.form.getValue('fromDate'), this.m.form.getValue('toDate'));
    }

    public async exportToSat(evt ?: Event) {
        evt?.preventDefault();

        try {
            await this.apiService.post<{}>('nCredit', 'Api/Sat/Export', {});
            await this.reload();
        } catch {
            this.toastr.error('Failed');
        }
    }

    async ngOnInit(): Promise<void> {
        this.reload();
    }

    private async reload() {
        this.m = null;

        let status = await this.getExportStatus();

        let initialFromDate = this.config.getCurrentDateAndTime().add(-10, 'days').format('YYYY-MM-DD');
        let initialToDate = this.config.getCurrentDateAndTime().format('YYYY-MM-DD');

        let m: Model = {
            status: {
                nrOfActiveCredits: status.nrOfActiveCredits,
                exportProfileName: status.exportProfileName
            },
            form:new FormsHelper(this.formBuilder.group({
                'fromDate': [initialFromDate, [Validators.required, this.validationService.getDateOnlyValidator()]],
                'toDate': [initialToDate, [Validators.required, this.validationService.getDateOnlyValidator()]]
            }))
        }

        await this.gotoPage(m, 0, initialFromDate, initialToDate);

        this.m = m;
    }

    private async getExportStatus() {
        return this.apiService.post<{ nrOfActiveCredits: number, exportProfileName: string }>('nCredit', 'Api/SatExport/ExportStatus', {}, { forceCamelCase: true })
    }

    private async gotoPage(m: Model, pageNr: number, fromDate: string, toDate: string) {
        let result = await this.getFilesPage(fromDate, toDate, pageNr);
        m.files = {
            ...result,
            pagingData : {
                currentPageNr: pageNr,
                totalNrOfPages: result.TotalNrOfPages,
                onGotoPage: x => this.gotoPage(m, x, result.Filter.FromDate, result.Filter.ToDate)
            }
        }
    }

    private async getFilesPage(fromDate: string, toDate: string, pageNr: number) {
        return await this.apiService.post<GetFilesResult>('nCredit', 'Api/Sat/GetFilesPage', {
            pageNr: pageNr,
            pageSize: 50,
            filter: {
                fromDate: fromDate,
                toDate: toDate,
            }
        });
    }
}

interface Model {
    status: {
        nrOfActiveCredits: number
        exportProfileName: string
    }
    files ?: LocalFiles
    form: FormsHelper
}

interface LocalFiles extends GetFilesResult{
    pagingData: TablePagerInitialData
}

interface GetFilesResult {
    CurrentPageNr: number
    TotalNrOfPages: number
    Page: GetFilesFile[]
    Filter: {
        FromDate: string,
        ToDate: string
    }
}

interface GetFilesFile {
    TransactionDate: string
    ExportResultStatus: {
        deliveredToProfileNames: string[]
        deliveredToProfileName: string
        errors: string[]
        warnings: string[]
        status: string
    }
    FileArchiveKey: string
    UserDisplayName: string
}