import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { CustomerPagesConfigService } from './customer-pages-config.service';

@Injectable({
    providedIn: 'root',
})
export class CustomerPagesAuthGuard  {
    constructor(private configService: CustomerPagesConfigService, private router: Router) {}

    canActivate(
        route: ActivatedRouteSnapshot,
        state: RouterStateSnapshot
    ): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {
        let loginTargetName = route.data.loginTargetName as string;
        let requiredUserRole = route.data.requiredUserRole as string;

        if (!this.hasRequiredFeatures(route)) {
            return this.router.createUrlTree(['/not-found']);
        }

        let getNoAccessUrl = () => 
            loginTargetName ? this.configService.getLoginUrl(loginTargetName) : '/access-denied';

        if (!this.configService.isAuthenticated) {
            document.location.href = getNoAccessUrl();
            return false;
        }

        if (!requiredUserRole) {
            return true;
        }
        if (this.configService.hasAuthenticatedRole(requiredUserRole)) {
            return true;
        } else {
            document.location.href = getNoAccessUrl();
            return false;
        }
    }

    private hasRequiredFeatures(route: ActivatedRouteSnapshot) {
        let requireAllTheseFeatures = route.data.requireFeatures as Array<string>;
        let requireFeature = route.data.requireFeature as string;
        let requireAnyOfTheseFeatures = route.data.requireAnyFeature as Array<string>;

        if (!requireAllTheseFeatures && !requireFeature && !requireAnyOfTheseFeatures) {
            return true;
        }

        if (requireFeature && !this.configService.isFeatureEnabled(requireFeature)) {
            return false;
        }

        if (requireAllTheseFeatures) {
            for (let f of requireAllTheseFeatures) {
                if (!this.configService.isFeatureEnabled(f)) {
                    return false;
                }
            }
        }

        if (requireAnyOfTheseFeatures) {
            if (!this.configService.isAnyFeatureEnabled(requireAnyOfTheseFeatures)) {
                return false;
            }
        }

        return true;
    }
}
