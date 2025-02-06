import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CustomerpagesShellInitialData } from '../../../shared-components/customerpages-shell/customerpages-shell.component';
import { UlStandardPreApplicationService } from '../../services/pre-application.service';
import { CustomerPagesConfigService } from '../../../common-services/customer-pages-config.service';
import { generateUniqueId } from 'src/app/common.types';
import { BankSharingMessageCode, KreditzDatashareIframeComponentInitialData } from 'projects/ntech-components/src/lib/kreditz-datashare-iframe/kreditz-datashare-iframe.component';

@Component({
    selector: 'np-pre-application-datashare',
    templateUrl: './pre-application-datashare.component.html',
    styleUrls: ['./pre-application-datashare.component.scss']
})
export class PreApplicationDatashareComponent {
    constructor(private route: ActivatedRoute, private storageService: UlStandardPreApplicationService, private config: CustomerPagesConfigService,
        private router: Router) {

    }

    public shellData: CustomerpagesShellInitialData = {
        logoRouterLink: null,
        skipBodyLayout: false,
        wideNavigation: false
    };

    public errorMessage: string;
    public m: Model;

    async ngOnInit() {
        await this.reload(this.route.snapshot.params['preApplicationId']);
    }

    private async reload(preApplicationId: string) {
        this.m = null;
        this.errorMessage = null;

        let {isEnabled, settings} = await this.storageService.getUlStandardWebApplicationSettings();
        if(!isEnabled) {
            this.errorMessage = this.config.baseCountry() === 'SE' ? 'Ansökan inaktiv' : 'Application not active'
            return;
        }

        let application = this.storageService.load(preApplicationId);
        if(!application) {
            this.errorMessage = this.config.baseCountry() === 'SE' ? 'Ansökan finns inte' : 'No such application exists'
            return;
        }

        let dataSharingSettings = settings?.dataSharing;
        if(dataSharingSettings?.providerName !== 'kreditz') {
            this.errorMessage = this.config.baseCountry() === 'SE' ? 'Delning ej aktivt' : 'Sharing disabled';
            return;
        }
        let applicant1 = application?.applicants?.length > 0 ? application.applicants[0] : null;
        if(!applicant1 || !applicant1.civicRegNr) {
            this.errorMessage = this.config.baseCountry() === 'SE' ? 'Delning ej möjligt' : 'Sharing not possible';
            return;
        }

        let useMock = settings.dataSharing.useMock;
        if(!this.config.isNTechTest() && useMock) {
            this.errorMessage = this.config.baseCountry() === 'SE' ? 'Delning ej möjligt' : 'Sharing not possible';
            return;
        }

        let caseId = (useMock ? 'kzm_' : 'kzp_') + generateUniqueId(25);
        applicant1.kreditzCaseId = caseId;
        this.storageService.save(preApplicationId, application);
        this.m = {
            preApplicationId: preApplicationId,
            kzData: useMock ? null : {
                baseCountry: this.config.baseCountry(),
                isNTechTest: this.config.isNTechTest(),
                clientId: settings.dataSharing.iFrameClientId,
                civicRegNr: this.config.isNTechTest() && dataSharingSettings.testCivicRegNr
                    ? dataSharingSettings.testCivicRegNr
                    : applicant1.civicRegNr,
                caseId: caseId,
                fetchDataForMonths: settings.dataSharing.fetchMonthCount,
                onMessage: (code, hideUi) => {
                    if(code === BankSharingMessageCode.authenticationCancelled) {
                        hideUi();
                        this.m = null;
                        this.errorMessage = this.config.baseCountry() === 'SE' ? 'Delning avbruten' : 'Sharing cancelled';
                    } else if(code === BankSharingMessageCode.userFlowCompleted) {
                        hideUi();
                        this.m.kzData = null;
                        let now = Date.now();
                        this.m.waiting = {
                            startTime: now,
                            lastPollTime: now,
                            waitSeconds: 0,
                        };
                        this.reScheduleWait(caseId);
                    }
                }
            },
            kzMockData: !useMock ? null : {

            }
        }
    }

    private onShareFailed() {
        this.m = null;
        this.errorMessage = this.errorMessage = this.config.baseCountry() === 'SE'
            ? 'Din bank har inte delat något efter fem minuter. Något har gått fel.'
            : 'Your bank did not share any data within five minutes. Something went wrong.';
        return;
    }

    private onBankDataShared() {
        let preApplicationId = this.m.preApplicationId;
        this.m = {
            preApplicationId: this.m.preApplicationId,
            hasAccountData: true
        };

        let application = this.storageService.load(preApplicationId);
        setTimeout(() => {
            if(!this.storageService.exists(preApplicationId)) {
                return; //Guard against backclicks, double clicks or other unforseen interactions
            }
            this.storageService.createApplication(application).then(x => {
                this.storageService.delete(preApplicationId);
                this.router.navigate(['ul-webapplications/application-received/' + x.ApplicationNr]);
            });
        }, 0);
    }

    reScheduleWait(caseId: string) {
        if(this.m.waiting.waitSeconds > 5*60) {
            this.onShareFailed();
        }
        setTimeout(() => {
            let now = Date.now();
            this.m.waiting.waitSeconds = Math.ceil((now - this.m.waiting.startTime) / 1000);
            if(now - this.m.waiting.lastPollTime >= 5000) {
                this.m.waiting.lastPollTime = now;
                this.storageService.hasAccountSharingDataArrived(caseId, true).then(x => {
                    if(x.hasAccountData) {
                        this.onBankDataShared();
                    } else {
                        this.reScheduleWait(caseId);
                    }
                })
            } else {
                this.reScheduleWait(caseId);
            }
        }, 500);
    }

    public simulateShareSucess(evt ? : Event) {
        evt?.preventDefault();
        this.onBankDataShared();
    }

    public simulateShareFailed(evt ? : Event) {
        evt?.preventDefault();
        this.onShareFailed();
    }
}

interface Model {
    preApplicationId: string
    kzData ?: KreditzDatashareIframeComponentInitialData
    waiting ?: {
        startTime: number
        lastPollTime: number
        waitSeconds: number
    }
    kzMockData ?: {

    }
    hasAccountData?: boolean
}
