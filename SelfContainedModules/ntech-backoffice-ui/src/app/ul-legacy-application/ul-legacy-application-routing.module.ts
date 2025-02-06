import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BackTargetResolverService } from '../common-services/backtarget-resolver.service';
import { RequireFeaturesGuard } from '../common-services/require-features.guard';
import { ConsumerCreditMiddlePermissionsGuard } from '../login/guards/consumercredit-middle-permissions-guard';
import { LegacyApplicationBasisPageComponent } from './pages/legacy-application-basis-page/legacy-application-basis-page.component';

let applicationRoutes = {
    path: 'loan-application',
    children: [
        {
            path: 'application-basis/:applicationNr',
            component: LegacyApplicationBasisPageComponent,
            data: {
                pageTitle: 'Application basis',
                useFluidLayoutShell: true,
            },
        }
    ],
    resolve: { backTarget: BackTargetResolverService },
    data: { requireFeatures: ['ntech.feature.unsecuredloans', 'ntech.feature.ullegacy'], routeParamBackTargetCode: 'UnsecuredLoanApplication' },
    canActivate: [RequireFeaturesGuard, ConsumerCreditMiddlePermissionsGuard],
};

const routes: Routes = [applicationRoutes];
@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class UlLegacyApplicationRoutingModule { }
