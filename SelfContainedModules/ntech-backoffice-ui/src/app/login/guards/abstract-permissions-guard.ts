import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { ConfigService } from 'src/app/common-services/config.service';

export abstract class AbstractPermissionsGuard  {
    constructor(private configService: ConfigService, private router: Router) {}

    abstract getAllowedProductsOrNullForAll(): string[];
    abstract getAllowedGroupNamesOrNullForAll(): string[];
    abstract getAllowedRolesNamesOrNullForAll(): string[];

    canActivate(
        route: ActivatedRouteSnapshot,
        state: RouterStateSnapshot
    ): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {
        let permissions = this.configService.getUserPermissions();

        let isAllowed =
            this.isAllowedByList(permissions.productNames, this.getAllowedProductsOrNullForAll()) &&
            this.isAllowedByList(permissions.groupNames, this.getAllowedGroupNamesOrNullForAll()) &&
            this.isAllowedByList(permissions.roleNames, this.getAllowedRolesNamesOrNullForAll());

        if (isAllowed) {
            return true;
        }

        return this.router.createUrlTree(['/not-authorized']);
    }

    /*
    Either the user need at least one of the permissions in the list or any is allowed
    */
    private isAllowedByList(userList: string[], allowedOrNullForAllList: string[] | null) {
        if (allowedOrNullForAllList === null) {
            return true;
        }
        for (let listItemName of allowedOrNullForAllList) {
            if (userList.indexOf(listItemName) >= 0) {
                return true;
            }
        }
        return false;
    }
}
