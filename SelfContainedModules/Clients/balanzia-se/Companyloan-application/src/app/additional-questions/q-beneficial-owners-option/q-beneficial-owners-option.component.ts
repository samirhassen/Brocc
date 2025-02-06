import { Component, OnInit } from '@angular/core';
import { Validators, FormGroup } from '@angular/forms';
import { StepRouteModel } from '../../backend/application-step';
import { QuestionsStep } from 'src/app/backend/questions-step';
import { environment } from 'src/environments/environment';
import { BeneficialOwnersModel } from 'src/app/backend/questions-model';

@Component({
  selector: 'q-beneficial-owners-option',
  templateUrl: './q-beneficial-owners-option.component.html',
  styleUrls: []
})
export class QBeneficialOwnersOptionComponent  extends QuestionsStep<BeneficialOwnersOptionFormDataModel>  {
    protected createForm(formData: BeneficialOwnersOptionFormDataModel): FormGroup {
        return this.fb.group({
            ownerOption: ['', [Validators.required] ]
        })
    }

    protected getIsForwardAllowed(formData: BeneficialOwnersOptionFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromQuestions(): BeneficialOwnersOptionFormDataModel {
        return {
            ownerOption: this.questions.beneficialOwners ? (this.questions.beneficialOwners.hasBeneficialOwners1 ? 'yes' : 'no') :  ''
        }
    }

    protected updateQuestionsFromForm(formData: BeneficialOwnersOptionFormDataModel) {
        let prev = this.questions.beneficialOwners
        let hasBeneficialOwners1 = formData.ownerOption === 'yes'
        this.questions.beneficialOwners = {
            hasBeneficialOwners1: hasBeneficialOwners1,
            beneficialOwners: prev ? BeneficialOwnersModel.filterOwners(hasBeneficialOwners1, prev.beneficialOwners) : null
        }
    }

    protected getStepName(): string {
        return 'q-beneficial-owners-option'
    }

    protected getNextStep(): StepRouteModel {
        if(this.questions.beneficialOwners.hasBeneficialOwners1) {
            return new StepRouteModel('beneficial-owners1')
        } else {
            return new StepRouteModel('company-sector')
        }

    }

    protected getPreviousStep(): StepRouteModel {
        if(!this.questions.collateral || this.questions.collateral.isApplicant) {
            return new StepRouteModel('collateral-option')
        } else {
            return new StepRouteModel('collateral-phone')
        }
    }
}

export class BeneficialOwnersOptionFormDataModel {
    ownerOption: string
}
