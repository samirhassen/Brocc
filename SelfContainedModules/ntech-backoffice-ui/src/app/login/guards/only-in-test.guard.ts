import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { ConfigService } from 'src/app/common-services/config.service';

@Injectable({
    providedIn: 'root',
})

/**
 * For routes only available when the system is in test mode.
 */
export class OnlyInTestGuard  {
    constructor(private configService: ConfigService, private router: Router) {}

    canActivate(
        route: ActivatedRouteSnapshot,
        state: RouterStateSnapshot
    ): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {
        return this.configService.isNTechTest() ? true : this.router.createUrlTree(['/not-found']);
    }
}
