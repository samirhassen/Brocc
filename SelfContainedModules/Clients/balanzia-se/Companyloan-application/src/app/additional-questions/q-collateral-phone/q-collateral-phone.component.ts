import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';
import { QuestionsStep } from 'src/app/backend/questions-step';

@Component({
  selector: 'q-collateral-phone',
  templateUrl: './q-collateral-phone.component.html',
  styleUrls: []
})
export class QCollateralPhoneComponent  extends QuestionsStep<CollateralPhoneFormDataModel>  {
    protected createForm(formData: CollateralPhoneFormDataModel): FormGroup {        
        return this.fb.group({
            phone: ['', [Validators.required, this.validationService.getPhoneValidator()] ],
        })
    }

    protected getIsForwardAllowed(formData: CollateralPhoneFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromQuestions(): CollateralPhoneFormDataModel {
        return {
            phone: this.questions.getCollateralOtherValue(x => x.phone),
        }
    }    

    protected updateQuestionsFromForm(formData: CollateralPhoneFormDataModel) {
        this.questions.setCollateralOtherValue(x => x.phone = formData.phone)
    }

    protected getStepName(): string {
        return 'q-collateral-phone'
    }

    protected getNextStep(): StepRouteModel {
        return new StepRouteModel('beneficial-owners-option')
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('collateral-email')
    }
}

export class CollateralPhoneFormDataModel {
    phone: string
}
