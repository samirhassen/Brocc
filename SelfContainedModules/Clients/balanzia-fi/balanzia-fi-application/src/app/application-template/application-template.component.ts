import { Component } from '@angular/core';
import { environment } from '../../environments/environment';
import { Router, ActivationEnd } from '@angular/router';
import { ApplicationModel } from '../backend/application-model';
import { LanguageService } from '../backend/languagesupport.service';
import { Title } from '@angular/platform-browser';
import { ApplicationForwardBackService } from '../backend/application-forward-back.service';
import { BehaviorSubject } from 'rxjs';

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
    isCoApplicant: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false)

    constructor(private router: Router, private translate: LanguageService, private titleService: Title, public forwardBackService: ApplicationForwardBackService) {
        this.currentLanguage = translate.currentLanguage.value
        translate.currentLanguage.subscribe(x => this.currentLanguage = x)
        titleService.setTitle('Brocclånet')
        router.events.subscribe(x => {
            if(x instanceof ActivationEnd) {
                let y : ActivationEnd = x
                this.isCalculator = !!y.snapshot.data.isCalculator
                this.isFinalStep = y.snapshot.data.isFinalStep === true
                this.isIdependent = y.snapshot.data.isIdependent === true
                this.isCoApplicant.next(y.snapshot.data.applicantNr === 2)
                let a : ApplicationModel = y.snapshot.data.application
                if(a) {
                    this.progressPercent = Math.round(a.getProgressPercent()).toString()
                } else {
                    this.progressPercent = null
                }
            }
        })
    }

    setLanguage(lang: string) {
        this.translate.setLanguage(lang)
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