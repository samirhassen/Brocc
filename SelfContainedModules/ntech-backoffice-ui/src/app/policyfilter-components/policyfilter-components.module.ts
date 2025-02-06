import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PolicyfilterRulesetListComponent } from './policyfilter-ruleset-list/policyfilter-ruleset-list.component';
import { PolicyfilterRulesetComponent } from './policyfilter-ruleset/policyfilter-ruleset.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CommonComponentsModule } from '../common-components/common-components.module';
import { RulesetDisplayTableComponent } from './policyfilter-ruleset/ruleset-display-table/ruleset-display-table.component';

@NgModule({
    declarations: [PolicyfilterRulesetListComponent, PolicyfilterRulesetComponent, RulesetDisplayTableComponent],
    imports: [CommonModule, FormsModule, ReactiveFormsModule, CommonComponentsModule],
    exports: [PolicyfilterRulesetListComponent, PolicyfilterRulesetComponent],
})
export class PolicyfilterComponentsModule {}
