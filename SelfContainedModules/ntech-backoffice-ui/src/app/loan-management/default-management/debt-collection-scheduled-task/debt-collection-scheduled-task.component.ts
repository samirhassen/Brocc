import { Component, OnInit } from '@angular/core';
import { JobrunnerTaskResultModel } from 'src/app/common-components/jobrunner-task-result/jobrunner-task-result.component';
import { TablePagerInitialData } from 'src/app/common-components/table-pager/table-pager.component';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';

@Component({
    selector: 'app-debt-collection-scheduled-task',
    templateUrl: './debt-collection-scheduled-task.component.html',
    styles: [],
})
export class DebtCollectionScheduledTaskComponent implements OnInit {
    constructor(private apiService: NtechApiService) {
        this.gotoPage = this.gotoPage.bind(this);
    }

    m: Model;

    ngOnInit(): void {
        this.apiService
            .post<{ eligableForDebtCollectionCount: number }>(
                'nCredit',
                'Api/ScheduledTasks/Fetch-SendCreditsToDebtCollection-InitialData',
                {}
            )
            .then((x) => {
                this.m = {
                    eligableForDebtCollectionCount: x.eligableForDebtCollectionCount,
                };
                this.gotoPage(0);
            });
    }

    public async startExport(evt?: Event) {
        this.m.result = await this.apiService.post<SendToDebtCollectionResult>(
            'nCredit',
            'Api/Credit/SendAllEligableToDebtCollection',
            {}
        );
        this.m.eligableForDebtCollectionCount = 0;
        this.gotoPage(0);
    }

    public async gotoPage(pageNr: number, evt?: Event) {
        this.m.files = await this.apiService.post('nCredit', 'Api/Credit/GetDebtCollectionFilesPage', {
            pageSize: 50,
            pageNr: pageNr,
        });
        this.m.filesPagingData = {
            currentPageNr: pageNr,
            totalNrOfPages: this.m.files.TotalNrOfPages,
            onGotoPage: (pageNr: number) => {
                this.gotoPage(pageNr);
            },
        };
    }

    public getDocumentUrl(archiveKey: string) {
        return this.apiService.getArchiveDocumentUrl(archiveKey);
    }
}

interface Model {
    eligableForDebtCollectionCount: number;
    result?: SendToDebtCollectionResult;
    files?: DebtCollectionFilesPage;
    filesPagingData?: TablePagerInitialData;
}

interface SendToDebtCollectionResult extends JobrunnerTaskResultModel {
    exportedCount: number;
}

interface DebtCollectionFilesPage {
    CurrentPageNr: number;
    TotalNrOfPages: number;
    Page: {
        TransactionDate: Date;
        CreditsCount: number;
        UserId: number;
        UserDisplayName: number;
        XlsFileArchiveKey: string;
        FileArchiveKey: string;
    }[];
}
