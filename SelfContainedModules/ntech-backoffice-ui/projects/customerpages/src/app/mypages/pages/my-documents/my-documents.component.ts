import { Component, OnInit } from '@angular/core';
import * as moment from 'moment';
import { CustomerPagesApiService } from '../../../common-services/customer-pages-api.service';
import { CustomerPagesConfigService } from '../../../common-services/customer-pages-config.service';
import {
    MyPagesMenuItemCode,
    MypagesShellComponentInitialData,
} from '../../components/mypages-shell/mypages-shell.component';
import { FetchCreditDocumentsResponseDocument, MyPagesApiService } from '../../services/mypages-api.service';

@Component({
    selector: 'np-my-documents',
    templateUrl: './my-documents.component.html',
    styles: [],
})
export class MyDocumentsComponent implements OnInit {
    constructor(private apiService: MyPagesApiService, private config: CustomerPagesConfigService) {}

    public m: Model;

    ngOnInit(): void {
        let m = new Model(this.apiService.shared());

        let products: Promise<any>[] = [];

        if (this.config.isLoansStandardEnabled()) {
            products.push(this.loadLoans(m));
        }

        Promise.all(products).then((_) => {
            m.sortDocuments();
            this.m = m;
        });
    }

    loadLoans(m: Model) {
        return this.apiService.fetchCreditDocuments().then((x) => {
            for (let document of x.Documents ?? []) {
                m.addLoanDocument(document);
            }
        });
    }
}

class Model {
    public documents: DocumentModel[];
    public shellInitialData: MypagesShellComponentInitialData;
    constructor(private customerPagesApiService: CustomerPagesApiService) {
        this.shellInitialData = {
            activeMenuItemCode: MyPagesMenuItemCode.Documents,
        };
        this.documents = [];
    }

    public addLoanDocument(document: FetchCreditDocumentsResponseDocument) {
        this.documents.push({
            Product: ProductCode.Loan,
            ProductId: document.CreditNr,
            TypeCode: document.DocumentTypeCode,
            Context: document.DocumentContext,
            Url: this.customerPagesApiService.getArchiveDocumentUrl(document.DocumentArchiveKey, true),
            Date: document.DocumentDate,
        });
    }

    public sortDocuments() {
        //valueOf is unix epoch
        this.documents.sort((x, y) => moment(y.Date).valueOf() - moment(x.Date).valueOf());
    }
}

enum ProductCode {
    Loan = 'Loan',
}

interface DocumentModel {
    Product: ProductCode;
    ProductId: string;
    Context: string;
    TypeCode: string;
    Url: string;
    Date: string;
}
