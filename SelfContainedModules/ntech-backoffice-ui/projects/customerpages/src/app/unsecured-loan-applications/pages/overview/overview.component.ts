import { Component, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import * as moment from 'moment';
import { CustomerPagesApiService } from '../../../common-services/customer-pages-api.service';
import { CustomerPagesConfigService } from '../../../common-services/customer-pages-config.service';
import { ApplicationsListComponentInitialData } from '../../../shared-components/applications-list/applications-list.component';
import { CustomerPagesApplicationsApiService } from '../../services/customer-pages-applications-api.service';

@Component({
    selector: 'np-overview',
    templateUrl: './overview.component.html',
    styleUrls: [],
})
export class OverviewComponent implements OnInit {
    constructor(
        private titleService: Title,
        private apiService: CustomerPagesApplicationsApiService,
        private config: CustomerPagesConfigService,
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
                    isMortgageLoan: false,
                    applications: x.Applications,
                    sharedApiService: this.sharedApiService,
                },
                userDisplayName: this.config.userDisplayName(),
                currentDateAndTime: this.config.getCurrentDateAndTime(),
            };
            this.m = m;
        });
    }
}

class Model {
    userDisplayName: string;
    currentDateAndTime: moment.Moment;
    applicationsListData: ApplicationsListComponentInitialData;
}
