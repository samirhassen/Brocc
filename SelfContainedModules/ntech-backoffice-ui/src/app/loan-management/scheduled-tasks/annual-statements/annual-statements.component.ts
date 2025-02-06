import { Component, OnInit } from '@angular/core';
import { TablePagerInitialData } from 'src/app/common-components/table-pager/table-pager.component';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';

@Component({
    selector: 'app-annual-statements',
    templateUrl: './annual-statements.component.html',
    styles: [],
})
export class AnnualStatementsComponent implements OnInit {
    constructor(private apiService: NtechApiService) {
        this.gotoPage = this.gotoPage.bind(this);
    }

    public m: Model;

    async ngOnInit(): Promise<void> {
        let result = await this.apiService.post<{ IsExportAllowed: boolean }>(
            'nCredit',
            'Api/CreditAnnualStatements/Fetch-Initial-Data',
            {}
        );
        this.m = {
            isExportAllowed: result?.IsExportAllowed,
        };
        this.gotoPage(0);
    }

    public async startExport(evt?: Event) {
        this.m.isExportAllowed = false;
        await this.apiService.post('nCredit', 'Api/CreditAnnualStatements/Create-Yearly-Report', {});
        await this.gotoPage(0);
    }

    public async gotoPage(pageNr: number, evt?: Event) {
        evt?.preventDefault();

        this.m.files = await this.apiService.post('nCredit', 'Api/CreditAnnualStatements/Fetch-Export-Files', {
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
interface AnnualStatementsFilesPage {
    CurrentPageNr: number;
    TotalNrOfPages: number;
    Page: {
        TransactionDate: Date;
        StatementCount: number;
        UserId: number;
        UserDisplayName: number;
        FileArchiveKey: string;
        ForYear: number;
        ExportResultStatus: string;
    }[];
}

interface Model {
    isExportAllowed: boolean;
    files?: AnnualStatementsFilesPage;
    filesPagingData?: TablePagerInitialData;
}
