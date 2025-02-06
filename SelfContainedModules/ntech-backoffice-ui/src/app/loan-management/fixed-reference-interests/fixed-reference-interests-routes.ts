import { Routes } from '@angular/router';
import { BackTargetResolverService } from 'src/app/common-services/backtarget-resolver.service';
import { RequireFeaturesGuard } from 'src/app/common-services/require-features.guard';
import { HighPermissionsGuard } from 'src/app/login/guards/high-permissions-guard';
import { ChangeRatesPageComponent } from './change-rates-page/change-rates-page.component';

export const fixedReferenceInterestRoutes: Routes = [
    {
        path: 'fixed-reference-interest',
        children: [
            {
                path: 'change-rates',
                component: ChangeRatesPageComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Reference interest rate' },
            },
        ],
        data: { requireAnyFeature: ['ntech.feature.mortgageloans.standard'] },
        resolve: { backTarget: BackTargetResolverService },
        canActivate: [RequireFeaturesGuard, HighPermissionsGuard],
    },
];
