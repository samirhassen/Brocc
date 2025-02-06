import { Routes } from '@angular/router';
import { BackTargetResolverService } from 'src/app/common-services/backtarget-resolver.service';
import { RequireFeaturesGuard } from 'src/app/common-services/require-features.guard';
import { HighPermissionsGuard } from 'src/app/login/guards/high-permissions-guard';
import { LoanOwnerManagementPageComponent } from './loan-owner-management-page/loan-owner-management-page.component';

export const loanOwnerManagementRoutes: Routes = [
    {
        path: 'loan-owner-management',
        children: [
            {
                path: 'edit-owners',
                component: LoanOwnerManagementPageComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Loan owner management' },
            },
        ],
        data: { requireAnyFeature: ['ntech.feature.mortgageloans.standard'] },
        resolve: { backTarget: BackTargetResolverService },
        canActivate: [RequireFeaturesGuard, HighPermissionsGuard],
    },
];
