import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import * as moment from 'moment';
import { NTechMath } from 'src/app/common-services/ntech.math';
import { CreditModel, MyPagesApiService } from '../../services/mypages-api.service';

@Component({
    selector: 'unpaid-notifications',
    templateUrl: './unpaid-notifications.component.html',
    styles: [],
})
export class UnpaidNotificationsComponent implements OnInit {
    constructor(private apiService: MyPagesApiService) {}

    ngOnInit(): void {}

    @Input()
    public initialData: UnpaidNotificationsComponentInitialData;

    public m: Model;

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let credits = this.initialData.credits;

        let nextNotificationDate: string = null;
        let creditsWithFutureNotifications = credits.filter((x) => !!x.NextNotificationDate);
        if (creditsWithFutureNotifications.length > 0) {
            nextNotificationDate = NTechMath.orderBy(
                creditsWithFutureNotifications,
                (x) => moment(x.NextNotificationDate).valueOf(),
                true
            )[0].NextNotificationDate;
        }

        let m: Model = {
            isOverview: this.initialData.isOverview,
            nextNotificationDate: nextNotificationDate,
            notifications: [],
        };

        for (let c of credits) {
            for (let n of c.Notifications.filter((y) => y.IsOpen)) {
                m.notifications.push({
                    notificationId: n.NotificationId,
                    dueDate: n.DueDate,
                    balanceAmount: n.BalanceAmount,
                    isOverdue: n.IsOverdue,
                    isExpanded: false,
                    directDebit: c.IsDirectDebitActive,
                    bankGiro: n.PaymentBankGiroNrDisplay,
                    ocrPaymentReference: n.OcrPaymentReferenceDisplay,
                    creditNr: c.CreditNr,
                    notificationUrl: n.PdfArchiveKey
                        ? this.apiService.shared().getArchiveDocumentUrl(n.PdfArchiveKey, true)
                        : null,
                    reminders: (n.Reminders ?? []).map((y) => {
                        return {
                            nr: y.ReminderNumber,
                            url: y.ArchiveKey
                                ? this.apiService.shared().getArchiveDocumentUrl(y.ArchiveKey, true)
                                : null,
                        };
                    }),
                });
            }
        }
        m.notifications = NTechMath.orderBy(m.notifications, (x) => x.notificationId);

        this.m = m;
    }
}

export class UnpaidNotificationsComponentInitialData {
    isOverview: boolean;
    credits: CreditModel[];
}

class Model {
    isOverview: boolean;
    nextNotificationDate: string;
    notifications: {
        notificationId: number;
        isExpanded: boolean;
        dueDate: string;
        isOverdue: boolean;
        balanceAmount: number;
        directDebit: boolean;
        bankGiro: string;
        ocrPaymentReference: string;
        creditNr: string;
        notificationUrl: string;
        reminders: {
            nr: number;
            url: string;
        }[];
    }[];
}
