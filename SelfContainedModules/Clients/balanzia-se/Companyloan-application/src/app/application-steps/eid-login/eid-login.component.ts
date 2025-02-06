import { OnInit, Inject, Component } from '@angular/core';
import { BehaviorSubject, Subscription } from 'rxjs';
import { NTechValidationService } from '../../backend/ntech-validation.service';
import { ApplicationForwardBackService } from '../../backend/application-forward-back.service';
import { ConfigService } from '../../backend/config.service';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ApiService, API_SERVICE } from '../../backend/api-service';
import { DOCUMENT, Location, PlatformLocation } from '@angular/common';
import { environment } from '../../../environments/environment';

@Component({
    selector: 'eid-login',
    templateUrl: './eid-login.component.html',
    styleUrls: []
  })
export class EidLoginComponent implements OnInit {
    form: FormGroup
    loginPending: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false)
    isLoginAllowed: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false)
    constructor(fb: FormBuilder, validationService: NTechValidationService, 
        @Inject(API_SERVICE) protected apiService: ApiService, private configService: ConfigService,
        @Inject(DOCUMENT) private document: Document) { 
        this.form = fb.group({
            ssn: ['', [Validators.required, validationService.getCivicNrValidator()] ]
        })
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

    login(evt: Event) {
        if(evt) {
            evt.preventDefault()
        }
        if(!this.form.valid) {
            return
        }
        this.loginPending.next(true)            
        this.apiService.createEidLoginSession(this.form.value.ssn, this.configService.getQueryStringParameters(), null).toPromise().then(x => {
            if(environment.useMockApi) {
                this.apiService.navigateToApplicationRoute('local-test-eid', null, null)
            } else {
                this.document.location.href = x.SignicatInitialUrl
            }                
        })          
    }

    ngOnInit() {        
        this.form.reset({ ssn: '' })

        this.subs.push(this.form.valueChanges.subscribe(_ => {
            this.isLoginAllowed.next(this.form.valid)
        }))
    }
}
