import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CreditNotification } from '../../credit.service';

@Component({
    selector: 'notifications-list',
    templateUrl: './notifications-list.component.html',
    styles: [],
})
export class NotificationsListComponent {
    constructor() {}

    @Input()
    public notifications: CreditNotification[];

    @Output()
    public onClickNotification: EventEmitter<CreditNotification> = new EventEmitter<CreditNotification>();
}
