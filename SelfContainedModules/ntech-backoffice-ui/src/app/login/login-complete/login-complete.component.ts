import { PlatformLocation } from '@angular/common';
import { Component, Injectable, OnInit } from '@angular/core';
import { ActivatedRoute, ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlCreationOptions, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { ConfigService } from 'src/app/common-services/config.service';

@Component({
    selector: 'app-login-complete',
    templateUrl: './login-complete.component.html',
    styles: [],
})
export class LoginCompleteComponent implements OnInit {
    constructor(public configService: ConfigService, activatedRoute: ActivatedRoute) {}

    ngOnInit(): void {}
}

@Injectable()
export class LoginCompleteGuard  {
    constructor(private configService: ConfigService, private router: Router, private platform: PlatformLocation) {}

    canActivate(
        route: ActivatedRouteSnapshot,
        state: RouterStateSnapshot
    ): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {
        let redirectAfterLoginUrlRaw = this.configService.getLoginState()?.redirectAfterLoginUrl;
        if (!redirectAfterLoginUrlRaw) {
            document.location.href = '/'; //This should not happen, send to backoffice
            return false;
        }
        let redirectAfterLoginUrl = new URL(redirectAfterLoginUrlRaw);
        if (redirectAfterLoginUrl.pathname.indexOf('login-complete') > 0) {
            document.location.href = '/'; //Login is looping, send them to backoffice
            return false;
        }

        let pathname = redirectAfterLoginUrl.pathname;
        let baseHref = this.platform.getBaseHrefFromDOM();
        if (pathname.startsWith(baseHref)) {
            pathname = decodeURIComponent(pathname.substring(baseHref.length - 1));
        }

        if (redirectAfterLoginUrl.search) {
            let searchParams = new URLSearchParams(redirectAfterLoginUrl.search);
            let extras: UrlCreationOptions = { queryParams: {} };
            searchParams.forEach((value, key) => {
                extras.queryParams[key] = value;
            });
            return this.router.createUrlTree([pathname], extras);
        } else {
            return this.router.createUrlTree([pathname]);
        }
    }
}
