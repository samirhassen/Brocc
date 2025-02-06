import { Component, OnInit, Inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { ApplicationForwardBackService } from 'src/app/backend/application-forward-back.service';
import { ApiService, API_SERVICE } from 'src/app/backend/api-service';
import { QuestionsModel } from 'src/app/backend/questions-model';

@Component({
  selector: 'q-result-success',
  templateUrl: './q-result-success.component.html',
  styleUrls: []
})
export class QResultSuccessComponent {
    questions: QuestionsModel

    constructor(private route: ActivatedRoute,
        @Inject(API_SERVICE) protected apiService: ApiService,
        protected forwardBackService: ApplicationForwardBackService) {
           
    }

    ngOnInit() {        
        this.subs.push(this.route.data.subscribe(x => {
            this.questions = x.questions
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
