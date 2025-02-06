import { Component, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { CustomerPagesApiService } from '../../../common-services/customer-pages-api.service';
import { ApplicationsListComponentInitialData } from '../../../shared-components/applications-list/applications-list.component';
import { CustomerPagesMortgageLoanApiService } from '../../services/customer-pages-ml-api.service';

@Component({
    selector: 'np-overview-ml',
    templateUrl: './overview.component.html',
    styles: [],
})
export class OverviewComponent implements OnInit {
    constructor(
        private titleService: Title,
        private apiService: CustomerPagesMortgageLoanApiService,
        private sharedApiService: CustomerPagesApiService
    ) {}

    public m: Model;

    ngOnInit(): void {
        this.titleService.setTitle('Ansökningar');
        this.reload();
    }

    reload() {
        this.apiService.fetchApplications().then((x) => {
            let m: Model = {
                applicationsListData: {
                    isMortgageLoan: true,
                    applications: x.Applications,
                    sharedApiService: this.sharedApiService,
                },
            };
            this.m = m;
        });
    }
}

class Model {
    applicationsListData: ApplicationsListComponentInitialData;
}
