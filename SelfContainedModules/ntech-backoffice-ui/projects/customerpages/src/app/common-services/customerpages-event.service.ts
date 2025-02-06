import { Injectable } from '@angular/core';
import { NtechEventServiceBase } from 'src/app/common-services/ntech-event.service';

@Injectable({
    providedIn: 'root',
})
export class CustomerPagesEventService extends NtechEventServiceBase {
    constructor() {
        super();
    }
}
