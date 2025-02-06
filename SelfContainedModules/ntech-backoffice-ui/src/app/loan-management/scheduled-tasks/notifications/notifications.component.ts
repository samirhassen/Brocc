import { Component } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { TablePagerInitialData } from 'src/app/common-components/table-pager/table-pager.component';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { StringDictionary } from 'src/app/common.types';

@Component({
    selector: 'app-notifications',
    templateUrl: './notifications.component.html',
    styles: ``
})
export class NotificationsComponent {
    constructor(private apiService: NtechApiService, private toastr: ToastrService) {

    }

    public m: Model;

    async ngOnInit(): Promise<void> {
        this.reload();
    }

    async startNotification(evt ?: Event) {
        evt?.preventDefault();

        let toLowerResult = (x: any) => {
            if (!x) {
                return x
            }
            for (let key of Object.keys(x)) {
                if (key.length > 1) {
                    x[key.substring(0, 1).toLocaleLowerCase() + key.substring(1)] = x[key]
                }
            }
            return x
        }

        try {
            let result  = await this.apiService.post<any>('nCredit', this.m.notifyUrl, {});
            this.m.notificationResult = toLowerResult(result);
            await this.gotoPage(this.m, 0, null);
        } catch(err: any) {
            this.toastr.error(err);
        }
    }

    getSkippedCreditNrs() : string[] {
        if(!this.m?.skipReasonsByCreditNr) {
            return [];
        } else {
            return Object.keys(this.m.skipReasonsByCreditNr)
        }
    }

    getArchiveDocumentUrl(archiveKey: string) {
        return this.apiService.getArchiveDocumentUrl(archiveKey)
    }

    private async reload() {
        this.m = null;

        let initialData = await this.getInitialData();
        let productData: NotificationsInitialDataProductResult = null;
        let notifyUrl: string = null;

        if(initialData.unsecuredLoans) {
            notifyUrl = 'Api/Credit/CreateNotifications';
            productData = initialData.unsecuredLoans;
        } else if(initialData.mortgageLoans) {
            notifyUrl = 'api/MortgageLoans/Notify'
            productData = initialData.mortgageLoans;
        } else if(initialData.companyLoans) {
            notifyUrl = 'api/CompanyCredit/Notify'
            productData = initialData.companyLoans;
        } else {
            this.toastr.warning('No product is active that can be notified');
            return;
        }

        let m: Model = {
            notifyUrl: notifyUrl,
            counts: {
                countNotNotifiedCurrently: productData.countNotNotifiedCurrently,
                countDeliveredThisPeriod: productData.countDeliveredThisPeriod,
                countCreatedByNotDeliveredCurrently: productData.countCreatedByNotDeliveredCurrently
            },
            notificationGroups: productData.notificationGroups,
            skipReasonsByCreditNr: productData.skipReasonsByCreditNr
        };

        await this.gotoPage(m, 0, null);

        this.m = m;
    }

    private async gotoPage(m: Model, pageNr: number, evt ?: Event) {
        evt?.preventDefault();

        let result = await this.getNotificationFilesPage(pageNr);

        m.files = {
            ...result,
            pagingData : {
                currentPageNr: pageNr,
                totalNrOfPages: result.TotalNrOfPages,
                onGotoPage: x => this.gotoPage(m, x, null)
            }
        };
    }

    private async getNotificationFilesPage(pageNr : number) {
        return await this.apiService.post<NotificationsPageResult>('nCredit', 'Api/Credit/GetNotificationFilesPage', {
            pageSize: 50, pageNr: pageNr
        });
    }

    private async getInitialData() {
        return await this.apiService.post<NotificationsInitialDataResult>('nCredit', 'Api/Credit/CreateNotificationsInitialData', {})
    }
}

interface Model {
    notifyUrl: string
    counts ?: {
        countNotNotifiedCurrently: number
        countDeliveredThisPeriod: number
        countCreatedByNotDeliveredCurrently: number
    }
    notificationGroups ? : string[][]
    notificationResult ?: {
        totalMilliseconds: number
        failCount: number
        successCount: number
        errors: string[]
    }
    skipReasonsByCreditNr ?: StringDictionary
    files?: LocalFiles
}

interface LocalFiles extends NotificationsPageResult {
    pagingData: TablePagerInitialData
}

interface NotificationsPageResult {
    CurrentPageNr: number
    TotalNrOfPages: number
    Page: NotificationsPageResultFile[]
}

interface NotificationsPageResultFile {
    TransactionDate: string
    NotificationCount: number
    FileArchiveKey: string
    UserDisplayName: string
}

interface NotificationsInitialDataResult {
    unsecuredLoans ?: NotificationsInitialDataProductResult
    companyLoans ?: NotificationsInitialDataProductResult
    mortgageLoans ?: NotificationsInitialDataProductResult
    getNotificationFilesPageUrl: string
}

interface NotificationsInitialDataProductResult {
    countDeliveredThisPeriod: number
    countCreatedByNotDeliveredCurrently: number
    countNotNotifiedCurrently: number
    notificationApiUrl: string
    skipReasonsByCreditNr: StringDictionary
    notificationGroups: string[][]
}