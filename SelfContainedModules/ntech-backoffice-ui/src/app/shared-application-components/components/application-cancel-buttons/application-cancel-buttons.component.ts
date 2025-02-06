import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { SharedApplicationApiService } from '../../services/shared-loan-application-api.service';

@Component({
    selector: 'application-cancel-buttons',
    templateUrl: './application-cancel-buttons.component.html',
    styles: [' form {margin-top: -58px }'],
})
export class ApplicationCancelButtonsComponent implements OnInit {
    constructor(private toastr: ToastrService) {}

    @Input()
    public initialData: ApplicationCancelButtonsComponentInitialData;
    public m: ApplicationActiveStateModel;

    ngOnInit() {}

    ngOnChanges(changes: SimpleChanges) {
        this.m = null;

        if (!this.initialData) {
            return;
        }

        let i = this.initialData;

        i.apiService.fetchApplicationInitialDataShared(i.applicationNr).then((applicationData) => {
            if (applicationData === 'noSuchApplicationExists') {
                this.toastr.warning('No such application exists');
                return;
            }

            this.m = {
                isActive: applicationData.applicationInfo.IsActive,
                isCancelled: applicationData.applicationInfo.IsCancelled,
                isWaitingForAdditionalInformation: applicationData.applicationInfo.IsWaitingForAdditionalInformation,
                isFinalDecisionMade: applicationData.applicationInfo.IsFinalDecisionMade,
            };
        });
    }

    cancelApplication(evt: Event) {
        evt?.preventDefault();

        if (!this.isCancelApplicationAllowed()) {
            this.toastr.warning('Cancel application not allowed.');
            return;
        }

        let i = this.initialData;

        i.apiService.cancelApplication(i.applicationNr).then((x) => {
            location.reload();
        });
    }

    reactivateApplication(evt: Event) {
        evt?.preventDefault();

        let i = this.initialData;

        i.apiService.reactivateCancelledApplication(i.applicationNr).then((x) => {
            location.reload();
        });
    }

    isApplicationActive() {
        if (!this.m) return false;

        return (
            this.m.isActive === true &&
            this.m.isFinalDecisionMade === false &&
            this.m.isWaitingForAdditionalInformation === false
        );
    }

    isReactivateApplicationAllowed() {
        if (!this.m) return false;

        return (
            this.m.isActive === false &&
            this.m.isCancelled === true &&
            this.m.isWaitingForAdditionalInformation === false
        );
    }

    isCancelApplicationAllowed() {
        if (!this.m) return false;

        return (
            this.m.isActive === true &&
            this.m.isFinalDecisionMade === false &&
            this.m.isWaitingForAdditionalInformation === false
        );
    }
}

class ApplicationActiveStateModel {
    isActive: boolean;
    isCancelled: boolean;
    isWaitingForAdditionalInformation: boolean;
    isFinalDecisionMade: boolean;
}

export class ApplicationCancelButtonsComponentInitialData {
    constructor(public applicationNr: string, public apiService: SharedApplicationApiService) {}
}
