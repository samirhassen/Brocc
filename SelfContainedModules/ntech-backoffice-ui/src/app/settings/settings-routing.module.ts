import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BackTargetResolverService } from '../common-services/backtarget-resolver.service';
import { RequireFeaturesGuard } from '../common-services/require-features.guard';
import { SettingsListComponent } from './pages/settings-list/settings-list.component';

const routes: Routes = [
    {
        path: 'settings',
        children: [
            {
                path: 'list',
                component: SettingsListComponent,
                data: { pageTitle: 'Settings', useFluidLayoutShell: true },
            },
        ],
        resolve: { backTarget: BackTargetResolverService },
        data: {},
        canActivate: [RequireFeaturesGuard],
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class SettingsRoutingModule {}
