import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { CustomerPagesConfigService } from '../../../../common-services/customer-pages-config.service';
import { CustomerPagesEventService } from '../../../../common-services/customerpages-event.service';
import {
    CustomerPagesMortgageLoanApiService,
    MlApplicationModel,
} from '../../../services/customer-pages-ml-api.service';
import { ApplicationWizardShellInitialData } from '../components/application-wizard-shell/application-wizard-shell.component';

@Component({
    selector: 'np-start-webapplication',
    templateUrl: './start-webapplication.component.html',
    styles: [],
})
export class StartWebapplicationComponent implements OnInit {
    constructor(
        private route: ActivatedRoute,
        private apiService: CustomerPagesMortgageLoanApiService,
        private configServive: CustomerPagesConfigService,
        private eventService: CustomerPagesEventService,
        private router: Router
    ) {}

    public m: Model;
    public errorMessage: string;

    ngOnInit(): void {
        this.reload(this.route.snapshot.params['sessionId']);
        this.eventService.applicationEvents.subscribe((x) => {
            if (x.eventCode === NextStepEventName && this.m) {
                let activeStepNr = this.m.wizardInitialData.activeStepNr;
                if (activeStepNr === 4) {
                    var session = this.m.session.value;
                    this.apiService.createApplication(session.application).then((newApplication) => {
                        let storageKey = getMlApplicationSessionModelStorageKey(session.sessionId);
                        localStorage.removeItem(storageKey);
                        sessionStorage.removeItem(storageKey);
                        this.router.navigateByUrl(
                            `/mortgage-loan-applications/open/application/${newApplication.ApplicationNr}/created`
                        );
                    });
                } else {
                    let data: { session: MlApplicationSessionModel } = JSON.parse(x.customData);
                    let session = data.session;
                    session.lastCompletedStep = this.m.wizardInitialData.activeStepNr;
                    this.m.wizardInitialData.activeStepNr = this.m.wizardInitialData.activeStepNr + 1;
                    this.storeSession(session);
                    this.m.session.next(session);
                }
            }
        });
    }

    private reload(sessionId: string) {
        this.m = null;

        this.loadSession(sessionId).then((session) => {
            if (!session || session.sessionId !== sessionId) {
                this.m = null;
                this.errorMessage = 'No such session exists';
                return;
            }

            let s = new BehaviorSubject<MlApplicationSessionModel>(null);

            this.m = {
                session: s,
                wizardInitialData: {
                    activeStepNr: session.lastCompletedStep ? session.lastCompletedStep + 1 : 1,
                    session: s,
                },
                test: this.configServive.isNTechTest()
                    ? {
                          isPopupVisible: false,
                      }
                    : null,
            };

            s.next(session);
        });
    }

    private loadSession(sessionId: string): Promise<MlApplicationSessionModel> {
        /*
        We move from localStorage which is basically permanent to session storage when starting to fill out the application.
        This ensures the user can be logged out or hit F5 without losing their filled out data but also doesnt store civicnrs and similar
        in local storage which might be a bit to permanent if this is IE a public computer.
        */
        let sessionKey = getMlApplicationSessionModelStorageKey(sessionId);
        let sessionDataRaw = sessionStorage.getItem(sessionKey) ?? localStorage.getItem(sessionKey);
        let session: MlApplicationSessionModel = sessionDataRaw ? JSON.parse(sessionDataRaw) : null;
        if (!session.isInitialized) {
            return this.apiService
                .shared()
                .fetchLoggedInUserDetails()
                .then((x) => {
                    let a = session.application;
                    if (!a.Applicants) {
                        a.Applicants = [
                            {
                                CivicRegNr: x.civicRegNr,
                            },
                        ];
                    }
                    session.isInitialized = true;
                    return session;
                });
        } else {
            let p = new Promise<MlApplicationSessionModel>((resolve) => resolve(session));
            return p;
        }
    }

    private storeSession(session: MlApplicationSessionModel) {
        sessionStorage.setItem(getMlApplicationSessionModelStorageKey(session.sessionId), JSON.stringify(session));
    }

    public autoFillInTest(evt?: Event) {
        evt?.preventDefault();
        this.eventService.emitApplicationEvent(AutoFillInTestEventName, this.m.wizardInitialData.activeStepNr);
    }
}

class Model {
    session: BehaviorSubject<MlApplicationSessionModel>;
    wizardInitialData: ApplicationWizardShellInitialData;
    test: {
        //TODO: This should be TestFunctionsModel and we should use the component from BO. Have not been able to get it to build so far though.
        isPopupVisible: boolean;
    };
}

export interface MlApplicationSessionModel {
    sessionId: string;
    creationDate: string;
    isInitialized?: boolean;
    lastCompletedStep?: number;
    application: MlApplicationModel;
}

export function getMlApplicationSessionModelStorageKey(sessionId: string) {
    return `MlApplicationSessionV1_${sessionId}`;
}

export const NextStepEventName = 'GotoNextApplicationStep';
export const AutoFillInTestEventName = 'AutoFillInTest';
