import { Component, Input, SimpleChanges } from '@angular/core';
import { Router } from '@angular/router';
import * as moment from 'moment';
import { Dictionary } from 'src/app/common.types';
import { CustomerPagesApiService } from '../../common-services/customer-pages-api.service';
import { CustomerPagesConfigService } from '../../common-services/customer-pages-config.service';
import { CustomerMessagesHelper } from '../customer-messages-helper';

@Component({
    selector: 'applications-list',
    templateUrl: './applications-list.component.html',
    styleUrls: ['./applications-list.component.scss'],
})
export class ApplicationsListComponent {
    constructor(private router: Router, private config: CustomerPagesConfigService) {}

    @Input()
    public initialData: ApplicationsListComponentInitialData;

    public m: Model;

    async ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let applications = this.initialData.applications;

        let unreadCounts = await this.getUnreadCounts(this.initialData.applications);

        let m: Model = {
            activeApplications: applications
                .filter((x) => x.IsActive)
                .map((x) => {
                    return {
                        applicationNr: x.ApplicationNr,
                        applicationDate: moment(x.ApplicationDate).toDate(),
                        unreadCount: unreadCounts[x.ApplicationNr],
                    };
                }),
            rejectedOrCancelledApplications: applications
                .filter((x) => x.IsRejected || x.IsCancelled)
                .map((x) => {
                    return {
                        applicationNr: x.ApplicationNr,
                        applicationDate: moment(x.ApplicationDate).toDate(),
                        isRejected: x.IsRejected,
                        unreadCount: unreadCounts[x.ApplicationNr],
                    };
                }),
            loanCreatedApplications: applications
                .filter((x) => x.IsFinalDecisionMade)
                .map((x) => {
                    return {
                        applicationNr: x.ApplicationNr,
                        applicationDate: moment(x.ApplicationDate).toDate(),
                        creditNr: x.CreditNr,
                        unreadCount: unreadCounts[x.ApplicationNr],
                    };
                }),
        };

        this.m = m;
    }

    navigateToApplication(applicationNr: string, evt?: Event) {
        evt?.preventDefault();

        let routePath = this.initialData.isMortgageLoan
            ? 'mortgage-loan-applications/secure/application'
            : '/unsecured-loan-applications/application';

        this.router.navigate([routePath, applicationNr]);
    }

    private getUnreadCounts(applications: SharedApplicationBasicInfoModel[]): Promise<Dictionary<number>> {
        if (this.config.isFeatureEnabled('ntech.feature.securemessages')) {
            let result: Dictionary<number> = {};
            let lookups = [];
            for (let application of applications) {
                let helper = new CustomerMessagesHelper(
                    application.ApplicationNr,
                    this.config,
                    this.initialData.sharedApiService
                );
                lookups.push(
                    helper.getSecureMessagesUnreadByCustomerCount().then((x) => {
                        result[application.ApplicationNr] = x.UnreadCount;
                    })
                );
            }
            return Promise.all(lookups).then((_) => {
                return result;
            });
        } else {
            let result: Dictionary<number> = {};
            for (let a of applications) {
                result[a.ApplicationNr] = 0;
            }
            return new Promise<Dictionary<number>>((resolve) => resolve(result));
        }
    }

    hasCustomerRelation(): boolean {
        return (
            this.m.activeApplications.length > 0 ||
            this.m.loanCreatedApplications.length > 0 ||
            this.m.rejectedOrCancelledApplications.length > 0
        );
    }
}

interface Model {
    activeApplications: {
        applicationNr: string;
        applicationDate: Date;
        unreadCount?: number;
    }[];
    rejectedOrCancelledApplications: {
        applicationNr: string;
        applicationDate: Date;
        isRejected: boolean;
        unreadCount?: number;
    }[];
    loanCreatedApplications: {
        applicationNr: string;
        applicationDate: Date;
        creditNr: string;
        unreadCount?: number;
    }[];
}

export interface ApplicationsListComponentInitialData {
    isMortgageLoan: boolean;
    applications: SharedApplicationBasicInfoModel[];
    sharedApiService: CustomerPagesApiService;
}

export interface SharedApplicationBasicInfoModel {
    ApplicationNr: string;
    IsActive: boolean;
    ApplicationDate: string;
    IsCancelled: boolean;
    IsRejected: boolean;
    IsFinalDecisionMade: boolean;
    CreditNr: string;
    IsInactiveMessagingAllowed: boolean;
}
