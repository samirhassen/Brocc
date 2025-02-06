import { Injectable, Inject } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, ParamMap, Router, Resolve } from '@angular/router';
import { Observable } from 'rxjs';
import { ApiService, API_SERVICE } from '../backend/api-service';
import { map } from 'rxjs/operators';
import { QuestionsModel } from '../backend/questions-model';

@Injectable({
    providedIn: 'root'
})
export class RequireQuestionsGuard implements Resolve<QuestionsModel> {
    constructor(
        @Inject(API_SERVICE) private apiService: ApiService,
        private router: Router) {
    }

    resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): QuestionsModel | Observable<QuestionsModel> | Promise<QuestionsModel> {
        return this.apiService.getQuestions(route.params.id).pipe(map(x => {
            if (x) {
                return x
            } else {
                this.router.navigate(['not-found'])
                return null
            }
        }))
    }
}
