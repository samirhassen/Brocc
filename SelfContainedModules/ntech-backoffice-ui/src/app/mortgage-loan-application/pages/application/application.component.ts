import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { CustomerInfoInitialData } from 'src/app/common-components/customer-info/customer-info.component';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { NtechEventService, ReloadApplicationEventName } from 'src/app/common-services/ntech-event.service';
import { ApplicationAssignedHandlersComponentInitialData } from 'src/app/shared-application-components/components/application-assigned-handlers/application-assigned-handlers.component';
import { ApplicationCancelButtonsComponentInitialData } from 'src/app/shared-application-components/components/application-cancel-buttons/application-cancel-buttons.component';
import { ApplicationCommentsInitialData } from 'src/app/shared-application-components/components/application-comments/application-comments.component';
import { WorkflowStepHelper } from 'src/app/shared-application-components/services/workflow-helper';
import { StepStatusBlockInitialData } from 'src/app/shared-application-components/components/step-status-block/step-status-block.component';
import { MortgageLoanApplicationApiService } from '../../services/mortgage-loan-application-api.service';
import { TestFunctionsModel } from 'src/app/common-components/test-functions-popup/test-functions-popup.component';
import { WorkflowStepInitialData } from '../../steps/workflow-step';
import { BehaviorSubject } from 'rxjs';

@Component({
    selector: 'app-application',
    templateUrl: './application.component.html',
    styleUrls: ['./application.component.scss'],
})
export class ApplicationComponent implements OnInit {
    constructor(
        private route: ActivatedRoute,
        private applicationApiService: MortgageLoanApplicationApiService,
        private eventService: NtechEventService,
        private toastr: ToastrService,
        private router: Router
    ) {
        this.navigateToApplicationMessages = this.navigateToApplicationMessages.bind(this);
    }

    public m: Model;
    public isLoading: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);

    ngOnInit(): void {
        this.eventService.applicationEvents.subscribe((x) => {
            if (x.eventCode === ReloadApplicationEventName && this.m?.applicationNr === x.customData) {
                this.reload(this.m.applicationNr);
            }
        });
        this.reload(this.route.snapshot.params['applicationNr']);
    }

    private async reload(applicationNr: string) {
        this.m = null;
        let x = await this.applicationApiService.fetchApplicationInitialData(applicationNr);

        if (x == 'noSuchApplicationExists') {
            this.toastr.warning('No such application exists');
            return;
        }

        let applicationNavigationTarget = this.createApplicationNavTarget(applicationNr);

        let testFunctions = new TestFunctionsModel();
        let m: Model = {
            applicationNr: x.applicationNr,
            cancelButtonsInitialData: new ApplicationCancelButtonsComponentInitialData(
                x.applicationNr,
                this.applicationApiService
            ),
            commentsInitialData: {
                applicationInfo: x.applicationInfo,
                applicationApiService: this.applicationApiService,
            },
            applicationCustomerInfo1InitialData: {
                customerId: x.customerIdByApplicantNr[1],
                backTarget: applicationNavigationTarget,
            },
            applicationCustomerInfo2InitialData:
                x.nrOfApplicants < 2
                    ? null
                    : {
                          customerId: x.customerIdByApplicantNr[2],
                          backTarget: applicationNavigationTarget,
                      },
            assignedHandlersInitialData: {
                applicationNr: x.applicationNr,
                applicationApiService: this.applicationApiService,
            },
            steps: [],
            unreadCustomerMessagesCount: 0,
            testFunctions: testFunctions,
        };

        let workflowModel = x.workflow.Model;
        for (let step of workflowModel.Steps) {
            let stepHelper = new WorkflowStepHelper(workflowModel, step, x.applicationInfo);
            m.steps.push({
                componentName: step.ComponentName,
                initialData: {
                    application: x,
                    applicationNavigationTarget: applicationNavigationTarget,
                    workflow: {
                        step: stepHelper,
                        model: workflowModel,
                    },
                    testFunctions: testFunctions,
                },
                statusInitialData: {
                    isActive: x.applicationInfo.IsActive,
                    isInitiallyExpanded: false,
                    step: stepHelper,
                },
            });
        }

        //Expand the first initial step
        if (x.applicationInfo.IsActive) {
            for (let step of m.steps) {
                if (step.statusInitialData.step.isStatusInitial()) {
                    step.statusInitialData.isInitiallyExpanded = true;
                    break;
                }
            }
        }

        let mainCustomerId = m.applicationCustomerInfo1InitialData?.customerId;
        let channelType = 'Application_MortgageLoan';
        let unreadResult = await this.applicationApiService.fetchApplicationUnreadCustomerMessagesCount(
            applicationNr,
            mainCustomerId,
            channelType
        );
        m.unreadCustomerMessagesCount = unreadResult.TotalMessageCount ?? 0;

        this.m = m;

        let titles = this.applicationApiService.createApplicationPageTitle(x.applicationInfo);
        this.eventService.setCustomPageTitle(titles?.title, titles?.browserTitle);
    }

    public getDocumentsUrl() {
        return this.router.createUrlTree(['mortgage-loan-application/documents', this.m.applicationNr]).toString();
    }

    private createApplicationNavTarget(applicationNr: string) {
        return CrossModuleNavigationTarget.create('MortgageLoanApplicationStandard', {
            applicationNr: applicationNr,
        });
    }

    navigateToApplicationMessages(evt?: Event): void {
        evt?.preventDefault();

        if (!this.m) return;

        this.router.navigate(['/secure-messages/channel/'], {
            queryParams: {
                channelId: this.m.applicationNr,
                customerId: this.m.applicationCustomerInfo1InitialData?.customerId,
                channelType: 'Application_MortgageLoan',
                backTarget: this.createApplicationNavTarget(this.m.applicationNr),
            },
        });
    }
}

class Model {
    applicationNr: string;
    unreadCustomerMessagesCount: number;
    cancelButtonsInitialData: ApplicationCancelButtonsComponentInitialData;
    commentsInitialData: ApplicationCommentsInitialData;
    applicationCustomerInfo1InitialData: CustomerInfoInitialData;
    applicationCustomerInfo2InitialData: CustomerInfoInitialData;
    assignedHandlersInitialData: ApplicationAssignedHandlersComponentInitialData;
    steps: {
        componentName: string;
        initialData: WorkflowStepInitialData;
        statusInitialData: StepStatusBlockInitialData;
    }[];
    testFunctions: TestFunctionsModel;
}
