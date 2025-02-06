import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import {
    PolicyfilterRuleSet,
    PolicyfilterRulesetInitialData,
} from 'src/app/policyfilter-components/policyfilter-ruleset/policyfilter-ruleset.component';
import { PolicyFiltersApiService } from '../../services/policy-filters-apiservice';

@Component({
    selector: 'app-policyfilter-ruleset',
    templateUrl: './policyfilter-ruleset.component.html',
    styles: [],
})
export class PolicyfilterRulesetComponent implements OnInit {
    public m: Model;

    constructor(
        private route: ActivatedRoute,
        private apiService: PolicyFiltersApiService,
        private toastr: ToastrService
    ) {}

    ngOnInit(): void {
        this.reload();
    }

    private reload() {
        this.apiService.fetchPolicyFilterRuleSets(true).then((x) => {
            let rulesetId = parseInt(this.route.snapshot.params['id']);
            let currentRuleset = (x.RuleSets || []).find((y) => y.Id === rulesetId);
            if (!currentRuleset) {
                this.toastr.warning('No such ruleset exists');
                return;
            }

            let ruleset: PolicyfilterRuleSet = JSON.parse(currentRuleset.ModelData);

            this.m = {
                ruleSetId: rulesetId,
                ruleSetInitialData: {
                    isAbTestingActive: false,
                    slotName: currentRuleset.SlotName,
                    ruleSetName: currentRuleset.RuleSetName,
                    ruleSet: ruleset,
                    onSave: (data: { editedRuleSet: PolicyfilterRuleSet; newRuleSetName: string }) => {
                        return this.apiService
                            .editPolicyFilterSet(rulesetId, data.editedRuleSet, data.newRuleSetName)
                            .then((x) => {
                                this.reload();
                                return new Promise<{ showPageAfter: boolean }>((resolve) => {
                                    resolve({ showPageAfter: false });
                                });
                            });
                    },
                    getDisplayFormattedRules: (x) => this.apiService.formatPolicyFilterRulesetForDisplay(x),
                    allRules: x.AllRules || [],
                },
            };
        });
    }
}

class Model {
    ruleSetId: number;
    ruleSetInitialData: PolicyfilterRulesetInitialData;
}
