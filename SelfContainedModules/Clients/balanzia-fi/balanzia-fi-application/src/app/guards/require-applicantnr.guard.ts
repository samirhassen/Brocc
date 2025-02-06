import { Injectable, Inject } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, ParamMap, Router, Resolve } from '@angular/router';
import { Observable } from 'rxjs';
import { ApiService, API_SERVICE } from '../backend/api-service';
import { ApplicationModel } from "../backend/application-model";
import { map } from 'rxjs/operators';

@Injectable({
    providedIn: 'root'
})
export class RequireApplicantNrGuard implements Resolve<number> {
    constructor(
        @Inject(API_SERVICE) private apiService: ApiService,
        private router: Router) {
    }

    resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): number | Observable<number> | Promise<number> {
        let a = route.params.applicantNr
        if(a) {
            return parseInt(a)
        } else {
            this.router.navigate(['not-found'])
            return null
        }
    }
}
