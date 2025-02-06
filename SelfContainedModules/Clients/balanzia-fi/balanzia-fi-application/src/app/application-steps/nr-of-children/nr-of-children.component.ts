import { Component } from '@angular/core';
import { FormGroup, Validators } from '@angular/forms';
import { StepRouteModel } from '../../backend/application-step';
import { ApplicationApplicantStep } from '../../backend/application-applicant-step';
import { NullableNumber } from '../../backend/common.types';

@Component({
  selector: 'nr-of-children',
  templateUrl: './nr-of-children.component.html',
  styleUrls: []
})
export class NrOfChildrenComponent  extends ApplicationApplicantStep<NrOfChildrenFormDataModel> {
    protected createForm(): FormGroup {
        return this.fb.group({
            nrOfChildren: ['', [Validators.required, this.validationService.getPositiveIntegerValidator()] ]
        })
    }
    
    protected getIsForwardAllowed(formData: NrOfChildrenFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromApplication(): NrOfChildrenFormDataModel {
        let a = this.application.getApplicant(this.applicantNr)        
        return a.nrOfChildren ? { nrOfChildren: this.validationService.formatInteger(a.nrOfChildren.value) } : null
    }

    protected updateApplicationFromForm(formData: NrOfChildrenFormDataModel) {
        this.application.setNrOfChildren(new NullableNumber(this.validationService.parseInteger(formData.nrOfChildren)), this.applicantNr)
    }

    protected getStepName(): string {
        return 'nr-of-children'
    }

    protected getNextStep(): StepRouteModel {
        return new StepRouteModel('employment', this.applicantNr)
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('marriage', this.applicantNr)
    }
}

class NrOfChildrenFormDataModel {
    nrOfChildren: string
}
