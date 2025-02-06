import { Injectable } from '@angular/core';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';

@Injectable({
    providedIn: 'root',
})
export class UserManagementApiService {
    constructor(private apiService: NtechApiService) {}

    public getAllUsers(showDeleted?: boolean) {
        return this.apiService.post<UserModel[]>('nUser', 'User/GetAllWithAdmin', { showDeleted });
    }

    public createUser(
        displayName: string,
        userType: string,
        adminStartDate: string,
        adminEndDate: string
    ): Promise<{
        userDisplayNameAlreadyInUse: boolean;
        errorMessage: string;
        createdUserId: number;
    }> {
        return this.apiService.post('nBackOffice', 'Admin/CreateUser2', {
            displayName,
            userType,
            adminStartDate,
            adminEndDate,
        });
    }

    public getActiveLoginMethods(): Promise<ActiveLoginMethodModel[]> {
        return this.apiService.post('nUser', 'AuthenticationMechanism/GetActiveLoginMethods', {});
    }

    public getDataForAdministerUser(userId: number): Promise<DataForAdministerUserModel> {
        return this.apiService.post('nUser', 'GroupMembership/GetDataForAdministerUser', { userid: userId });
    }

    public deactivateUser(userId: number): Promise<{ UserId: number }> {
        return this.apiService.post('nUser', 'User/DeactivateUser', { userId });
    }

    public reactivateUser(userId: number): Promise<{ UserId: number }> {
        return this.apiService.post('nUser', 'User/ReactivateUser', { userId });
    }

    public createAdminGroupMembership(request: CreateGroupmembershipRequest): Promise<{ Id: number }> {
        return this.apiService.post('nUser', 'GroupMembership/CreateAdmin', request);
    }

    public createNonAdminGroupMembership(request: CreateGroupmembershipRequest): Promise<{ Id: number }> {
        return this.apiService.post('nUser', 'GroupMembership/CreateNonAdmin', request);
    }

    public beginGroupMembershipCancellation(groupMembershipId: number) {
        return this.apiService.post('nUser', 'GroupMembership/BeginCancellation', { groupMembershipId });
    }

    public createLoginMethod(request: {
        userId: number;
        adUsername: string;
        providerEmail: string;
        providerObjectId: string;
        upwUsername: string;
        upwPassword: string;
        authenticationType: string;
        providerName: string;
        userIdentityAndCredentialsType: string;
    }): Promise<{ errorMessage?: string }> {
        return this.apiService.post('nBackOffice', 'Admin/CreateLoginMethod', request);
    }

    public removeLoginMethod(id: number): Promise<{ errorMessage?: string }> {
        return this.apiService.post('nBackOffice', 'Admin/RemoveLoginMethod', { id });
    }

    public fetchGroupMembershipCancellationsToCommit(): Promise<GroupToCancelOrApproveModel[]> {
        return this.apiService.post('nUser', 'GroupMembership/CancellationsToCommitWithHigh', {});
    }

    public fetchGroupMembershipGroupsNeedingApproval(): Promise<{
        groupsNeedingApproval: GroupToCancelOrApproveModel[];
    }> {
        return this.apiService.post('nUser', 'GroupMembership/GroupsNeedingApprovalWithHigh', {});
    }

    public fetchGroupsAboutToExpireCreatedByUser(
        userId: number
    ): Promise<{ groupsAboutToExpire: GroupToCancelOrApproveModel[] }> {
        return this.apiService.post('nUser', 'GroupMembership/GroupsAboutToExpireCreatedByUserWithHigh', { userId });
    }

    public approveGroupMembership(groupMembershipId: number): Promise<void> {
        return this.apiService.post('nUser', 'GroupMembership/ApproveWithHigh', { groupMembershipId });
    }

    public disapproveGroupMembership(groupMembershipId: number): Promise<void> {
        return this.apiService.post('nUser', 'GroupMembership/DisapproveWithHigh', { groupMembershipId });
    }

    public commitGroupmembershipCancellation(groupMembershipId: number): Promise<{ errorMsg: string }> {
        return this.apiService.post('nUser', 'GroupMembership/CommitCancellationWithHigh', { groupMembershipId });
    }

    public undoGroupmembershipCancellation(groupMembershipId: number): Promise<void> {
        return this.apiService.post('nUser', 'GroupMembership/UndoCancellationWithHigh', { groupMembershipId });
    }
}

export class CreateGroupmembershipRequest {
    userId: number;
    startDate: string;
    endDate: string;
    group: string;
    product: string;
}

export interface UserModel {
    Id: number;
    CreatedById: number;
    CreationDate: string;
    Name: string;
    DeletionDate?: string;
    DeletedBy?: number;
}

export interface ActiveLoginMethodModel {
    AuthenticationType: string;
    DisplayName: string;
    IsAllowedForProvider: boolean;
    IsAllowedForRegularUser: boolean;
    IsAllowedForSystemUser: boolean;
    ProviderName: string;
    UserIdentityAndCredentialsType: string;
}

export interface DataForAdministerUserModel {
    user: AdministerUserUserModel;
    groups: AdministerUserGroupModel[];
    expiredGroups: any[];
    loginMethods: AdministerUserLoginMethodModel[];
}

export interface AdministerUserGroupModel {
    ApprovedById: number;
    CreationDate: string;
    EndDate: string;
    EndedOrCancelledDate: string;
    ForProduct: string;
    GroupName: string;
    Id: number;
    IsActive: boolean;
    IsApproved: boolean;
    PendingCancellation: any;
    StartDate: string;
}

export interface AdministerUserLoginMethodModel {
    Id: number;
    AuthenticationProvider: string;
    AuthenticationType: string;
    UserIdentity: string;
}

export interface AdministerUserUserModel {
    DeletedBy: number;
    DeletionDate: string;
    DisplayName: string;
    IsRemoveAuthenticationMechanismAllowed: boolean;
    IsSystemUser: boolean;
    ProviderName: string;
    UserId: number;
}

export interface GroupToCancelOrApproveModel {
    DisplayName: string;
    ForProduct: string;
    GroupName: string;
    Id: number;
    StartDate: string;
    EndDate: string;
    CreationDate: string;
}
