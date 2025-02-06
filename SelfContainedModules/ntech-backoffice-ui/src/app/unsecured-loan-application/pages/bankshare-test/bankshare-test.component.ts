import { Component, OnInit } from '@angular/core';
import { UntypedFormBuilder, Validators } from '@angular/forms';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { ToastrService } from 'ngx-toastr';
import { BankSharingMessageCode, KreditzDatashareIframeComponentInitialData } from 'projects/ntech-components/src/lib/kreditz-datashare-iframe/kreditz-datashare-iframe.component';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { FormsHelper } from 'src/app/common-services/ntech-forms-helper';
import { NTechValidationService } from 'src/app/common-services/ntech-validation.service';
import { Dictionary, StringDictionary, generateUniqueId } from 'src/app/common.types';

@Component({
    selector: 'app-bankshare-test',
    templateUrl: './bankshare-test.component.html',
    styles: [
    ]
})
export class BankshareTestComponent implements OnInit {
    constructor(private config: ConfigService, private apiService: NtechApiService, private formBuilder: UntypedFormBuilder,
        private validationService: NTechValidationService, private toastr: ToastrService, private sanitizer: DomSanitizer) { }

    public m: Model;

    async ngOnInit() {
        if(!this.config.isFeatureEnabled('ntech.feature.unsecuredloans.datasharing')) {
            this.m = {
                isEnabled: false
            }
            return;
        }
        let settings = await this.apiService.post<BankSharingSettings>('NTechHost', 'Api/PreCredit/BankShareTest/Settings', {});

        if(!(settings.isEnabled && settings.providerName === 'kreditz')) {
            this.m = {
                isEnabled: false
            }
            return;
        }

        let formData : Dictionary<any> = {
            'civicRegNr': ['', [Validators.required, this.validationService.getCivicRegNrValidator({ require12DigitCivicRegNrSe: true })]]
        };
        if(settings.providerName === 'kreditz') {
            formData['monthCount'] = ['12', [Validators.required, this.validationService.getIntegerWithBoundsValidator(1, 12)]]
        }

        this.m = {
            isEnabled: true,
            settings: settings,
            startSharing: {
                form: new FormsHelper(this.formBuilder.group(formData)),
            },
            civicRegNrPlaceholder: this.config.baseCountry() === 'SE' ? 'ÅÅÅÅMMDDXXXX' : ''
        }
    }

    public startSharing(evt ?: Event) {
        evt?.preventDefault();

        let civicRegNr: string = this.m.startSharing.form.getValue('civicRegNr');
        let monthCount: number = parseInt(this.m.startSharing.form.getValue('monthCount'));
        this.m.startSharing = null;
        if(this.m.settings.providerName === 'kreditz') {
            let kreditzCaseId = 'tt' + generateUniqueId(30);
            this.m.kreditz = {
                initialData: {
                    clientId: this.m.settings.kreditzIFrameClientId,
                    civicRegNr: civicRegNr,
                    caseId: kreditzCaseId,
                    fetchDataForMonths: monthCount,
                    onMessage: (messageCode, hideUi) => {
                        if(messageCode === BankSharingMessageCode.authenticationCancelled) {
                            hideUi();
                            this.toastr.warning('Cancelled by user');
                            this.m.kreditz = null;
                        } else if(messageCode == BankSharingMessageCode.userFlowCompleted) {
                            hideUi();
                            this.m.kreditz = null;
                            let now = Date.now();
                            this.m.polling = {
                                startTimeMs: now,
                                lastPollMs: now,
                                elapsedSeconds: 0
                            }
                            this.scheduleWaitingTick('kreditz', kreditzCaseId);
                        }
                    },
                    baseCountry: this.config.baseCountry(),
                    isNTechTest: this.config.isNTechTest()
                }
            }

        }
    }

    private scheduleWaitingTick(providerName: string, id: string) {
        setTimeout(() => {
            let now = Date.now();
            this.m.polling.elapsedSeconds = Math.ceil((now - this.m.polling.startTimeMs) / 1000);
            if(this.m.polling.elapsedSeconds > 5*60) {
                this.toastr.error('Failed');
                this.m.polling = null;
                return;
            }
            if(now - this.m.polling.lastPollMs >= 5000) {
                this.m.polling.lastPollMs = now;
                this.poll(providerName, id).then(x => {
                    if(x.hasData) {
                        this.m.polling = null;
                        let downloadUrl: SafeUrl = null;
                        if(x.rawJsonData) {
                            let blob = new Blob([x.rawJsonData], { type: 'application/json' });
                            downloadUrl = this.sanitizer.bypassSecurityTrustUrl(window.URL.createObjectURL(blob))
                            let scoringData = this.parseScoringData(providerName, x.rawJsonData)
                            this.m.bankData = {
                                rawData: {
                                    data: this.validationService.formatJsonForDisplay(x.rawJsonData),
                                    downloadUrl: downloadUrl
                                },
                                scoringData: scoringData ? {
                                    data: scoringData
                                } : null,
                                tableRows: Object.keys(x.parsedData ?? {}).map(y => ({ title: y, value: x.parsedData[y] }))
                            }
                        } else {
                            this.toastr.error('Missing data')
                        }
                    } else {
                        this.scheduleWaitingTick(providerName, id);
                    }
                })
            } else {
                this.scheduleWaitingTick(providerName, id);
            }
        }, 500);
    }

    private parseScoringData(providerName: string, rawData: string) {
        if(providerName !== 'kreditz') {
            return undefined;
        }
        try {
            return this.validationService.formatJsonForDisplay(JSON.stringify(JSON.parse(rawData).alta));
        } catch {
            return undefined;
        }
    }

    private poll(providerName: string, id: string) {
        return this.apiService.post<{
            hasData: boolean,
            rawJsonData: string
            parsedData: StringDictionary
        }>('NTechHost', 'Api/PreCredit/BankShareTest/Poll', {
            id: id,
            providerName: providerName
        }, { skipLoadingIndicator: true });
    }

    public download(evt ?: Event) {
        evt?.preventDefault();

        //Create a dataurl, add it to temporary hidden anchor and click that to trigger a download.
        const blob = new Blob([this.m.bankData.rawData.data], { type: 'application/json' });
        const url = window.URL.createObjectURL(blob);
        const safeUrl: SafeUrl = this.sanitizer.bypassSecurityTrustUrl(url);
        const link = document.createElement('a');
        link.href = safeUrl as any;
        link.download = 'bankdata.json';
        link.target = '_blank';
        link.click();
    }
}

interface BankSharingSettings {
    isEnabled: boolean
    providerName: string
    kreditzIFrameClientId: string
}

interface Model {
    settings?: BankSharingSettings
    isEnabled: boolean
    startSharing ?: {
        form: FormsHelper
    }
    kreditz ?: {
        initialData ?: KreditzDatashareIframeComponentInitialData
    }
    polling ?: {
        startTimeMs: number,
        elapsedSeconds: number,
        lastPollMs: number
    }
    bankData ?: {
        rawData ?: {
            data: string
            downloadUrl : SafeUrl
        }
        scoringData ?: {
            data: string
        }
        tableRows ?: { title: string, value: string }[]
    }
    civicRegNrPlaceholder ?: string
}
