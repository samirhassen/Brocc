import { Component } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { TablePagerInitialData } from 'src/app/common-components/table-pager/table-pager.component';
import { ToggleBlockInitialData } from 'src/app/common-components/toggle-block/toggle-block.component';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { generateUniqueId } from 'src/app/common.types';

@Component({
    selector: 'app-termination-letters',
    templateUrl: './termination-letters.component.html',
    styles: ``
})
export class TerminationLettersComponent {
    constructor(private apiService: NtechApiService, private toastr: ToastrService) {

    }

    private instanceId: string

    async ngOnInit(): Promise<void> {
        this.instanceId = generateUniqueId(20);
        this.reload();
    }

    private async reload() {
        this.m = null;

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
            }
        }

        await this.gotoPage(m, 0);

        this.m = m;
    }

    async createTerminationLetters(evt ?: Event) {
        evt?.preventDefault();

        try {
            await this.apiService.post<{}>('nCredit', 'Api/Credit/CreateTerminationLetters', {});
            this.toastr.info('File created');
            await this.updateStatus(this.m);
            await this.gotoPage(this.m, 0);
        } catch(err: any) {
            this.toastr.error('Failed');
        }
    }

    getArchiveDocumentUrl(archiveKey: string) {
        return this.apiService.getArchiveDocumentUrl(archiveKey)
    }

    private async updateStatus(m: Model) {
        let status = await this.getTerminationLetterStatus();
        m.counts = {
            eligableCount: status.eligableCount
        }
    }

    private async gotoPage(m: Model, pageNr: number) {
        let result = await this.getFilesPage(pageNr);
        m.files = {
            ...result,
            pagingData : {
                currentPageNr: pageNr,
                totalNrOfPages: result.TotalNrOfPages,
                onGotoPage: x => this.gotoPage(m, x)
            }
        }
    }

    private getTerminationLetterStatus() {
        return this.apiService.post<{ eligableCount: number }>('nCredit', 'Api/Credit/TerminationLetterStatus', {})
    }

    private getFilesPage(pageNr: number) {
        return this.apiService.post<GetFilesResult>('nCredit', 'Api/Credit/GetTerminationLetterFilesPage', { pageNr, pageSize: 50 })
    }

    public m: Model
}

interface Model {
    counts ?: {
        eligableCount: number
    }
    triggerInitialData: ToggleBlockInitialData
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
    LettersCount: number
    UserId: number
    UserDisplayName: number
    FileArchiveKey: string
}