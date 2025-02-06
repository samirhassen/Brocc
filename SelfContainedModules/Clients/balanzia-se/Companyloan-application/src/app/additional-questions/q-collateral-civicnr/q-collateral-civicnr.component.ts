import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';
import { QuestionsStep } from 'src/app/backend/questions-step';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'q-collateral-civicnr',
  templateUrl: './q-collateral-civicnr.component.html',
  styleUrls: []
})
export class QCollateralCivicNrComponent  extends QuestionsStep<CollateralCivicNrFormDataModel>  {
    protected createForm(formData: CollateralCivicNrFormDataModel): FormGroup {        
        return this.fb.group({
            civicNr: ['', [Validators.required, this.validationService.getCivicNrValidator()] ]
        })
    }

    protected getIsForwardAllowed(formData: CollateralCivicNrFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromQuestions(): CollateralCivicNrFormDataModel {
        return {
            civicNr: this.questions.getCollateralOtherValue(x => x.civicNr)
        }
    }    

    protected updateQuestionsFromForm(formData: CollateralCivicNrFormDataModel) {
        this.questions.setCollateralOtherValue(x => x.civicNr = formData.civicNr)
    }

    protected getStepName(): string {
        return 'q-collateral-civicnr'
    }

    protected getNextStep(): StepRouteModel {
        return new StepRouteModel('collateral-name')
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('collateral-option')
    }
}

export class CollateralCivicNrFormDataModel {
    civicNr: string
}
