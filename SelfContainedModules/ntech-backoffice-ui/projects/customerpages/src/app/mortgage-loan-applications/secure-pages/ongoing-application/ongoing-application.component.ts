import { Component, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { CustomerPagesEventService } from '../../../common-services/customerpages-event.service';
import { CustomerPagesApplicationMessagesInitialData } from '../../../shared-components/customer-pages-application-messages/customer-pages-application-messages.component';
import { CustomerPagesMortgageLoanApiService } from '../../services/customer-pages-ml-api.service';

@Component({
    selector: 'np-ongoing-application',
    templateUrl: './ongoing-application.component.html',
    styleUrls: ['./ongoing-application.component.scss'],
})
export class OngoingApplicationComponent implements OnInit {
    constructor(
        private route: ActivatedRoute,
        private eventService: CustomerPagesEventService,
        private titleService: Title,
        private customerPagesApiService: CustomerPagesMortgageLoanApiService,
        private router: Router
    ) {}

    public m: Model;

    ngOnInit(): void {
        this.reload(this.route.snapshot.params['applicationNr']);
    }

    private async reload(applicationNr: string) {
        if (this.m?.subs) {
            for (let sub of this.m.subs) {
                sub.unsubscribe();
            }
        }

        let application = (await this.customerPagesApiService.fetchApplication(applicationNr))?.Application;
        if (!application) {
            return;
        }

        let objectSummary: { addressText: string } = null;
        if (application.ObjectSummary) {
            let s = application.ObjectSummary;
            let parts = [
                s.AddressStreet,
                s.AddressApartmentNr || s.AddressSeTaxOfficeApartmentNr,
                s.AddressZipCode,
                s.AddressCity,
            ].filter((x) => !!x);
            objectSummary = {
                addressText: parts.join(', '),
            };
        }

        let routePrefix = ['mortgage-loan-applications/secure/application', applicationNr];

        let informationTasks: TaskModel[] = [];
        if (application.IsKycTaskActive) {
            informationTasks.push({
                headerText: 'Kundkännedom',
                isTaskAccepted: application.IsKycTaskApproved ? true : null,
                taskRoute: [...routePrefix, 'kyc'],
            });
        }
        let m: Model = {
            applicationNr: applicationNr,
            subs: [],
            messagesInitialData: {
                applicationNr: applicationNr,
                isApplicationActive: application.IsActive,
                isInitiallyExpanded: false,
                isMortgagaeLoan: true,
                isInactiveMessagingAllowed: application.IsInactiveMessagingAllowed,
            },
            isActive: application.IsActive,
            objectSummary: objectSummary,
            decisionSummary: application.LatestAcceptedDecision
                ? {
                      loanAmount: application.LatestAcceptedDecision.LoanAmount,
                  }
                : null,
            informationTasks: informationTasks,
        };

        m.subs.push(
            this.eventService.applicationEvents.subscribe((x) => {
                if (x?.isReloadApplicationEvent() && this.m?.applicationNr === x.customData) {
                    this.reload(x.customData);
                }
            })
        );

        this.m = m;
        this.titleService.setTitle('Ansökan ' + applicationNr);
    }

    getIconClass(isAccepted: boolean, isRejected: boolean) {
        let isOther = !isAccepted && !isRejected;
        return {
            'glyphicon-ok': isAccepted,
            'glyphicon-remove': isRejected,
            'glyphicon-minus': isOther,
            'glyphicon': true,
            'text-success': isAccepted,
            'text-danger': isRejected,
        };
    }

    onTaskClicked(task: TaskModel, evt?: Event) {
        evt?.preventDefault();
        this.router.navigate(task.taskRoute);
    }
}

interface Model {
    applicationNr: string;
    subs: Subscription[];
    messagesInitialData: CustomerPagesApplicationMessagesInitialData;
    isActive: boolean;
    objectSummary?: {
        addressText: string;
    };
    decisionSummary?: {
        loanAmount: number;
    };
    informationTasks: TaskModel[];
}

class TaskModel {
    headerText: string;
    isTaskAccepted?: boolean;
    taskRoute: string[];
}
