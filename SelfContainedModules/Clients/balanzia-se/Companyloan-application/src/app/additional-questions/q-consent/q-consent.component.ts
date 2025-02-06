import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { StepRouteModel } from '../../backend/application-step';
import { QuestionsStep } from 'src/app/backend/questions-step';
import { environment } from 'src/environments/environment';
import { BehaviorSubject } from 'rxjs';

@Component({
  selector: 'q-consent',
  templateUrl: './q-consent.component.html',
  styleUrls: []
})
export class QConsentComponent  extends QuestionsStep<ConsentFormDataModel>  {
    isApplicantCollateral : BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false)

    protected createForm(formData: ConsentFormDataModel): FormGroup {        
        this.isApplicantCollateral.next(this.questions.collateral && this.questions.collateral.isApplicant === true)
        return this.fb.group({
            applicantConsent: ['', this.isApplicantCollateral.value ? [Validators.required] : [] ],
            othersConsent: ['', [Validators.required] ],
        })
    }

    protected getIsForwardAllowed(formData: ConsentFormDataModel): boolean {
        return this.form.valid && (formData.othersConsent === true && (!this.isApplicantCollateral.value || formData.applicantConsent === true))
    }

    protected getFormUpdateFromQuestions(): ConsentFormDataModel {
        let c = this.questions.consent
        return {
            applicantConsent: c ? c.applicantConsent : null,
            othersConsent: c ? c.othersConsent : null
        }
    }    

    protected updateQuestionsFromForm(formData: ConsentFormDataModel) {
        this.questions.consent = {
            applicantConsent: formData.applicantConsent,
            othersConsent: formData.othersConsent
        }
    }
    
    protected getStepName(): string {
        return 'q-consent'
    }

    protected getNextStep(): StepRouteModel {
        return null        
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('bankaccount')
    }

    protected customForward() {
        if(this.questions.hasBeenSentToServer) {
            this.apiService.navigateToQuestionsRoute('result-failed', null)
        } else {
            let request = this.questions.toServerModel()
            this.forwardBackService.isLoading.next(true)
            if(!environment.useMockApi) {
                //Allow back and resend in test since it often simplifies local testing
                this.questions.hasBeenSentToServer = true
            }
            this.apiService.saveQuestions(this.questions)
            this.apiService.submitAdditionalQuestions(request).toPromise().then(x => {
                this.forwardBackService.isLoading.next(false)
                this.apiService.navigateToQuestionsRoute('result-success', this.questions.id)
            }, x => {
                this.apiService.navigateToQuestionsRoute('result-failed', null)
                this.forwardBackService.isLoading.next(false)
            })
        }
        
        return true
    }  
}

export class ConsentFormDataModel {
    applicantConsent: boolean
    othersConsent: boolean
}
