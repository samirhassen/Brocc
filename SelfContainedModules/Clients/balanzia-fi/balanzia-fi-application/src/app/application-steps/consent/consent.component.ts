import { Component } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { StepRouteModel, ApplicationStep } from '../../backend/application-step';
import { BehaviorSubject } from 'rxjs';
import { translateApplicationToServerModel } from '../../backend/application-model.server';
import { Data } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'consent',
  templateUrl: './consent.component.html',
  styleUrls: []
})
export class ConsentComponent extends ApplicationStep<ConsentFormDataModel> {
    userLanguage: string
    protected createForm(): FormGroup {
        return this.application.nrOfApplicants.value > 1 
        ? this.fb.group({
                satConsent1: [false, [Validators.required] ],
                customerConsent1: [false, [Validators.required] ],
                informationConsent1: [false, [Validators.required] ],
                satConsent2: [false, [Validators.required] ],
                customerConsent2: [false, [Validators.required] ],
                informationConsent2: [false, [Validators.required] ]
            })
        : this.fb.group({
            satConsent1: [false, [Validators.required] ],
            customerConsent1: [false, [Validators.required] ],
            informationConsent1: [false, [Validators.required] ]
        })
    }

    protected onDataChanged(x: Data) { 
        super.onDataChanged(x)
        this.userLanguage = x.userLanguage
    }

    protected isFinalStep(): boolean { return true }

    protected getIsForwardAllowed(formData: ConsentFormDataModel): boolean {
        return this.form.valid && !(this.forwardBackService.isLoading.value)
    }

    protected getFormUpdateFromApplication(): ConsentFormDataModel {
        let a = this.application
        return a.consent ? { 
            satConsent1: a.consent.satConsent,
            satConsent2: a.consent.satConsent,
            customerConsent1: a.consent.customerConsent,
            customerConsent2: a.consent.customerConsent,
            informationConsent1: a.consent.informationConsent,
            informationConsent2: a.consent.informationConsent
         } : null
    }

    protected updateApplicationFromForm(formData: ConsentFormDataModel) {
        let satConsent = this.application.nrOfApplicants.value > 1 ? formData.satConsent1 && formData.satConsent2 : formData.satConsent1
        let informationConsent = this.application.nrOfApplicants.value > 1 ? formData.informationConsent1 && formData.informationConsent2 : formData.informationConsent1
        let customerConsent = this.application.nrOfApplicants.value > 1 ? formData.customerConsent1 && formData.customerConsent2 : formData.customerConsent1        
        this.application.setDataConsent(satConsent, informationConsent, customerConsent)
    }

    protected getStepName(): string {
        return 'consent'
    }

    protected getNextStep(): StepRouteModel {
        throw new Error("Not in use"); //Not used since we override forward
    }

    protected getPreviousStep(): StepRouteModel {
        if(this.application.hasConsolidation === true) {
            return new StepRouteModel('consolidation-amount')
        } else {
            return new StepRouteModel('consolidation-option')
        }
    }

    onForward() {
        let f : ConsentFormDataModel = this.form.value
        this.updateApplicationFromForm(f)
        this.apiService.saveApplication(this.application)
        let serverApplication = translateApplicationToServerModel(this.application, this.userLanguage, this.translateService)
        this.forwardBackService.isLoading.next(true)
        this.apiService.createApplication(serverApplication).subscribe(x => {
            this.forwardBackService.isLoading.next(false)
            if(x.isFailed) {
                this.apiService.navigateToRoute('result-failed', this.application.id)
            } else if(x.applicationNr) {
                this.apiService.deleteApplication(this.application)
                this.apiService.navigateToRouteRaw(`app/${x.applicationNr}/result-success`)
            } else if(x.failedUrl) {
                window.location.href = x.failedUrl
            } else if(x.redirectToUrl) {
                window.location.href = x.redirectToUrl
            } else {
                throw new Error('Failed')
            }
        })
    }
}

export class ConsentFormDataModel {
    satConsent1: boolean
    customerConsent1: boolean
    informationConsent1: boolean
    satConsent2: boolean
    customerConsent2: boolean
    informationConsent2: boolean
}