import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BackTargetResolverService } from '../common-services/backtarget-resolver.service';
import { RequireFeaturesGuard } from '../common-services/require-features.guard';
import { ConsumerCreditHighPermissionsGuard } from '../login/guards/consumercredit-high-permissions-guard';
import { PolicyfilterRulesetComponent } from './pages/policyfilter-ruleset/policyfilter-ruleset.component';
import { PolicyfilterRulesetsComponent } from './pages/policyfilter-rulesets/policyfilter-rulesets.component';

let policyFilterRoutes = {
    path: 'loan-policyfilters',
    children: [
        {
            path: 'rulesets',
            component: PolicyfilterRulesetsComponent,
            data: { pageTitle: 'Policyfilter rulesets', useFluidLayoutShell: true },
        },
        {
            path: 'ruleset/:id',
            component: PolicyfilterRulesetComponent,
            data: {
                pageTitle: 'Policyfilter ruleset',
                useFluidLayoutShell: true,
                customDefaultBackRoute: ['/settings/list'], //Always defaults to component setting
            },
        },
    ],
    resolve: { backTarget: BackTargetResolverService },
    data: { requireAnyFeature: ['ntech.feature.unsecuredloans.standard', 'ntech.feature.mortgageloans.standard'] },
    canActivate: [RequireFeaturesGuard, ConsumerCreditHighPermissionsGuard],
};

const routes: Routes = [policyFilterRoutes];
@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class LoanPolicyFiltersRoutingModule {}
