import { Component, Input, OnInit } from '@angular/core';
import { ToggleBlockInitialData } from 'src/app/common-components/toggle-block/toggle-block.component';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { generateUniqueId } from 'src/app/common.types';
import {
    ApplicationsListInitialData,
    GetAssignedCountChangedEventName,
} from 'src/app/shared-application-components/components/applications-list/applications-list.component';
import { MortgageLoanApplicationApiService } from '../../services/mortgage-loan-application-api.service';

@Component({
    selector: 'app-applications',
    templateUrl: './applications.component.html',
    styles: [],
})
export class ApplicationsComponent implements OnInit {
    constructor(private eventService: NtechEventService, private apiService: MortgageLoanApplicationApiService) {}

    @Input()
    public isAssignedApplications: boolean;

    public m: Model;

    ngOnInit(): void {
        let assignedCountChangedEventName = GetAssignedCountChangedEventName(true);
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
            unassignedApplicationsInitialData: {
                toggle: {
                    headerText: `Not assigned`,
                    toggleBlockId: generateUniqueId(10),
                },
                list: {
                    isForMortgageLoans: true,
                    isAssignedApplications: false,
                    applicationApiService: this.apiService,
                    cacheDiscriminator: 'ml-not-assigned',
                },
            },
            assignedApplicationsInitialData: {
                toggle: {
                    headerText: `Assigned`,
                    toggleBlockId: generateUniqueId(10),
                    isInitiallyExpanded: true,
                },
                list: {
                    isForMortgageLoans: true,
                    isAssignedApplications: true,
                    applicationApiService: this.apiService,
                    cacheDiscriminator: 'ml-assigned',
                },
            },
        };
    }
}

class Model {
    unassignedApplicationsInitialData: {
        toggle: ToggleBlockInitialData;
        list: ApplicationsListInitialData;
    };
    assignedApplicationsInitialData: {
        toggle: ToggleBlockInitialData;
        list: ApplicationsListInitialData;
    };
}
