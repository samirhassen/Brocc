import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { NtechEventService, ReloadApplicationEventName } from 'src/app/common-services/ntech-event.service';
import { ApplicationBasisComponentInitialData } from '../../components/application-basis/application-basis.component';
import { UnsecuredLoanApplicationApiService } from '../../services/unsecured-loan-application-api.service';

@Component({
    selector: 'app-application-basis-page',
    templateUrl: './application-basis-page.component.html',
    styles: [],
})
export class ApplicationBasisPageComponent implements OnInit {
    constructor(
        private route: ActivatedRoute,
        private applicationApiService: UnsecuredLoanApplicationApiService,
        private eventService: NtechEventService
    ) {}

    public m: Model;

    async ngOnInit(): Promise<void> {
        this.eventService.applicationEvents.subscribe((x) => {
            if (
                x.eventCode === ReloadApplicationEventName &&
                this.m?.applicationBasisInitialData?.application?.applicationNr === x.customData
            ) {
                this.reload(this.m?.applicationBasisInitialData?.application?.applicationNr);
            }
        });
        await this.reload(this.route.snapshot.params['applicationNr']);
    }

    private async reload(applicationNr: string) {
        this.m = null;

        let result = await this.applicationApiService.fetchApplicationInitialData(applicationNr);

        if (result === 'noSuchApplicationExists') {
            return;
        }

        this.m = {
            applicationBasisInitialData: {
                application: result,
                forceReadonly: !result.applicationInfo.IsActive,
            },
        };
    }
}

class Model {
    applicationBasisInitialData: ApplicationBasisComponentInitialData;
}
