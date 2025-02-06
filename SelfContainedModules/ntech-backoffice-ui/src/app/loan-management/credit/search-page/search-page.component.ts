import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { TestFunctionsModel } from 'src/app/common-components/test-functions-popup/test-functions-popup.component';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { createCreateTestFunctionsModel } from '../credit-menu/credit-menu.component';
import { CreditService } from '../credit.service';

@Component({
    selector: 'app-search-page',
    templateUrl: './search-page.component.html',
    styles: [],
})
export class SearchPageComponent implements OnInit {
    constructor(
        private configService: ConfigService,
        private apiService: NtechApiService,
        private creditService: CreditService,
        private toastrService: ToastrService,
        private router: Router
    ) {}

    public m: Model;

    ngOnInit(): void {
        let m: Model = {
            testFunctions: createCreateTestFunctionsModel(
                this.configService,
                this.apiService,
                this.creditService,
                this.toastrService,
                this.router,
                null,
                () => {}
            ),
        };

        this.m = m;
    }
}

interface Model {
    testFunctions: TestFunctionsModel;
}
