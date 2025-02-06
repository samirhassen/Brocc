import { Component, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { ComplexApplicationListRow } from 'src/app/common-services/complex-application-list';
import { CustomerPagesEventService } from '../../../common-services/customerpages-event.service';
import { CustomerPagesAgreementInitialData } from '../../components/customer-pages-agreement/customer-pages-agreement.component';
import { CustomerPagesApplicationMessagesInitialData } from '../../../shared-components/customer-pages-application-messages/customer-pages-application-messages.component';
import { CustomerPagesApplicationOfferInitialData } from '../../components/customer-pages-application-offer/customer-pages-application-offer.component';
import { CustomerPagesBankAccountsInitialData } from '../../components/customer-pages-bankaccounts/customer-pages-bankaccounts.component';
import { DirectDebitInitialData } from '../../components/customer-pages-direct-debit/customer-pages-direct-debit.component';

import {
    ApplicationExtendedModel,
    ApplicationTask,
    CustomerPagesApplicationsApiService,
    DirectDebitTaskModel,
} from '../../services/customer-pages-applications-api.service';
import { IApplicationTasksSupportFunctions, TaskDisplayState } from './customer-pages-application-tasks.component';

@Component({
    selector: 'np-customer-pages-application',
    templateUrl: './customer-pages-application.component.html',
    styleUrls: ['./customer-pages-application-tasks.scss'],
})
export class CustomerPagesApplicationComponent implements OnInit, IApplicationTasksSupportFunctions {
    constructor(
        private route: ActivatedRoute,
        private apiService: CustomerPagesApplicationsApiService,
        private eventService: CustomerPagesEventService,
        private titleService: Title,
        private router: Router
    ) {}

    public m: Model;

    public focusedState: TaskDisplayState = 'focused';
    public normalState: TaskDisplayState = 'normal';

    ngOnInit(): void {
        this.reload(this.route.snapshot.params['applicationNr'], null);
    }

    private reload(applicationNr: string, currentlyOpenTask?: TaskModel) {
        if (this.m?.subs) {
            for (let sub of this.m.subs) {
                sub.unsubscribe();
            }
        }
        this.apiService.fetchApplication(applicationNr).then((x) => {
            let hasRejectedOffer = false;
            let hasAcceptedOffer = false;
            if (x.Application?.CurrentOffer?.OfferItems) {
                let offerItems = ComplexApplicationListRow.fromDictionary(
                    'temp',
                    1,
                    x.Application?.CurrentOffer?.OfferItems
                );
                hasRejectedOffer = offerItems.getUniqueItem('customerDecisionCode') == 'rejected';
                hasAcceptedOffer = offerItems.getUniqueItem('customerDecisionCode') == 'accepted';
            }

            let isWaitingForOffer = !x.Application?.CurrentOffer && x.Application?.IsFutureOfferPossible;
            let isPossibleToDecideOffer = x.Application?.CurrentOffer?.IsPossibleToDecide;

            //Put new tasks above this line
            let betweenOfferAndSignatureTaskGroup = new TaskGroup('Villkoren för erbjudandet');
            let signatureTaskGroup = new TaskGroup('Signering av avtal');
            let paymentTaskGroup = new TaskGroup('Väntar på utbetalning');

            let m: Model = {
                applicationNr: applicationNr,
                offerInitialData: x.Application.CurrentOffer
                    ? {
                          applicationNr: applicationNr,
                          offerItems: x.Application.CurrentOffer.OfferItems,
                          isPossibleToDecide: isPossibleToDecideOffer,
                      }
                    : null,
                hasRejectedOffer: hasRejectedOffer,
                isWaitingForOffer: isWaitingForOffer,
                isActive: x.Application.IsActive,
                subs: [],
                tasks: [],
                taskGroups: [betweenOfferAndSignatureTaskGroup, signatureTaskGroup, paymentTaskGroup],
                messagesInitialData: {
                    isInitiallyExpanded: false,
                    applicationNr: applicationNr,
                    isApplicationActive: x.Application.IsActive,
                    isMortgagaeLoan: false,
                    isInactiveMessagingAllowed: x.Application.IsInactiveMessagingAllowed,
                },
            };

            let addTask = (task: TaskModel) => {
                m.tasks.push(task);
                //headertext used as a poor mans taskId here. if headers become dynamic in the future, change this for some sort of task code
                if (
                    currentlyOpenTask &&
                    task.headerText === currentlyOpenTask.headerText &&
                    this.isTaskOpenable(task)
                ) {
                    m.openTask = { model: task.createOpenTask(), parentTask: task };
                }
                return task;
            };

            if (x.Application.KycTask) {
                let task = addTask({
                    headerText: 'Kundkännedom',
                    status: x.Application.KycTask,
                    isKycTask: true,
                    createOpenTask: () => {
                        throw 'Not openable'
                    },
                });
                betweenOfferAndSignatureTaskGroup.tasks.push(task);
            }

            if (x.Application.BankAccountsTask) {
                if(x.Application.KycTask?.IsActive === true) {
                    //We ensure kyc questions are answered before account. This is safe to remove if needed. It just helps a bit with the automation step order.
                    x.Application.BankAccountsTask.IsActive = false;
                }
                let task = addTask({
                    headerText: 'Mottagarkonton för utbetalning',
                    status: x.Application.BankAccountsTask,
                    createOpenTask: () => {
                        return {
                            headerText: 'Utbetalningar',
                            bankAccountsInitialData: {
                                applicationNr: applicationNr,
                                task: x.Application.BankAccountsTask,
                                otherLoanTypes: x.Application.Enums.OtherLoanTypes,
                            },
                        };
                    },
                });
                betweenOfferAndSignatureTaskGroup.tasks.push(task);
            }

            if (x.Application.AgreementTask) {
                let task = addTask({
                    headerText: 'Signering av avtal',
                    status: x.Application.AgreementTask,
                    createOpenTask: () => {
                        return {
                            headerText: 'Signering av avtal',
                            agreementInitialData: {
                                applicationNr: applicationNr,
                                task: x.Application.AgreementTask,
                            },
                        };
                    },
                });
                signatureTaskGroup.tasks.push(task);
            }

            if (x.Application.DirectDebitTask) {
                if(x.Application.KycTask?.IsActive === true) {
                    //We ensure kyc questions are answered before account. This is safe to remove if needed. It just helps a bit with the automation step order.
                    x.Application.DirectDebitTask.IsActive = false;
                }
                let task = addTask(this.getDirectDebitTask(applicationNr, x.Application.DirectDebitTask));
                betweenOfferAndSignatureTaskGroup.tasks.push(task);
            }

            if (x.Application.IsActive) {
                let betweenOfferAndSignatureDisplayState: TaskDisplayState = 'hidden';

                let hasBetweenOfferAndSignatureReachedVisible = hasAcceptedOffer;
                let hasBetweenOfferAndSignatureReachedActive = hasAcceptedOffer;
                let hasBetweenOfferAndSignatureBeenCompleted = betweenOfferAndSignatureTaskGroup.areAllTasksAccepted();

                betweenOfferAndSignatureDisplayState = !hasBetweenOfferAndSignatureReachedVisible
                    ? 'hidden'
                    : !hasBetweenOfferAndSignatureBeenCompleted
                    ? 'focused'
                    : 'normal';

                if (betweenOfferAndSignatureDisplayState === 'focused') {
                    betweenOfferAndSignatureTaskGroup.displayState = 'focused';
                    if (hasBetweenOfferAndSignatureReachedActive) {
                        betweenOfferAndSignatureTaskGroup.texts = [
                            'För att kunna gå vidare med din ansökan behöver du gå igenom nedanstående uppgifter.',
                        ];
                    }
                } else if (betweenOfferAndSignatureDisplayState === 'normal') {
                    betweenOfferAndSignatureTaskGroup.displayState = 'normal';
                }

                TaskGroup.setupBetweenOfferAndSignatureGroup(hasAcceptedOffer, betweenOfferAndSignatureTaskGroup);
                TaskGroup.setupSignatureGroup(
                    hasBetweenOfferAndSignatureBeenCompleted,
                    x.Application,
                    signatureTaskGroup
                );

                if (
                    betweenOfferAndSignatureTaskGroup.isDisplayNormalAndHasAllTasksAccepted() &&
                    signatureTaskGroup.isDisplayNormalAndHasAllTasksAccepted()
                ) {
                    paymentTaskGroup.displayState = 'focused';
                    paymentTaskGroup.texts = [
                        'Din ansökan behandlas där de sista kontrollerna genomförs.',
                        'Utbetalning sker normalt inom ett par arbetsdagar.',
                    ];
                }
            }

            m.subs.push(
                this.eventService.applicationEvents.subscribe((x) => {
                    if (x?.isReloadApplicationEvent() && this.m?.applicationNr === x.customData) {
                        //note: if we ever want to preseve popups sometimes but not always, replace reload application event with a custom event that has a preservePopup boolean
                        this.reload(x.customData, this.m?.openTask?.parentTask);
                    } else if (x?.isReloadApplicationAndClosePopupEvent() && this.m?.applicationNr === x.customData) {
                        this.reload(x.customData);
                    }
                })
            );

            this.m = m;
            this.titleService.setTitle('Ansökan ' + applicationNr);
        });
    }

    private getDirectDebitTask(applicationNr: string, task: DirectDebitTaskModel) {
        return {
            headerText: 'Autogiro',
            status: task,
            createOpenTask: () => {
                return {
                    headerText: 'Autogiro',
                    directDebitInitialData: {
                        applicationNr: applicationNr,
                        task: task,
                    },
                };
            },
        };
    }

    getFilteredTaskGroups(displayState: TaskDisplayState) {
        if (!this.m.taskGroups) {
            return [];
        }
        return this.m.taskGroups.filter((x) => x.displayState === displayState);
    }

    async openTask(task: TaskModel, evt?: Event) {
        evt?.preventDefault();

        if (!this.isTaskOpenable(task)) {
            return;
        }

        if(task.isKycTask) {
            let {sessionId} = await this.apiService.createKycQuestionSession(this.m.applicationNr);
            this.eventService.isLoading.next(true);
            this.router.navigate(['public-kyc/questions-session/' + sessionId])
        } else {
            this.m.openTask = { parentTask: task, model: task.createOpenTask() };
        }
    }

    closeTask(evt?: Event) {
        evt?.preventDefault();

        this.m.openTask = null;
    }

    private isTaskOpenable(task: TaskModel) {
        return task.status.IsActive || (task.status.IsAccepted && !task.isKycTask);
    }
}

export class CustomerPagesApplicationReloadEvent {
    public static EventName: string = 'CustomerPagesApplicationReloadEvent';

    constructor(public applicationNr: string, public initiallyOpenTaskCode: string) {}
}

class Model {
    applicationNr: string;
    offerInitialData: CustomerPagesApplicationOfferInitialData;
    isWaitingForOffer: boolean;
    hasRejectedOffer: boolean;
    isActive: boolean;
    subs: Subscription[];
    tasks: TaskModel[];
    openTask?: {
        parentTask: TaskModel;
        model: OpenTaskModel;
    };
    taskGroups: TaskGroup[];
    messagesInitialData: CustomerPagesApplicationMessagesInitialData;
}

class TaskGroup {
    constructor(public title: string) {}

    texts: string[] = [];
    displayState: TaskDisplayState = 'hidden';
    tasks: TaskModel[] = [];

    areAllTasksAccepted() {
        return this.tasks.findIndex((x) => x?.status?.IsAccepted !== true) < 0;
    }

    isDisplayNormalAndHasAllTasksAccepted() {
        return this.displayState === 'normal' && this.areAllTasksAccepted();
    }

    static setupBetweenOfferAndSignatureGroup(hasAcceptedOffer: boolean, betweenOfferAndSignatureTaskGroup: TaskGroup) {
        let betweenOfferAndSignatureDisplayState: TaskDisplayState = 'hidden';

        let hasBetweenOfferAndSignatureReachedVisible = hasAcceptedOffer;
        let hasBetweenOfferAndSignatureReachedActive = hasAcceptedOffer;
        let hasBetweenOfferAndSignatureBeenCompleted = betweenOfferAndSignatureTaskGroup.areAllTasksAccepted();

        betweenOfferAndSignatureDisplayState = !hasBetweenOfferAndSignatureReachedVisible
            ? 'hidden'
            : !hasBetweenOfferAndSignatureBeenCompleted
            ? 'focused'
            : 'normal';

        if (betweenOfferAndSignatureDisplayState === 'focused') {
            betweenOfferAndSignatureTaskGroup.displayState = 'focused';
            if (!hasBetweenOfferAndSignatureReachedActive) {
                betweenOfferAndSignatureTaskGroup.texts = [
                    'För att kunna gå vidare med ansökan behöver du gå igenom nedanstående uppgifter.',
                ];
            }
        } else if (betweenOfferAndSignatureDisplayState === 'normal') {
            betweenOfferAndSignatureTaskGroup.displayState = 'normal';
        }
    }

    static setupSignatureGroup(
        hasBetweenOfferAndSignatureBeenCompleted: boolean,
        application: ApplicationExtendedModel,
        signatureTaskGroup: TaskGroup
    ) {
        let signatureDisplayState: TaskDisplayState = 'hidden';

        let hasSignatureReachedVisible = hasBetweenOfferAndSignatureBeenCompleted;
        let hasSignatureReachedActive = application?.AgreementTask?.IsActive || application?.AgreementTask?.IsAccepted;
        let hasSignatureBeenCompleted = signatureTaskGroup.areAllTasksAccepted();

        signatureDisplayState = !hasSignatureReachedVisible
            ? 'hidden'
            : !hasSignatureBeenCompleted
            ? 'focused'
            : 'normal';

        if (signatureDisplayState === 'focused') {
            if (!hasSignatureReachedActive) {
                signatureTaskGroup.texts = [
                    'Din ansökan är under behandling, vi återkopplar till dig när någonting förändras i ditt ärende.',
                ];
            }
            signatureTaskGroup.displayState = 'focused';
        } else if (signatureDisplayState === 'normal') {
            signatureTaskGroup.displayState = 'normal';
        }
    }
}

export class TaskModel {
    headerText: string;
    status: ApplicationTask;
    createOpenTask: () => OpenTaskModel;
     /*
     NOTE: This is probably not the best long term solution. The whole task abstraction in use here is more of a problem than helpful. If you find this thinking about adding more
           similiar booleans, consider just rewriting this whole page and api without this whole task abstraction and task group thing. It will likely make for way simpler code going forward.
     */
    isKycTask?: boolean
}

export interface ISharedWithTasksFunctions {}

class OpenTaskModel {
    headerText: string;
    agreementInitialData?: CustomerPagesAgreementInitialData;
    bankAccountsInitialData?: CustomerPagesBankAccountsInitialData;
    directDebitInitialData?: DirectDebitInitialData;
}
