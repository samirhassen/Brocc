import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { LoanPolicyFiltersRoutingModule } from './loan-policy-filters-routing.module';
import { CommonComponentsModule } from '../common-components/common-components.module';
import { PolicyfilterRulesetsComponent } from './pages/policyfilter-rulesets/policyfilter-rulesets.component';
import { PolicyfilterComponentsModule } from '../policyfilter-components/policyfilter-components.module';
import { PolicyfilterRulesetComponent } from './pages/policyfilter-ruleset/policyfilter-ruleset.component';
import { SharedApplicationComponentsModule } from '../shared-application-components/shared-application-components.module';
import { PolicyFiltersApiService } from './services/policy-filters-apiservice';

@NgModule({
    providers: [PolicyFiltersApiService],
    declarations: [PolicyfilterRulesetsComponent, PolicyfilterRulesetComponent],
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        LoanPolicyFiltersRoutingModule,
        CommonComponentsModule,
        SharedApplicationComponentsModule,
        PolicyfilterComponentsModule,
    ],
    exports: [
        PolicyfilterRulesetsComponent, //Component setting
    ],
})
export class LoanPolicyFiltersModule {}
