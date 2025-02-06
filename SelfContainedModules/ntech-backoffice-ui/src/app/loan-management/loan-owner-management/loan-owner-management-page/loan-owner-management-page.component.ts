import { Component, OnInit } from '@angular/core';
import { CreditService } from '../../credit/credit.service';
import { ToastrService } from 'ngx-toastr';

@Component({
    selector: 'loan-owner-management-page',
    templateUrl: './loan-owner-management-page.component.html',
    styleUrls: [],
})
export class LoanOwnerManagementPageComponent implements OnInit {
    constructor(private creditService: CreditService, private toastrService: ToastrService) {}

    public m: Model;

    async ngOnInit() {
        await this.reload();
    }

    private async reload() {
        const owners = await this.creditService.mlFetchLoanOwner(null);
        this.m = {
            availableLoanOwnerOptions: owners.availableLoanOwnerOptions,
            newOwner: null,
            loansString: null,
            preview: null,
        };
    }

    async calculatePreview(evt?: Event) {
        evt?.preventDefault();

        if (this.m.loansString === null || this.m.newOwner === null || this.m.newOwner === '[none]') {
            this.toastrService.error('Invalid fields.');
            this.m.preview = null;
            return;
        }

        const previewModel = await this.creditService.mlBulkEditLoanOwnerPreview(this.getLoans(), this.m.newOwner);
        if (!previewModel.isValid) {
            this.toastrService.error(previewModel.validationErrorMessage ?? 'One or more of the loans are not valid.');
            return;
        }

        this.m.preview = {
            newOwner: previewModel.loanOwnerName,
            nrOfLoans: previewModel.nrOfLoansEdit,
        };
    }

    public async commitUpdate(evt?: Event) {
        evt?.preventDefault();

        await this.creditService.mlBulkEditLoanOwner(this.getLoans(), this.m.newOwner).catch((_) => {
            this.toastrService.error('Could not bulk edit loan owners. Please contact support.');
            return;
        });

        this.m.loansString = null;
        this.m.newOwner = null;
        this.m.preview = null;
    }

    public isCalculateDisabled() {
        return !this.m?.loansString || !this.m.loansString.trim() || !this.m?.newOwner || this.m.newOwner === '[none]';
    }

    public clearPreview(evt?: Event) {
        evt?.preventDefault();
        this.m.preview = null;
    }

    private getLoans = (): string[] =>
        this.m.loansString
            .split(',')
            .map((loan) => loan.trim())
            .filter((loan) => loan !== '');
}

interface Model {
    availableLoanOwnerOptions: string[];
    newOwner: string;
    loansString: string;
    preview?: {
        newOwner: string;
        nrOfLoans: number;
    };
}
