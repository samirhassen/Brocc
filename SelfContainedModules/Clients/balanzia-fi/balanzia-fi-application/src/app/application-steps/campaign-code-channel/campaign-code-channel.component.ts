import { Component } from '@angular/core';
import { FormGroup, Validators } from '@angular/forms';
import { ApplicationStep, StepRouteModel } from '../../backend/application-step';

@Component({
    selector: 'campaign-code-channel',
    templateUrl: './campaign-code-channel.component.html',
    styleUrls: []
})
export class CampaignCodeChannelComponent extends ApplicationStep<CampaignCodeCodeFormDataModel>  {
    channels: ChannelUiModel[] = [
        new ChannelUiModel('brevhem','H00010'), 
        new ChannelUiModel('internet', 'H00020'), 
        new ChannelUiModel('tidning', 'H00030'),
        new ChannelUiModel('rekommendation', 'H00040'),
        new ChannelUiModel('tvradio', 'H00050'),
        new ChannelUiModel('annat', 'H00060'),
    ]

    protected createForm(): FormGroup {
        return this.fb.group({
            channelCode: ['', Validators.required]
        })
    }

    protected getIsForwardAllowed(formData: CampaignCodeCodeFormDataModel): boolean {
        return this.form.valid
    }

    protected getFormUpdateFromApplication(): CampaignCodeCodeFormDataModel {
        return this.application.campaignCodeOrChannel && this.application.campaignCodeOrChannel.isChannel ? { channelCode: this.application.campaignCodeOrChannel.code } : null
    }

    protected updateApplicationFromForm(formData: CampaignCodeCodeFormDataModel) {
        this.application.setDataCampaignCodeChannel(formData.channelCode)
    }

    protected getStepName(): string {
        return 'campaign-code-channel'
    }

    protected getNextStep(): StepRouteModel {
        return new StepRouteModel('ssn', 1)
    }

    protected getPreviousStep(): StepRouteModel {
        return new StepRouteModel('campaign-code')
    }
}

export class CampaignCodeCodeFormDataModel {
    channelCode: string
}

export class ChannelUiModel {
    public translationCode
    constructor(private translationCodePre: string, public channelCode: string) {
        this.translationCode = `channel.${translationCodePre}`
    }    
}