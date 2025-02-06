import { Component, OnInit } from '@angular/core';
import { ApplicationsSearchInitialData } from 'src/app/shared-application-components/components/applications-search/applications-search.component';
import { MortgageLoanApplicationApiService } from '../../services/mortgage-loan-application-api.service';

@Component({
    selector: 'app-search-applications',
    templateUrl: './search-applications.component.html',
    styles: [],
})
export class SearchApplicationsComponent implements OnInit {
    constructor(private apiService: MortgageLoanApplicationApiService) {}

    public m: Model;

    ngOnInit(): void {
        this.m = {
            applicationsSearchInitialData: new ApplicationsSearchInitialData(true, this.apiService),
        };
    }
}

class Model {
    applicationsSearchInitialData: ApplicationsSearchInitialData;
}
