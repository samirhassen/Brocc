import { Component, OnInit } from '@angular/core';
import { ToggleBlockInitialData } from 'src/app/common-components/toggle-block/toggle-block.component';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { generateUniqueId } from 'src/app/common.types';
import {
    ApplicationsListInitialData,
    GetAssignedCountChangedEventName,
} from 'src/app/shared-application-components/components/applications-list/applications-list.component';
import { ApplicationsSearchInitialData } from 'src/app/shared-application-components/components/applications-search/applications-search.component';
import { UnsecuredLoanApplicationApiService } from '../../services/unsecured-loan-application-api.service';

@Component({
    selector: 'app-applications',
    templateUrl: './applications.component.html',
    styles: [],
})
export class ApplicationsComponent implements OnInit {
    constructor(private apiService: UnsecuredLoanApplicationApiService, private eventService: NtechEventService) {}

    public m: Model;

    ngOnInit(): void {
        let assignedCountChangedEventName = GetAssignedCountChangedEventName(false);
        this.eventService.applicationEvents.subscribe((x) => {
            if (x.eventCode === assignedCountChangedEventName) {
                let eventData = x.customData as { newCount: number };
                this.eventService.signalUpdateToggleBlock(
                    this.m.unassignedApplicationsInitialData.toggle.toggleBlockId,
                    { newTitle: `Not assigned (${eventData?.newCount})` }
                );
            }
        });

        this.m = {
            currentTabName: 'applicationsList',
            applicationsSearchInitialData: new ApplicationsSearchInitialData(false, this.apiService),
            unassignedApplicationsInitialData: {
                toggle: {
                    headerText: `Not assigned`,
                    toggleBlockId: generateUniqueId(10),
                },
                list: {
                    isAssignedApplications: false,
                    isForMortgageLoans: false,
                    applicationApiService: this.apiService,
                    cacheDiscriminator: 'ul-not-assigned',
                },
            },
            assignedApplicationsInitialData: {
                toggle: {
                    headerText: `Assigned`,
                    toggleBlockId: generateUniqueId(10),
                    isInitiallyExpanded: true,
                },
                list: {
                    isAssignedApplications: true,
                    isForMortgageLoans: false,
                    applicationApiService: this.apiService,
                    cacheDiscriminator: 'ul-assigned',
                },
            },
        };
    }

    setCurrentTab(tabName: string, evt?: Event): void {
        evt?.preventDefault();

        this.m.currentTabName = tabName;
    }

    getNavPillsClass(tabName: string) {
        return { active: this.m.currentTabName === tabName };
    }
}

export class Model {
    currentTabName: string;
    applicationsSearchInitialData: ApplicationsSearchInitialData;
    unassignedApplicationsInitialData: {
        toggle: ToggleBlockInitialData;
        list: ApplicationsListInitialData;
    };
    assignedApplicationsInitialData: {
        toggle: ToggleBlockInitialData;
        list: ApplicationsListInitialData;
    };
}
