import { Component, Input, OnInit } from '@angular/core';
import { BankAccountNrValidationResult } from 'src/app/common-services/bank-account-nr-validation.service';

@Component({
    selector: 'bank-account-nr-view',
    templateUrl: './bank-account-nr-view.component.html',
    styles: [],
})
export class BankAccountNrViewComponent implements OnInit {
    constructor() {}

    @Input()
    public account: BankAccountNrValidationResult;

    ngOnInit(): void {}

    getTypeDisplayName() {
        if (!this.account || !this.account.IsValid) {
            return '';
        }

        let t = this.account.ValidAccount.BankAccountNrType;
        if (t === 'IBANFi') return 'Iban';
        else if (t === 'IBAN') return 'Iban';
        else if (t === 'BankAccountSe') return 'Regular account';
        else if (t === 'BankGiroSe') return 'Bankgiro';
        else if (t === 'PlusGiroSe') return 'Plusgiro';
        else return t;
    }
}
