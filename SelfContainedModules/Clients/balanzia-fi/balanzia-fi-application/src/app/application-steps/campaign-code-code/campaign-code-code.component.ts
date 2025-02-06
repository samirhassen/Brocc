import { Component } from '@angular/core';
import { FormGroup, Validators } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';
import { ApplicationModel } from 'src/app/backend/application-model';

@Component({
  selector: 'campaign-code-code',
  templateUrl: './campaign-code-code.component.html',
  styleUrls: []
})
export class CampaignCodeCodeComponent extends ApplicationStep<CampaignCodeCodeFormDataModel> {
    protected createForm(): FormGroup {
        return this.fb.group({
            campaignCode: ['', Validators.required]
        })
    }

    protected getIsForwardAllowed(formData: CampaignCodeCodeFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromApplication(): CampaignCodeCodeFormDataModel {
        return this.application && this.application.campaignCodeOrChannel && !this.application.campaignCodeOrChannel.isChannel 
            ? { campaignCode: this.application.campaignCodeOrChannel.code } 
            : null
    }

    protected updateApplicationFromForm(formData: CampaignCodeCodeFormDataModel) {
        this.application.setDataCampaignCodeCode(formData.campaignCode)
    }

    protected getStepName(): string {
        return 'campaign-code-code'
    }

    protected getNextStep(): StepRouteModel {
        return getNextStepAfterCampaignCodeCode(this.application)
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('campaign-code')
    }
}

class CampaignCodeCodeFormDataModel {
    campaignCode: string
}

export function getNextStepAfterCampaignCodeCode(application: ApplicationModel) {
    return new StepRouteModel('ssn', 1)
}