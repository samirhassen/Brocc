import { NgModule } from '@angular/core';
import { SecureMessagesListComponent } from './secure-messages-list/secure-messages-list.component';
import { RouterModule, Routes } from '@angular/router';
import { SecureMessagesCreateComponent } from './secure-messages-create/secure-messages-create.component';
import { BackTargetResolverService } from '../common-services/backtarget-resolver.service';
import { MiddlePermissionsGuard } from '../login/guards/middle-permissions-guard';
import { RequireFeaturesGuard } from '../common-services/require-features.guard';
import { SecureMessagesChannelComponent } from './secure-messages-channel/secure-messages-channel.component';

//You add components here by doing ng generate component secure-messages/<new-component-name>
const routes: Routes = [
    {
        path: 'secure-messages',
        children: [
            {
                path: 'list',
                component: SecureMessagesListComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Messages' },
            },
            {
                path: 'create',
                component: SecureMessagesCreateComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Messages' },
            },
            {
                path: 'channel',
                component: SecureMessagesChannelComponent,
                data: { useFluidLayoutShell: true, pageTitle: 'Message' },
            },
        ],
        data: { requireFeature: 'ntech.feature.securemessages' },
        resolve: { backTarget: BackTargetResolverService },
        canActivate: [RequireFeaturesGuard, MiddlePermissionsGuard],
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class SecureMessagesRoutingModule {}
