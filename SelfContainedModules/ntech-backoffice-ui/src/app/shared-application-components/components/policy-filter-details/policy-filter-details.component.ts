import { Component, Input } from '@angular/core';
import { PolicyFilterDetailsDisplayItem } from 'src/app/loan-policyfilters/services/policy-filters-apiservice';

@Component({
    selector: 'policy-filter-details',
    templateUrl: './policy-filter-details.component.html',
    styles: [],
})
export class PolicyFilterDetailsComponent {
    constructor() {}

    @Input()
    public detailPhases?: {
        phaseDisplayName: string;
        isManualControlPhase: boolean;
        items: PolicyFilterDetailsDisplayItem[];
    }[];

    public getPolicyItemDisplayCode(item: PolicyFilterDetailsDisplayItem) {
        if (item.IsRejectedByRule === true && item.IsManualControlPhase) {
            return 'manualControl';
        } else if (item.IsRejectedByRule === true) {
            return 'reject';
        } else if (item.IsRejectedByRule === false) {
            return 'accept';
        } else {
            return 'noDecision';
        }
    }
}
