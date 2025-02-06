import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BackTargetResolverService } from '../common-services/backtarget-resolver.service';
import { AdminPermissionsGuard } from '../login/guards/admin-permissions-guard';
import { GenerateComponent } from './generate/generate.component';
import { KeyComponent } from './key/key.component';
import { ListComponent } from './list/list.component';

const routes: Routes = [
    {
        path: 'api-keys',
        children: [
            { path: 'list', component: ListComponent, data: { useFluidLayoutShell: true, pageTitle: 'Api keys' } },
            {
                path: 'generate',
                component: GenerateComponent,
                data: {
                    useFluidLayoutShell: true,
                    pageTitle: 'Generate api key',
                    customDefaultBackRoute: ['/api-keys/list'],
                },
            },
            {
                path: 'key/:keyId',
                component: KeyComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Api key', customDefaultBackRoute: ['/api-keys/list'] },
            },
        ],
        data: {},
        resolve: { backTarget: BackTargetResolverService },
        canActivate: [AdminPermissionsGuard],
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ApiKeysRoutingModule {}
