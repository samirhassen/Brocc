import { Component } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { TablePagerInitialData } from 'src/app/common-components/table-pager/table-pager.component';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';

@Component({
    selector: 'app-trapets-aml-export',
    templateUrl: './trapets-aml-export.component.html',
    styles: ``
})
export class TrapetsAmlExportComponent {
    constructor(private apiService: NtechApiService, private formBuilder: UntypedFormBuilder, private config: ConfigService,
        private validationService: NTechValidationService, private toastr: ToastrService) {

    }

    async ngOnInit(): Promise<void> {
        this.reload();
    }

    public m: Model


    public async search(evt ?: Event) {
        evt?.preventDefault();

        await this.gotoPage(this.m, 0, this.m.form.getValue('fromDate'), this.m.form.getValue('toDate'));
    }

    public async export(evt ?: Event) {
        evt?.preventDefault();

        try {
            await this.apiService.post<{}>('nCredit', 'Api/TrapetsAml/Export', {});
            this.toastr.info('Done');
            await this.reload();
        } catch {
            this.toastr.error('Failed');
        }
    }

    public getArchiveDocumentUrl(archiveKey: string) {
        return this.apiService.getArchiveDocumentUrl(archiveKey)
    }

    private async reload() {
        this.m = null;

        let status = await this.getExportStatus();

        let initialFromDate = this.config.getCurrentDateAndTime().add(-10, 'days').format('YYYY-MM-DD');
        let initialToDate = this.config.getCurrentDateAndTime().format('YYYY-MM-DD');


        let m: Model = {
            status: {
                exportProfileName: status.exportProfileName
            },
            form:new FormsHelper(this.formBuilder.group({
                'fromDate': [initialFromDate, [Validators.required, this.validationService.getDateOnlyValidator()]],
                'toDate': [initialToDate, [Validators.required, this.validationService.getDateOnlyValidator()]]
            }))
        };

        await this.gotoPage(m, 0, initialFromDate, initialToDate);

        this.m = m;
    }

    private async getExportStatus() {
        return this.apiService.post<{ nrOfActiveCredits: number, exportProfileName: string }>('nCredit', 'Api/TrapetsAmlExport/ExportStatus', {}, { forceCamelCase: true })
    }

    private async gotoPage(m: Model, pageNr: number, fromDate: string, toDate: string) {
        let result = await this.getFilesPage(fromDate, toDate, pageNr);
        m.files = {
            ...result,
            pagingData : {
                currentPageNr: pageNr,
                totalNrOfPages: result.TotalNrOfPages,
                onGotoPage: x => this.gotoPage(m, x, fromDate, toDate)
            }
        }
    }

    private async getFilesPage(fromDate: string, toDate: string, pageNr: number) {
        return await this.apiService.post<GetFilesResult>('nCredit', 'Api/TrapetsAml/GetFilesPage', {
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
        exportProfileName: string
    }
    form: FormsHelper
    files ?: LocalFiles
}

interface LocalFiles extends GetFilesResult{
    pagingData: TablePagerInitialData
}

interface GetFilesResult {
    CurrentPageNr: number
    TotalNrOfPages: number
    Page: GetFilesFile[]
}

interface GetFilesFile {

    TransactionDate: string
    ExportResultStatus: {
        deliveredToProfileName: string
        errors: string[]
        warnings: string[]
        status: string
    }
    FileArchiveKey: string
    UserDisplayName: string
}