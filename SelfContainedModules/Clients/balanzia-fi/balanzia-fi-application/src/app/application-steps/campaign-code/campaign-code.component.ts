import { Component } from '@angular/core';
import { FormGroup, FormControl } from '@angular/forms';
import { StepRouteModel, ApplicationStep } from '../../backend/application-step';

@Component({
    selector: 'campaign-code',
    templateUrl: './campaign-code.component.html',
    styleUrls: []
})
export class CampaignCodeComponent extends ApplicationStep<CampaignCodeFormDataModel> {
    protected createForm(): FormGroup {
        return new FormGroup({
            campaignCode: new FormControl(),
          })
    }

    protected getIsForwardAllowed(formData: CampaignCodeFormDataModel): boolean {
        return formData && formData.campaignCode && formData.campaignCode.length > 0
    }

    protected getFormUpdateFromApplication(): CampaignCodeFormDataModel {
        return (this.application.hasCampaignCode === true || this.application.hasCampaignCode === false) ? {campaignCode: this.application.hasCampaignCode ? 'yes' : 'no'} : null
    }

    protected updateApplicationFromForm(formData: CampaignCodeFormDataModel) {
        this.application.setDataCampaignCode(formData.campaignCode === 'yes')
    }

    protected getStepName(): string {
        return 'campaign-code'
    }

    protected getNextStep(): StepRouteModel {
        if(this.application.hasCampaignCode === true) {
            return new StepRouteModel('campaign-code-code')
        } else {
            return new StepRouteModel('campaign-code-channel')
        }
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('nr-of-applicants')
    }
}

export class CampaignCodeFormDataModel {
    campaignCode: string
}
