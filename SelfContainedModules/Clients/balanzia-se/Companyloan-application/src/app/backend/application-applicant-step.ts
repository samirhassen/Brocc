import { ApplicationStep } from './application-step';
import { Data, ActivatedRoute } from '@angular/router';
import { Inject } from '@angular/core';
import { API_SERVICE, ApiService } from './api-service';
import { FormBuilder } from '@angular/forms';
import { NTechValidationService } from './ntech-validation.service';
import { ApplicationForwardBackService } from './application-forward-back.service';

export abstract class ApplicationApplicantStep<TFormDataModel> extends ApplicationStep<TFormDataModel> {
    constructor(route: ActivatedRoute,
        @Inject(API_SERVICE) apiService: ApiService,
        fb: FormBuilder,
        validationService: NTechValidationService,
        forwardBackService: ApplicationForwardBackService) {
            super(route, apiService, fb, validationService, forwardBackService)
        }    

    protected onDataChanged(x: Data) { 

    }
}