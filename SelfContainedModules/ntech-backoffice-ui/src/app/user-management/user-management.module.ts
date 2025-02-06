import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserManagementRoutingModule } from './user-management-routing.module';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonComponentsModule } from '../common-components/common-components.module';
import { UserManagementApiService } from './services/user-management-api.service';
import { AdministerUsersComponent } from './pages/administer-users/administer-users.component';
import { AdministerUserComponent } from './pages/administer-user/administer-user.component';
import { ApproveUserComponent } from './pages/approve-user/approve-user.component';
import { CreateAdminComponent } from './pages/create-admin/create-admin.component';

@NgModule({
    providers: [UserManagementApiService],
    declarations: [AdministerUsersComponent, AdministerUserComponent, ApproveUserComponent, CreateAdminComponent],
    imports: [CommonModule, ReactiveFormsModule, UserManagementRoutingModule, CommonComponentsModule],
})
export class UserManagementModule {}
