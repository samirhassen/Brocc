import { Component, OnInit } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { CustomerPagesConfigService } from '../../../common-services/customer-pages-config.service';
import { CustomerPagesEventService } from '../../../common-services/customerpages-event.service';

@Component({
    selector: 'np-shell',
    templateUrl: './shell.component.html',
    styles: [],
})
export class ShellComponent implements OnInit {
    constructor(public config: CustomerPagesConfigService, private eventService: CustomerPagesEventService) {}

    isLoading: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);

    ngOnInit(): void {
        this.eventService.isLoading.subscribe((x) => {
            //Good old ExpressionChangedAfterItHasBeenCheckedError: https://stackoverflow.com/questions/49914840/expressionchangedafterithasbeencheckederror-on-loadingindicator
            Promise.resolve(null).then(() => this.isLoading.next(x));
        });
    }

    logOut(evt?: Event) {
        evt?.preventDefault();

        document.location.href = this.config.config()?.LogoutUrl;
    }
}
