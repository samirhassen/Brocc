import { Component, OnInit, Inject } from '@angular/core';
import { ConfigService } from '../backend/config.service';
import { BehaviorSubject } from 'rxjs';
import { ApiService, API_SERVICE } from '../backend/api-service';
import { StringDictionary } from '../backend/common.types';
import { startAdditionalQuestionsSession } from '../additional-questions/login.helper';

@Component({
    selector: 'login-complete',
    templateUrl: './login-complete.component.html',
    styleUrls: []
})
export class LoginCompleteComponent implements OnInit {
    loginStatus : BehaviorSubject<string> = new BehaviorSubject<string>('pending')

    constructor(private configService: ConfigService, @Inject(API_SERVICE) protected apiService: ApiService) { 
        
    }

    ngOnInit() {
        let ps = this.configService.getQueryStringParameters()
        this.loginStatus.next('pending')
        
        let sessionId = ps['sessionId']
        let loginToken = ps['loginToken']

        if(!sessionId || !loginToken) {
            this.loginStatus.next('failed')
        } else {
            this.apiService.completeEidLoginSession(sessionId, loginToken).toPromise().then(x => {
                let cd = JSON.parse(x.CustomData)
                if(cd.purpose === 'application') {
                    this.loginStatus.next('success')
                    this.apiService.startApplication(x).subscribe(y => {
                        this.apiService.navigateToApplicationRoute('orgnr', y.id)
                    })
                } else if(cd.purpose === 'questions') {
                    this.loginStatus.next('success')
                    startAdditionalQuestionsSession(this.apiService, x.LoginSessionDataToken, cd.applicationNr)
                }
            }, err => {
                this.loginStatus.next('failed')
            })
        }
    }

}
