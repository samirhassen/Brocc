import { Routes } from '@angular/router';
import { BackTargetResolverService } from 'src/app/common-services/backtarget-resolver.service';
import { RequireFeaturesGuard } from 'src/app/common-services/require-features.guard';
import { HighPermissionsGuard } from 'src/app/login/guards/high-permissions-guard';
import { RegisterMlSePageComponent } from './register-ml-se-page/register-ml-se-page.component';
import { LoannrsMlSePageComponent } from './loannrs-ml-se-page/loannrs-ml-se-page.component';

export const registerMlSeRoutes: Routes = [
    {
        path: 'ml-se',
        children: [
            {
                path: 'register',
                component: RegisterMlSePageComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Register loan' },
            },
            {
                path: 'loan-nrs',
                component: LoannrsMlSePageComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Loan nrs' },
            },            
        ],
        data: { requireFeatures: ['ntech.feature.mortgageloans.standard', 'ntech.feature.mortgageloans.manualregister'], requireClientCountry: 'SE' },
        resolve: { backTarget: BackTargetResolverService },
        canActivate: [RequireFeaturesGuard, HighPermissionsGuard],
    },
];
