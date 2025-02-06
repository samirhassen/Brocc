import { Component, TemplateRef, ViewChild } from '@angular/core';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { ToastrService } from 'ngx-toastr';
import { BehaviorSubject } from 'rxjs';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';

function delay(ms: number) {
    return new Promise( resolve => setTimeout(resolve, ms) );
}

async function aiHelpQuery(apiService: NtechApiService, query: string, result: BehaviorSubject<{ answer: string, isComplete: boolean}>) {
    let queryResult = await apiService.post<{ id: string, answer: string, isComplete: boolean }>('NTechHost', 'Api/Help/Query', { newQuery: query });
    let id = queryResult.id;
    result.next({ answer: queryResult.answer, isComplete: queryResult.isComplete});
    if(queryResult.isComplete) {
        return;
    }
    let waitMs = 300;
    let giveUpCounterMs = 0;
    while(true) {
        queryResult = await apiService.post<{ id: string, answer: string, isComplete: boolean }>('NTechHost', 'Api/Help/Query', { ongoingQueryId: id }, { skipLoadingIndicator: true });
        result.next({ answer: queryResult.answer, isComplete: queryResult.isComplete});
        if(queryResult.isComplete) {
            return;
        }
        giveUpCounterMs += waitMs;
        if(giveUpCounterMs > 45000) {
            result.next({ answer: result.value.answer + " [stopped waiting for result]", isComplete: true });
            return;
        }
        await delay(waitMs);
    }
}

@Component({
    selector: 'ai-search-popup',
    templateUrl: './ai-search-popup.component.html',
    styles: [],
})
export class AiSearchPopupComponent {
    constructor(
        eventService: NtechEventService,
        apiService: NtechApiService,
        toastr: ToastrService,
        private modalService: BsModalService
    ) {
        eventService.applicationEvents.subscribe((x) => {
            if (x.eventCode === AiSearchEventCode) {
                let result = new BehaviorSubject<{ answer: string, isComplete: boolean}>({ answer: '', isComplete: false });
                aiHelpQuery(apiService, x.customData, result); //Do not await this. We want streaming results
                this.showResponse(WaitingResponseText);
                result.subscribe(x => {
                    this.m.response = x.answer;
                    if(x.isComplete) {
                        result.unsubscribe();
                    }
                });
            }
        });
    }

    public m: Model;

    public modalRef: BsModalRef;

    @ViewChild('popupTemplate', { static: true })
    public popupTemplate: TemplateRef<any>;

    private showResponse(response: string) {
        this.m = {
            response: response,
        };
        this.modalRef = this.modalService.show(this.popupTemplate, { class: 'modal-xl', ignoreBackdropClick: true });
    }
}

export const AiSearchEventCode = 'AiSearch';
export const WaitingResponseText = 'Thinking...';

export function parseCustomerSearchQuery(query: string, config: ConfigService) {
    if (config.isFeatureEnabled('ntech.feature.helpsearch') && (query ?? '').toLowerCase().startsWith('q:')) {
        return {
            isHelpQuery: true,
            parsedQuery: query.substring(2).trim(),
        };
    } else {
        return {
            isHelpQuery: false,
            parsedQuery: query,
        };
    }
}

interface Model {
    response: string;
}
