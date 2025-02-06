import { Component, Input, SimpleChanges } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import {
    AssignedHandler,
    SharedApplicationApiService,
} from 'src/app/shared-application-components/services/shared-loan-application-api.service';

@Component({
    selector: 'application-assigned-handlers',
    templateUrl: './application-assigned-handlers.component.html',
    styles: [],
})
export class ApplicationAssignedHandlersComponent {
    constructor(private toastrService: ToastrService) {}
    @Input() public initialData: ApplicationAssignedHandlersComponentInitialData;
    public m: Model;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        this.fetchHandlersState(false, false);
    }

    fetchHandlersState(isExpanded?: boolean, isAddUserMode?: boolean) {
        let wasExpanded = this.m?.isExpanded;
        let wasUserAddMode = this.m?.isAddUserMode;

        this.m = null;

        if (!this.initialData) {
            return;
        }

        let i = this.initialData;
        i.applicationApiService
            .fetchApplicationAssignedHandlers(i.applicationNr, true, true)
            .then((x) => {
                if (!x) return;

                this.m = {
                    assignedHandlers: x.AssignedHandlers,
                    possibleAssignedHandlers: x.PossibleHandlers?.filter(
                        (h) => !x?.AssignedHandlers?.find((y) => y.UserId == h.UserId)
                    ),
                    selectedAddHandlerUserId: null,
                    isExpanded: isExpanded ?? wasExpanded,
                    isAddUserMode: isAddUserMode ?? wasUserAddMode,
                };
            })
            .catch((_) => {
                this.toastrService.warning('Error fetching handler assignment.');
            });
    }

    toggleExpanded(evt?: Event) {
        evt?.preventDefault();
        this.m.isAddUserMode = false;
        this.m.isExpanded = !this.m.isExpanded;
    }

    removeAssignedHandler(userId: number, evt?: Event) {
        evt?.preventDefault();

        if (!userId) return;

        let i = this.initialData;
        i.applicationApiService.setApplicationAssignedHandlers(i.applicationNr, null, [userId]).then((_) => {
            this.fetchHandlersState(null, false);
        });
    }

    beginEdit(evt?: Event) {
        evt?.preventDefault();
        this.m.isAddUserMode = true;
    }

    commitEdit(evt?: Event) {
        evt?.preventDefault();

        if (!this.m.selectedAddHandlerUserId) return;

        let i = this.initialData;
        i.applicationApiService
            .setApplicationAssignedHandlers(i.applicationNr, [this.m.selectedAddHandlerUserId], null)
            .then((_) => {
                this.fetchHandlersState(true, false);
            });
    }

    cancelEdit(evt?: Event) {
        evt?.preventDefault();
        this.m.selectedAddHandlerUserId = null;
        this.m.isAddUserMode = false;
    }
}

export class Model {
    assignedHandlers: AssignedHandler[];
    selectedAddHandlerUserId: number;
    possibleAssignedHandlers: AssignedHandler[];
    isAddUserMode: boolean;
    isExpanded: boolean;
}

export class ApplicationAssignedHandlersComponentInitialData {
    applicationNr: string;
    applicationApiService: SharedApplicationApiService;
}
