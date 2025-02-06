import { Component } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { TablePagerInitialData } from 'src/app/common-components/table-pager/table-pager.component';
import { ToggleBlockInitialData } from 'src/app/common-components/toggle-block/toggle-block.component';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { generateUniqueId } from 'src/app/common.types';

@Component({
  selector: 'app-book-keeping',
  templateUrl: './book-keeping.component.html',
  styles: ``
})
export class BookKeepingComponent {
    constructor(private apiService: NtechApiService, private formBuilder: UntypedFormBuilder,
        private config: ConfigService, private validationService: NTechValidationService, private toastr: ToastrService) {
    }

    private instanceId: string

    public m: Model;

    async ngOnInit(): Promise<void> {
        this.instanceId = generateUniqueId(20);
        this.reload();
    }

    public async search(evt ?: Event) {
        evt?.preventDefault();

        await this.gotoPage(this.m, 0, this.m.form.getValue('fromDate'), this.m.form.getValue('toDate'));
    }

    private async reload() {
        this.m = null;

        let initialFromDate = this.config.getCurrentDateAndTime().add(-10, 'days').format('YYYY-MM-DD');
        let initialToDate = this.config.getCurrentDateAndTime().format('YYYY-MM-DD');

        let m: Model = {
            rulesUiUrl: this.apiService.getUiGatewayUrl('nCredit', 'Ui/BookKeeping/EditRules'),
            rulesXlsUrl: this.apiService.getUiGatewayUrl('nCredit', 'Api/Bookkeeping/RulesAsXls'),
            triggerInitialData: {
                headerText: 'Trigger job manually',
                useFixedBorder: true,
                isInitiallyExpanded: false,
                toggleBlockId: this.instanceId,
                onExpandedToggled: async (isExpanded) => {
                    if(isExpanded && !this.m.pending) {
                        await this.updateStatus(this.m);
                    }
                }
            },
            form:new FormsHelper(this.formBuilder.group({
                'fromDate': [initialFromDate, [Validators.required, this.validationService.getDateOnlyValidator()]],
                'toDate': [initialToDate, [Validators.required, this.validationService.getDateOnlyValidator()]]
            }))
        }

        await this.gotoPage(m, 0, initialFromDate, initialToDate);

        this.m = m;
    }

    public async createBookKeeping(evt ?: Event ){
        evt?.preventDefault();

        let result = await this.apiService.post<{noNewTransactions: boolean, warnings: string[]}>('nCredit', 'Api/BookkeepingFiles/CreateFile', {});

        if (result.noNewTransactions) {
            this.toastr.warning('No new transactions exist');
            return;
        }

        let hasWarnings = result.warnings?.length > 0;
        if(hasWarnings) {
            this.toastr.warning(result.warnings[0]);
        }

        setTimeout(() => {
            this.toastr.info('File created');
            this.reload();
        }, hasWarnings ? 2000 : 0); //allow some time to see the warning
    }

    getArchiveDocumentUrl(archiveKey: string) {
        return this.apiService.getArchiveDocumentUrl(archiveKey)
    }

    private async updateStatus(m: Model) {
        let result = await this.apiService.post<{ Dates: string[], ExportProfileName:string }>('nCredit', 'Api/BookkeepingFiles/ExportStatus', {});
        m.pending = {
            dates: result.Dates ?? [],
            exportProfileName: result.ExportProfileName
        };

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
        return await this.apiService.post<GetFilesResult>('nCredit', 'Api/BookkeepingFiles/GetFilesPage', {
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
    pending ?: {
        dates: string[]
        exportProfileName: string
    }
    rulesUiUrl: string
    rulesXlsUrl: string
    files ?: LocalFiles
    form: FormsHelper
    triggerInitialData: ToggleBlockInitialData
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
    FromTransactionDate: string
    ToTransactionDate: string
    FileArchiveKey: string
    XlsFileArchiveKey: string
    UserDisplayName: string
}
