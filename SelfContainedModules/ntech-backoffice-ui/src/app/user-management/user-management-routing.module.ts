import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BackTargetResolverService } from '../common-services/backtarget-resolver.service';
import { AdminPermissionsGuard } from '../login/guards/admin-permissions-guard';
import { HighPermissionsGuard } from '../login/guards/high-permissions-guard';
import { AdministerUserComponent } from './pages/administer-user/administer-user.component';
import { AdministerUsersComponent } from './pages/administer-users/administer-users.component';
import { ApproveUserComponent } from './pages/approve-user/approve-user.component';
import { CreateAdminComponent } from './pages/create-admin/create-admin.component';

const routes: Routes = [
    {
        path: 'user-management',
        children: [
            {
                path: 'administer-user/:userid',
                component: AdministerUserComponent,
                data: {
                    pageTitle: 'Administer user',
                    useFluidLayoutShell: true,
                    customDefaultBackRoute: ['/user-management/administer-users'],
                },
                canActivate: [AdminPermissionsGuard],
            },
            {
                path: 'administer-users',
                component: AdministerUsersComponent,
                data: { pageTitle: 'User administration', useFluidLayoutShell: true },
                canActivate: [AdminPermissionsGuard],
            },
            {
                path: 'approve-user',
                component: ApproveUserComponent,
                data: { pageTitle: 'Approve user', useFluidLayoutShell: true },
                canActivate: [HighPermissionsGuard],
            },
            {
                path: 'create-admin',
                component: CreateAdminComponent,
                data: { pageTitle: 'Create administrator', useFluidLayoutShell: true },
                canActivate: [HighPermissionsGuard],
            },
        ],
        resolve: { backTarget: BackTargetResolverService },
    },
];
@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class UserManagementRoutingModule {}
