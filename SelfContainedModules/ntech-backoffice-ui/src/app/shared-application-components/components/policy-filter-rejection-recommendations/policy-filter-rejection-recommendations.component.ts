import { Component, Input, OnInit } from '@angular/core';
import { Dictionary } from 'src/app/common.types';
import { PolicyFilterCreditRecommendationModel } from 'src/app/loan-policyfilters/services/policy-filters-apiservice';
import {
    PolicyFilterCommonService,
    PolicyFilterRejectionReason,
} from 'src/app/policyfilter-components/policy-filter-common.service';

@Component({
    selector: 'policy-filter-rejection-recommendations',
    templateUrl: './policy-filter-rejection-recommendations.component.html',
    styles: [],
})
export class PolicyFilterRejectionRecommendationsComponent implements OnInit {
    private rejectionReasons: Dictionary<PolicyFilterRejectionReason>;

    constructor(private policyService: PolicyFilterCommonService) {}

    async ngOnInit(): Promise<void> {
        this.rejectionReasons = await this.policyService.getRejectionReasonsDictionary();
    }
    @Input()
    public recommendation: PolicyFilterCreditRecommendationModel;

    getRecommendationText() {
        let p = this.recommendation?.PolicyFilterResult;
        let isAcceptRecommended = p?.IsAcceptRecommended;
        if (isAcceptRecommended === true) {
            return p.IsManualControlRecommended === true ? 'Accept with manual control' : 'Accept';
        } else if (isAcceptRecommended === false) {
            return 'Reject';
        } else {
            return '-';
        }
    }

    getRejectionReasons() {
        let p = this.recommendation?.PolicyFilterResult;
        if (!p || p.IsAcceptRecommended !== false) {
            return [];
        }
        return p.RejectionReasonNames ?? [];
    }

    getRejectionReasonDisplayName(name: string) {
        return this.rejectionReasons ? this.rejectionReasons[name]?.DisplayName || name : name;
    }
}
