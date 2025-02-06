import { Component, Input, OnInit } from '@angular/core';
import { CreditRecommendationLtlResult } from 'src/app/loan-policyfilters/services/policy-filters-apiservice';

@Component({
    selector: 'application-policy-info-ltl-details',
    templateUrl: './application-policy-info-ltl-details.component.html',
    styles: [],
})
export class ApplicationPolicyInfoLtlDetailsComponent implements OnInit {
    constructor() {}

    @Input()
    public ltlDetails: CreditRecommendationLtlResult;

    ngOnInit(): void {}
}
