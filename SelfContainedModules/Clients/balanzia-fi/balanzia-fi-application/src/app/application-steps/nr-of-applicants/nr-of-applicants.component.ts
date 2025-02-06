import { Component } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';
import { NullableNumber } from '../../backend/common.types';
import { getNextStepAfterCampaignCodeCode } from '../campaign-code-code/campaign-code-code.component';

@Component({
    selector: 'nr-of-applicants',
    templateUrl: './nr-of-applicants.component.html',
    styleUrls: []
})
export class NrOfApplicantsComponent extends ApplicationStep<NrOfApplicantsFormDataModel> {
    protected createForm(): FormGroup {
        return new FormGroup({
            nrOfApplicants: new FormControl(),
          })
    }
    
    protected getIsForwardAllowed(formData: NrOfApplicantsFormDataModel): boolean {
        return formData.nrOfApplicants && formData.nrOfApplicants.length > 0
    }

    protected getFormUpdateFromApplication(): NrOfApplicantsFormDataModel {
        return this.application.nrOfApplicants ? { nrOfApplicants: this.application.nrOfApplicants.value.toString() } : null
    }

    protected updateApplicationFromForm(formData: NrOfApplicantsFormDataModel) {
        this.application.setDataNrOfApplicants(new NullableNumber(this.validationService.parseInteger(formData.nrOfApplicants)))
    }

    protected getStepName(): string {
        return 'nr-of-applicants'
    }

    protected getNextStep(): StepRouteModel {
        if(this.application.skipCampaignStep === true) {           
            return new StepRouteModel('ssn', 1)
        } 
        else if(this.application.getPreFilledCampaignCode()) {
            return getNextStepAfterCampaignCodeCode(this.application)
        } else {
            return new StepRouteModel('campaign-code')
        }        
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('calculator')
    }
}

class NrOfApplicantsFormDataModel {
    nrOfApplicants: string
}
