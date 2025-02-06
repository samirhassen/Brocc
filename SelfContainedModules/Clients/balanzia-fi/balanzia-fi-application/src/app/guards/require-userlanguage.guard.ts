import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, RouterStateSnapshot, ParamMap, Router, Resolve } from '@angular/router';
import { Observable, of } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';

@Injectable({
    providedIn: 'root'
})
export class RequireUserLanguageGuard implements Resolve<string> {
    constructor(private translate: TranslateService) { }

    resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): string | Observable<string> | Promise<string> {
        return of(this.translate.currentLang)
    }
}
