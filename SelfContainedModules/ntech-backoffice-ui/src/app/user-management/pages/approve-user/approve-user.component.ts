import { Component, OnInit } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { ConfigService } from 'src/app/common-services/config.service';
import { GroupToCancelOrApproveModel, UserManagementApiService } from '../../services/user-management-api.service';

@Component({
    selector: 'app-approve-user',
    templateUrl: './approve-user.component.html',
    styles: [],
})
export class ApproveUserComponent implements OnInit {
    public m: Model = null;

    constructor(
        private configService: ConfigService,
        private userApiService: UserManagementApiService,
        private toastr: ToastrService
    ) {}

    ngOnInit(): void {
        let serviceRegistry = this.configService.getServiceRegistry();
        this.userApiService.fetchGroupMembershipCancellationsToCommit().then((cancellations) => {
            this.userApiService.fetchGroupMembershipGroupsNeedingApproval().then((needsApproval) => {
                this.userApiService
                    .fetchGroupsAboutToExpireCreatedByUser(this.configService.getCurrentUserId())
                    .then((aboutToExpire) => {
                        this.m = {
                            isPredCreditIncluded: serviceRegistry.containsService('nPreCredit'),
                            groupMembershipsToApprove: this.castToLocal(needsApproval?.groupsNeedingApproval || []),
                            groupMembershipCancellationsToCommit: this.castToLocal(cancellations || []),
                            expiredGroups: this.castToLocal(aboutToExpire?.groupsAboutToExpire || []),
                        };
                    });
            });
        });
    }

    handle(m: GroupToCancelOrApproveModelWithLocalState, isApproved: boolean, evt?: Event) {
        evt?.preventDefault();

        let result: Promise<void>;
        if (isApproved) {
            result = this.userApiService.approveGroupMembership(m.Id);
        } else {
            result = this.userApiService.disapproveGroupMembership(m.Id);
        }

        result.then(() => {
            if (isApproved) {
                m.IsApproved = true;
            } else {
                m.IsCanceled = true;
            }
        });
    }

    cancelGroupMembership(g: GroupToCancelOrApproveModelWithLocalState, evt?: Event) {
        evt?.preventDefault();
        this.m.membershipToCancelIfConfirmed = g;
    }

    commitGroupmembershipCancellation(evt?: Event) {
        evt.preventDefault();
        this.userApiService.commitGroupmembershipCancellation(this.m.membershipToCancelIfConfirmed.Id).then((x) => {
            if (x.errorMsg) {
                this.toastr.error(x.errorMsg);
            } else {
                this.m.membershipToCancelIfConfirmed.IsCanceled = true;
                this.m.membershipToCancelIfConfirmed = null;
            }
        });
    }

    undoGroupmembershipCancellation(membershipToUndo: GroupToCancelOrApproveModelWithLocalState, evt?: Event) {
        evt?.preventDefault();
        this.userApiService.undoGroupmembershipCancellation(membershipToUndo.Id).then(() => {
            membershipToUndo.IsUndone = true;
            membershipToUndo = null; //wut?
        });
    }

    rejectCancellation(evt?: Event) {
        evt?.preventDefault();
        this.m.membershipToCancelIfConfirmed = null;
    }

    private castToLocal(items: GroupToCancelOrApproveModel[]): GroupToCancelOrApproveModelWithLocalState[] {
        return items as GroupToCancelOrApproveModelWithLocalState[];
    }
}

class Model {
    isPredCreditIncluded: boolean;
    groupMembershipsToApprove: GroupToCancelOrApproveModelWithLocalState[];
    groupMembershipCancellationsToCommit: GroupToCancelOrApproveModelWithLocalState[];
    expiredGroups: GroupToCancelOrApproveModelWithLocalState[];
    membershipToCancelIfConfirmed?: GroupToCancelOrApproveModelWithLocalState;
}

interface GroupToCancelOrApproveModelWithLocalState extends GroupToCancelOrApproveModel {
    IsUndone?: boolean;
    IsCanceled?: boolean;
    IsApproved?: boolean;
}
