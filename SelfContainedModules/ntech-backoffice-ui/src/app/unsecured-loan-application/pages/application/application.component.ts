import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { UnsecuredLoanApplicationApiService } from '../../services/unsecured-loan-application-api.service';
import { WorkflowStepInitialData } from '../../steps/workflow-step';
import { CustomerInfoInitialData } from 'src/app/common-components/customer-info/customer-info.component';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { ApplicationCommentsInitialData } from 'src/app/shared-application-components/components/application-comments/application-comments.component';
import { StepStatusBlockInitialData } from '../../../shared-application-components/components/step-status-block/step-status-block.component';
import { NtechEventService, ReloadApplicationEventName } from 'src/app/common-services/ntech-event.service';
import { TestFunctionsModel } from 'src/app/common-components/test-functions-popup/test-functions-popup.component';
import { ApplicationCancelButtonsComponentInitialData } from 'src/app/shared-application-components/components/application-cancel-buttons/application-cancel-buttons.component';
import { ApplicationAssignedHandlersComponentInitialData } from 'src/app/shared-application-components/components/application-assigned-handlers/application-assigned-handlers.component';
import { WorkflowStepHelper } from 'src/app/shared-application-components/services/workflow-helper';
import { ConfigService } from 'src/app/common-services/config.service';

@Component({
    selector: 'app-application',
    templateUrl: './application.component.html',
    styles: [],
})
export class ApplicationComponent implements OnInit {
    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private applicationApiService: UnsecuredLoanApplicationApiService,
        private eventService: NtechEventService,
        private config: ConfigService
    ) {
        this.navigateToApplicationMessages = this.navigateToApplicationMessages.bind(this);
    }

    public m: Model;

    public error: {
        applicationDoesNotExist?: boolean;
        unupportedWorkflowVersion?: {
            applicationVersion: string;
            currentVersion: string;
        };
    };

    ngOnInit(): void {
        this.eventService.applicationEvents.subscribe((x) => {
            if (x.eventCode === ReloadApplicationEventName && this.m?.applicationNr === x.customData) {
                this.reload(this.m.applicationNr);
            }
        });
        this.reload(this.route.snapshot.params['applicationNr']);
    }

    navigateToApplicationMessages(evt?: Event): void {
        evt?.preventDefault();

        if (!this.m) return;

        this.router.navigate(['/secure-messages/channel/'], {
            queryParams: {
                channelId: this.m.applicationNr,
                customerId: this.m.applicationCustomerInfo1InitialData?.customerId,
                channelType: 'Application_UnsecuredLoan',
                backTarget: this.createApplicationNavTarget(this.m.applicationNr),
            },
        });
    }

    createApplicationNavTarget(applicationNr: string): CrossModuleNavigationTarget {
        return CrossModuleNavigationTarget.create('NewUnsecuredLoanApplication', {
            applicationNr: applicationNr,
        });
    }

    private reload(applicationNr: string) {
        this.m = null;
        this.error = null;

        this.applicationApiService
            .fetchApplicationInitialData(applicationNr)
            .then((result) => {
                if (result === 'noSuchApplicationExists') {
                    this.error = {
                        applicationDoesNotExist: true,
                    };
                    return;
                }

                let wf = result.workflow;
                let workflowModel = wf.Model;
                if (workflowModel.WorkflowVersion !== wf.ApplicationVersion) {
                    this.error = {
                        unupportedWorkflowVersion: {
                            applicationVersion: wf.ApplicationVersion.toString(),
                            currentVersion: workflowModel.WorkflowVersion.toString(),
                        },
                    };
                    return;
                }

                let applicationNavigationTarget = this.createApplicationNavTarget(applicationNr);
                let testFunctions = new TestFunctionsModel();
                let m: Model = {
                    applicationNr: applicationNr,
                    isTest: this.config.isNTechTest(),
                    applicationCustomerInfo1InitialData: {
                        customerId: result.customerIdByApplicantNr[1],
                        backTarget: applicationNavigationTarget,
                    },
                    applicationCustomerInfo2InitialData:
                        result.nrOfApplicants < 2
                            ? null
                            : {
                                  customerId: result.customerIdByApplicantNr[2],
                                  backTarget: applicationNavigationTarget,
                              },
                    commentsInitialData: {
                        applicationInfo: result.applicationInfo,
                        applicationApiService: this.applicationApiService,
                    },
                    cancelButtonsInitialData: new ApplicationCancelButtonsComponentInitialData(
                        applicationNr,
                        this.applicationApiService
                    ),
                    assignedHandlersInitialData: {
                        applicationNr: applicationNr,
                        applicationApiService: this.applicationApiService,
                    },
                    steps: [],
                    unreadCustomerMessagesCount: 0,
                    testFunctions: testFunctions,
                };

                for (let step of workflowModel.Steps) {
                    let stepHelper = new WorkflowStepHelper(workflowModel, step, result.applicationInfo);
                    m.steps.push({
                        componentName: step.ComponentName,
                        initialData: {
                            application: result,
                            applicationNavigationTarget: applicationNavigationTarget,
                            workflow: {
                                step: stepHelper,
                                model: workflowModel,
                            },
                            testFunctions: testFunctions,
                        },
                        statusInitialData: {
                            isActive: result.applicationInfo.IsActive,
                            isInitiallyExpanded: false,
                            step: stepHelper,
                        },
                    });
                }
                //Expand the first initial step
                if (result.applicationInfo.IsActive) {
                    for (let step of m.steps) {
                        if (step.statusInitialData.step.isStatusInitial()) {
                            step.statusInitialData.isInitiallyExpanded = true;
                            break;
                        }
                    }
                }

                let titles = this.applicationApiService.createApplicationPageTitle(result.applicationInfo);
                this.eventService.setCustomPageTitle(titles?.title, titles?.browserTitle);

                this.eventService.setApplicationNr(result.applicationInfo.ApplicationNr);

                this.m = m;
            })
            .then((_) => {
                let mainCustomerId = this.m.applicationCustomerInfo1InitialData?.customerId;
                let channelType = 'Application_UnsecuredLoan';
                this.applicationApiService
                    .fetchApplicationUnreadCustomerMessagesCount(applicationNr, mainCustomerId, channelType)
                    .then((x) => {
                        this.m.unreadCustomerMessagesCount = x.TotalMessageCount ?? 0;
                    });
            });
    }
}

class Model {
    applicationNr: string;
    steps: {
        componentName: string;
        initialData: WorkflowStepInitialData;
        statusInitialData: StepStatusBlockInitialData;
    }[];
    applicationCustomerInfo1InitialData: CustomerInfoInitialData;
    applicationCustomerInfo2InitialData: CustomerInfoInitialData;
    commentsInitialData: ApplicationCommentsInitialData;
    cancelButtonsInitialData: ApplicationCancelButtonsComponentInitialData;
    assignedHandlersInitialData: ApplicationAssignedHandlersComponentInitialData;
    unreadCustomerMessagesCount: number;
    testFunctions: TestFunctionsModel;
    isTest: boolean;
}
