import { Component } from '@angular/core';
import { FormGroup, FormControl } from '@angular/forms';
import { StepRouteModel, ApplicationStep } from '../../backend/application-step';

@Component({
  selector: 'consolidation-option',
  templateUrl: './consolidation-option.component.html',
  styleUrls: []
})
export class ConsolidationOptionComponent extends ApplicationStep<ConsolidationOptionFormDataModel> {
    protected createForm(): FormGroup {
        return new FormGroup({
            hasConsolidation: new FormControl(),
          })
    }

    protected getIsForwardAllowed(formData: ConsolidationOptionFormDataModel): boolean {
        return formData && formData.hasConsolidation && formData.hasConsolidation.length > 0
    }

    protected getFormUpdateFromApplication(): ConsolidationOptionFormDataModel {
        return (this.application.hasConsolidation === true || this.application.hasConsolidation === false) ? {hasConsolidation: this.application.hasConsolidation ? 'yes' : 'no'} : null
    }

    protected updateApplicationFromForm(formData: ConsolidationOptionFormDataModel) {
        this.application.setDataConsolidationOption(formData.hasConsolidation === 'yes')
    }

    protected getStepName(): string {
        return 'consolidation-option'
    }

    protected getNextStep(): StepRouteModel {
        if(this.application.hasConsolidation === true) {            
            return new StepRouteModel('consolidation-amount')
        } else {
            return new StepRouteModel('consent')
        }
    }

    protected getPreviousStep(): StepRouteModel {
        let lastApplicant = this.application.getApplicant(this.application.nrOfApplicants.value)

        if(lastApplicant.hasOtherLoans === true) {
            let lastLoanType = lastApplicant.otherLoansOptions[lastApplicant.otherLoansOptions.length - 1]
            return new StepRouteModel('other-loans-amount', lastApplicant.applicantNr, lastLoanType)
        } else {
            return new StepRouteModel('other-loans-options', lastApplicant.applicantNr)
        }
    }
}

export class ConsolidationOptionFormDataModel {
    hasConsolidation: string
}
