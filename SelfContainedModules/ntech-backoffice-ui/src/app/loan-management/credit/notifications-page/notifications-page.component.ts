import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import * as moment from 'moment';
import { ToastrService } from 'ngx-toastr';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { CreditNotification, CreditService } from '../credit.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';

@Component({
    selector: 'app-notifications-page',
    templateUrl: './notifications-page.component.html',
    styles: [],
})
export class NotificationsPageComponent implements OnInit {
    constructor(
        private route: ActivatedRoute,
        private eventService: NtechEventService,
        private creditService: CreditService,
        private configService: ConfigService,
        private validationServie: NTechValidationService,
        private toastr: ToastrService,
        private apiService: NtechApiService
    ) {}

    async ngOnInit(): Promise<void> {
        this.route.paramMap.subscribe((params: ParamMap) => {
            this.reload(params.get('creditNr'));
        });
    }

    @ViewChild('promisedToPayDateInput')
    public promisedToPayDateInput: ElementRef<HTMLInputElement>;

    public m: Model;

    private async reload(creditNr: string) {
        this.m = null;

        if (!creditNr) {
            let title = 'Credit notifications';
            this.eventService.setCustomPageTitle(title, title);
            return;
        }
        this.eventService.setCustomPageTitle(`Credit ${creditNr}`, `Credit notifications ${creditNr}`);

        let result = await this.creditService.getNotifications(creditNr);

        this.m = {
            creditNr: creditNr,
            totalUnpaidAmount: result.totalUnpaidAmount,
            totalOverDueUnpaidAmount: result.totalOverDueUnpaidAmount,
            isMortgageLoansEnabled: this.configService.isFeatureEnabled('ntech.feature.mortgageloans'),
            creditStatus: result.creditStatus,
            promisedToPayDate: result.promisedToPayDate,
            isPromisedToPayDateEditMode: false,
            today: result.today,
            paidNotifications: result.notifications.filter((x) => x.IsPaid),
            unpaidNotifications: result.notifications.filter((x) => !x.IsPaid),
            inactivateTerminationLetters: !result.hasTerminationLettersThatSuspendTheCreditProcess || !result.latestActiveCreditProcessSuspendingTerminationLetterDuedate 
                ? null 
                : {
                    isPossibleToInactivateTerminationLetter: result.creditStatus === 'Normal' 
                        && result.hasTerminationLettersThatSuspendTheCreditProcess 
                        && !!result.latestActiveCreditProcessSuspendingTerminationLetterDuedate,
                    dueDate: result.latestActiveCreditProcessSuspendingTerminationLetterDuedate,
                    isEditing: false,
                    documents: result.latestActiveCreditProcessSuspendingTerminationLetters
                }
        };
    }

    beginEditPromisedToPayDate(evt?: Event) {
        if (evt) {
            evt.preventDefault();
        }
        this.m.isPromisedToPayDateEditMode = true;
        this.m.promisedToPayDateEdit = moment(this.m.today).add(14, 'days').format('YYYY-MM-DD');
        setTimeout(() => {
            //This is needed since the next digest cycle is what sets promisedToPayDateInput because of ngIf
            this.promisedToPayDateInput.nativeElement.focus();
        }, 0);
    }

    async addPromisedToPayDate(evt?: Event) {
        evt?.preventDefault();

        if (!this.validationServie.isValidDateOnly(this.m.promisedToPayDateEdit)) {
            this.toastr.warning('Invalid date');
            return;
        }

        let newValue = this.validationServie.parseDateOnly(this.m.promisedToPayDateEdit);
        try {
            await this.creditService.addPromisedToPayDate(this.m.creditNr, newValue);
            this.m.promisedToPayDate = newValue;
            this.m.isPromisedToPayDateEditMode = false;
            this.m.promisedToPayDateEdit = null;
        } catch (e) {
            this.toastr.error('Failed');
        }
    }

    cancelAddPromisedToPayDate(evt?: Event) {
        evt?.preventDefault();

        this.m.isPromisedToPayDateEditMode = false;
        this.m.promisedToPayDateEdit = null;
    }

    async removePromisedToPayDate(evt?: Event) {
        evt?.preventDefault();

        try {
            await this.creditService.removedPromisedToPayDate(this.m.creditNr);
            this.m.isPromisedToPayDateEditMode = false;
            this.m.promisedToPayDateEdit = null;
            this.reload(this.m.creditNr);
        } catch (e) {
            this.toastr.error('Failed');
        }
    }

    isPromisedToPayDateEditValid(allowEmpty: boolean) {
        if (!this.m.promisedToPayDateEdit) {
            return allowEmpty;
        }
        return this.validationServie.isValidDateOnly(this.m.promisedToPayDateEdit);
    }

    beginEditTerminationLetters(evt ?: Event) {
        evt?.preventDefault();

        this.m.inactivateTerminationLetters.isEditing = true;
    }

    cancelEditTerminationLetters(evt ?: Event) {
        evt?.preventDefault();

        this.m.inactivateTerminationLetters.isEditing = false;
        this.m.inactivateTerminationLetters.isPendingInactivate = false;
    }

    async commitEditTerminationLetters(evt ?: Event) {
        evt?.preventDefault();

        if(this.m.inactivateTerminationLetters.isPendingInactivate) {
            let creditNr = this.m.creditNr;
            let {inactivatedOnCreditNrs} = await this.creditService.inactivateTerminationLetters([creditNr]);
            if(inactivatedOnCreditNrs?.indexOf(creditNr) >= 0) {
                this.toastr.info('Termination letter inactivated');
                await this.reload(creditNr);
            } else {
                this.toastr.warning('Failed to inactivate termination letter');
                this.m.inactivateTerminationLetters.isEditing = false;
                this.m.inactivateTerminationLetters.isPendingInactivate = false;
            }
        }
    }    

    beginInactivateTerminationLetters(evt ?: Event) {
        evt?.preventDefault()

        this.m.inactivateTerminationLetters.isPendingInactivate = true;
    }

    getArchiveDocumentUrl(archiveKey: string) {
        return this.apiService.getArchiveDocumentUrl(archiveKey, true);
    }
    isAlternatePaymentPlansActive() {
        return this.configService.isFeatureEnabled('ntech.feature.paymentplan');
    }
}

class Model {
    creditNr: string;
    totalUnpaidAmount: number;
    totalOverDueUnpaidAmount: number;
    isMortgageLoansEnabled: boolean;
    creditStatus: string;
    promisedToPayDate: string;
    isPromisedToPayDateEditMode: boolean;
    promisedToPayDateEdit?: string;
    isPromisedToPayDateEditValid?: boolean;
    today: string;
    unpaidNotifications: CreditNotification[];
    paidNotifications: CreditNotification[];
    inactivateTerminationLetters: {
        isPossibleToInactivateTerminationLetter: boolean;
        dueDate: string;
        isEditing: boolean;
        isPendingInactivate?: boolean;
        documents ?: {
            customerId: number,
            archiveKey: string,
            coTerminationCreditNrs: string[]
        }[]
    }
}
