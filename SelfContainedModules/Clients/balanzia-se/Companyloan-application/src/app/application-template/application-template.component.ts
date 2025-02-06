import { Component } from '@angular/core';
import { environment } from '../../environments/environment';
import { Router, ActivationEnd } from '@angular/router';
import { ApplicationModel } from '../backend/application-model';
import { Title } from '@angular/platform-browser';
import { ApplicationForwardBackService } from '../backend/application-forward-back.service';
import { BehaviorSubject } from 'rxjs';
import { QuestionsModel } from '../backend/questions-model';

@Component({
  selector: 'application-template',
  templateUrl: './application-template.component.html',
  styleUrls: ['./application-template.component.css']
})
export class ApplicationTemplateComponent {
    env = environment;
    isCalculator: boolean
    isFinalStep: boolean
    isIdependent: boolean
    currentLanguage: string
    progressPercent: string = null

    constructor(private router: Router, private titleService: Title, public forwardBackService: ApplicationForwardBackService) {
        titleService.setTitle('Balanzia företagslånet')
        router.events.subscribe(x => {
            if(x instanceof ActivationEnd) {                
                let y : ActivationEnd = x
                this.isCalculator = !!y.snapshot.data.isCalculator
                this.isFinalStep = y.snapshot.data.isFinalStep === true
                this.isIdependent = y.snapshot.data.isIdependent === true
                let a : ApplicationModel = y.snapshot.data.application
                let q : QuestionsModel = y.snapshot.data.questions
                if(a) {
                    this.progressPercent = Math.round(a.getProgressPercent()).toString()
                } else if(q) {
                    this.progressPercent = Math.round(q.getProgressPercent()).toString()                    
                } else {
                    this.progressPercent = null
                }
            }
        })
    }

    back(evt: Event) {
        if(evt) {
            evt.preventDefault()
        }
        if(!this.forwardBackService.isBackAllowed.value) {
            return
        }
        this.forwardBackService.onBack.emit()
    }    
}