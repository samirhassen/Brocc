import { Component } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { StepRouteModel, ApplicationStep } from '../../backend/application-step';
import { BehaviorSubject } from 'rxjs';
import { translateApplicationToServerModel } from '../../backend/application-model.server';
import { Data } from '@angular/router';
import { startAdditionalQuestionsSession } from 'src/app/additional-questions/login.helper';

@Component({
  selector: 'consent',
  templateUrl: './consent.component.html',
  styleUrls: []
})
export class ConsentComponent extends ApplicationStep<ConsentFormDataModel> {
    protected createForm(): FormGroup {
        return this.fb.group({
       
            creditReportConsent: [false, [Validators.required] ]
        })      
    }

    protected onDataChanged(x: Data) { 
        super.onDataChanged(x)
    }

    protected isFinalStep(): boolean { return true }

    protected getIsForwardAllowed(formData: ConsentFormDataModel): boolean {
        return this.form.valid && !(this.forwardBackService.isLoading.value)
    }

    protected getFormUpdateFromApplication(): ConsentFormDataModel {
        let a = this.application
        return a.consent ? { 
            creditReportConsent: a.consent.creditReportConsent } : null
    }

    protected updateApplicationFromForm(formData: ConsentFormDataModel) {
        this.application.setDataConsent(formData.creditReportConsent)
    }

    protected getStepName(): string {
        return 'consent'
    }

    protected getNextStep(): StepRouteModel {
        throw new Error("Not in use"); //Not used since we override forward
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel(this.application.hasOtherLoans() ? 'other-loans-amount' : 'has-other-loans') 
    }

    onForward() {
        if(this.application.hasBeenSentToServer) {
            if(this.application.serverResponse) {
                this.apiService.navigateToApplicationRoute('result-success', this.application.id)
            } else {
                this.apiService.navigateToApplicationRoute('result-failed', this.application.id)
            }            
        } else {
            let f : ConsentFormDataModel = this.form.value
            this.updateApplicationFromForm(f)
            this.apiService.saveApplication(this.application)
            let serverApplication = translateApplicationToServerModel(this.application)
            this.forwardBackService.isLoading.next(true)
    
            this.application.hasBeenSentToServer = true
            this.apiService.saveApplication(this.application)
    
            this.apiService.createApplication(serverApplication).toPromise().then(x => {
                this.application.serverResponse = x
                this.apiService.saveApplication(this.application)
                this.forwardBackService.isLoading.next(false)

                if(x.Offer) {
                    startAdditionalQuestionsSession(this.apiService, x.LoginSessionDataToken, x.ApplicationNr)
                } else {
                    this.apiService.navigateToApplicationRoute('result-success', this.application.id)
                }                
            }, _ => this.apiService.navigateToApplicationRoute('result-failed', this.application.id))
        }
    }
}

export class ConsentFormDataModel {

    creditReportConsent: boolean
}
