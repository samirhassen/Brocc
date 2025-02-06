import { Injectable } from '@angular/core'; //From https://medium.com/grensesnittet/loading-status-in-angular-done-right-aeed09cfbea6
import { HttpResponse, HttpRequest, HttpHandler, HttpEvent, HttpInterceptor } from '@angular/common/http';
import { NtechEventService } from './ntech-event.service';
import { BehaviorSubject, Observable } from 'rxjs';

/*


*/
/*
https://github.com/angular/angular/issues/18155
The fact that you cant pass custom data in request options is so sad.
 */
export const LoaderInterceptorSkipHeader = 'X-NTech-Skip-Loader';

export abstract class LoaderInterceptorBase implements HttpInterceptor {
    //NOTE: Dont put a constructor here since this is shared with customer pages which has a different set of injected services
    private requests: HttpRequest<any>[] = [];

    abstract isLoadingSubject(): BehaviorSubject<boolean>;

    removeRequest(req: HttpRequest<any>) {
        const i = this.requests.indexOf(req);
        if (i >= 0) {
            this.requests.splice(i, 1);
        }
        this.isLoadingSubject().next(this.requests.length > 0);
    }

    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        if (req.headers.has(LoaderInterceptorSkipHeader)) {
            const headers = req.headers.delete(LoaderInterceptorSkipHeader);
            return next.handle(req.clone({ headers }));
        } else {
            this.requests.push(req);

            this.isLoadingSubject().next(true);
            return new Observable((observer) => {
                const subscription = next.handle(req).subscribe(
                    (event) => {
                        if (event instanceof HttpResponse) {
                            this.removeRequest(req);
                            observer.next(event);
                        }
                    },
                    (err) => {
                        this.removeRequest(req);
                        observer.error(err);
                    },
                    () => {
                        this.removeRequest(req);
                        observer.complete();
                    }
                );
                return () => {
                    this.removeRequest(req);
                    subscription.unsubscribe();
                };
            });
        }
    }
}

@Injectable()
export class LoaderInterceptor extends LoaderInterceptorBase implements HttpInterceptor {
    constructor(private eventService: NtechEventService) {
        super();
    }

    isLoadingSubject() {
        return this.eventService.isLoading;
    }
}
