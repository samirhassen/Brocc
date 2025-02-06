import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';
import { QuestionsStep } from 'src/app/backend/questions-step';
import { environment } from 'src/environments/environment';
import { QuestionsCollateralModel } from 'src/app/backend/questions-model';

@Component({
  selector: 'q-collateral-option',
  templateUrl: './q-collateral-option.component.html',
  styleUrls: []
})
export class QCollateralOptionComponent  extends QuestionsStep<CollateralOptionFormDataModel>  {
    protected createForm(formData: CollateralOptionFormDataModel): FormGroup {        
        return this.fb.group({
            collateralOption: ['', [Validators.required] ]
        })
    }

    protected getIsForwardAllowed(formData: CollateralOptionFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromQuestions(): CollateralOptionFormDataModel {
        return {
            collateralOption: this.questions.collateral ? (this.questions.collateral.isApplicant ? 'applicant' : 'other') :  ''
        }
    }    

    protected updateQuestionsFromForm(formData: CollateralOptionFormDataModel) {
        let prev = this.questions.collateral
        let isApplicant = formData.collateralOption === 'applicant'
        let c : QuestionsCollateralModel = {
            isApplicant: isApplicant,
            nonApplicantPerson: !isApplicant && prev != null ? prev.nonApplicantPerson : null //Dont lose the applicant when going back and forward without changing this            
        }        
        if(!c.isApplicant && !c.nonApplicantPerson) {
            c.nonApplicantPerson = {}
        }
        this.questions.collateral = c
    }

    protected getStepName(): string {
        return 'q-collateral-option'
    }

    protected getNextStep(): StepRouteModel {
        if(this.questions.collateral.isApplicant) {
            return new StepRouteModel('beneficial-owners-option')
        } else {
            return new StepRouteModel('collateral-civicnr')
        }        
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('offer')
    }
}

export class CollateralOptionFormDataModel {
    collateralOption: string
}
