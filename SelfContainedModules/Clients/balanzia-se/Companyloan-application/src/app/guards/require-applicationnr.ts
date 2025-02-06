import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, RouterStateSnapshot, Router, Resolve } from '@angular/router';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class RequireApplicationNrGuard implements Resolve<string> {
    constructor(
        private router: Router) {
    }

    resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): string | Observable<string> | Promise<string> {
        let applicationNr : string = route.params.applicationNr
        if(applicationNr) {
            return applicationNr
        } else {
            this.router.navigate(['not-found'])
            return null
        }
    }
}
