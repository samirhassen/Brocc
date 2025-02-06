import { Component, OnInit } from '@angular/core';
import { CustomerPagesConfigService } from '../../../common-services/customer-pages-config.service';
import {
    MyPagesMenuItemCode,
    MypagesShellComponentInitialData,
} from '../../components/mypages-shell/mypages-shell.component';
import { MyPagesApiService } from '../../services/mypages-api.service';

@Component({
    selector: 'np-mypages-overview',
    templateUrl: './mypages-overview.component.html',
    styles: [],
})
export class MypagesOverviewComponent implements OnInit {
    constructor(private config: CustomerPagesConfigService, private apiService: MyPagesApiService) {}

    public m: Model;

    ngOnInit(): void {
        this.apiService.fetchCredits(null, true).then((x) => {
            this.m = {
                isStandardLoansActive: this.config.isLoansStandardEnabled(),
                shellInitialData: {
                    activeMenuItemCode: MyPagesMenuItemCode.Overview,
                    hasNoCustomerRelation: x.ActiveCredits.length === 0 && x.InactiveCredits.length === 0,
                },
            };
        });
    }
}

export class Model {
    shellInitialData: MypagesShellComponentInitialData;
    isStandardLoansActive: boolean;
}
