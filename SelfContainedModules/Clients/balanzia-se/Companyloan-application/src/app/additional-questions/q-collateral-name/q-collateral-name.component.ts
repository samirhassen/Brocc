import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';
import { QuestionsStep } from 'src/app/backend/questions-step';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'q-collateral-name',
  templateUrl: './q-collateral-name.component.html',
  styleUrls: []
})
export class QCollateralNameComponent  extends QuestionsStep<CollateralNameFormDataModel>  {
    protected createForm(formData: CollateralNameFormDataModel): FormGroup {        
        return this.fb.group({
            firstName: ['', [Validators.required] ],
            lastName: ['', [Validators.required] ]
        })
    }

    protected getIsForwardAllowed(formData: CollateralNameFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromQuestions(): CollateralNameFormDataModel {
        return {
            firstName: this.questions.getCollateralOtherValue(x => x.firstName),
            lastName: this.questions.getCollateralOtherValue(x => x.lastName)
        }
    }    

    protected updateQuestionsFromForm(formData: CollateralNameFormDataModel) {
        this.questions.setCollateralOtherValue(x => x.firstName = formData.firstName)
        this.questions.setCollateralOtherValue(x => x.lastName = formData.lastName)
    }

    protected getStepName(): string {
        return 'q-collateral-name'
    }

    protected getNextStep(): StepRouteModel {
        return new StepRouteModel('collateral-email')
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('collateral-civicnr')
    }
}

export class CollateralNameFormDataModel {
    firstName: string
    lastName: string
}
