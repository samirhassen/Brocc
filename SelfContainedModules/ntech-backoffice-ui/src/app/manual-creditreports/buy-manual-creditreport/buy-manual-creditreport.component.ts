import { Component, OnInit } from '@angular/core';
import { ListCreditreportsInitialData } from '../list-creditreports/list-creditreports.component';
import { SearchInitialData } from '../search-creditreports/search-creditreports.component';
import { CreditReportProviderModel, ManualCreditReportsApiService } from '../services/manual-creditreports-api.service';

@Component({
    selector: 'app-buy-manual-creditreport',
    templateUrl: './buy-manual-creditreport.component.html',
    styles: [],
})
export class BuyManualCreditreportComponent implements OnInit {
    constructor(private apiService: ManualCreditReportsApiService) {}

    public m: Model = null;

    ngOnInit(): void {
        this.apiService.getProviders().then((x) => {
            let companyProviders = x.filter((x) => x.isCompanyProvider);
            let personProviders = x.filter((x) => !x.isCompanyProvider);
            this.m = {
                companyProviders: companyProviders,
                personProviders: personProviders,
            };
            this.reset();
        });
    }

    private reset() {
        this.m.listInitialData = null;
        this.m.searchInitialData = {
            allowCompany: this.m.companyProviders && this.m.companyProviders.length > 0,
            allowPerson: this.m.personProviders && this.m.personProviders.length > 0,
            onSearch: (x) => {
                this.m.searchInitialData = null;
                this.m.listInitialData = {
                    isCompany: x.isCompany,
                    civicRegNrOrOrgnr: x.civicRegNrOrOrgnr,
                    onReset: () => this.reset(),
                    providers: x.isCompany ? this.m.companyProviders : this.m.personProviders,
                };
            },
        };
    }
}

class Model {
    companyProviders: CreditReportProviderModel[];
    personProviders: CreditReportProviderModel[];
    searchInitialData?: SearchInitialData;
    listInitialData?: ListCreditreportsInitialData;
}
