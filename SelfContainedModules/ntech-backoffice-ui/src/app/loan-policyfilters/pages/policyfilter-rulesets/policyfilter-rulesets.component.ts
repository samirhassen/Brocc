import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import {
    PolicyfilterRulesetListInitialData,
    PolicyFilterRuleSetListItemModel,
} from 'src/app/policyfilter-components/policyfilter-ruleset-list/policyfilter-ruleset-list.component';
import { PolicyFiltersApiService } from '../../services/policy-filters-apiservice';

@Component({
    selector: 'policyfilter-rulesets',
    templateUrl: './policyfilter-rulesets.component.html',
    styles: [],
})
export class PolicyfilterRulesetsComponent implements OnInit {
    constructor(private apiService: PolicyFiltersApiService, private router: Router) {}

    public m: Model;

    ngOnInit(): void {
        this.reload();
    }

    reload() {
        this.apiService.fetchPolicyFilterRuleSets(false).then((x) => {
            this.init(x?.RuleSets || []);
        });
    }

    init(ruleSets: PolicyFilterRuleSetListItemModel[]) {
        this.m = {
            ruleSetsInitialData: {
                isEditAllowed: true,
                isAbTestingEnabled: false,
                ruleSets: ruleSets,
                handleAddNew: () => {
                    return this.apiService.createOrGetPendingPolicyFilterSet().then((x) => {
                        this.navigateToRuleSet(x.Id);
                        return { showPageAfter: false };
                    });
                },
                handleEdit: (ruleSetId: number) => {
                    this.navigateToRuleSet(ruleSetId);
                    return new Promise((resolve) => resolve({ showPageAfter: false }));
                },
                handleMove: (ruleSetId: number, newSlotName: string) => {
                    return this.apiService.changePolicyFilterSlot(ruleSetId, newSlotName).then((_) => {
                        this.reload();
                        return new Promise((resolve) => resolve({ showPageAfter: false }));
                    });
                },
            },
        };
    }

    private navigateToRuleSet(ruleSetId: number) {
        let targetToHere: CrossModuleNavigationTarget = CrossModuleNavigationTarget.create(
            'LoanStandardPolicyFilters',
            {}
        );
        this.router.navigate(['/loan-policyfilters/ruleset/', ruleSetId?.toString()], {
            queryParams: { backTarget: targetToHere.getCode() },
        });
    }
}

class Model {
    ruleSetsInitialData: PolicyfilterRulesetListInitialData;
}
