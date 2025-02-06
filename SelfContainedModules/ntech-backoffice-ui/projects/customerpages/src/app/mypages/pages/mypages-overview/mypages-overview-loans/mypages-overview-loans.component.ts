import { Component, OnInit } from '@angular/core';
import { NTechMath } from 'src/app/common-services/ntech.math';
import { UnpaidNotificationsComponentInitialData } from '../../../components/unpaid-notifications/unpaid-notifications.component';
import { CreditModel, MyPagesApiService } from '../../../services/mypages-api.service';

@Component({
    selector: 'mypages-overview-loans',
    templateUrl: './mypages-overview-loans.component.html',
    styles: [],
})
export class MypagesOverviewLoansComponent implements OnInit {
    constructor(private apiService: MyPagesApiService) {}

    public m: Model;

    ngOnInit(): void {
        this.apiService.fetchCredits().then((x) => {
            let m: Model = {
                totalBalanceAmount: NTechMath.sum(x.ActiveCredits, (x) => x.CapitalBalance),
                unpaidNotificationsInitialData: {
                    isOverview: true,
                    credits: x.ActiveCredits,
                },
                activeCredits: x.ActiveCredits,
            };

            this.m = m;
        });
    }

    getLoanLink(): string {
        if (this.m.activeCredits.length === 1) {
            const creditNr = this.m.activeCredits.find((credit) => credit.CreditNr !== undefined).CreditNr;

            return '/my/sl/loan/' + creditNr;
        }

        return '/my/loans';
    }
}

export class Model {
    totalBalanceAmount: number;
    unpaidNotificationsInitialData: UnpaidNotificationsComponentInitialData;
    activeCredits: CreditModel[];
}
