import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BackTargetResolverService } from 'src/app/common-services/backtarget-resolver.service';
import { RequireFeaturesGuard } from 'src/app/common-services/require-features.guard';
import { MiddlePermissionsGuard } from 'src/app/login/guards/middle-permissions-guard';
import { PositiveCreditRegisterMainComponent } from './positive-credit-register-main/positive-credit-register-main.component';

const routes: Routes = [
    {
        path: 'positive-credit-register',
        children: [
            {
                path: 'main',
                component: PositiveCreditRegisterMainComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Positive credit register' },
            },
        ],
        data: { requireFeatures: ['ntech.feature.ullegacy', 'ntech.feature.positivecreditregister'] },
        resolve: { backTarget: BackTargetResolverService },
        canActivate: [RequireFeaturesGuard, MiddlePermissionsGuard],
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class PositiveCreditRegisterRoutingModule {}
