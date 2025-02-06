import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { CreditService } from '../../credit.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';

@Component({
    selector: 'ml-se-loan-owner-management',
    templateUrl: './ml-se-loan-owner-management.component.html',
    styles: [],
})
export class MlSeLoanOwnerManagementComponent implements OnInit {
    constructor(private creditService: CreditService, private eventService: NtechEventService) {}

    @Input()
    public creditNr: string;

    public m: Model;

    ngOnInit(): void {}

    async ngOnChanges(_: SimpleChanges) {
        this.m = null;

        const loanOwner = await this.creditService.mlFetchLoanOwner(this.creditNr);
        this.m = {
            creditNr: this.creditNr,
            loanOwnerName: loanOwner.loanOwnerName ?? '[none]',
            availableLoanOwnerOptions: loanOwner.availableLoanOwnerOptions,
            isLoanOwnerManagementEditMode: false,
            loanOwnerEdit: null,
        };
    }

    beginEditLoanOwnerManagement(evt?: Event) {
        evt?.preventDefault();

        this.m.isLoanOwnerManagementEditMode = true;
        this.m.loanOwnerEdit = this.m.loanOwnerName;
    }

    async confirmLoanOwner(evt?: Event) {
        evt?.preventDefault();

        const confirmedLoanOwner = await this.creditService.mlEditLoanOwner(this.m.creditNr, this.m.loanOwnerEdit);
        this.m.loanOwnerName = confirmedLoanOwner.loanOwnerName;
        this.m.isLoanOwnerManagementEditMode = false;
        this.eventService.signalReloadCreditComments(this.m.creditNr);
    }

    cancelEditLoanOwner(evt?: Event) {
        evt?.preventDefault();

        this.m.loanOwnerEdit = null;
        this.m.isLoanOwnerManagementEditMode = false;
    }
}

interface Model {
    creditNr: string;
    loanOwnerName: string;
    availableLoanOwnerOptions: string[];
    isLoanOwnerManagementEditMode: boolean;
    loanOwnerEdit: string;
}
