import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BackTargetResolverService } from '../common-services/backtarget-resolver.service';
import { AdminPermissionsGuard } from '../login/guards/admin-permissions-guard';
import { ErrorsComponent } from './pages/errors/errors.component';

const routes: Routes = [
    {
        path: 'system-health',
        children: [
            {
                path: 'errors',
                component: ErrorsComponent,
                data: { pageTitle: 'System health', useFluidLayoutShell: true },
                canActivate: [AdminPermissionsGuard],
            },
        ],
        resolve: { backTarget: BackTargetResolverService },
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class SystemHealthRoutingModule {}
