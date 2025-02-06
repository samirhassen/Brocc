import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BackTargetResolverService } from '../common-services/backtarget-resolver.service';
import { MiddlePermissionsGuard } from '../login/guards/middle-permissions-guard';
import { RequireFeaturesGuard } from '../common-services/require-features.guard';
import { SearchComponent } from './search/search.component';
import { CustomerComponent } from './customer/customer.component';
import { CustomerCardComponent } from './customer-card/customer-card.component';

const routes: Routes = [
    {
        path: 'customer-overview',
        children: [
            {
                path: 'search/:query',
                component: SearchComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Customer overview' },
            },
            {
                path: 'customer/:customerId',
                component: CustomerComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Customer overview' },
            },
        ],
        data: { requireFeature: 'ntech.feature.customeroverview' },
        resolve: { backTarget: BackTargetResolverService },
        canActivate: [RequireFeaturesGuard, MiddlePermissionsGuard],
    },
    {
        path: 'customer-overview/customer-card/:customerId',
        component: CustomerCardComponent,
        data: { useFluidLayoutShell: true, pageTitle: 'Customer' },
        resolve: { backTarget: BackTargetResolverService },
        canActivate: [],
    }    
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class CustomerOverviewRoutingModule {}
