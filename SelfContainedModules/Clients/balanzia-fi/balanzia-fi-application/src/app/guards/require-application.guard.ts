import { Injectable, Inject } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, ParamMap, Router, Resolve } from '@angular/router';
import { Observable } from 'rxjs';
import { ApiService, API_SERVICE } from '../backend/api-service';
import { ApplicationModel } from "../backend/application-model";
import { map } from 'rxjs/operators';

@Injectable({
    providedIn: 'root'
})
export class RequireApplicationGuard implements Resolve<ApplicationModel> {
    constructor(
        @Inject(API_SERVICE) private apiService: ApiService,
        private router: Router) {
    }

    resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): ApplicationModel | Observable<ApplicationModel> | Promise<ApplicationModel> {
        return this.apiService.getApplication(route.params.id).pipe(map(x => {
            if (x) {
                return x
            } else {
                this.router.navigate(['not-found'])
                return null
            }
        }))
    }
}
