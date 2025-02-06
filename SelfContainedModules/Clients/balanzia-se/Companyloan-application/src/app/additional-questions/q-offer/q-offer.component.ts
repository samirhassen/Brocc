import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';
import { QuestionsStep } from 'src/app/backend/questions-step';
import { NTechMath } from 'src/app/backend/ntech.math';

@Component({
  selector: 'q-offer',
  templateUrl: './q-offer.component.html',
  styleUrls: []
})
export class QOfferComponent  extends QuestionsStep<OfferFormDataModel>  {

    protected createForm(formData: OfferFormDataModel): FormGroup {
        return this.form = this.fb.group({
            
        })
    }
    
    toMonthlyInterest(n: number) {
        if(!n) {
            return 0
        }
        return NTechMath.roundToPlaces(n / 12, 2)
    }
    
    protected getIsForwardAllowed(formData: OfferFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromQuestions(): OfferFormDataModel {
        return {
            
        }
    }
    protected updateQuestionsFromForm(formData: OfferFormDataModel) {
        this.questions.offerStepPassed = true
    }
    protected getStepName(): string {
        return 'q-offer'
    }
    protected getNextStep(): StepRouteModel {     
        return new StepRouteModel('collateral-option')
    }
    protected getPreviousStep(): StepRouteModel {
        return null
    }    
}

export class OfferFormDataModel {
    
}
