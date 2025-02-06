import { Component, OnInit } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { TablePagerInitialData } from 'src/app/common-components/table-pager/table-pager.component';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';

@Component({
    selector: 'app-reminders',
    templateUrl: './reminders.component.html',
    styles: [],
})
export class RemindersComponent implements OnInit {
    constructor(private apiService: NtechApiService, private toastr: ToastrService) {
        this.gotoPage = this.gotoPage.bind(this);
    }

    public m: RemindersComponentModel;

    async ngOnInit(): Promise<void> {
        this.reload();
    }

    private async reload() {
        this.m = null;

        let pageNr = 0;
        let result = await this.GetRemindersPage(pageNr, true);
        let m: RemindersComponentModel = {
            initialData: result.InitialData,
            executeChecks: {
                skipRecentReminders: false,
                skipRunOrder: false,
            },
        };
        this.setPage(pageNr, result, m);
        this.m = m;
    }

    public async gotoPage(pageNr: number, evt?: Event) {
        evt?.preventDefault();

        let result = await this.GetRemindersPage(pageNr, false);
        this.setPage(pageNr, result, this.m);
    }

    public async startExport(evt?: Event) {
        evt?.preventDefault();
        let m = this.m;
        let result = await this.apiService.post<CreateRemindersResult>('nCredit', m.initialData.createFileUrl, {
            skipRunOrderCheck: m.executeChecks.skipRunOrder,
            skipRecentRemindersCheck: m.executeChecks.skipRecentReminders,
        });
        if (result.errors && result.errors.length > 0) {
            this.toastr.error('Failed: ' + result.errors[0]);
        } else {
            this.toastr.info('File created');
        }
        await this.reload();
    }

    public getDocumentUrl(archiveKey: string) {
        return this.apiService.getArchiveDocumentUrl(archiveKey);
    }

    public onChangeSkipRunOrder(evt: Event) {
        evt.preventDefault();
        this.m.executeChecks.skipRunOrder = (evt.currentTarget as any).checked;
    }

    public onChangeSkipRecentReminders(evt: Event) {
        evt.preventDefault();
        this.m.executeChecks.skipRecentReminders = (evt.currentTarget as any).checked;
    }

    private setPage(pageNr: number, result: RemiderFilesPage, m: RemindersComponentModel) {
        m.files = result;
        m.filesPagingData = {
            currentPageNr: pageNr,
            totalNrOfPages: m.files.TotalNrOfPages,
            onGotoPage: (pageNr: number) => {
                this.gotoPage(pageNr);
            },
        };
    }

    private GetRemindersPage(pageNr: number, includeInitialData: boolean): Promise<RemiderFilesPage> {
        return this.apiService.post('nCredit', 'Api/Credit/GetReminderFilesPage', {
            pageSize: 50,
            pageNr,
            includeInitialData,
        });
    }
}

interface RemindersComponentModel {
    initialData: ReminderPageInitialData;
    files?: RemiderFilesPage;
    filesPagingData?: TablePagerInitialData;
    executeChecks: {
        skipRecentReminders: boolean;
        skipRunOrder: boolean;
    };
}

interface RemiderFilesPage {
    CurrentPageNr: number;
    TotalNrOfPages: number;
    Page: {
        TransactionDate: Date;
        ReminderCount: number;
        UserId: number;
        UserDisplayName: string;
        FileArchiveKey: string;
        ArchiveDocumentUrl: string;
    }[];
    InitialData: ReminderPageInitialData;
}

interface ReminderPageInitialData {
    status: ReminderStatusData;
    createFileUrl: string;
    hasPerLoanDueDay: boolean;
    notificationProcessSettings: {
        ReminderFeeAmount: number;
        SkipReminderLimitAmount: number;
    };
}

interface ReminderStatusData {
    NotificationCountInMonth: number;
    NrOfCurrentDeliveredReminders: number;
    NrOfCurrentNotDeliveredReminders: number;
    NrOfNotificationsPendingReminders: number;
    NrOfRecentlyCreatedReminders: number;
    SkippedReminders: {
        CreditNr: string;
        NotificationDueDate: Date;
        SkippedReason: string;
    }[];
}

interface CreateRemindersResult {
    errors: string[];
    warnings: string[];
    totalMilliseconds: number;
}
