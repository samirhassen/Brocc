import { ApplicationStep } from './application-step';
import { Data, ActivatedRoute } from '@angular/router';
import { Inject } from '@angular/core';
import { API_SERVICE, ApiService } from './api-service';
import { FormBuilder } from '@angular/forms';
import { NTechValidationService } from './ntech-validation.service';
import { ApplicantModel } from './application-model';
import { ApplicationForwardBackService } from './application-forward-back.service';
import { TranslateService } from '@ngx-translate/core';

export abstract class ApplicationApplicantStep<TFormDataModel> extends ApplicationStep<TFormDataModel> {
    protected applicantNr: number
    
    constructor(route: ActivatedRoute,
        @Inject(API_SERVICE) apiService: ApiService,
        fb: FormBuilder,
        validationService: NTechValidationService,
        forwardBackService: ApplicationForwardBackService, translateService: TranslateService) {
            super(route, apiService, fb, validationService, forwardBackService, translateService)
        }    

    protected onDataChanged(x: Data) { 
        this.applicantNr = x.applicantNr
    }

    protected getApplicant() : ApplicantModel {
        return this.application.getApplicant(this.applicantNr)
    }
}