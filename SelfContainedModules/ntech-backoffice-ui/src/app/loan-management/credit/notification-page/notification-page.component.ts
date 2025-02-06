import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { CreditService, GetNotificationDetailsResult, PaymentOrderUiItem } from '../credit.service';

@Component({
    selector: 'app-notification-page',
    templateUrl: './notification-page.component.html',
    styles: [],
})
export class NotificationPageComponent implements OnInit {
    constructor(
        private route: ActivatedRoute,
        private eventService: NtechEventService,
        private creditService: CreditService,
        private apiService: NtechApiService,
        private toastr: ToastrService
    ) {}

    async ngOnInit(): Promise<void> {
        this.route.paramMap.subscribe((params: ParamMap) => {
            let notificationId = parseInt(params.get('notificationId'));
            this.reload(notificationId);
        });
    }

    public m: Model;

    private async reload(notificationId: number) {
        this.m = null;

        if (!notificationId) {
            let title = 'Credit';
            this.eventService.setCustomPageTitle(title, title);
            return;
        }

        let result = await this.creditService.getNotificationDetails(notificationId);

        this.eventService.setCustomPageTitle(
            `Credit ${result.CreditNr}`,
            `Credit notification ${result.CreditNr} - ${result.DueDate}`
        );
        let m: Model = {
            ...result,
            creditNr: result.CreditNr,
            notificationId: notificationId,
            primaryOcr: result.SharedOcrPaymentReference ?? result.OcrPaymentReference,
            singleCreditOcr: result.SharedOcrPaymentReference ? result.OcrPaymentReference : null
        };

        this.m = m;
    }

    beginEdit(evt?: Event) {
        evt?.preventDefault();
        this.m.editData = { writtenOff: [] };
    }

    cancelEdit(evt?: Event) {
        evt?.preventDefault();
        this.m.editData = null;
    }

    async saveEdit(evt?: Event) {
        evt?.preventDefault();

        if (this.m.editData.writtenOff.length == 0) {
            //Null operation. Nothing selected for writeoff
            this.m.editData = null;
            return;
        }

        try {
            await this.creditService.writeOffSingleNotification(this.m.notificationId, this.m.editData.writtenOff);
            this.eventService.signalReloadCreditComments(this.m.creditNr);
            this.reload(this.m.notificationId);
        } catch (e) {
            this.toastr.error((e as any)?.statusText ?? 'Failed');
        }
    }

    writeOff(t: PaymentOrderUiItem, evt?: Event) {
        if (evt) {
            evt.preventDefault();
        }
        this.m.editData.writtenOff.push(t.UniqueId);
    }

    isPendingWriteOff(t: PaymentOrderUiItem) {
        if (!this.isEdit()) {
            return false;
        }
        return this.m.editData.writtenOff.indexOf(t.UniqueId) >= 0;
    }

    pendingWriteOffAmount(t: PaymentOrderUiItem) {
        var sum = 0;
        if (!this.m) {
            return sum;
        }
        if (this.isPendingWriteOff(t)) {
            sum = sum + this.m.Balance[t.UniqueId + 'RemainingAmount'];
        }
        return sum;
    }

    totalPendingWriteOffSum() {
        var sum = 0;
        if (!this.m) {
            return sum;
        }
        for (let t of this.m.PaymentOrderItems) {
            sum = sum + this.pendingWriteOffAmount(t);
        }
        return sum;
    }

    isEditEnabled() {
        return this.m && Math.abs(this.m.Balance.TotalRemainingAmount) > 0.0001;
    }

    isEditAllowed(t: PaymentOrderUiItem) {
        return (
            (t.OrderItem.IsBuiltin && (t.OrderItem.Code === 'ReminderFee' || t.OrderItem.Code === 'NotificationFee') || t.OrderItem.IsBuiltin === false) && Math.abs(this.m.Balance[t.UniqueId + 'RemainingAmount']) > 0.001
        );
    }

    isEdit() {
        return !!this.m.editData;
    }

    getCreditFilteredPaymentDetailsAsXlsUrl(paymentId: number) {
        return this.apiService.getUiGatewayUrl('nCredit', 'Api/AccountTransaction/CreditFilteredPaymentDetailsAsXls', [
            ['paymentId', paymentId.toString()],
            ['creditNr', this.m.creditNr],
        ]);
    }

    getArchiveDocumentUrl(archiveKey: string) {
        return this.apiService.getArchiveDocumentUrl(archiveKey, true);
    }
}

interface Model extends GetNotificationDetailsResult {
    primaryOcr: string;
    singleCreditOcr: string;
    creditNr: string;
    notificationId: number;
    editData?: {
        writtenOff: string[];
    };
}
