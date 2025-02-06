import { Injectable } from '@angular/core'; //From https://medium.com/grensesnittet/loading-status-in-angular-done-right-aeed09cfbea6
import { HttpInterceptor } from '@angular/common/http';
import { BehaviorSubject } from 'rxjs';
import { CustomerPagesEventService } from './customerpages-event.service';
import { LoaderInterceptorBase } from 'src/app/common-services/loader.interceptor';

@Injectable()
export class CustomerPagesLoaderInterceptor extends LoaderInterceptorBase implements HttpInterceptor {
    constructor(private eventService: CustomerPagesEventService) {
        super();
    }

    isLoadingSubject(): BehaviorSubject<boolean> {
        return this.eventService.isLoading;
    }
}
