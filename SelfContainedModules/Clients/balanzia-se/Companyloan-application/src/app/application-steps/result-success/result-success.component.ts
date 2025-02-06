import { Component, OnInit, Inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { ApplicationForwardBackService } from 'src/app/backend/application-forward-back.service';
import { ApiService, API_SERVICE } from 'src/app/backend/api-service';
import { ApplicationModel } from 'src/app/backend/application-model';

@Component({
  selector: 'result-success',
  templateUrl: './result-success.component.html',
  styleUrls: []
})
export class ResultSuccessComponent {
    application: ApplicationModel

    constructor(private route: ActivatedRoute,
        @Inject(API_SERVICE) protected apiService: ApiService,
        protected forwardBackService: ApplicationForwardBackService) {
           
    }

    ngOnInit() {        
        this.subs.push(this.route.data.subscribe(x => {
            this.application = x.application
            this.forwardBackService.isBackAllowed.next(false)
        }))
    }

    ngOnDestroy() {
        this.unsub()
    }

    subs: Subscription[] = []

    unsub() {
        if(this.subs) {
            for(let s of this.subs) {
                s.unsubscribe()
            }
            this.subs = []
        }
    }
}
