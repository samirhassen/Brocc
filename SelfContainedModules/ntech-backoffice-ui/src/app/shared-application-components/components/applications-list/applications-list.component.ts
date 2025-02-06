import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Subscription } from 'rxjs';
import { TablePagerInitialData } from 'src/app/common-components/table-pager/table-pager.component';
import { TestFunctionsModel } from 'src/app/common-components/test-functions-popup/test-functions-popup.component';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechLocalStorageService } from 'src/app/common-services/ntech-localstorage.service';
import { Dictionary } from 'src/app/common.types';
import {
    ApplicationsListAssignedHandlerModel,
    LoanStandardApplicationSearchHit,
    SharedApplicationApiService,
    WorkflowStepModel,
} from '../../services/shared-loan-application-api.service';
import { WorkflowHelper } from '../../services/workflow-helper';

const PageSize: number = 25;

@Component({
    selector: 'applications-list',
    templateUrl: './applications-list.component.html',
    styles: [],
})
export class ApplicationsListComponent implements OnInit {
    constructor(
        private eventService: NtechEventService,
        private router: Router,
        private fb: UntypedFormBuilder,
        private configService: ConfigService,
        private toastr: ToastrService,
        private localStorage: NTechLocalStorageService
    ) {}

    @Input()
    public initialData: ApplicationsListInitialData;

    public m: Model;
    public assignedHandlerUserId: string;

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        let cache = this.getCacheContainer();

        this.gotoDataPage(
            0,
            cache.getWithDefault<string>('providerName', () => 'all'),
            cache.getWithDefault<string>('listName', () => 'all'),
            cache.getWithDefault<string>('assignedHandler', () => this.configService.getCurrentUserId().toString())
        );
    }

    getApplicationNavigationTree(applicationNr: string) {
        return [
            this.initialData.isForMortgageLoans
                ? '/mortgage-loan-application/application/'
                : '/unsecured-loan-application/application/',
            applicationNr,
        ];
    }

    getApplicationNavigtionParameters(applicationNr: string) {
        return { backTarget: this.m.targetToHere.getCode() };
    }

    private navigateToApplication(applicationNr: string, evt?: Event) {
        evt?.preventDefault();
        this.router.navigate(this.getApplicationNavigationTree(applicationNr), {
            queryParams: this.getApplicationNavigtionParameters(applicationNr),
        });
    }

    private gotoDataPage(pageNr: number, providerName: string, listName: string, assignedHandlerUserId: string) {
        let allToNull = (x: string) => (x === 'all' ? null : x);
        this.initialData.applicationApiService
            .fetchStandardWorkListDataPage(
                allToNull(providerName),
                allToNull(listName),
                this.toAssignedHandlerModel(assignedHandlerUserId),
                PageSize,
                pageNr,
                {
                    includeListCounts: true,
                    includeProviders: true,
                    includeWorkflowModel: true,
                }
            )
            .then((result) => {
                //updates title for unassigned applications with AssignableCount
                if (!this.initialData.isAssignedApplications && result.AssignableCount)
                    this.eventService.emitApplicationEvent(
                        GetAssignedCountChangedEventName(this.initialData.isForMortgageLoans),
                        { newCount: result.AssignableCount }
                    );

                if (this.m?.subs) {
                    this.m.subs.forEach((x) => x.unsubscribe());
                }

                let m: Model = {
                    isForMortgageLoans: this.initialData.isForMortgageLoans,
                    isAssignedApplications: this.initialData.isAssignedApplications,
                    targetToHere: CrossModuleNavigationTarget.create(
                        this.initialData.isForMortgageLoans
                            ? 'MortgageLoanApplicationsStandard'
                            : 'NewUnsecuredLoanApplications',
                        {}
                    ),
                    form: new FormsHelper(
                        this.fb.group({
                            providerName: [providerName, [Validators.required]],
                            listName: [listName, [Validators.required]],
                            assignedHandler: [assignedHandlerUserId, [Validators.required]],
                        })
                    ),
                    providers: this.getProviderNames(result.ProviderDisplayNameByName),
                    steps: this.getWorkflowSteps(result.CurrentWorkflowModel.Steps, result.ListCountsByName),
                    assignedHandlers: this.getAssignedHandlers(result.AssignedHandlerDisplayNameByUserId),
                    searchResult: null,
                    testFunctions: this.createTestFunctions(),
                    subs: [],
                };

                m.searchResult = {
                    items: result.PageApplications,
                    pagingInitialData: {
                        totalNrOfPages: result.TotalNrOfPages,
                        currentPageNr: result.CurrentPageNr,
                        onGotoPage: (pageNr) => {
                            this.gotoDataPage(pageNr, providerName, listName, assignedHandlerUserId);
                        },
                    },
                };

                let cache = this.getCacheContainer();

                ['providerName', 'listName', 'assignedHandler'].forEach((x) => {
                    m.subs.push(
                        m.form.getFormControl(null, x).valueChanges.subscribe((_) => {
                            if (
                                x !== 'assignedHandler' ||
                                (x === 'assignedHandler' && this.initialData.isAssignedApplications)
                            ) {
                                cache.set<string>(x, this.m.form.getValue(x));
                            }
                            this.getDataPage(0);
                        })
                    );
                });

                this.m = m;
            });
    }

    private getProviderNames(providersByName: Dictionary<string>) {
        let providersArr: {
            ProviderName: string;
            DisplayToEnduserName: string;
        }[] = [];
        Object.keys(providersByName).forEach((provider) => {
            providersArr.push({
                ProviderName: provider,
                DisplayToEnduserName: providersByName[provider],
            });
        });
        return providersArr;
    }

    private getAssignedHandlers(handlersByUserId: Dictionary<string>) {
        let handlersArr: { UserId: string; UserDisplayName: string }[] = [];
        Object.keys(handlersByUserId).forEach((handler) => {
            handlersArr.push({
                UserId: handler,
                UserDisplayName: handlersByUserId[handler],
            });
        });
        return handlersArr;
    }

    private getWorkflowSteps(workflowSteps: WorkflowStepModel[], listCounts: Dictionary<number>) {
        let workflowStepsArr: {
            DisplayName: string;
            InitialListName: string;
            InitialListCurrentMemberCount: any;
        }[] = [];
        workflowSteps.forEach((step) => {
            let initialListName = WorkflowHelper.getInitialListName(step.Name);
            workflowStepsArr.push({
                DisplayName: step.DisplayName,
                InitialListName: WorkflowHelper.getInitialListName(step.Name),
                InitialListCurrentMemberCount: listCounts[initialListName] || 0,
            });
        });
        return workflowStepsArr;
    }

    private toAssignedHandlerModel(assignedHandlerUserId: string) {
        let assignedHandlerModel: ApplicationsListAssignedHandlerModel = {
            AssignedHandlerUserId: this.initialData.isAssignedApplications ? assignedHandlerUserId : null,
            ExcludeAssignedApplications: !this.initialData.isAssignedApplications,
            ExcludeUnassignedApplications: this.initialData.isAssignedApplications,
        };

        return assignedHandlerModel;
    }

    private getDataPage(pageNr: number, evt?: Event) {
        evt?.preventDefault();
        let toValue = (x: string) => this.m.form.getValue(x);
        this.gotoDataPage(pageNr, toValue('providerName'), toValue('listName'), toValue('assignedHandler'));
    }

    getProviderDisplayName(providerName: string) {
        let providers = this.m?.providers;
        if (providers) {
            for (let provider of providers) {
                if (provider.ProviderName === providerName) {
                    return provider.DisplayToEnduserName;
                }
            }
        }
        return providerName;
    }

    private createTestFunctions() {
        let testFunctions = new TestFunctionsModel();

        let addApplicationRandomizer = (
            text: string,
            request: {
                isFinalDecisionMade?: boolean;
                isRejected?: boolean;
                isCancelled?: boolean;
                nrOfApplicants?: number;
                memberOfListName?: string;
            }
        ) => {
            testFunctions.addFunctionCall(text, () => {
                this.initialData.applicationApiService.findRandomApplication(request).then((x) => {
                    if (x.ApplicationNr) {
                        this.navigateToApplication(x.ApplicationNr);
                    } else {
                        this.toastr.warning('No such application exists');
                    }
                });
            });
        };

        addApplicationRandomizer('Goto random active application - 1 applicant', {
            nrOfApplicants: 1,
        });
        addApplicationRandomizer('Goto random active application - 2 applicants', {
            nrOfApplicants: 2,
        });
        addApplicationRandomizer('Goto random rejected application', {
            isRejected: true,
        });
        addApplicationRandomizer('Goto random cancelled application', {
            isCancelled: true,
        });
        addApplicationRandomizer('Goto random created loan application', {
            isFinalDecisionMade: true,
        });

        return testFunctions;
    }

    private getCacheContainer() {
        return this.localStorage.getUserContainer(
            'application-list-' + this.initialData.cacheDiscriminator,
            this.configService.getCurrentUserId(),
            '20220621.01'
        );
    }
}

class Model {
    isForMortgageLoans: boolean;
    isAssignedApplications: boolean;
    targetToHere: CrossModuleNavigationTarget;
    form: FormsHelper;
    providers: { ProviderName: string; DisplayToEnduserName: string }[];
    steps: {
        DisplayName: string;
        InitialListName: string;
        InitialListCurrentMemberCount: number;
    }[];
    assignedHandlers: { UserId: string; UserDisplayName: string }[];
    searchResult?: {
        items: LoanStandardApplicationSearchHit[];
        pagingInitialData: TablePagerInitialData;
    };
    testFunctions: TestFunctionsModel;
    subs: Subscription[];
}

export class ApplicationsListInitialData {
    public isAssignedApplications: boolean;
    public isForMortgageLoans: boolean; //The other alternative is unsecured loan
    applicationApiService: SharedApplicationApiService;
    cacheDiscriminator: string;
}

export function GetAssignedCountChangedEventName(isForMortgageLoans: boolean) {
    //These have no special meaning, just a unique string to share between caller and callee
    return isForMortgageLoans ? '808b1bb9-ffab-4d5a-ac38-ae29583893ed' : 'eca501d2-d9cf-464f-bf26-fd41199acffc';
}
