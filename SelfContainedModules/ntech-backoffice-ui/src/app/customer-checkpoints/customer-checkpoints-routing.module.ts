import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BackTargetResolverService } from '../common-services/backtarget-resolver.service';
import { MiddlePermissionsGuard } from '../login/guards/middle-permissions-guard';
import { RequireFeaturesGuard } from '../common-services/require-features.guard';
import { CheckpointsMainComponent } from './checkpoints-main/checkpoints-main.component';
import { CheckpointsForCustomerComponent } from './checkpoints-for-customer/checkpoints-for-customer.component';

const routes: Routes = [
    {
        path: 'customer-checkpoints',
        children: [
            {
                path: 'main',
                component: CheckpointsMainComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Customer checkpoints' },
            },
            {
                path: 'for-customer/:customerId',
                component: CheckpointsForCustomerComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Customer checkpoints' },
            },
        ],
        data: { requireFeature: 'ntech.feature.customercheckpoints' },
        resolve: { backTarget: BackTargetResolverService },
        canActivate: [RequireFeaturesGuard, MiddlePermissionsGuard],
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class CustomerCheckpointsRoutingModule {}
