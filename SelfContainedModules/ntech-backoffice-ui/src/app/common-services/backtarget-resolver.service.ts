import { Injectable } from '@angular/core';
import { ActivatedRoute, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { toUrlSafeBase64String } from '../common.types';

@Injectable({
    providedIn: 'root',
})
export class BackTargetResolverService  {
    constructor() {}

    resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {
        let backTarget: string = route.queryParams.backTarget;
        if (backTarget) {
            return backTarget;
        } else {
            return null;
        }
    }
}

export class CrossModuleNavigationTarget {
    private constructor(private rawCode: string) {}

    public getCrossModuleNavigationUrl(backTarget?: CrossModuleNavigationTarget) {
        let url = '/Ui/CrossModuleNavigate?targetCode=' + this.rawCode;
        if (backTarget) {
            url += '&backTargetCode=' + backTarget.getCode();
        }
        return url;
    }

    public getCode() {
        return this.rawCode;
    }

    public toString() {
        return this.rawCode;
    }

    static parseBackTargetFromRouteSnapshot(
        activatedRouteSnapshot: ActivatedRouteSnapshot
    ): CrossModuleNavigationTarget {
        let backTarget: string = activatedRouteSnapshot?.data?.backTarget;
        if (!backTarget) {
            backTarget = activatedRouteSnapshot?.firstChild?.data?.backTarget;
        }
        if (!backTarget) {
            backTarget = activatedRouteSnapshot?.queryParams?.backTarget;
        }
        if (!backTarget) {
            backTarget = activatedRouteSnapshot?.firstChild?.queryParams?.backTarget;
        }
        if (backTarget && backTarget.startsWith('t-')) {
            return new CrossModuleNavigationTarget(backTarget);
        } else {
            return null;
        }
    }

    static parseBackTargetFromRoute(activatedRoute: ActivatedRoute): CrossModuleNavigationTarget {
        return CrossModuleNavigationTarget.parseBackTargetFromRouteSnapshot(activatedRoute?.snapshot);
    }

    static create(targetName: string, targetContext: { [key: string]: string }): CrossModuleNavigationTarget {
        if (targetName == null) return null;

        return new CrossModuleNavigationTarget('t-' + toUrlSafeBase64String({ targetName, targetContext }));
    }
}
