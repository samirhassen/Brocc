import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { splitIntoPages, TablePagerInitialData } from 'src/app/common-components/table-pager/table-pager.component';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { AccountTransactionDetails, CreditCapitalTransaction, CreditService } from '../../credit.service';

@Component({
    selector: 'credit-details-transactions',
    templateUrl: './transactions.component.html',
    styles: [],
})
export class TransactionsComponent implements OnInit {
    constructor(private apiService: NtechApiService, private creditService: CreditService) {
        this.gotoPage = this.gotoPage.bind(this);
    }

    @Input()
    public capitalTransactions: CreditCapitalTransaction[];

    public m: Model;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.capitalTransactions) {
            return;
        }

        let trs = this.capitalTransactions.map((x) => x as CreditCapitalTransactionWithDetails);
        let pages = splitIntoPages(trs, 20);

        this.m = {
            pageItems: null,
            pages: pages,
            pagingData: null,
        };

        if (pages.length > 0) {
            this.gotoPage(0);
        }
    }

    async showOrHideAccountTransactionDetails(t: CreditCapitalTransactionWithDetails, evt?: Event) {
        evt?.preventDefault();

        if (t.transactionDetails) {
            t.transactionDetails = null;
            return;
        }

        let result = await this.creditService.getCapitalDebtTransactionDetails(t.id);
        t.transactionDetails = result.Details;
    }

    getEventTransactionDetailsXlsUrl(businessEventId: number) {
        if (!businessEventId) {
            return null;
        }
        return this.apiService.getUiGatewayUrl(
            'nCredit',
            'Api/AccountTransaction/BusinessEventTransactionDetailsAsXls',
            [['businessEventId', businessEventId.toString()]]
        );
    }

    getCreditFilteredPaymentDetailsAsXlsUrl(details: AccountTransactionDetails) {
        return this.apiService.getUiGatewayUrl('nCredit', 'Api/AccountTransaction/CreditFilteredPaymentDetailsAsXls', [
            ['paymentId', details.IncomingPaymentId.toString()],
            ['creditNr', details.CreditNr],
        ]);
    }

    private gotoPage(pageNr: number) {
        this.m.pagingData = {
            currentPageNr: pageNr,
            totalNrOfPages: this.m.pages.length,
            onGotoPage: (x) => this.gotoPage(x),
        };
        this.m.pageItems = this.m.pages[pageNr];
    }
}

class Model {
    pages: CreditCapitalTransactionWithDetails[][];
    pagingData: TablePagerInitialData;
    pageItems: CreditCapitalTransactionWithDetails[];
}

interface CreditCapitalTransactionWithDetails extends CreditCapitalTransaction {
    transactionDetails?: AccountTransactionDetails;
}
