import { Component, OnInit, Inject } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { ApplicationForwardBackService } from '../backend/application-forward-back.service';
import { Subscription } from 'rxjs';
import { API_SERVICE, ApiService } from '../backend/api-service';
import { ConfigService } from '../backend/config.service';

@Component({
    selector: 'local-test-eid',
    templateUrl: './local-test-eid.component.html',
    styleUrls: []
})
export class LocalTestEidComponent implements OnInit {
    form: FormGroup

    constructor(fb: FormBuilder, 
        private forwardBackService: ApplicationForwardBackService, 
        @Inject(API_SERVICE) protected apiService: ApiService,
        private configService: ConfigService) { 
        this.form = fb.group({

        })
    }

    ngOnInit() {
        this.form.reset()
        this.forwardBackService.isBackAllowed.next(false)
        this.forwardBackService.isForwardAllowed.next(true)

        this.subs.push(this.forwardBackService.onForward.subscribe(x => {
            this.unsub()

            let p = this.configService.getQueryStringParameters()
            p['sessionId'] = 'testId'
            p['loginToken'] = 'testToken'
            this.apiService.navigateToApplicationRoute('login-complete', null, null)
        }))        
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
