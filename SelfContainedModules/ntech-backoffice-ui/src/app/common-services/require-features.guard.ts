import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { ConfigService } from './config.service';
import { LoggingService } from './logging.service';

@Injectable({
    providedIn: 'root',
})
export class RequireFeaturesGuard  {
    constructor(private configService: ConfigService, private router: Router, private loggingService: LoggingService) {}
    canActivate(
        route: ActivatedRouteSnapshot,
        state: RouterStateSnapshot
    ): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {
        let requireAllTheseFeatures = route.data.requireFeatures as Array<string>;
        let requireFeature = route.data.requireFeature as string;
        let requireAnyOfTheseFeatures = route.data.requireAnyFeature as Array<string>;
        let requireClientCountry = route.data.requireClientCountry as string;

        if (!requireAllTheseFeatures && !requireFeature && !requireAnyOfTheseFeatures) {
            return true;
        }

        let onRejected = () => {
            this.loggingService.debug(`Route ${route.url} not allowed due to missing features`);
            return this.router.createUrlTree(['/not-found']);
        };

        if (requireFeature && !this.configService.isFeatureEnabled(requireFeature)) {
            return onRejected();
        }

        if(requireClientCountry && this.configService.baseCountry() !== requireClientCountry) {
            return onRejected();
        }

        if (requireAllTheseFeatures) {
            for (let f of requireAllTheseFeatures) {
                if (!this.configService.isFeatureEnabled(f)) {
                    return onRejected();
                }
            }
        }

        if (requireAnyOfTheseFeatures) {
            if (!this.configService.isAnyFeatureEnabled(requireAnyOfTheseFeatures)) {
                return onRejected();
            }
        }

        return true;
    }
}
