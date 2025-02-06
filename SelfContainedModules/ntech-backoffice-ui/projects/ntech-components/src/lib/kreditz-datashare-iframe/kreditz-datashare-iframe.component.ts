import { Component, HostListener, Input, SimpleChanges } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

@Component({
    selector: 'kreditz-datashare-iframe',
    templateUrl: './kreditz-datashare-iframe.component.html',
    styleUrls: ['./kreditz-datashare-iframe.component.scss']
})
export class KreditzDatashareIframeComponent {
    constructor(private sanitizer: DomSanitizer) { }

    @Input()
    public initialData: KreditzDatashareIframeComponentInitialData;
    public m: Model;

    @HostListener('window:message', ['$event'])
    public onIFrameMessageEvent(event: any): void {
        if(!(event?.origin ?? '').toLowerCase().includes('vista.kreditz.com')) {
            return;
        }
        let data : string = event?.data;
        if(!data) {
            return;
        }
        let parsedData = JSON.parse(data);
        let i = this.initialData;
        if(parsedData?.case_id !== i.caseId) {
            return;
        }
        let message: string = parsedData?.message;
        let emitMessage = (code: BankSharingMessageCode) => {
            i.onMessage(code, () => {
                this.m.isHidden = true;
            })
        };
        if(message === 'bank(s) selected') {
            emitMessage(BankSharingMessageCode.bankSelected);
        } else if(message === 'banking authentication started') {
            emitMessage(BankSharingMessageCode.authenticationStarted);
        } else if(message === 'user cancelled bank authentication' ) {
            emitMessage(BankSharingMessageCode.authenticationCancelled);
        } else if(message === 'banking authentication finished') {
            emitMessage(BankSharingMessageCode.authenticationFinished);
        } else if(message === 'User flow completed') {
            emitMessage(BankSharingMessageCode.userFlowCompleted);
        }
    }

    ngOnChanges(_: SimpleChanges) {
        this.m = null;
        let url = 'https://vista.kreditz.com/connect_bank';
        let separator = '?'
        let addQueryParameter = (name: string, value: string) => {
            url += `${separator}${encodeURIComponent(name)}=${encodeURIComponent(value)}`;
            separator = '&';
        }
        let i = this.initialData;
        addQueryParameter('client_id', i.clientId);
        if(i.baseCountry === 'SE') {
            addQueryParameter('market', 'SE');
            addQueryParameter('locale', 'sw');
        } else if(i.baseCountry === 'FI') {
            addQueryParameter('market', 'FI');
            addQueryParameter('locale', 'fi');
        } else {
            throw new Error('Not implemented for this country');
        }

        if(i.isNTechTest) {
            addQueryParameter('env', 'sandbox'); //This is absent in their prod example. Possibly live is prod. Find out.
        }
        addQueryParameter('ssn_number', i.civicRegNr);
        addQueryParameter('case_id', i.caseId);
        addQueryParameter('type', 'Private');
        addQueryParameter('iframe', 'true');
        addQueryParameter('fetch_data_for_months', i.fetchDataForMonths.toFixed(0)); //1 -> 12

        this.m = {
            iframeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url),
            isHidden: false
        }

    }
}

interface Model {
    iframeUrl: SafeResourceUrl
    isHidden: boolean
}

export enum BankSharingMessageCode {
    bankSelected,
    authenticationStarted,
    authenticationCancelled,
    authenticationFinished,
    userFlowCompleted
}

export interface KreditzDatashareIframeComponentInitialData {
    clientId: string
    civicRegNr: string
    caseId: string
    fetchDataForMonths : number
    onMessage : (messageCode: BankSharingMessageCode, hideUi: () => void) => void
    baseCountry: string
    isNTechTest: boolean
}