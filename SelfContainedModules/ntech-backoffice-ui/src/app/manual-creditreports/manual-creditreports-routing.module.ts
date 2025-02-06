import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BackTargetResolverService } from '../common-services/backtarget-resolver.service';
import { RequireFeaturesGuard } from '../common-services/require-features.guard';
import { MiddlePermissionsGuard } from '../login/guards/middle-permissions-guard';
import { BuyManualCreditreportComponent } from './buy-manual-creditreport/buy-manual-creditreport.component';

const routes: Routes = [
    {
        path: 'manual-creditreports',
        children: [
            { path: '', pathMatch: 'full', redirectTo: 'buy' },
            { path: 'buy', component: BuyManualCreditreportComponent },
        ],
        resolve: { backTarget: BackTargetResolverService },
        data: { requireFeature: 'ntech.feature.manualCreditReports', pageTitle: 'Manual creditreports' },
        canActivate: [RequireFeaturesGuard, MiddlePermissionsGuard],
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ManualCreditreportsRoutingModule {}
