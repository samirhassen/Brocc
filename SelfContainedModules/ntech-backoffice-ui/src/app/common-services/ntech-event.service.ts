import { EventEmitter, Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { generateUniqueId } from '../common.types';

export abstract class NtechEventServiceBase {
    //NOTE: Dont put a constructor here since this is shared with customer pages which has a different set of injected services
    public isLoading = new BehaviorSubject(false);

    public applicationEvents: EventEmitter<ApplicationEvent> = new EventEmitter<ApplicationEvent>();

    public emitApplicationEvent<T>(eventCode: string, customData?: T, context?: string) {
        this.applicationEvents.emit(new ApplicationEvent(eventCode, customData, context));
    }

    public generateDialogId() {
        return generateUniqueId(10);
    }

    public signalReloadApplication(applicationNr: string) {
        this.applicationEvents.emit(new ApplicationEvent(ReloadApplicationEventName, applicationNr));
    }

    public signalReloadApplicationAndClosePopup(applicationNr: string) {
        this.applicationEvents.emit(new ApplicationEvent(ReloadApplicationAndClosePopupEventName, applicationNr));
    }

    public signalUpdateToggleBlock(toggleBlockId: string, updates: { newTitle?: string }) {
        this.applicationEvents.emit(
            new ApplicationEvent(UpdateToggleBlock, { toggleBlockId, newTitle: updates?.newTitle })
        );
    }

    public signalReloadCreditComments(creditNr: string) {
        this.applicationEvents.emit(new ApplicationEvent(ReloadCreditCommentsEventName, creditNr));
    }
}

export const ReloadApplicationAndClosePopupEventName = 'ReloadUnsecuredLoanApplicationAndClosePopup';
export const ReloadApplicationEventName = 'ReloadUnsecuredLoanApplication';
export const UpdateToggleBlock = 'UpdateToggleBlock';
export const ReloadCreditCommentsEventName = 'ReloadCreditComments';

@Injectable({
    providedIn: 'root',
})
export class NtechEventService extends NtechEventServiceBase {
    constructor() {
        super();
    }

    public setCustomPageTitle(title: string, browserTitle: string) {
        this.applicationEvents.emit(
            new ApplicationEvent('setCustomPageTitle', { title: title, browserTitle: browserTitle })
        );
    }
    public setApplicationNr(applicationNr: string) {
        this.applicationEvents.emit(new ApplicationEvent('setApplicationNr', { applicationNr: applicationNr }));
    }
}

export class ApplicationEvent {
    constructor(public eventCode: string, public customData?: any, public context?: string) {}

    public isReloadApplicationEvent() {
        return this.eventCode === ReloadApplicationEventName;
    }

    public isReloadApplicationAndClosePopupEvent() {
        return this.eventCode === ReloadApplicationAndClosePopupEventName;
    }
}
