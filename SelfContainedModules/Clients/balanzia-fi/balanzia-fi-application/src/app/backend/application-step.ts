import { ApplicationModel } from './application-model';
import { BehaviorSubject, Subscription } from 'rxjs';
import { FormGroup, FormBuilder } from '@angular/forms';
import { ActivatedRoute, Data } from '@angular/router';
import { Component, Inject, Injectable, OnInit } from '@angular/core';
import { API_SERVICE, ApiService } from './api-service';
import { NTechValidationService } from './ntech-validation.service';
import { ApplicationForwardBackService } from './application-forward-back.service';
import { TranslateService } from '@ngx-translate/core';

@Injectable() 
export abstract class ApplicationStep<TFormDataModel> implements OnInit {
    application: ApplicationModel
    form: FormGroup
    private formSubscription : Subscription
    private forwadSubscription: Subscription
    private backSubscription: Subscription

    constructor(private route: ActivatedRoute,
        @Inject(API_SERVICE) protected apiService: ApiService,
        protected fb: FormBuilder,
        protected validationService: NTechValidationService,
        protected forwardBackService: ApplicationForwardBackService,
        protected translateService: TranslateService) {
            route.data.subscribe(x => {
                this.application = x.application
                this.onDataChanged(x)
                this.form = this.createForm()
                if(this.formSubscription) {
                    this.formSubscription.unsubscribe()
                }
                this.formSubscription = this.form.valueChanges.subscribe(x => {
                    let f : TFormDataModel = x
                    this.forwardBackService.isForwardAllowed.next(this.getIsForwardAllowed(f))
                })
                this.form.reset()
                let i = this.getFormUpdateFromApplication()
                if(i) {
                    this.form.patchValue(i)
                    this.forwardBackService.isForwardAllowed.next(this.getIsForwardAllowed(i))
                } else {
                    this.forwardBackService.isForwardAllowed.next(false)
                }
            })

            if(this.forwadSubscription) {
                this.forwadSubscription.unsubscribe()
            }
            this.forwadSubscription = this.forwardBackService.onForward.subscribe(x => {
                this.forward()
            })

            if(this.backSubscription) {
                this.backSubscription.unsubscribe()
            }
            this.backSubscription = this.forwardBackService.onBack.subscribe(x => {
                this.back()
            })
    }

    ngOnInit() {        
        this.forwardBackService.isFinalStep.next(this.isFinalStep())
        this.afterNgOnInit()
    }

    protected abstract createForm() : FormGroup    
    protected abstract getIsForwardAllowed(formData: TFormDataModel) : boolean
    protected abstract getFormUpdateFromApplication() : TFormDataModel
    protected abstract updateApplicationFromForm(formData: TFormDataModel)
    protected abstract getStepName(): string
    protected abstract getNextStep(): StepRouteModel
    protected abstract getPreviousStep(): StepRouteModel
    protected onDataChanged(x: Data) { }
    protected isFinalStep(): boolean { return false }
    protected afterNgOnInit() {

    }

    protected onForward() {
        let f : TFormDataModel = this.form.value
        this.updateApplicationFromForm(f)
        this.apiService.saveApplication(this.application)
        let n = this.getNextStep()
        this.apiService.navigateToRoute(n.stepName, this.application.id, n.applicantNr, n.loanType)
    }

    forward() {
        this.onForward()
    }

    back() {
        let n = this.getPreviousStep()
        this.apiService.navigateToRoute(n.stepName, this.application.id, n.applicantNr, n.loanType)
    }
}

export class StepRouteModel {
    constructor(public stepName: string, public applicantNr?: number, public loanType? : string)  {}
}