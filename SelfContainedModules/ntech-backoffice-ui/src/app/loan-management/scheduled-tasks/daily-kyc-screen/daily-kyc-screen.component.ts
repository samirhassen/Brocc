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
    selector: 'app-daily-kyc-screen',
    templateUrl: './daily-kyc-screen.component.html',
    styles: ``
})
export class DailyKycScreenComponent {
    constructor(private apiService: NtechApiService, private formBuilder: UntypedFormBuilder,
        private config: ConfigService, private validationService: NTechValidationService, private toastr: ToastrService) {
    }

    private instanceId: string

    public m: Model;

    async ngOnInit(): Promise<void> {
        this.instanceId = generateUniqueId(20);
        this.reload();
    }

    public async screenCustomers(evt ?: Event) {
        evt?.preventDefault();

        try {
            await this.apiService.post<{}>('nCredit', 'Api/Kyc/ScreenCustomers', {});
            this.toastr.info('Done');
            await this.reload();
        } catch {
            this.toastr.error('Failed');
        }
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
            triggerInitialData: {
                headerText: 'Trigger job manually',
                useFixedBorder: true,
                isInitiallyExpanded: false,
                toggleBlockId: this.instanceId,
                onExpandedToggled: async (isExpanded) => {
                    if(isExpanded && !this.m.counts) {
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

    private async updateStatus(m: Model) {
        let result = await this.apiService.post<{ UnscreenedCount: number }>('nCredit', 'Api/DailyKycScreen/Status', {});
        m.counts = {
            unscreenedCount: result.UnscreenedCount
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
        return await this.apiService.post<GetFilesResult>('nCredit', 'Api/Kyc/GetFilesPage', {
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
    counts ?: {
        unscreenedCount: number
    }
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
    TransactionDate: number
    NrOfCustomersScreened: number
    NrOfCustomersConflicted: number
    UserId: number
    UserDisplayName: string
}