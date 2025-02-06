import { Component, OnInit } from '@angular/core';
import { ConfigService } from 'src/app/common-services/config.service';
import { Router, ActivationEnd, ActivatedRouteSnapshot } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { NtechEventService } from 'src/app/common-services/ntech-event.service';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { clearCachedAuthSession } from 'src/app/login/login-manager';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { Title } from '@angular/platform-browser';
import { StringDictionary, toUrlSafeBase64String } from 'src/app/common.types';
import { AiSearchEventCode, parseCustomerSearchQuery } from '../ai-search-popup/ai-search-popup.component';

@Component({
    selector: 'layout-shell',
    templateUrl: './layout-shell.component.html',
    styleUrls: [],
})
export class LayoutShellComponent implements OnInit {
    useFluidLayoutShell: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);
    isLoading: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);
    public isCustomerSearchActive: boolean;
    public searchQuery: BehaviorSubject<string> = new BehaviorSubject<string>('');
    public headerBlock: BehaviorSubject<HeaderBlockModel> = new BehaviorSubject<HeaderBlockModel>(null);

    constructor(
        private router: Router,
        public config: ConfigService,
        private eventService: NtechEventService,
        private apiService: NtechApiService,
        titleService: Title,
        private configService: ConfigService,
    ) {
        this.isCustomerSearchActive =
            config.hasAnyUserGroup(['Middle']) && config.isFeatureEnabled('ntech.feature.customeroverview');
        eventService.applicationEvents.subscribe((x) => {
            if (x.eventCode === 'SetLayoutShellSearchQuery') {
                this.searchQuery.next(x.customData);
            }
        });
        this.headerBlock.subscribe((x) => {
            if (!x) {
                titleService.setTitle('Näktergal backoffice');
            } else {
                titleService.setTitle(x.browserTitle);
            }
        });
        router.events.subscribe((x) => {
            if (x instanceof ActivationEnd) {
                let y: ActivationEnd = x;
                let snapshot = y.snapshot.firstChild ?? y.snapshot;
                let data = snapshot?.data;
                this.useFluidLayoutShell.next(data?.useFluidLayoutShell === true);

                //Clone the routing table data so we dont overwrite it
                let customDefaultBackRoute = this.clone<string[]>(data?.customDefaultBackRoute);

                if (data?.pageTitle) {
                    let pageTitle: string = data?.pageTitle;
                    let backTarget: CrossModuleNavigationTarget = null;
                    let backRouterLink: string[] = null;
                    let applicationNr: string = data?.applicationNr;
                    if (customDefaultBackRoute) {
                        backRouterLink = this.resolveCustomDefaultRoute(customDefaultBackRoute, snapshot);
                    } else if(data?.routeParamBackTargetCode) {
                        backTarget = this.resolveRouteParamBackTargetCode(data.routeParamBackTargetCode, snapshot);
                    } else {
                        // When setting a fixed backtarget, ex. testmodule going from new backoffice back to nTest
                        if (data?.fixedBackTarget) {
                            backTarget = data.fixedBackTarget;
                        } else {
                            // Receive backtarget from url, ex. from Magellan to new backoffice.
                            backTarget = CrossModuleNavigationTarget.parseBackTargetFromRouteSnapshot(y.snapshot);
                        }
                    }
                    this.headerBlock.next({
                        title: pageTitle,
                        backTarget: backTarget,
                        backRouterLink: backRouterLink,
                        applicationNr: applicationNr,
                        browserTitle: pageTitle,
                    });
                } else {
                    this.headerBlock.next(null);
                }
            }
        });
        eventService.isLoading.subscribe((x) => {
            //Good old ExpressionChangedAfterItHasBeenCheckedError: https://stackoverflow.com/questions/49914840/expressionchangedafterithasbeencheckederror-on-loadingindicator
            Promise.resolve(null).then(() => this.isLoading.next(x));
        });
        eventService.applicationEvents.subscribe((x) => {
            if (x.eventCode === 'setCustomPageTitle')
                this.setCurrentHeaderBlock(x.customData?.title, null, x.customData?.browserTitle);

            if (x.eventCode === 'setApplicationNr') this.setCurrentHeaderBlock(null, x.customData?.applicationNr, null);
        });
    }

    private clone<T>(value: T) {
        if (!value) {
            return null;
        }
        return JSON.parse(JSON.stringify(value));
    }
    /*
    The purpose of this is to let you do things like:
    { path: 'new-credit-check/:applicationNr', data : { customDefaultBackRoute: ['/unsecured-loan-application/application/:applicationNr', ] }, },
    And having the applicationNr be passed along. If this turns out to be widely used we can probably rebuild it to handle any :<pattern>.
    */
    private resolveCustomDefaultRoute(customDefaultBackRoute: string[], snapshot: ActivatedRouteSnapshot) {
        let params = snapshot.paramMap;
        for (let paramName of params.keys) {
            for (var i = 0; i < customDefaultBackRoute.length; i++) {
                customDefaultBackRoute[i] = customDefaultBackRoute[i].replace(`:${paramName}`, params.get(paramName));
            }
        }
        return customDefaultBackRoute;
    }

    private resolveRouteParamBackTargetCode(routeParamBackTargetCode: string, snapshot: ActivatedRouteSnapshot) : CrossModuleNavigationTarget {
        let context : StringDictionary = {};
        let params = snapshot.paramMap;
        for (let paramName of params.keys) {
            context[paramName] = params.get(paramName);
        }
        return CrossModuleNavigationTarget.create(routeParamBackTargetCode, context);
    }

    ngOnInit(): void {}

    currentDate() {
        return this.configService.getCurrentDateAndTime().toDate();
    }

    onBack(evt?: Event) {
        //NOTE: backRouterLink does not end up here. It's handled directly in the ui
        evt?.preventDefault();

        let navigateExternal = (url?: string) => {
            if (!url) {
                document.location.href = this.config.getServiceRegistry().getServiceRootUrl('nBackOffice');
            } else {
                document.location.href = url;
            }
        };

        let backTarget = this.headerBlock.value.backTarget;
        if (!backTarget) {
            navigateExternal();
        } else {
            this.apiService.shared.resolveCrossModuleNavigationTarget(backTarget.getCode()).then(
                (x) => {
                    if (x.LocalEmbeddedBackofficeUrl) {
                        this.router.navigateByUrl(x.LocalEmbeddedBackofficeUrl);
                    } else {
                        navigateExternal(x?.Url);
                    }
                },
                (_) => {
                    navigateExternal();
                }
            );
        }
    }

    searchCustomer(evt?: Event) {
        evt?.preventDefault();
        let {isHelpQuery, parsedQuery} = parseCustomerSearchQuery(this.searchQuery.value, this.configService);
        if(isHelpQuery) {
            this.eventService.emitApplicationEvent(AiSearchEventCode, parsedQuery);
        } else {
            this.router.navigate(['/customer-overview/search', 'eq__' + toUrlSafeBase64String(this.searchQuery.value)]);
        }
    }

    onSearchQueryChanged(evt: Event) {
        let query : string = (evt.currentTarget as any).value;
        this.searchQuery.next((query ?? '').trim());
    }

    logOut(evt?: Event) {
        evt?.preventDefault();
        clearCachedAuthSession();
        document.location.href = this.config.config()?.LogoutUrl;
    }

    setCurrentHeaderBlock(title: string, applicationNr: string, browserTitle: string): void {
        let current = this.headerBlock.value;
        if (current) {
            if (title !== null) {
                current.title = title;
                current.browserTitle = browserTitle ?? title;
            }
            if (applicationNr !== null) current.applicationNr = applicationNr;

            this.headerBlock.next(current);
        }
    }
}

export interface HeaderBlockModel {
    title: string;
    browserTitle: string;
    backTarget: CrossModuleNavigationTarget;
    backRouterLink: string[];
    applicationNr: string;
}
