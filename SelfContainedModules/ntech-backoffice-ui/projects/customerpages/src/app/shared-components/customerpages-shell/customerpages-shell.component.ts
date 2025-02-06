import { Component, Input, OnInit } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { CustomerPagesConfigService } from '../../common-services/customer-pages-config.service';
import { CustomerPagesEventService } from '../../common-services/customerpages-event.service';

@Component({
    selector: 'customerpages-shell',
    templateUrl: './customerpages-shell.component.html',
    styles: [],
})
export class CustomerpagesShellComponent implements OnInit {
    constructor(public config: CustomerPagesConfigService, private eventService: CustomerPagesEventService) {}

    isLoading: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);

    @Input()
    public initialData: CustomerpagesShellInitialData;
    public isTestPopupVisible: boolean = false;

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

    async executeTestFunction(f: TestFunction, evt ?: Event) {
        evt?.preventDefault();
        if(!this.config.isNTechTest()) {
            return;
        }
        await f.execute();
        this.isTestPopupVisible = false;
    }
}

export class CustomerpagesShellInitialData {
    logoRouterLink: string[];
    skipBodyLayout?: boolean;
    wideNavigation?: boolean;
    test ?: {
        functionCalls: TestFunction[]
    }
}

interface TestFunction {
    displayText: string,
    execute: () => Promise<void>
}

