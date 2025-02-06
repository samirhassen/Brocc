import { Routes } from '@angular/router';
import { BackTargetResolverService } from 'src/app/common-services/backtarget-resolver.service';
import { RequireFeaturesGuard } from 'src/app/common-services/require-features.guard';
import { MiddlePermissionsGuard } from 'src/app/login/guards/middle-permissions-guard';
import { DebtCollectionScheduledTaskComponent } from './debt-collection-scheduled-task/debt-collection-scheduled-task.component';
import { DebtCollectionComponent } from './debt-collection/debt-collection.component';
import { TerminationLetterComponent } from './termination-letter/termination-letter.component';

export const defaultManagementRoutes: Routes = [
    {
        path: 'default-management',
        children: [
            {
                path: 'termination-letter',
                component: TerminationLetterComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Termination letter management' },
            },
            {
                path: 'debt-collection',
                component: DebtCollectionComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Debt collection management' },
            },
            {
                path: 'debt-collection-task',
                component: DebtCollectionScheduledTaskComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Send credits to debt collection' },
            },
        ],
        data: {
            requireAnyFeature: [
                'ntech.feature.unsecuredloans',
                'ntech.feature.companyloans',
                'ntech.feature.mortgageloans.standard',
            ],
        },
        resolve: { backTarget: BackTargetResolverService },
        canActivate: [RequireFeaturesGuard, MiddlePermissionsGuard],
    },
];
