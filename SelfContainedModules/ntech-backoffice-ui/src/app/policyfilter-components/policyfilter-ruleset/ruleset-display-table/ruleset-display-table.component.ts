import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Dictionary } from 'src/app/common.types';
import { PolicyFilterCommonService, PolicyFilterRejectionReason } from '../../policy-filter-common.service';
import { ModelPhase, ModelRuleRow, RuleSetDisplayModel } from '../policyfilter-ruleset.component';

@Component({
    selector: 'ruleset-display-table',
    templateUrl: './ruleset-display-table.component.html',
    styles: [],
})
export class RulesetDisplayTableComponent implements OnInit {
    private rejectionReasons: Dictionary<PolicyFilterRejectionReason>;

    constructor(private policyService: PolicyFilterCommonService) {}

    async ngOnInit(): Promise<void> {
        this.rejectionReasons = await this.policyService.getRejectionReasonsDictionary();
    }

    @Input()
    public ruleSet: RuleSetDisplayModel;

    @Input()
    public inEditMode: boolean;

    @Output()
    public removeRule = new EventEmitter<ModelRuleRow>();

    onRemoveRule(rule: ModelRuleRow, evt?: Event) {
        evt?.preventDefault();
        this.removeRule.emit(rule);
    }

    getRejectionReasonDisplayName(name: string) {
        return this.rejectionReasons ? this.rejectionReasons[name]?.DisplayName || name : name;
    }

    toggleRejectionReasonDisplay(phase: ModelPhase, rejectionReasonName: string, evt?: Event) {
        evt?.preventDefault();
        for (let rule of phase.rules) {
            rule.hasActiveRejectionReason = false;
        }
        phase.activeRejectionReasonName =
            phase.activeRejectionReasonName === rejectionReasonName ? null : rejectionReasonName;
        if (!phase.activeRejectionReasonName) {
            return;
        }
        for (let rule of phase.rules) {
            if (rule.rejectionReasonName === phase.activeRejectionReasonName) {
                rule.hasActiveRejectionReason = true;
            }
        }
    }
}
