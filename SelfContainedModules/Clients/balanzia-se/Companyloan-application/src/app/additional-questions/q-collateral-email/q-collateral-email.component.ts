import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';
import { QuestionsStep } from 'src/app/backend/questions-step';

@Component({
  selector: 'q-collateral-email',
  templateUrl: './q-collateral-email.component.html',
  styleUrls: []
})
export class QCollateralEmailComponent  extends QuestionsStep<CollateralEmailFormDataModel>  {
    protected createForm(formData: CollateralEmailFormDataModel): FormGroup {        
        return this.fb.group({
            email: ['', [Validators.required, this.validationService.getEmailValidator()] ],
        })
    }

    protected getIsForwardAllowed(formData: CollateralEmailFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromQuestions(): CollateralEmailFormDataModel {
        return {
            email: this.questions.getCollateralOtherValue(x => x.email),
        }
    }    

    protected updateQuestionsFromForm(formData: CollateralEmailFormDataModel) {
        this.questions.setCollateralOtherValue(x => x.email = formData.email)
    }

    protected getStepName(): string {
        return 'q-collateral-email'
    }

    protected getNextStep(): StepRouteModel {
        return new StepRouteModel('collateral-phone')
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('collateral-name')
    }
}

export class CollateralEmailFormDataModel {
    email: string
}
